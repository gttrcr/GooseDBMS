using Google.Apis.Forms.v1;
using Goose.Type.Config;

namespace Goose.Type.DBMS
{
    public class GooseDB
    {
        public List<GooseTable> Tables { get; private set; }

        public GooseDB()
        {
            Tables = new();
        }

        public GooseDB(FormsResource formsResource, List<Table> tables)
        {
            Tables = new();
            tables.ForEach(x => Tables.Add(new(formsResource.Responses.List(x.FormID).Execute(), formsResource.Get(x.FormID).Execute(), x)));
        }

        public GooseDB(GooseDB gooseDB)
        {
            Tables = new(gooseDB.Tables);
        }

        public GooseDB? Compare(GooseDB gooseDB)
        {
            GooseDB? differenceGooseSchema = null;

            List<string?> addedFormID = Tables.Select(x => x.Table.FormID).Except(gooseDB.Tables.Select(x => x.Table.FormID)).ToList();
            if (addedFormID.Count > 0)
            {
                differenceGooseSchema ??= new();
                differenceGooseSchema.Tables.AddRange(Tables.Where(x => addedFormID.Contains(x.Table.FormID)));
            }

            List<string?> removedFormID = gooseDB.Tables.Select(x => x.Table.FormID).Except(Tables.Select(x => x.Table.FormID)).ToList();
            if (removedFormID.Count > 0)
            {
                differenceGooseSchema ??= new();
                differenceGooseSchema.Tables.AddRange(Tables.Where(x => removedFormID.Contains(x.Table.FormID)));
            }

            for (int i = 0; i < gooseDB.Tables.Count; i++)
            {
                GooseTable? differenceTable = Tables[i].Compare(gooseDB.Tables.First(x => x.Table.FormID.Equals(Tables[i].Table.FormID)));
                if (differenceTable != null)
                {
                    differenceGooseSchema ??= new();
                    differenceGooseSchema.Tables.Add(differenceTable);
                }
            }

            return differenceGooseSchema;
        }

        public override string ToString()
        {
            return string.Join(Environment.NewLine, Tables);
        }
    }
}