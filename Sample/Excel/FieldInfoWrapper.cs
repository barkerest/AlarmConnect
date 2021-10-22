using System;
using System.Reflection;

namespace Sample.Excel
{
    internal class FieldInfoWrapper<TEntity> : PropertyOrFieldWrapper<TEntity>
        where TEntity : class
    {
        public FieldInfoWrapper(FieldInfo fieldInfo)
        {
            FieldInfo = fieldInfo ?? throw new ArgumentNullException();
        }
        
        public FieldInfo FieldInfo { get; }
        
        public override object Get(TEntity entity)
        {
            return FieldInfo.GetValue(entity);
        }

        public override void Set(TEntity entity, object value)
        {
            FieldInfo.SetValue(entity, value);
        }

        public override string Name => FieldInfo.Name;

        public override MemberInfo MemberInfo => FieldInfo;
    }
}
