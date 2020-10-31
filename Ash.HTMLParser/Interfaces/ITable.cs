using System.Collections.Generic;

namespace Ash.HTMLParser
{
    public interface ITable
    {
        public IReadOnlyList<string> Header { get; }

        public IReadOnlyList<IReadOnlyList<string>> Rows { get; }

        public int ColumnsCount { get; }

        public int RowsCount { get; }
    }
}