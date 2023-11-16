using Goose;
using Goose.Type.Config;
using Goose.Type.DBMS;

namespace Test
{
    class Program
    {
        public static void Main()
        {
            try
            {
                GooseConfig.CreateConfig("goose_config.json",
                "client_secret.json",
                new List<string>() { "https://docs.google.com/forms/d/e/2948jgkjrhXDtH7653563he56453hH4hse4SHsh40l9k3fD/viewform?usp=pp_url&entry.5497120025=name&entry.223105871=surname" },
                new List<string>() { "298h4gGdsfgGr36u367h5h52hBHsdh55hy4hqhs4sr-hHRH56jRh" });

                DBMS gooseDBMS = new("goose_config_new.json");
                GooseTable? gooseTable = gooseDBMS.Select("select count(*) as c from GooseTable1 where name = 'io'");

                gooseDBMS.DataReceivedCallback = (gooseDB, previousGooseDB, differenceGooseDB) =>
                {
                    Console.WriteLine(differenceGooseDB);
                };

                gooseDBMS.Insert("GooseTable1", new List<string>() { "ciccio", "pippo" });
                gooseDBMS.Insert("GooseTable1", new Dictionary<string, string> { { "name", "io" }, { "surname", " " } });
                gooseDBMS.Insert("GooseTable1", new Dictionary<string, string> { { "name", "io" } });

                DBMS.Block();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}