using Goose;
using Goose.Type.DBMS;

namespace Test
{
    class Program
    {
        public static void Main()
        {
            try
            {
                // Config.CreateConfig("goose_config.json",
                // "client_secret.json",
                // new List<string>() { "https://docs.google.com/forms/d/e/2948jgkjrhXDtH7653563he56453hH4hse4SHsh40l9k3fD/viewform?usp=pp_url&entry.5497120025=name&entry.223105871=surname" },
                // new List<string>() { "298h4gGdsfgGr36u367h5h52hBHsdh55hy4hqhs4sr-hHRH56jRh" });

                Owner gooseOwner = new("goose_config_new.json");
                GooseTable? gooseTable = gooseOwner.Select("select count(*) as c from GooseTable1 where name = 'io'");

                gooseOwner.DataReceivedCallback = (gooseDB, previousGooseDB, differenceGooseDB) =>
                {
                    Console.WriteLine(differenceGooseDB);
                };

                gooseOwner.Insert("GooseTable1", new List<string>() { "ciccio", "pippo" });
                gooseOwner.Insert("GooseTable1", new Dictionary<string, string> { { "name", "io" }, { "surname", " " } });
                gooseOwner.Insert("GooseTable1", new Dictionary<string, string> { { "name", "io" } });

                User user = new("goose_config_new.json");
                user.Insert("GooseTable1", new List<string>() { "noooome", "cognomeee" });

                DBMS.Block();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}