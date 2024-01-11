using System.Data;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Forms.v1;
using Google.Apis.Util.Store;
using Newtonsoft.Json;
using System.Data.SQLite;
using Goose.Type.DBMS;
using Goose.Type.Config;
using System.Collections.Specialized;
using System.Web;
using System.Drawing;

namespace Goose
{
    public class DBMS
    {
        protected Config DBConfig { get; set; }
        protected FormsService FormsService { get; set; }
        protected static SQLiteConnection? SQLite { get; set; }
        protected GooseDB? GooseDB { get; set; }

        public static UserCredential? Credential(string? clientSecretFilePath, string[] scopes, string credPath, DBMS? dBMSinstance)
        {
            if (clientSecretFilePath == null)
                return null;

            if (!File.Exists(clientSecretFilePath))
            {
                if (dBMSinstance == null)
                    Console.WriteLine("[GooseDBMS] ClientSecret at " + clientSecretFilePath + " was not found");
                else
                    dBMSinstance?.WriteLine("ClientSecret at " + clientSecretFilePath + " was not found", LogSeverity.Warn);

                return null;
            }

            UserCredential? credential = null;
            using (FileStream stream = new(clientSecretFilePath, FileMode.Open, FileAccess.Read))
            {
                credPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), Path.Combine(".credentials/", credPath));
                if (dBMSinstance == null)
                    Console.WriteLine("[GooseDBMS] Credential file saved to: " + credPath);
                else
                    dBMSinstance?.WriteLine("Credential file saved to: " + credPath, LogSeverity.Info);
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(GoogleClientSecrets.FromStream(stream).Secrets, scopes, "user", CancellationToken.None, new FileDataStore(credPath, true)).Result;
            }

            return credential;
        }

        public void WriteLine(string? value, LogSeverity logSeverity)
        {
            if (logSeverity >= DBConfig.LogSeverity)
            {
                ConsoleColor color = Console.ForegroundColor;
                switch (logSeverity)
                {
                    case LogSeverity.Info:
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        break;
                    case LogSeverity.Warn:
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        break;
                    case LogSeverity.Error:
                        Console.ForegroundColor = ConsoleColor.Red;
                        break;
                }
                
                Console.WriteLine("[GooseDBMS] " + value);
                
                Console.ForegroundColor = color;
            }
        }

        public DBMS(string configJson)
        {
            if (!File.Exists(configJson))
                throw new Exception("Cannot find file " + configJson);

            Config? obj = JsonConvert.DeserializeObject<Config>(File.ReadAllText(configJson)) ?? throw new Exception("The configuration file " + configJson + " is null");
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

            DBConfig = obj;
            FormsService = new();
            GooseDB = null;
        }

        //Block the main thread forever
        public static void Block()
        {
            while (true)
                Thread.Sleep(int.MaxValue);
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
    }
}