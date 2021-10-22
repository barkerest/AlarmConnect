using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using NPOI.SS.UserModel;

namespace Sample.Excel
{
	internal static class SpreadsheetColumnHelper
	{
		public static readonly (Type, SpreadsheetCellDataType, string)[] TypeConversions =
		{
			(typeof(bool), SpreadsheetCellDataType.Boolean, null),
			(typeof(byte), SpreadsheetCellDataType.Number, "0"),
			(typeof(sbyte), SpreadsheetCellDataType.Number, "#,##0"),
			(typeof(short), SpreadsheetCellDataType.Number, "#,##0"),
			(typeof(ushort), SpreadsheetCellDataType.Number, "#,##0"),
			(typeof(int), SpreadsheetCellDataType.Number, "#,##0"),
			(typeof(uint), SpreadsheetCellDataType.Number, "#,##0"),
			(typeof(long), SpreadsheetCellDataType.Number, "#,##0"),
			(typeof(ulong), SpreadsheetCellDataType.Number, "#,##0"),
			(typeof(float), SpreadsheetCellDataType.Number, "#,##0.00"),
			(typeof(double), SpreadsheetCellDataType.Number, "#,##0.00"),
			(typeof(decimal), SpreadsheetCellDataType.Number, "#,##0.00"),
			(typeof(DateTime), SpreadsheetCellDataType.Date, "MM/dd/yyyy"),
			(typeof(object), SpreadsheetCellDataType.String, null)
		};

	}
	
	public class SpreadsheetColumn<T> : ISpreadsheetColumn<T> where T : class
	{
		public SpreadsheetColumn(Expression<Func<T, object>> property, string title = null, HorizontalAlignment? alignment = null, VerticalAlignment? verticalAlignment = null, string formatString = null)
		{
			Property = PropertyOrFieldWrapper<T>.Create(property.GetMemberInfo(requireWrite:false));
			
			if (property.Body is NewExpression nex)
			{
				title = nex.GetMemberArgumentValue("title", title);
                
				formatString = nex.GetMemberArgumentValue("format", formatString);
				
				var alignmentArg = nex.GetMemberArgument("alignment");
				if (alignmentArg != null)
				{
					if (alignmentArg.Type == typeof(HorizontalAlignment))
					{
						alignment = alignmentArg.Evaluate<HorizontalAlignment>();
					}
					else if (alignmentArg.Type == typeof(int))
					{
						alignment = (HorizontalAlignment) alignmentArg.Evaluate<int>();
					}
					else
					{
						var alignmentValue = alignmentArg.Evaluate()?.ToString();
						if (!string.IsNullOrEmpty(alignmentValue))
						{
							alignment = Enum.Parse<HorizontalAlignment>(alignmentValue, true);
						}
					}
				}

                alignmentArg = nex.GetMemberArgument("verticalAlignment");
                if (alignmentArg != null)
                {
                    if (alignmentArg.Type == typeof(VerticalAlignment))
                    {
                        verticalAlignment = alignmentArg.Evaluate<VerticalAlignment>();
                    }
                    else if (alignmentArg.Type == typeof(int))
                    {
                        verticalAlignment = (VerticalAlignment) alignmentArg.Evaluate<int>();
                    }
                    else
                    {
                        var alignmentValue = alignmentArg.Evaluate()?.ToString();
                        if (!string.IsNullOrEmpty(alignmentValue))
                        {
                            verticalAlignment = Enum.Parse<VerticalAlignment>(alignmentValue, true);
                        }
                    }
                }

                
            }
			
			Title = title ?? Property.Name;

			var type = (Property.MemberInfo is FieldInfo fi)
				           ? fi.FieldType
				           : (Property.MemberInfo is PropertyInfo pi)
					           ? pi.PropertyType
					           : typeof(string);

			var conv = SpreadsheetColumnHelper.TypeConversions.Any(x => x.Item1 == type)
				           ? SpreadsheetColumnHelper.TypeConversions.First(x => x.Item1 == type)
				           : SpreadsheetColumnHelper.TypeConversions.First(x => x.Item1 == typeof(object));

			if (alignment.HasValue)
			{
				Alignment = alignment.Value;
			}
			else
			{
				Alignment = conv.Item2 == SpreadsheetCellDataType.String
					            ? HorizontalAlignment.Left
					            : conv.Item2 == SpreadsheetCellDataType.Boolean
						            ? HorizontalAlignment.Center
						            : HorizontalAlignment.Right;
			}

            VerticalAlignment = verticalAlignment ?? VerticalAlignment.Bottom;

            FormatString = formatString ?? conv.Item3;
		}

		public IPropertyOrFieldWrapper<T> Property { get; }
		public string Title { get; }
		public HorizontalAlignment Alignment { get; }

        public VerticalAlignment VerticalAlignment { get; }
        public string            FormatString      { get; }
        
        public object GetValue(object entity)
        {
            if (!(entity is T item)) throw new InvalidCastException();
            return Property.Get(item);
        }
    }
}
