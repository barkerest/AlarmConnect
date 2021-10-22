using System.Reflection;

namespace Sample.Excel
{
    public interface IPropertyOrFieldWrapper<TEntityType> where TEntityType : class
    {
        object Get(TEntityType entity);

        void Set(TEntityType entity, object value);
        
        string Name { get; }
        
        MemberInfo MemberInfo { get; }
    }

    
}
