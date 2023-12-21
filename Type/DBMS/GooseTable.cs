using Google.Apis.Forms.v1.Data;
using Goose.Type.Config;

namespace Goose.Type.DBMS
{
    public class GooseTable
    {
        public Table Table { get; private set; }
        public List<GooseRow> Rows { get; private set; }

        public GooseTable()
        {
            Table = new();
            Rows = new();
        }

        public GooseTable(ListFormResponsesResponse listFormResponsesResponse, Form form, Table t)
        {
            if (!form.Info.Title.Underscore().Equals(t.Name))
                throw new Exception("Form title [" + form.Info.Title + "] is different from associated table name [" + t.Name + "]");

            Table = new(t);
            Rows = new();
            listFormResponsesResponse.Responses?.ToList().ForEach(x => Rows.Add(new(x, t.Columns)));
        }

        public GooseTable(Table table, List<GooseRow> rows)
        {
            Table = new(table);
            Rows = new(rows);
        }

        public GooseTable? Compare(GooseTable gooseTable)
        {
            GooseTable? differenceGooseTable = null;
            List<string>? addedRows = Rows.Select(x => x.RowID).Except(gooseTable.Rows.Select(x => x.RowID)).ToList();
            if (addedRows.Count > 0)
            {
                differenceGooseTable ??= new();
                differenceGooseTable.Rows.AddRange(Rows.Where(x => addedRows.Contains(x.RowID)));
            }

            List<string> removedRows = gooseTable.Rows.Select(x => x.RowID).Except(Rows.Select(x => x.RowID)).ToList();
            if (removedRows.Count > 0)
            {
                differenceGooseTable ??= new();
                differenceGooseTable.Rows.AddRange(gooseTable.Rows.Where(x => removedRows.Contains(x.RowID)));
            }

            return differenceGooseTable == null ? null : new(gooseTable.Table, differenceGooseTable.Rows);
        }

        public override string ToString()
        {
            string str = Table.Name.DoNotPrint(string.Empty, "Name: ", Environment.NewLine);
            str += Table.FormID.DoNotPrint(null, "FormID: ", Environment.NewLine);
            str += Table.PrefilledFormID.DoNotPrint(null, "PrefilledformID: ", Environment.NewLine);
            str += Table.Columns.Count > 0 ? "Columns: " + Environment.NewLine + string.Join(Environment.NewLine, Table.Columns) + Environment.NewLine : string.Empty;
            str += string.Join(Environment.NewLine, Rows);
            return str;
        }
    }
}