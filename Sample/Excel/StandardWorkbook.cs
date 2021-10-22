using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace Sample.Excel
{
	public class StandardWorkbook
	{
		public StandardWorkbook(XSSFWorkbook workbook, IFont headerFont, IFont recordFont, ICellStyle headerStyle, ICellStyle recordTextStyle, ICellStyle recordFloatStyle, ICellStyle recordIntegerStyle, ICellStyle recordBooleanStyle, ICellStyle recordDateStyle)
		{
			Workbook = workbook;
			HeaderFont = headerFont;
			RecordFont = recordFont;
			HeaderStyle = headerStyle;
			RecordTextStyle = recordTextStyle;
			RecordFloatStyle = recordFloatStyle;
			RecordIntegerStyle = recordIntegerStyle;
			RecordBooleanStyle = recordBooleanStyle;
			RecordDateStyle = recordDateStyle;
		}

		public XSSFWorkbook Workbook           { get; }
		public IFont        HeaderFont         { get; }
		public IFont        RecordFont         { get; }
		public ICellStyle   HeaderStyle        { get; }
		public ICellStyle   RecordTextStyle    { get; }
		public ICellStyle   RecordFloatStyle   { get; }
		public ICellStyle   RecordIntegerStyle { get; }
		public ICellStyle   RecordBooleanStyle { get; }
		public ICellStyle   RecordDateStyle    { get; }

		private List<ICellStyle> _styles = new List<ICellStyle>();

		public StandardWorkbook(string fontName = "Calibri", float fontSize = 11f)
		{
			Workbook = new XSSFWorkbook();

			HeaderFont                    = Workbook.CreateFont();
			HeaderFont.FontName           = fontName;
			HeaderFont.FontHeightInPoints = fontSize;
			HeaderFont.IsBold             = true;

			RecordFont                    = Workbook.CreateFont();
			RecordFont.FontName           = fontName;
			RecordFont.FontHeightInPoints = fontSize;

			var fmt = Workbook.CreateDataFormat();

			HeaderStyle = Workbook.CreateCellStyle();
			HeaderStyle.SetFont(HeaderFont);
			HeaderStyle.Alignment         = HorizontalAlignment.Center;
			HeaderStyle.VerticalAlignment = VerticalAlignment.Bottom;
			_styles.Add(HeaderStyle);

			RecordTextStyle = Workbook.CreateCellStyle();
			RecordTextStyle.SetFont(RecordFont);
			RecordTextStyle.Alignment         = HorizontalAlignment.Left;
			RecordTextStyle.VerticalAlignment = VerticalAlignment.Bottom;
			RecordTextStyle.DataFormat        = fmt.GetFormat("@");
			_styles.Add(RecordTextStyle);

			RecordFloatStyle = Workbook.CreateCellStyle();
			RecordFloatStyle.SetFont(RecordFont);
			RecordFloatStyle.Alignment         = HorizontalAlignment.Right;
			RecordFloatStyle.VerticalAlignment = VerticalAlignment.Bottom;
			RecordFloatStyle.DataFormat        = fmt.GetFormat("#,##0.00");
			_styles.Add(RecordFloatStyle);

			RecordIntegerStyle = Workbook.CreateCellStyle();
			RecordIntegerStyle.SetFont(RecordFont);
			RecordIntegerStyle.Alignment         = HorizontalAlignment.Right;
			RecordIntegerStyle.VerticalAlignment = VerticalAlignment.Bottom;
			RecordIntegerStyle.DataFormat        = fmt.GetFormat("#,##0");
			_styles.Add(RecordIntegerStyle);

			RecordBooleanStyle = Workbook.CreateCellStyle();
			RecordBooleanStyle.SetFont(RecordFont);
			RecordBooleanStyle.Alignment         = HorizontalAlignment.Center;
			RecordBooleanStyle.VerticalAlignment = VerticalAlignment.Bottom;
			_styles.Add(RecordBooleanStyle);

			RecordDateStyle = Workbook.CreateCellStyle();
			RecordDateStyle.SetFont(RecordFont);
			RecordDateStyle.Alignment         = HorizontalAlignment.Right;
			RecordDateStyle.VerticalAlignment = VerticalAlignment.Bottom;
			RecordDateStyle.DataFormat        = fmt.GetFormat("MM/dd/yyyy");
			_styles.Add(RecordDateStyle);
		}

		private ICellStyle FindStyle(IFont font, HorizontalAlignment alignment, VerticalAlignment verticalAlignment, string format)
		{
			var   fontIndex   = font.Index;
			short formatIndex = string.IsNullOrEmpty(format) ? (short) 0 : Workbook.CreateDataFormat().GetFormat(format);
			
            foreach (var style in _styles)
			{
				if (style.FontIndex == fontIndex && style.DataFormat == formatIndex && style.Alignment == alignment && style.VerticalAlignment == verticalAlignment)
				{
					return style;
				}
			}

			var ret = Workbook.CreateCellStyle();
			ret.SetFont(font);
			ret.DataFormat        = formatIndex;
			ret.Alignment         = alignment;
			ret.VerticalAlignment = verticalAlignment;

			_styles.Add(ret);

			return ret;
		}
        
        public ICellStyle GetRecordCellStyle(HorizontalAlignment alignment, VerticalAlignment verticalAlignment = VerticalAlignment.Bottom, string format = null) => FindStyle(RecordFont, alignment, verticalAlignment, format);

		public ICellStyle GetHeaderCellStyle(HorizontalAlignment alignment = HorizontalAlignment.Center, VerticalAlignment verticalAlignment = VerticalAlignment.Bottom, string format = null) => FindStyle(HeaderFont, alignment, verticalAlignment, format);

		public StandardSheet BuildSheet(string name, Action<StandardSheet> build = null)
		{
			var ret = new StandardSheet(this, Workbook.CreateSheet(name));

			build?.Invoke(ret);

			return ret;
		}

		public StandardWorkbook AddSheet<T>(string name, IEnumerable<T> records, params Expression<Func<T, object>>[] columnDefinitions) where T : class
		{
			return AddSheet(name, records, columnDefinitions.Select(x => new SpreadsheetColumn<T>(x)).ToArray());
		}

		public StandardWorkbook AddSheet<T>(string name, IEnumerable<T> records, params SpreadsheetColumn<T>[] columnDefinitions) where T : class
		{
			BuildSheet(name, (sheet) =>
			{
				sheet.SetHeaderRow((row) =>
				{
					foreach (var col in columnDefinitions)
					{
						row.AppendCell(col.Title);
					}
				});

				foreach (var record in records)
				{
					sheet.AppendRow((row) =>
					{
						foreach (var col in columnDefinitions)
						{
							row.AppendCell(col.Property.Get(record), col.Alignment, col.VerticalAlignment, col.FormatString);
						}
					});
				}
			});

			return this;
		}
	}
}
