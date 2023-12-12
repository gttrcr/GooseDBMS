namespace Goose.Type.Config
{
    public class Column
    {
        public long Entry { get; set; }
        public string Value { get; set; }
        public string Key { get; set; }

        public Column()
        {
            Entry = 0;
            Value = string.Empty;
            Key = string.Empty;
        }

        public Column(long entry, string value, string key)
        {
            Entry = entry;
            Value = value;
            Key = key;
        }

        public override string ToString()
        {
            return "Entry: " + Entry + "\tValue: " + Value + "\tKey: " + Key;
        }
    }
}