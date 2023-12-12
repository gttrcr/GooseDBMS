using System.Reflection;
using Google.Apis.Services;
using Goose.Type.Config;
using Newtonsoft.Json;
using Google.Apis.Forms.v1;
using System.Data.SQLite;
using Goose.Type.DBMS;

namespace Goose
{
    public class Owner : DBMS
    {
        public delegate void DataReceivedDelegate(GooseDB gooseDB, GooseDB previousGooseDB, GooseDB differenceGooseDB);
        public DataReceivedDelegate? DataReceivedCallback;

        public Owner(string configJson) : base(configJson)
        {
            Config? obj = JsonConvert.DeserializeObject<Config>(File.ReadAllText(configJson)) ?? throw new Exception("The configuration file " + configJson + " is null");
            if (obj.ClientSecretFilePath == null && obj.ApiKey == null)
                throw new Exception("ClientSecretFilePath or ApiKey must be set for Owner role");
            if (obj.ClientSecretFilePath != null && obj.ApiKey != null)
                throw new Exception("ClientSecretFilePath or ApiKey must be set for Owner role. Not both");

            List<Table> tablesWithNullorEmptyFormID = obj.Tables.Where(x => string.IsNullOrEmpty(x.FormID)).ToList();
            if (tablesWithNullorEmptyFormID.Count > 0)
                throw new Exception("FormID of tables " + string.Join(", ", tablesWithNullorEmptyFormID.Select(x => x.Name)) + " must be set too");

            if (obj.ClientSecretFilePath != null)
                FormsService = new(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = Credential(DBConfig.ClientSecretFilePath, new[]
                    {
                        FormsService.Scope.FormsResponsesReadonly,
                        FormsService.Scope.FormsBody,
                    }),
                    ApplicationName = Assembly.GetExecutingAssembly().GetName().Name,
                });

            if (obj.ApiKey != null)
                throw new Exception("ApiKey is not supported yet");

            DataReceivedCallback = null;
            GooseDB = new(FormsService.Forms, DBConfig.Tables);
            UpdateLocalDB();
            new Thread(DataReceivedService) { IsBackground = true }.Start();
        }

        private void UpdateLocalDB()
        {
            if (SQLite == null)
            {
                SQLite = new SQLiteConnection("Data Source=goose.db;Version=3;New=True;Compress=True;");
                SQLite.Open();
            }

            GooseDB?.Tables.ForEach(x =>
            {
                SQLiteCommand command;
                string query = string.Join(", ", x.Table.Columns.Select(y => y.Value.Underscore() + " VARCHAR(1000)"));
                query = "CREATE TABLE IF NOT EXISTS " + x.Table.Name.Underscore() + " (GooseID VARCHAR(1000) PRIMARY KEY" + (string.IsNullOrEmpty(query) ? string.Empty : ", " + query) + ")";
                command = SQLite.CreateCommand();
                command.CommandText = query;
                command.ExecuteNonQuery();

                x.Rows.ForEach(y =>
                {
                    query = string.Join(", ", y.Cells.Select(z => z.Key.Underscore()));
                    query = "INSERT INTO " + x.Table.Name.Underscore() + " (GooseID" + (string.IsNullOrEmpty(query) ? string.Empty : ", " + query) +
                    ") VALUES ('" + y.RowID + "'" + (string.IsNullOrEmpty(query) ? string.Empty : ", " + string.Join(", ", y.Cells.Select(z => "'" + z.Value + "'")))
                    + ") ON CONFLICT(GooseID) DO NOTHING";

                    command = SQLite.CreateCommand();
                    command.CommandText = query;
                    command.ExecuteNonQuery();
                });
            });
        }

        private void DataReceivedService()
        {
            GooseDB previousGooseDB = new();
            while (true)
            {
                Thread.Sleep(2000);
                GooseDB = new(FormsService.Forms, DBConfig.Tables);
                UpdateLocalDB();
                GooseDB? differenceGooseDB = GooseDB.Compare(previousGooseDB);
                if (differenceGooseDB != null)
                    DataReceivedCallback?.Invoke(GooseDB, previousGooseDB, differenceGooseDB);
                previousGooseDB = GooseDB;
            }
        }

        public GooseTable? Select(string query)
        {
            if (SQLite == null)
                return null;

            SQLiteCommand command = SQLite.CreateCommand();
            command.CommandText = query;
            SQLiteDataReader select = command.ExecuteReader();

            List<GooseRow> rows = new();
            while (select.Read())
            {
                Dictionary<string, string?> dictionary = new();
                for (int i = 0; i < select.FieldCount; i++)
                {
                    object value = select.GetValue(i);
                    dictionary.Add(select.GetName(i), value.GetType().Equals(typeof(DBNull)) ? string.Empty : value.ToString());
                }
                rows.Add(new(Guid.NewGuid().ToString(), dictionary));
            }

            Table table = new() { Name = query };
            for (int i = 0; i < select.FieldCount; i++)
                table.Columns.Add(new Column(0, select.GetName(i), string.Empty));

            return new(table, rows);
        }
    }
}