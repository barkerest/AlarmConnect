using System;
using System.Collections.Generic;
using NPOI.SS.UserModel;

namespace Sample.Excel
{
	public class StandardRow
	{
		private StandardWorkbook _book;
		private StandardSheet _sheet;

		public IRow Row { get; }
		
		private int _nextCell = 0;
		
		public int RowNumber { get; }
		
		public bool IsHeader { get; }

		internal StandardRow(StandardWorkbook book, StandardSheet sheet, int row, bool header = false)
		{
			_book = book ?? throw new ArgumentNullException();
			_sheet = sheet ?? throw new ArgumentNullException();
			RowNumber = row;
			IsHeader = header;
			Row = sheet.Sheet.GetRow(row) ?? sheet.Sheet.CreateRow(row);
		}

		public ICell AppendCell(long value)
		{
			var ret = GetNextCell(_book.RecordIntegerStyle);
			ret.SetCellValue(value);
			return ret;
		}

		public ICell AppendCell(ulong value)
		{
			var ret = GetNextCell(_book.RecordIntegerStyle);
			ret.SetCellValue(value);
			return ret;
		}

		public ICell AppendCell(double value)
		{
			var ret = GetNextCell(_book.RecordFloatStyle);
			ret.SetCellValue(value);
			return ret;
		}

		public ICell AppendCell(decimal value)
		{
			var ret = GetNextCell(_book.RecordFloatStyle);
			ret.SetCellValue((double)value);
			return ret;
		}

		public ICell AppendCell(DateTime value)
		{
			var ret = GetNextCell(_book.RecordDateStyle);
			ret.SetCellValue(value);
			return ret;
		}

		public ICell AppendCell(bool value)
		{
			var ret   = GetNextCell(_book.RecordBooleanStyle);
			ret.SetCellValue(value);
			return ret;
		}
		
		public ICell AppendCell(string value)
		{
			var ret = GetNextCell(_book.RecordTextStyle);
			ret.SetCellValue(value);
			return ret;
		}

		private static readonly Dictionary<Type, HorizontalAlignment> DefaultAlignment = new Dictionary<Type, HorizontalAlignment>()
		{
			{typeof(byte), HorizontalAlignment.Right},
			{typeof(sbyte), HorizontalAlignment.Right},
			{typeof(short), HorizontalAlignment.Right},
			{typeof(ushort), HorizontalAlignment.Right},
			{typeof(int), HorizontalAlignment.Right},
			{typeof(uint), HorizontalAlignment.Right},
			{typeof(long), HorizontalAlignment.Right},
			{typeof(ulong), HorizontalAlignment.Right},
			{typeof(float), HorizontalAlignment.Right},
			{typeof(double), HorizontalAlignment.Right},
			{typeof(decimal), HorizontalAlignment.Right},
			{typeof(DateTime), HorizontalAlignment.Right},
			{typeof(bool), HorizontalAlignment.Center},
			{typeof(string), HorizontalAlignment.Left}
		};
		
		private static readonly Dictionary<Type, string> DefaultFormat = new Dictionary<Type, string>()
		{
			{typeof(string), "@"},
			{typeof(byte), "#,##0"},
			{typeof(sbyte), "#,##0"},
			{typeof(short), "#,##0"},
			{typeof(ushort), "#,##0"},
			{typeof(int), "#,##0"},
			{typeof(uint), "#,##0"},
			{typeof(long), "#,##0"},
			{typeof(ulong), "#,##0"},
			{typeof(float), "#,##0.00"},
			{typeof(double), "#,##0.00"},
			{typeof(decimal), "#,##0.00"},
			{typeof(DateTime), "MM/dd/yyyy"},
		};
		
		public ICell AppendCell(object value, HorizontalAlignment? alignment = null, VerticalAlignment? verticalAlignment = null, string format = null)
		{
			var valueType = value?.GetType() ?? typeof(string);
			var align     = alignment ?? (DefaultAlignment.ContainsKey(valueType) ? DefaultAlignment[valueType] : HorizontalAlignment.Left);
            var valign    = verticalAlignment ?? VerticalAlignment.Bottom;
			var fmt       = string.IsNullOrEmpty(format) ? (DefaultFormat.ContainsKey(valueType) ? DefaultFormat[valueType] : null) : null;
			var style     = IsHeader ? _book.GetHeaderCellStyle(align, valign, fmt) : _book.GetRecordCellStyle(align, valign, fmt);
			var ret       = GetNextCell(style, style);
			
			switch (value)
			{
				case null:
					break;
				case byte val:
					ret.SetCellValue(val);
					break;
				case sbyte val:
					ret.SetCellValue(val);
					break;
				case short val:
					ret.SetCellValue(val);
					break;
				case ushort val:
					ret.SetCellValue(val);
					break;
				case int val:
					ret.SetCellValue(val);
					break;
				case uint val:
					ret.SetCellValue(val);
					break;
				case long val:
					ret.SetCellValue(val);
					break;
				case ulong val:
					ret.SetCellValue(val);
					break;
				case float val:
					ret.SetCellValue(val);
					break;
				case double val:
					ret.SetCellValue(val);
					break;
				case decimal val:
					ret.SetCellValue((double)val);
					break;
				case DateTime val:
					ret.SetCellValue(val);
					break;
				case bool val:
					ret.SetCellValue(val);
					break;
				default:
					ret.SetCellValue(value.ToString());
					break;
			}

			return ret;
		}

		private ICell GetNextCell(ICellStyle defaultStyle, ICellStyle headerStyle = null)
		{
			var cellNum = _nextCell + 1;
			_nextCell++;
			if (headerStyle is null) headerStyle = _book.HeaderStyle;
			if (defaultStyle is null) defaultStyle = _book.RecordTextStyle;
			var style = IsHeader ? headerStyle : defaultStyle;
			var ret = Row.GetCell(cellNum) ?? Row.CreateCell(cellNum);
			ret.CellStyle = style;
			return ret;
		}
		
		public ICell AppendCell()
		{
			var ret = GetNextCell(_book.RecordTextStyle);
			return ret;
		}
		
	}
}
