using Goose;
using Goose.Type.Config;
using Goose.Type.DBMS;
using Newtonsoft.Json;

namespace Test
{
    class Program
    {
        public static void Main()
        {
            try
            {
                // Config.CreateConfig("goose_test.json",
                // "client_secret.json",
                // new List<string>() { "https://docs.google.com/forms/d/e/1FAIpQLScgV-6ktrfkKUAuCtqJpnrRxuxO-AOEXPQtkTqUqJHTCKoexQ/viewform?usp=pp_url&entry.2005620554=1&entry.1444089516=2&entry.447085842=5&entry.1045781291=6@c.co&entry.1065046570=8&entry.1166974658=7&entry.1545673926=auth0%7C648c16a04a5f17311d565420&entry.839337160=comememememe" },
                // new List<string>() { "1EiKT4lVV3JNNSDlnRAIzPMfwBGP6u-HmCNC_bvUA7i4" });

                Owner gooseOwner = new("goose.json");
                GooseTable? gooseTable = Owner.Select("select count(*) as c from Informazioni_di_contatto_FreedhOMe where name = 'io'");

                gooseOwner.DataReceivedCallback = (gooseDB, previousGooseDB, differenceGooseDB) =>
                {
                    File.WriteAllText("json", JsonConvert.SerializeObject(differenceGooseDB));
                };

                // bool ins = gooseOwner.Insert("GooseTable1", new List<string>() { "ciccio", "pippo" });
                // ins = gooseOwner.Insert("GooseTable1", new Dictionary<string, string> { { "name", "io" }, { "surname", " " } });
                // ins = gooseOwner.Insert("GooseTable1", new Dictionary<string, string> { { "name", "io" } });

                // User user = new("goose_config_new.json");
                // user.Insert("GooseTable1", new List<string>() { "noooome", "cognomeee" });

                DBMS.Block();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}