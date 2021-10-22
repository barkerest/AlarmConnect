using System;
using System.Reflection;

namespace Sample.Excel
{
    internal class PropertyInfoWrapper<TEntity> : PropertyOrFieldWrapper<TEntity>
        where TEntity : class
    {
        public PropertyInfoWrapper(PropertyInfo propertyInfo)
        {
            PropertyInfo = propertyInfo ?? throw new ArgumentNullException();
        }
        
        public PropertyInfo PropertyInfo { get; }

        public override object Get(TEntity entity)
        {
            if (!PropertyInfo.CanRead) throw new InvalidOperationException();
            return PropertyInfo.GetValue(entity);
        }

        public override void Set(TEntity entity, object value)
        {
            if (!PropertyInfo.CanWrite) throw new InvalidOperationException();
            PropertyInfo.SetValue(entity, value);
        }

        public override string Name => PropertyInfo.Name;

        public override MemberInfo MemberInfo => PropertyInfo;
    }
}
