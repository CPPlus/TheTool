using DBTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XLSXTools;

namespace TheTool
{
    public class AdditionalSQLHashRecordMatcher
    {
        private DB db;
        private string[] srcTables;
        private ComparisonData data;
        private int[] keyIndices;

        public AdditionalSQLHashRecordMatcher(string[] srcTables, ComparisonData data, DB db)
        {
            this.srcTables = srcTables;
            this.db = db;
            this.data = data;

            TurnKeyColumnsToIndices(data.additionalKeyColumns);
        }

        public string[][] Match(TableData[] datas, MatchedRecordComparer comparer, DiscrepancyOutputter outputter)
        {
            List<string[]> result = new List<string[]>();

            // Extract nonmatching records.
            Dictionary<string, List<string[]>> firstRecords;
            Dictionary<string, List<string[]>> secondRecords;
            ExtractRecords(out firstRecords, out secondRecords);

            if (firstRecords.Count != 0 || secondRecords.Count != 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Warning: Not enough key columns. Experimental algorithm is turned on.");

                Console.WriteLine("First records: ");
                foreach (KeyValuePair<string, List<string[]>> pair in firstRecords) 
                {
                    Console.WriteLine("Hash:");
                    foreach (string[] record in pair.Value)
                    {
                        for (int i = 0; i < record.Length; i++)
                        {
                            Console.Write(record[i] + ", ");
                        }
                        Console.WriteLine();
                    }
                }

                Console.WriteLine("Second records: ");
                foreach (KeyValuePair<string, List<string[]>> pair in secondRecords)
                {
                    Console.WriteLine("Hash:");
                    foreach (string[] record in pair.Value)
                    {
                        for (int i = 0; i < record.Length; i++)
                        {
                            Console.Write(record[i] + ", ");
                        }
                        Console.WriteLine();
                    }
                }

                Console.ResetColor();
            }
            
            foreach (KeyValuePair<string, List<string[]>> pairOne in firstRecords)
            {
                foreach (KeyValuePair<string, List<string[]>> pairTwo in secondRecords)
                {
                    // Comparing records with same hash at the moment.
                    if (pairOne.Key.Equals(pairTwo.Key))
                    {
                        MatchRecords(pairOne.Value.ToArray(), pairTwo.Value.ToArray(), comparer, outputter);
                    }
                }
            }

            return result.ToArray();
        }

        private void MatchRecords(string[][] recordSetOne, string[][] recordSetTwo, MatchedRecordComparer comparer, DiscrepancyOutputter outputter)
        {
            HashSet<string> checkedIndices = new HashSet<string>();
            for (int i = 0; i < recordSetOne.Length; i++)
            {
                string[] recordOne = recordSetOne[i];

                string[] bestMatch = null;
                double smallestDelta = double.MaxValue;
                for (int j = 0; j < recordSetTwo.Length; j++)
                {
                    string[] recordTwo = recordSetTwo[j];
                    if (checkedIndices.Contains(recordTwo[1])) continue;

                    double delta = CalculateGreatestDelta(recordOne, recordTwo);
                    if (delta < smallestDelta)
                    {
                        bestMatch = recordTwo;
                        smallestDelta = delta;
                    }
                }
                

                if (bestMatch != null)
                {
                    checkedIndices.Add(bestMatch[1]);

                    string[] recordOneNoHash = RemoveHashFromRecord(recordOne);
                    string[] recordTwoNoHash = RemoveHashFromRecord(bestMatch);

                    int recordColumnCount = Math.Min(recordOneNoHash.Length, recordTwoNoHash.Length);

                    string[] newOne = new string[recordColumnCount];
                    string[] newTwo = new string[recordColumnCount];

                    Array.Copy(recordOneNoHash, newOne, recordColumnCount);
                    Array.Copy(recordTwoNoHash, newTwo, recordColumnCount);

                    comparer.Compare(newOne, newTwo);
                }
            }

            // Print missing.
            for (int i = 0; i < recordSetTwo.Length; i++)
            {
                if (!checkedIndices.Contains(recordSetTwo[i][1]))
                {
                    outputter.MissingRecord(RemoveHashFromRecord(recordSetTwo[i]), 0);
                }
            }

            checkedIndices.Clear();
        }

        private string[] RemoveHashFromRecord(string[] record)
        {
            string[] result = new string[record.Length - 1];
            Array.Copy(record, 1, result, 0, result.Length);
            return result;
        }

        private double CalculateGreatestDelta(string[] recordOne, string[] recordTwo)
        {
            double result = 0;
            for (int i = 0; i < Math.Min(recordOne.Length, recordTwo.Length); i++)
            {
                if (IsKeyIndex(i))
                {
                    double numberOne;
                    bool successOne = double.TryParse(recordOne[i], out numberOne);

                    double numberTwo;
                    bool successTwo = double.TryParse(recordTwo[i], out numberTwo);

                    if (successOne && successTwo)
                    {
                        result += Math.Abs(numberOne - numberTwo);
                    }
                }
            }
            return result;
        }

        private bool IsKeyIndex(int index)
        {
            foreach (int keyIndex in keyIndices)
                if (index - 1 == keyIndex)
                    return true;

            return false;
        }

        private void TurnKeyColumnsToIndices(string[] keyColumns)
        {
            keyIndices = new int[keyColumns.Length];
            for (int i = 0; i < keyColumns.Length; i++)
            {
                keyIndices[i] = XLSXUtils.LetterIndexToInt(keyColumns[i]);
            }
        }

        private void ExtractRecords(out Dictionary<string, List<string[]>> firstRecords, out Dictionary<string, List<string[]>> secondRecords)
        {
            Recordset firstNonMatched = db.QueryResult(GetTable1NotUniqueQuery());
            firstRecords = GroupByHash(firstNonMatched);
            firstNonMatched.Close();
            
            Recordset secondNonMatched = db.QueryResult(GetTable2NotUniqueQuery());
            secondRecords = GroupByHash(secondNonMatched);
            firstNonMatched.Close();
        }

        private Dictionary<string, List<string[]>> GroupByHash(Recordset recordset)
        {
            Dictionary<string, List<string[]>> result = new Dictionary<string, List<string[]>>();

            while (recordset.Read())
            {
                // Read record.
                string[] record = new string[recordset.GetFieldCount()];
                for (int i = 0; i < record.Length; i++)
                {
                    record[i] = recordset.GetString(i);
                }

                // Group.
                if (!result.ContainsKey(record[0]))
                {
                    List<string[]> list = new List<string[]>();
                    list.Add(record);
                    result.Add(record[0], list);
                }
                else
                {
                    result[record[0]].Add(record);
                }
            }

            return result;
        }

        private string GetTable1NotUniqueQuery()
        {
            return @"
                SELECT * FROM #table1NotUnique
                UNION
                SELECT * FROM #table1Unique WHERE F1 IN
                (SELECT F1 FROM #table2NotUnique GROUP BY F1 HAVING COUNT(*) > 1)";
        }

        private string GetTable2NotUniqueQuery()
        {
            return @"
                SELECT * FROM #table2NotUnique
                UNION
                SELECT * FROM #table2Unique WHERE F1 IN
                (SELECT F1 FROM #table1NotUnique GROUP BY F1 HAVING COUNT(*) > 1)";
        }
    }
}
