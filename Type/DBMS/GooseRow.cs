using Google.Apis.Forms.v1.Data;
using Goose.Type.Config;

namespace Goose.Type.DBMS
{
    public class GooseRow
    {
        public string RowID { get; private set; }
        public Dictionary<string, string?> Cells { get; private set; }

        public GooseRow(FormResponse formResponse, List<Column> columns)
        {
            Cells = new();
            RowID = formResponse.ResponseId;
            formResponse.Answers.ToList().ForEach(x =>
            {
                Column? column = columns.FirstOrDefault(y => y.Key.Equals(x.Value.QuestionId));
                if (column != null)
                    Cells.Add(column.Value, x.Value.TextAnswers.Answers.First().Value);
            });
        }

        public GooseRow(string rowID, Dictionary<string, string?> cells)
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