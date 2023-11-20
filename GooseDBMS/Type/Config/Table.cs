namespace Goose.Type.Config
{
    public class Table
    {
        public string Name { get; set; }
        public List<Column> Columns { get; set; }
        public string? FormID { get; set; }
        public string? PrefilledFormID { get; set; }
        public string? SpreadsheetID { get; set; }

        public Table()
        {
            Name = string.Empty;
            Columns = new();
        }

        public Table(Table t)
        {
            Name = t.Name;
            Columns = t.Columns;
            FormID = t.FormID;
            PrefilledFormID = t.PrefilledFormID;
            SpreadsheetID = t.SpreadsheetID;
        }
    }
}