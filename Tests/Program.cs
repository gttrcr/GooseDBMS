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
                // Config.CreateConfig("goose_new.json",
                // "client_secret.json",
                // new List<string>() { "https://docs.google.com/forms/d/e/1FAIpQLSfCC90aPArGoc5SJFU4Dg5qV4CRFKfjUZWJc-urOdY3ehuxQA/viewform?usp=pp_url&entry.637106372=nome&entry.680296813=cognome&entry.613902835=indirizzo@mail.com&entry.1882116277=123&entry.404487981=2023-12-03&entry.1661359581=M&entry.33189120=AV&entry.1590617374=citt%C3%A0dires&entry.465386359=civico3&entry.639005055=GTTRCR96P03G488M&entry.2120200289=Carta+di+identit%C3%A0&entry.731372942=numero&entry.1754005134=2023-12-26" },
                // new List<string>() { "1R0ZbDeTyxbIU54pZKKszFaKLa3v5b5mvjd-g5W9rrXI" });

                Owner gooseOwner = new("goose.json");
                GooseTable? gooseTable = gooseOwner.Select("select count(*) as c from Informazioni_di_contatto_FreedhOMe where name = 'io'");

                gooseOwner.DataReceivedCallback = (gooseDB, previousGooseDB, differenceGooseDB) =>
                {
                    Console.WriteLine(differenceGooseDB);
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