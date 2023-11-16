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