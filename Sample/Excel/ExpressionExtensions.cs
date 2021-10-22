using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Sample.Excel
{
    public static class ExpressionExtensions
    {
        /// <summary>
        /// Determines if the specified type is an anonymous type.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private static bool IsAnonymous(this Type type)
        {
            return type.IsSealed
                   && !type.IsPublic
                   && type.Name.Contains("AnonymousType")
                   && type.GetCustomAttributes(typeof(CompilerGeneratedAttribute), false).Length > 0;
        }
        
	    /// <summary>
	    /// Gets the property info from the expression.
	    /// </summary>
	    /// <param name="expr"></param>
	    /// <param name="requiredReturnType"></param>
	    /// <param name="requireRead"></param>
	    /// <param name="requireWrite"></param>
	    /// <param name="throwOnError"></param>
	    /// <typeparam name="TEntity"></typeparam>
	    /// <returns></returns>
        public static PropertyInfo GetPropertyInfo<TEntity>(
            this Expression<Func<TEntity, object>> expr,
            Type                                   requiredReturnType = null,
            bool                                   requireRead        = true,
            bool                                   requireWrite       = true,
            bool                                   throwOnError       = true
        )
            where TEntity : class
        {
            return (PropertyInfo) GetMemberInfo(
                expr,
                requiredReturnType,
                requireRead,
                requireWrite,
                MemberTypes.Property,
                throwOnError);
        }

	    /// <summary>
	    /// Gets the field info from the expression.
	    /// </summary>
	    /// <param name="expr"></param>
	    /// <param name="requiredReturnType"></param>
	    /// <param name="throwOnError"></param>
	    /// <typeparam name="TEntity"></typeparam>
	    /// <returns></returns>
        public static FieldInfo GetFieldInfo<TEntity>(
            this Expression<Func<TEntity, object>> expr,
            Type                                   requiredReturnType = null,
            bool                                   throwOnError       = true
        )
            where TEntity : class
        {
            return (FieldInfo) GetMemberInfo(
                expr,
                requiredReturnType,
                true,
                true,
                MemberTypes.Field,
                throwOnError);
        }
        
        /// <summary>
        /// Gets a member argument from an anonymous type new expression.
        /// </summary>
        /// <param name="expr"></param>
        /// <param name="memberName">The member to extract from the expression, case insensitive.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static Expression GetMemberArgument(this NewExpression expr, string memberName)
        {
	        if (!expr.Type.IsAnonymous())
	        {
		        throw new ArgumentException("New expression must construct an anonymous type to get the member argument.");
	        }
	        
	        if (expr.Arguments.Count != expr.Members.Count)
	        {
		        throw new ArgumentException("Argument/member count mismatch in NewExpression.");
	        }

	        for (var i = 0; i < expr.Members.Count; i++)
	        {
		        if (string.Equals(expr.Members[i].Name, memberName, StringComparison.OrdinalIgnoreCase))
		        {
			        return expr.Arguments[i];
		        }
	        }

	        return null;
        }

        /// <summary>
        /// Gets the value of an argument from an anonymous type new expression.
        /// </summary>
        /// <param name="expr"></param>
        /// <param name="memberName"></param>
        /// <param name="defaultValue"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T GetMemberArgumentValue<T>(this NewExpression expr, string memberName, T defaultValue = default)
        {
	        var arg = expr.GetMemberArgument(memberName);
	        if (arg is null) return defaultValue;
	        var result = arg.Evaluate();

	        if (result is null) return defaultValue;

	        if (result is T resultValue)
	        {
		        return resultValue;
	        }

	        return (T) Convert.ChangeType(result, typeof(T));
        }

        /// <summary>
        /// Gets the member expression from the supplied expression.
        /// </summary>
        /// <param name="expr"></param>
        /// <param name="requiredReturnType"></param>
        /// <param name="requireRead"></param>
        /// <param name="requireWrite"></param>
        /// <param name="requireMemberType"></param>
        /// <param name="throwOnError"></param>
        /// <typeparam name="TEntity"></typeparam>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static MemberInfo GetMemberInfo<TEntity>(
            this Expression<Func<TEntity, object>> expr,
            Type                                   requiredReturnType = null,
            bool                                   requireRead        = true,
            bool                                   requireWrite       = true,
            MemberTypes?                           requireMemberType  = null,
            bool                                   throwOnError       = true)
            where TEntity : class
        {
	        var e = expr.Body;

	        // allow for anonymous types with a "member", "property", or "field" member defining the member expression.
	        if (e is NewExpression nex)
	        {
		        e = nex.GetMemberArgument("member")
		            ?? nex.GetMemberArgument("property")
		            ?? nex.GetMemberArgument("field")
			        ?? throw new ArgumentException("Anonymous type defined in expression must contain \"member\", \"property\", or \"field\" member.");
	        }
	        
            MemberExpression mex = e as MemberExpression;

            if (mex is null && e is UnaryExpression uex
                            && (uex.NodeType == ExpressionType.Convert ||
                                uex.NodeType == ExpressionType.ConvertChecked))
            {
                mex = uex.Operand as MemberExpression;
            }

            if (mex is null)
            {
	            if (throwOnError)
                    throw new ArgumentException("Expression must define a member expression. eg: (x) => x.Name");
                return null;
            }

            if (!(mex.Expression is ParameterExpression pex))
            {
                if (throwOnError)
                    throw new ArgumentException("Member expression must reference the strongly typed parameter.");
                return null;
            }

            if (pex.Type != typeof(TEntity))
            {
                if (throwOnError)
                    throw new ArgumentException("Member expression must reference the strongly typed parameter.");
                return null;
            }

            if (requireMemberType.HasValue && requireMemberType != MemberTypes.All &&
                mex.Member.MemberType != requireMemberType)
            {
                if (throwOnError)
                    throw new ArgumentException($"Member expression must provide a {requireMemberType} member.");
                return null;
            }

            switch (mex.Member.MemberType)
            {
                case MemberTypes.Field:
                    var fieldInfo = (FieldInfo) mex.Member;

                    if (requiredReturnType != null && !requiredReturnType.IsAssignableFrom(fieldInfo.FieldType))
                    {
                        if (throwOnError)
                            throw new ArgumentException($"Field must return a type of {requiredReturnType}.");
                        return null;
                    }

                    return fieldInfo;
                case MemberTypes.Method:
                    var methodInfo = (MethodInfo) mex.Member;

                    if (requiredReturnType != null && !requiredReturnType.IsAssignableFrom(methodInfo.ReturnType))
                    {
                        if (throwOnError)
                            throw new ArgumentException($"Method must return a type of {requiredReturnType}.");
                        return null;
                    }

                    return methodInfo;
                case MemberTypes.Property:
                    var propertyInfo = (PropertyInfo) mex.Member;

                    if (requiredReturnType != null && !requiredReturnType.IsAssignableFrom(propertyInfo.PropertyType))
                    {
                        if (throwOnError)
                            throw new ArgumentException($"Property must return a type of {requiredReturnType}.");
                        return null;
                    }

                    if (requireRead && !propertyInfo.CanRead)
                    {
                        if (throwOnError)
                            throw new ArgumentException("Property must be able to be read.");
                        return null;
                    }

                    if (requireWrite && !propertyInfo.CanWrite)
                    {
                        if (throwOnError)
                            throw new ArgumentException("Property must be able to be set.");
                        return null;
                    }

                    return propertyInfo;
                default:
                    if (throwOnError)
                        throw new ArgumentException("Only fields, properties, and methods are supported.");
                    return null;
            }
        }

        private static bool IsConstantExpression(this Expression expr, bool allowConvert)
        {
	        if (expr is ConstantExpression) return true;
	        if (expr is MemberExpression memberExpression) return memberExpression.Expression.IsConstantExpression(false);
	        if (expr is UnaryExpression unaryExpression 
	            && unaryExpression.NodeType == ExpressionType.Convert
	            && allowConvert)
	        {
		        return unaryExpression.Operand.IsConstantExpression(false);
	        }
	        return false;
        }

        private static T GetConstantValue<T>(this Expression expr)
        {
	        var t = typeof(T);
	        if (expr.Type == t)
	        {
		        if (expr is ConstantExpression constantExpression) return (T) constantExpression.Value;
		        if (expr is MemberExpression memberExpression)
		        {
			        var parent = memberExpression.Expression.GetConstantValue();
			        if (memberExpression.Member is PropertyInfo propertyInfo)
			        {
				        return (T)propertyInfo.GetValue(parent);
			        }

			        if (memberExpression.Member is FieldInfo fieldInfo)
			        {
				        return (T)fieldInfo.GetValue(parent);
			        }
		        
			        throw new InvalidOperationException("Not a property/field expression.");
		        }

		        if (expr is UnaryExpression unaryExpression 
		            && unaryExpression.NodeType == ExpressionType.Convert)
		        {
			        var parent = unaryExpression.Operand.GetConstantValue();
			        return (T) Convert.ChangeType(parent, t);
		        }
	        
		        throw new InvalidOperationException("Not a constant expression.");
	        }

	        return (T)Convert.ChangeType(GetConstantValue(expr), t);
        }
        
        private static object GetConstantValue(this Expression expr)
        {
	        if (expr is ConstantExpression constantExpression) return constantExpression.Value;
	        if (expr is MemberExpression memberExpression)
	        {
		        var parent = memberExpression.Expression.GetConstantValue();
		        if (memberExpression.Member is PropertyInfo propertyInfo)
		        {
			        return propertyInfo.GetValue(parent);
		        }

		        if (memberExpression.Member is FieldInfo fieldInfo)
		        {
			        return fieldInfo.GetValue(parent);
		        }
		        
		        throw new InvalidOperationException("Not a property/field expression.");
	        }

	        if (expr is UnaryExpression unaryExpression 
	            && unaryExpression.NodeType == ExpressionType.Convert)
	        {
		        var parent = unaryExpression.Operand.GetConstantValue();
		        return Convert.ChangeType(parent, unaryExpression.Type);
	        }
	        
	        throw new InvalidOperationException("Not a constant expression.");
        }
        
        /// <summary>
        /// Evaluates an expression returning the resulting value.
        /// </summary>
        /// <param name="expr"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">The expression tree could not be evaluated.</exception>
        public static object Evaluate(this Expression expr)
        {
	        if (expr is LambdaExpression lambdaExpression)
	        {
		        if (lambdaExpression.Parameters.Any())
		        {
			        throw new InvalidOperationException($"Cannot evaluate lambda expression with parameters.");
		        }
		        return lambdaExpression.Compile().DynamicInvoke(null);
	        }

	        if (expr.IsConstantExpression(true)) return expr.GetConstantValue();

	        return Expression.Lambda(expr).Compile().DynamicInvoke(null);
        }

        public static T Evaluate<T>(this Expression expr)
        {
	        if (expr is Expression<Func<T>> funcExpression)
	        {
		        return funcExpression.Compile().Invoke();
	        }

	        var t = typeof(T);
	        object result;
	        
	        
	        if (expr is LambdaExpression lambdaExpression)
	        {
		        if (lambdaExpression.Parameters.Any())
		        {
			        throw new InvalidOperationException($"Cannot evaluate lambda expression with parameters.");
		        }

		        result = lambdaExpression.Compile().DynamicInvoke(null);

		        if (result is T ret)
		        {
			        return ret;
		        }
		        
		        return (T)Convert.ChangeType(result, t);
	        }

	        if (expr.IsConstantExpression(true)) return expr.GetConstantValue<T>();
		    
	        result = Expression.Lambda(expr).Compile().DynamicInvoke(null);

	        if (result is T retVal)
	        {
		        return retVal;
	        }

	        return (T) Convert.ChangeType(result, t);
        }
        
    }
}
