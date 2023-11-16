using System.Collections.Specialized;
using System.Data;
using System.Reflection;
using System.Web;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Forms.v1;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Newtonsoft.Json;
using System.Data.SQLite;
using Goose.Type.DBMS;
using Goose.Type.Config;

namespace Goose
{
    public class DBMS
    {
        private GooseConfig DBConfig { get; set; }
        private FormsService FormsService { get; set; }
        // public SheetsService SheetsService { get; private set; }

        public delegate void DataReceivedDelegate(GooseDB gooseDB, GooseDB previousGooseDB, GooseDB differenceGooseDB);
        public DataReceivedDelegate? DataReceivedCallback;

        private GooseDB? GooseDB { get; set; }

        public static UserCredential? Credential(string? clientSecretFilePath, string[] scopes)
        {
            if (clientSecretFilePath == null)
                return null;

            using FileStream stream = new(clientSecretFilePath, FileMode.Open, FileAccess.Read);
            string credPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            credPath = Path.Combine(credPath, ".credentials/google-dotnet-quickstart.json");
            Console.WriteLine("Credential file saved to: " + credPath);
            UserCredential credential = GoogleWebAuthorizationBroker.AuthorizeAsync(GoogleClientSecrets.FromStream(stream).Secrets, scopes, "user", CancellationToken.None, new FileDataStore(credPath, true)).Result;
            return credential;
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

        private static SQLiteConnection? SQLite { get; set; }

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
                string query = string.Join(", ", x.Table.Columns.Select(y => y.Value + " VARCHAR(1000)"));
                query = "CREATE TABLE IF NOT EXISTS " + x.Table.Name + " (GooseID VARCHAR(1000) PRIMARY KEY" + (string.IsNullOrEmpty(query) ? string.Empty : ", " + query) + ")";
                command = SQLite.CreateCommand();
                command.CommandText = query;
                command.ExecuteNonQuery();

                x.Rows.ForEach(y =>
                {
                    query = string.Join(", ", y.Cells.Select(z => z.Key.Value));
                    query = "INSERT INTO " + x.Table.Name + " (GooseID" + (string.IsNullOrEmpty(query) ? string.Empty : ", " + query) +
                    ") VALUES ('" + y.RowID + "'" + (string.IsNullOrEmpty(query) ? string.Empty : ", " + string.Join(", ", y.Cells.Select(z => "'" + z.Value + "'")))
                    + ") ON CONFLICT(GooseID) DO NOTHING";

                    command = SQLite.CreateCommand();
                    command.CommandText = query;
                    command.ExecuteNonQuery();
                });
            });
        }

        public DBMS(string configJson)
        {
            if (!File.Exists(configJson))
                throw new Exception("Cannot find file " + configJson);

            GooseConfig? obj = JsonConvert.DeserializeObject<GooseConfig>(File.ReadAllText(configJson)) ?? throw new Exception("The configuration file " + configJson + " is null");
            if (obj.Tables.Count == 0)
                throw new Exception("Empty table list. Key is Tables");

            List<Table> tablesWithNullOrEmptyName = obj.Tables.Where(x => string.IsNullOrEmpty(x.Name)).ToList();
            if (tablesWithNullOrEmptyName.Count > 0)
                throw new Exception("There are " + tablesWithNullOrEmptyName.Count + " tables with empty name");

            List<Table> tablseWithEmptyColumnList = obj.Tables.Where(x => x.Columns.Count == 0).ToList();
            if (tablseWithEmptyColumnList.Count > 0)
                throw new Exception("Empty column list in tables " + string.Join(", ", tablseWithEmptyColumnList.Select(x => x.Name)));

            List<Table> tablesWithColumnWithNoKey = obj.Tables.Where(x => x.Columns.Any(y => y.Entry == 0)).ToList();
            if (tablesWithColumnWithNoKey.Count > 0)
                throw new Exception("Entry is 0 for some column in tables " + string.Join(", ", tablesWithColumnWithNoKey.Select(x => x.Name)));

            List<Table> tablesWithColumnWithNullOrEmptyValue = obj.Tables.Where(x => x.Columns.Any(y => string.IsNullOrEmpty(y.Value))).ToList();
            if (tablesWithColumnWithNullOrEmptyValue.Count > 0)
                throw new Exception("Value is null or empty for some column in tables " + string.Join(" ", tablesWithColumnWithNullOrEmptyValue.Select(x => x.Name)));

            List<Table> tablesWithColumnWithNullOrEmptyKey = obj.Tables.Where(x => x.Columns.Any(y => string.IsNullOrEmpty(y.Key))).ToList();
            if (tablesWithColumnWithNullOrEmptyKey.Count > 0)
                throw new Exception("Key is null or empty for some column in tables " + string.Join(" ", tablesWithColumnWithNullOrEmptyKey.Select(x => x.Name)));

            List<Table> tablesWithNullOrEmptyPrefilledFormID = obj.Tables.Where(x => string.IsNullOrEmpty(x.PrefilledFormID)).ToList();
            if (tablesWithNullOrEmptyPrefilledFormID.Count > 0)
                throw new Exception("PrefilledFormID is null or empty in tables " + string.Join(", ", tablesWithNullOrEmptyPrefilledFormID.Select(x => x.Name)));

            List<Table> tablesWithNullorEmptyFormID = obj.Tables.Where(x => string.IsNullOrEmpty(x.FormID)).ToList();
            if (obj.ClientSecretFilePath != null && tablesWithNullorEmptyFormID.Count > 0)
                throw new Exception("When ClientSecretFilePath is set, FormID of tables " + string.Join(", ", tablesWithNullorEmptyFormID.Select(x => x.Name)) + " must be set too");

            DBConfig = obj;
            GooseDB = null;

            FormsService = new(new BaseClientService.Initializer()
            {
                HttpClientInitializer = Credential(DBConfig.ClientSecretFilePath, new[]
                {
                    FormsService.Scope.FormsResponsesReadonly,
                    FormsService.Scope.FormsBody,
                }),
                ApplicationName = Assembly.GetExecutingAssembly().GetName().Name,
            });

            // SheetsService = new(new BaseClientService.Initializer()
            // {
            //     HttpClientInitializer = Credential(DBConfig.ClientSecretFilePath, new[]
            //     {
            //         SheetsService.Scope.Spreadsheets,
            //     }),
            //     ApplicationName = Assembly.GetExecutingAssembly().GetName().Name,
            // });

            DataReceivedCallback = null;
            if (FormsService.HttpClientInitializer != null)
            {
                GooseDB = new(FormsService.Forms, DBConfig.Tables);
                UpdateLocalDB();
                new Thread(DataReceivedService) { IsBackground = true }.Start();
            }
        }

        //Insert a record in tableName based on all columns value
        public bool Insert(string tableName, List<string> columns)
        {
            Table? t = DBConfig.Tables.Find(x => x.Name.Equals(tableName));
            if (t?.Columns.Count != columns.Count)
                throw new Exception("Number of column in table " + tableName + " is different from the number of columns passed as input");

            Uri uri = new("https://docs.google.com/forms/d/e/");
            Uri uri1 = new(uri, t.PrefilledFormID + "/");
            Uri uri2 = new(uri1, "formResponse");

            UriBuilder uriBuilder = new(uri2);
            NameValueCollection query = HttpUtility.ParseQueryString(uriBuilder.Query);
            query["submit"] = "Submit";
            for (int i = 0; i < t.Columns.Count; i++)
                query["entry." + t.Columns[i].Entry] = columns[i];

            uriBuilder.Query = query.ToString();
            return Http.Get(uriBuilder.ToString(), out string content);
        }

        //Insert a record in tableName based on a dictionary of values
        public bool Insert(string tableName, Dictionary<string, string> columns)
        {
            Table? t = DBConfig.Tables.Find(x => x.Name.Equals(tableName)) ?? throw new Exception("Cannot find table " + tableName);
            List<string> columnsNamePassed = columns.Keys.ToList();
            List<string> columnsNameConfigured = t.Columns.Select(x => x.Value).ToList();
            List<string> columnsNameWithoutDefinition = columnsNamePassed.Except(columnsNameConfigured).ToList();
            if (columnsNameWithoutDefinition.Count > 0)
                throw new Exception("Columns " + string.Join(" ", columnsNameWithoutDefinition) + " are not defined for table " + tableName);

            List<string> columnToInsert = new();
            for (int i = 0; i < columnsNameConfigured.Count; i++)
                columnToInsert.Add(columnsNamePassed.Contains(columnsNameConfigured[i]) ? columns[columnsNameConfigured[i]] : string.Empty);

            return Insert(tableName, columnToInsert);
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
                Dictionary<Column, string?> dictionary = new();
                for (int i = 0; i < select.FieldCount; i++)
                {
                    object value = select.GetValue(i);
                    dictionary.Add(new Column(0, select.GetName(i), string.Empty), value.GetType().Equals(typeof(DBNull)) ? string.Empty : value.ToString());
                }
                rows.Add(new(Guid.NewGuid().ToString(), dictionary));
            }

            Table table = new()
            {
                Name = query
            };
            for (int i = 0; i < select.FieldCount; i++)
                table.Columns.Add(new Column(0, select.GetName(i), string.Empty));

            return new(table, rows);

            // // if (t != null)
            // // {
            // // string code = @"
            // //     t.
            // // ";

            // // // string whereStatenent = string.Join(".", where.Select(x => "Where(x=>" + x + ")"));
            // // // code = code.Replace("#", whereStatenent);
            // // ScriptOptions scriptOptions = ScriptOptions.Default.AddReferences(typeof(System.Linq.Enumerable).Assembly.Location);
            // // scriptOptions = scriptOptions.AddImports("System.Linq");
            // // Script script = CSharpScript.Create<GooseTable>(code, scriptOptions, typeof(SelectObject));

            // // Mutex.WaitOne();
            // // // object result = script.RunAsync(new SelectObject()
            // // // {
            // // //     GooseDB = GooseDB,
            // // //     From = from
            // // // }).Result.ReturnValue;
            // // List<(string, List<(string, string?)>)> test = t.Rows.Select(x => (x.RowID, x.Cells.Select(x => (x.Key.Value, x.Value)).ToList())).ToList();

            // GooseTable? t = GooseDB?.Tables.FirstOrDefault(x => x.Table.Name.Equals(from));
            // List<GooseRow>? rows = t?.Rows?.Select(x => new GooseRow(x.RowID, x.Cells.Where(y =>
            // {
            //     bool filter = select.Count > 0 && select[0] == "*";
            //     for (int i = 0; i < select.Count; i++)
            //         filter |= y.Key.Value.Equals(select[i]);
            //     return filter;
            // }).ToDictionary(y => y.Key, y => y.Value))).ToList();

            // if (rows != null)
            //     t = new(rows);

            // return t;
        }

        //Block the main thread forever
        public static void Block()
        {
            while (true)
                Thread.Sleep(int.MaxValue);
        }
    }
}