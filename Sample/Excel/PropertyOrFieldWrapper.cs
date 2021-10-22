using System;
using System.Reflection;

namespace Sample.Excel
{
    public abstract class PropertyOrFieldWrapper<TEntity> : IPropertyOrFieldWrapper<TEntity>
        where TEntity : class
    {
        public abstract object Get(TEntity entity);

        public abstract void Set(TEntity entity, object value);
        
        public abstract string Name { get; }

        public abstract MemberInfo MemberInfo { get; }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;

            if (obj is IPropertyOrFieldWrapper<TEntity> other)
                return string.Equals(Name, other.Name, StringComparison.Ordinal);

            if (obj is MemberInfo prop)
                return string.Equals(Name, prop.Name, StringComparison.Ordinal);

            if (obj is string name)
                return string.Equals(Name, name, StringComparison.Ordinal);

            return false;
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        public static IPropertyOrFieldWrapper<TEntity> Create(MemberInfo memberInfo)
        {
            if (memberInfo is null) return null;
            if (memberInfo is PropertyInfo propInfo) return new PropertyInfoWrapper<TEntity>(propInfo);
            if (memberInfo is FieldInfo fieldInfo) return new FieldInfoWrapper<TEntity>(fieldInfo);
            throw new ArgumentException();
        }
        
    }
}
