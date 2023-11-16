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
            if (form.Info.Title != t.Name)
                throw new Exception("Form title [" + form.Info.Title + "] is different from associated table name [" + t.Name + "]");

            Table = new(t);
            Rows = new();
            listFormResponsesResponse.Responses.ToList().ForEach(x => Rows.Add(new(x, t.Columns)));
        }

        public GooseTable(Table table, List<GooseRow> rows)
        {
            Table = new(table);
            Rows = new(rows);
        }

        public GooseTable? Compare(GooseTable gooseTable)
        {
            GooseTable? differenceGooseTable = null;
            if (Table.Name != gooseTable.Table.Name)
            {
                differenceGooseTable ??= new();
                differenceGooseTable.Table.Name = Table.Name;
            }
            if (Table.PrefilledFormID != gooseTable.Table.PrefilledFormID)
            {
                differenceGooseTable ??= new();
                differenceGooseTable.Table.PrefilledFormID = Table.PrefilledFormID;
            }
            if (Table.SpreadsheetID != gooseTable.Table.SpreadsheetID)
            {
                differenceGooseTable ??= new();
                differenceGooseTable.Table.SpreadsheetID = Table.SpreadsheetID;
            }

            List<string> addedColumns = Table.Columns.Select(x => x.Key).Except(gooseTable.Table.Columns.Select(x => x.Key)).ToList();
            if (addedColumns.Count > 0)
            {
                differenceGooseTable ??= new();
                differenceGooseTable.Table.Columns.AddRange(Table.Columns.Where(x => addedColumns.Contains(x.Key)));
            }

            List<string> removedColumns = gooseTable.Table.Columns.Select(x => x.Key).Except(Table.Columns.Select(x => x.Key)).ToList();
            if (removedColumns.Count > 0)
            {
                differenceGooseTable ??= new();
                differenceGooseTable.Table.Columns.AddRange(gooseTable.Table.Columns.Where(x => removedColumns.Contains(x.Key)));
            }

            for (int i = 0; i < Table.Columns.Count; i++)
            {
                Column column = gooseTable.Table.Columns.First(x => x.Key.Equals(Table.Columns[i].Key));
                Column? differenceColumn = null;
                if (column.Entry != Table.Columns[i].Entry)
                {
                    differenceColumn ??= new();
                    differenceColumn.Entry = Table.Columns[i].Entry;
                }
                if (column.Value != Table.Columns[i].Value)
                {
                    differenceColumn ??= new();
                    differenceColumn.Value = Table.Columns[i].Value;
                }

                if (differenceColumn != null)
                    differenceGooseTable?.Table.Columns.Add(differenceColumn);
            }

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

            return differenceGooseTable;
        }

        public override string ToString()
        {
            string str = "Name: " + Table.Name + Environment.NewLine;
            str += "FormID: " + Table.FormID + Environment.NewLine;
            str += "PrefilledformID: " + Table.PrefilledFormID + Environment.NewLine;
            str += "SpreadsheetID: " + Table.SpreadsheetID + Environment.NewLine;
            str += "Columns: " + Environment.NewLine;
            Table.Columns.ForEach(x => str += "\t" + x + Environment.NewLine);
            str += "Data: " + Environment.NewLine;
            Rows.ForEach(x => str += x + Environment.NewLine);
            return str;
        }
    }
}