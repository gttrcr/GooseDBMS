using System.Reflection;
using System.Web;
using System.Collections.Specialized;
using Google.Apis.Forms.v1;
using Google.Apis.Forms.v1.Data;
using Newtonsoft.Json;
using Google.Apis.Services;

namespace Goose.Type.Config
{
    public struct GooseConfigEntry<T>
    {
        public string PrefilledUrl { get; set; }
        public string FormID { get; set; }
        public List<string> Export { get; set; }
        public Dictionary<string, T> Dynamic { get; set; }
    }

    public class Config
    {
        public string? ClientSecretFilePath { get; set; }
        public string? ApiKey { get; set; }
        public List<Table> Tables { get; set; }
        public LogSeverity LogSeverity { get; set; }

        public Config()
        {
            LogSeverity = LogSeverity.Info;
            Tables = new();
        }

        public static void CreateConfig<T>(string configJson, string clientSecretFilePath, List<GooseConfigEntry<T>> gooseConfigEntries, LogSeverity logSeverity)
        {
            List<GooseConfigEntry<T>> invalidUrl = gooseConfigEntries.Where(x => !Uri.TryCreate(x.PrefilledUrl, UriKind.RelativeOrAbsolute, out Uri? uri)).ToList();
            if (invalidUrl.Count > 0)
                throw new Exception("There are some invalid url: " + Environment.NewLine + string.Join(Environment.NewLine, invalidUrl.Select(x => x.PrefilledUrl)));

            Config gooseConfig = new() { ClientSecretFilePath = clientSecretFilePath };
            for (int i = 0; i < gooseConfigEntries.Count; i++)
            {
                GooseConfigEntry<T> gooseConfigEntry = gooseConfigEntries[i];
                Table table = new()
                {
                    PrefilledFormID = new Uri(gooseConfigEntry.PrefilledUrl).Segments[4],
                    FormID = gooseConfigEntry.FormID,
                    Dynamic = gooseConfigEntry.Dynamic
                };

                FormsService formsService = new(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = Goose.DBMS.Credential(clientSecretFilePath, new[] { FormsService.Scope.FormsBody }, "goosedbms_config_credential.json", null),
                    ApplicationName = Assembly.GetExecutingAssembly().GetName().Name,
                });

                Form form = formsService.Forms.Get(table.FormID).Execute();
                table.Name = form.Info.Title.Underscore();

                NameValueCollection nvc = HttpUtility.ParseQueryString(new Uri(gooseConfigEntry.PrefilledUrl).Query);
                nvc.Remove("usp");

                IList<Item> filteredByProperties = form.Items.Where(x => x.PageBreakItem == null && x.QuestionItem != null).ToList();
                if (new List<int>() { nvc.Count, gooseConfigEntry.Export.Count }.Distinct().Count() != 1)
                    throw new Exception(form.Info.Title + ") The number of arguments in query string (" + nvc.Count + ") and Exports in Exports (" + gooseConfigEntry.Export.Count + ") must be the same");

                for (int j = 0; j < nvc.Count; j++)
                {
                    string? key = nvc.Keys[j];
                    string? value = nvc[key];
                    if (key == null)
                        throw new Exception("Key is null in the array of prefilled arguments");
                    else if (value == null)
                        throw new Exception("Value is null in the array of prefilled arguments");
                    else
                        table.Columns.Add(new(int.Parse(key.Split('.')[1]), value.Underscore(), filteredByProperties[j].QuestionItem.Question.QuestionId, gooseConfigEntry.Export[j]));
                }

                gooseConfig.Tables.Add(table);
            }

            gooseConfig.LogSeverity = logSeverity;
            File.WriteAllText(configJson, JsonConvert.SerializeObject(gooseConfig, Formatting.Indented));
        }
    }
}