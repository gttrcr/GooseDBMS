namespace Goose.Type.Config
{
    public class Column
    {
        public long Entry { get; set; }
        public string Value { get; set; }
        public string Key { get; set; }
        public string Export { get; set; }

        public Column()
        {
            Entry = 0;
            Value = string.Empty;
            Key = string.Empty;
            Export = string.Empty;
        }

        public Column(long entry, string value, string key, string export)
        {
            Entry = entry;
            Value = value;
            Key = key;
            Export = export;
        }

        public override string ToString()
        {
            return "Entry: " + Entry + "\tValue: " + Value + "\tKey: " + Key + "\tExport: " + Export;
        }
    }
}