using System;
using NPOI.SS.UserModel;

namespace Sample.Excel
{
	public class StandardSheet
	{
		public ISheet Sheet { get; }

		private StandardWorkbook _book;
		private int _lastRow = 0;

		internal StandardSheet(StandardWorkbook book, ISheet sheet)
		{
			_book = book ?? throw new ArgumentNullException();
			Sheet = sheet ?? throw new ArgumentNullException();
		}

		public StandardRow SetHeaderRow(Action<StandardRow> build = null)
		{
			var ret = new StandardRow(_book, this, 0, true);

			build?.Invoke(ret);

			return ret;
		}

		public StandardRow AppendRow(Action<StandardRow> build = null)
		{
			var ret = new StandardRow(_book, this, _lastRow + 1, false);
			_lastRow = ret.RowNumber;
			
			build?.Invoke(ret);

			return ret;
		}
	}
}
