using System.Reflection;
using Newtonsoft.Json;
using Google.Apis.Forms.v1;
using Google.Apis.Services;
using Google.Apis.Forms.v1.Data;
using System.Web;
using System.Collections.Specialized;

namespace Goose.Type.Config
{
    public class Config
    {
        public string? ClientSecretFilePath { get; set; }
        public string? ApiKey { get; set; }
        public List<Table> Tables { get; set; }

        public Config()
        {
            Tables = new();
        }

        public static void CreateConfig(string configJson, string clientSecretFilePath, List<string> prefilledUrls, List<string> formIDs)
        {
            if (prefilledUrls.Count != formIDs.Count)
                throw new Exception("The number of prefilled urls (" + prefilledUrls.Count + ") must be the same as the number of forms (" + formIDs.Count + ")");

            List<string> invalidUrl = prefilledUrls.Where(x => !Uri.TryCreate(x, UriKind.RelativeOrAbsolute, out Uri? uri)).ToList();
            if (invalidUrl.Count > 0)
                throw new Exception("There are some invalid url: " + Environment.NewLine + string.Join(Environment.NewLine, invalidUrl));
            List<Uri> prefilledUris = prefilledUrls.Select(x => new Uri(x)).ToList();

            Config gooseConfig = new() { ClientSecretFilePath = clientSecretFilePath };

            for (int i = 0; i < prefilledUris.Count; i++)
            {
                Uri uri = prefilledUris[i];
                Table table = new()
                {
                    PrefilledFormID = uri.Segments[4],
                    FormID = formIDs[i]
                };

                FormsService formsService = new(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = Goose.DBMS.Credential(clientSecretFilePath, new[] { FormsService.Scope.FormsBody, }),
                    ApplicationName = Assembly.GetExecutingAssembly().GetName().Name,
                });

                Form form = formsService.Forms.Get(table.FormID).Execute();
                table.Name = form.Info.Title.Underscore();

                NameValueCollection nvc = HttpUtility.ParseQueryString(uri.Query);
                nvc.Remove("usp");

                IList<Item> filteredByProperties = form.Items.Where(x => x.PageBreakItem == null && x.QuestionItem != null).ToList();
                if (nvc.Count != filteredByProperties.Count)
                    throw new Exception(form.Info.Title + ") The number of arguments in query string (" + nvc.Count + ") must be the same as the number of filtered items in form (" + filteredByProperties.Count + ")");

                for (int j = 0; j < filteredByProperties.Count; j++)
                {
                    string? key = nvc.Keys[j];
                    string? value = nvc[key];
                    if (key == null)
                        throw new Exception("Key is null in the array of prefilled arguments");
                    else if (value == null)
                        throw new Exception("Value is null in the array of prefilled arguments");
                    else
                        table.Columns.Add(new(int.Parse(key.Split('.')[1]), value.Underscore(), filteredByProperties[j].QuestionItem.Question.QuestionId, string.Empty));
                }

                gooseConfig.Tables.Add(table);
            }
            
            File.WriteAllText(configJson, JsonConvert.SerializeObject(gooseConfig, Formatting.Indented));
        }
    }
}