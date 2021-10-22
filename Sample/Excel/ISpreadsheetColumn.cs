using NPOI.SS.UserModel;

namespace Sample.Excel
{
    public interface ISpreadsheetColumn
    {
        string Title { get; }
		
        HorizontalAlignment Alignment { get; }
        
        VerticalAlignment VerticalAlignment { get; }
		
        string FormatString { get; }

        object GetValue(object entity);
    }
    
	public interface ISpreadsheetColumn<T> : ISpreadsheetColumn where T : class
	{
		IPropertyOrFieldWrapper<T> Property { get; }
	}
}
