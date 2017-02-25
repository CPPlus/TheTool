using DBTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheTool
{
    public class SQLHashRecordMatcher
    {
        private const int HASH_FIELDS_COUNT = 2;
        private const int RECORDS_IN_MATCHED_RECORD = 2;
        private const int HASH_FIELD_INDEX = 0;

        private DB db;
        private string[] srcTables;

        private ComparisonData data;
        private RoundingSettings settings;

        public SQLHashRecordMatcher(string[] srcTables, string connectionString, ComparisonData data, RoundingSettings settings)
        {
            this.srcTables = srcTables;
            this.data = data;
            this.settings = settings;
            
            db = new SqlServerDB(connectionString);
            db.Connect();

            db.Query(@"
                IF OBJECT_ID('dbo.table1', 'U') IS NOT NULL 
                DROP TABLE dbo.table1; 

                IF OBJECT_ID('dbo.table2', 'U') IS NOT NULL 
                DROP TABLE dbo.table2; 
            ");
        }

        public DiscrepancyType Match(TableData[] datas, DiscrepancyOutputter outputter)
        {
            PrepareTables();

            DiscrepancyType result = DiscrepancyType.NONE;
            DiscrepancyType type;

            type = InitialMatch(datas, outputter);
            if (type > result)
                result = type;
            
            type = AdditionalMatch(datas, outputter);
            if (type > result)
                result = type;

            return result;
        }

        private DiscrepancyType InitialMatch(TableData[] datas, DiscrepancyOutputter outputter)
        {
            string query = BuildOnlyMatchableRowsQuiery();
            MatchedRecordComparer comparer = new MatchedRecordComparer(data, settings, outputter);
            Recordset matches = db.QueryResult(query);
            DiscrepancyType result = DiscrepancyType.NONE;
            while (matches.Read())
            {
                string[] matchedRecord = new string[matches.GetFieldCount() - HASH_FIELDS_COUNT];
                int nextIndexToWrite = 0;
                for (int i = 0; i < matches.GetFieldCount(); i++)
                {
                    // Skip hash fields.
                    if (i == HASH_FIELD_INDEX || i == datas[0].columnCount + 2)
                        continue;

                    matchedRecord[nextIndexToWrite++] = matches.GetString(i);
                }

                string[] first, second;
                ExtractRecords(matchedRecord, datas[0].columnCount + 1, datas[1].columnCount + 1, out first, out second);
                DiscrepancyType type = comparer.Compare(first, second);

                if (type > result)
                    result = type;
            }
            matches.Close();

            return result;
        }

        private DiscrepancyType AdditionalMatch(TableData[] datas, DiscrepancyOutputter outputter)
        {
            DiscrepancyType result = DiscrepancyType.NONE;

            MatchedRecordComparer comparer = new MatchedRecordComparer(data, settings, outputter);
            AdditionalSQLHashRecordMatcher matcher = new AdditionalSQLHashRecordMatcher(srcTables, data, db);
            matcher.Match(datas, comparer, outputter);

            return result;
        }

        private void ExtractRecords(string[] matchedRecord, int firstRecordColumnCount, int secondRecordColumnCount, out string[] first, out string[] second)
        {
            int recordColumnCount = Math.Min(firstRecordColumnCount, secondRecordColumnCount);
            first = ExtractSubstringArray(matchedRecord, 0, recordColumnCount);
            second = ExtractSubstringArray(matchedRecord, firstRecordColumnCount, firstRecordColumnCount + recordColumnCount);
        }

        private string[] ExtractSubstringArray(string[] initial, int start, int end)
        {
            List<string> result = new List<string>();
            for (int i = start; i < end; i++)
            {
                result.Add(initial[i]);
            }
            return result.ToArray();
        }

        public void Cleanup()
        {
            db.Close();
        }

        private string BuildOnlyMatchableRowsQuiery()
        {
            return string.Format(@"
                SELECT t1.*, t2.* 
                FROM ({0}) t1 
                FULL OUTER JOIN ({1}) t2 
                ON t1.F1 = t2.F1
                ORDER BY t2.F2",
                //"table1",
                //"table2");
                GetTable1UniquesQuery(),
                GetTable2UniquesQuery());
        }

        private string GetTable1UniquesQuery()
        {
            return string.Format(@"
                SELECT * FROM #table1Unique WHERE F1 NOT IN
                (SELECT F1 FROM #table2NotUnique GROUP BY F1 HAVING COUNT(*) > 0)");
        }

        private string GetTable2UniquesQuery()
        {
            return string.Format(@"
                SELECT * FROM #table2Unique WHERE F1 NOT IN
                (SELECT F1 FROM #table1NotUnique GROUP BY F1 HAVING COUNT(*) > 0)");
        }

        private void PrepareTables()
        {
            db.Query(@"
                if OBJECT_ID('tempdb..#table1Unique') IS NOT NULL DROP TABLE #table1Unique
                if OBJECT_ID('tempdb..#table1NotUnique') IS NOT NULL DROP TABLE #table1NotUnique
                if OBJECT_ID('tempdb..#table2Unique') IS NOT NULL DROP TABLE #table2Unique
                if OBJECT_ID('tempdb..#table2NotUnique') IS NOT NULL DROP TABLE #table2NotUnique

                -- Unique from table1
                SELECT * INTO #table1Unique FROM table1 WHERE F1 NOT IN
                (SELECT F1 FROM table1 GROUP BY F1 HAVING COUNT(*) > 1)

                -- Non-unique from table1
                SELECT * INTO #table1NotUnique FROM table1 WHERE F1 IN
                (SELECT F1 FROM table1 GROUP BY F1 HAVING COUNT(*) > 1)

                -- Unique from table2
                SELECT * INTO #table2Unique FROM table2 WHERE F1 NOT IN
                (SELECT F1 FROM table2 GROUP BY F1 HAVING COUNT(*) > 1)

                -- Non-unique from table2
                SELECT * INTO #table2NotUnique FROM table2 WHERE F1 IN
                (SELECT F1 FROM table2 GROUP BY F1 HAVING COUNT(*) > 1)");
        }
    }
}
