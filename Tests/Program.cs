using Goose;
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
                // new List<string>() { "https://docs.google.com/forms/d/e/1FAIpQLSfCC90aPArGoc5SJFU4Dg5qV4CRFKfjUZWJc-urOdY3ehuxQA/viewform?usp=pp_url&entry.637106372=a&entry.680296813=b&entry.613902835=a@c.co&entry.1882116277=12&entry.404487981=2023-12-06&entry.1661359581=M&entry.33189120=BR&entry.1590617374=rg&entry.465386359=werg&entry.639005055=GTTRCR96P03G489M&entry.2120200289=nic type&entry.731372942=6u&entry.1754005134=2023-12-19&entry.568109288=1e0cd4c2-7c75-4608-a42d-9594c7ba324e" },
                // new List<string>() { "1R0ZbDeTyxbIU54pZKKszFaKLa3v5b5mvjd-g5W9rrXI" });

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