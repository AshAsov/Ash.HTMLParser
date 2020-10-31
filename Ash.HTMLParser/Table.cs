using System.Collections.Generic;

namespace Ash.HTMLParser
{
    public partial class Parser
    {
        private class Table : ITable
        {
            public List<string> InnerHeader { get; set; } = new List<string>();

            public List<List<string>> InnerRows { get; set; } = new List<List<string>>();

            public IReadOnlyList<string> Header => InnerHeader;

            public IReadOnlyList<IReadOnlyList<string>> Rows => InnerRows;

            public int ColumnsCount => InnerHeader.Count;

            public int RowsCount => InnerRows.Count;
        }
    }
}