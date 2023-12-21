using Goose.Type.Config;
using Newtonsoft.Json;

namespace Goose
{
    public class User : DBMS
    {
        public User(string configJson) : base(configJson)
        {
            Config? obj = JsonConvert.DeserializeObject<Config>(File.ReadAllText(configJson)) ?? throw new Exception("The configuration file " + configJson + " is null");
            if (obj.ClientSecretFilePath != null)
                throw new Exception("User cannot access to ClientSecretFilePath");

            DBConfig = obj;
            FormsService = new();
            GooseDB = null;
        }
    }
}