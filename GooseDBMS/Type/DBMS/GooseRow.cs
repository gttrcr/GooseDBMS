using Google.Apis.Forms.v1.Data;
using Goose.Type.Config;

namespace Goose.Type.DBMS
{
    public class GooseRow
    {
        public string RowID { get; private set; }
        public Dictionary<Column, string?> Cells { get; private set; }

        public GooseRow(FormResponse formResponse, List<Column> columns)
        {
            Cells = new();
            RowID = formResponse.ResponseId;
            formResponse.Answers.ToList().ForEach(x =>
            {
                Column? column = columns.FirstOrDefault(y => y.Key.Equals(x.Key)) ?? throw new Exception("Columns with key [" + string.Join(", ", x.Key) + "] have no definition");
                Cells.Add(column, x.Value.TextAnswers.Answers.First().Value);
            });
        }

        public GooseRow(string rowID, Dictionary<Column, string?> cells)
        {
            RowID = rowID;
            Cells = new(cells);
        }

        public override string ToString()
        {
            return RowID + "|" + string.Join("|", Cells.Values) + "|";
        }
    }
}