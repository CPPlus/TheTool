using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XLSXTools;

namespace TheTool
{
    public class XLSXDiscrepancyOutputter : DiscrepancyOutputter
    {
        private const int ID_COLUMN_INDEX = 0;

        private XLSXWriter writer;
        private SortedDictionary<string, int> columnDiscrepancyCounts = new SortedDictionary<string, int>();
        private SortedDictionary<string, int> columnRoundingDiscrepancyCounts = new SortedDictionary<string, int>();

        private int missingRowsInFirstFileCount = 0;
        private int missingRowsInSecondFileCount = 0;
        private int rowsWithDiscrepanciesCount = 0;
        private int rowsWithRoundingCount = 0;

        public XLSXDiscrepancyOutputter(string discrepancyDestFilePath)
        {
            writer = new XLSXWriter(discrepancyDestFilePath);
        }

        public void Cleanup()
        {
            writer.Finish();
            writer.Close();
        }

        public void Header(string[] header)
        {
            writer.SetWorksheet("Discrepancies");
            WriteHeader(header, true);

            writer.SetWorksheet("Missing Records");
            WriteHeader(header, false);
        }

        public void MissingRecord(string[] record, int missingRecordIndex)
        {
            writer.SetWorksheet("Missing Records");

            string fileLabel;
            if (missingRecordIndex == 0)
            {
                fileLabel = "MASTER";
                missingRowsInFirstFileCount++;
            } else
            {
                fileLabel = "VALIDATED";
                missingRowsInSecondFileCount++;
            }
            
            for (int i = 0; i < record.Length; i++)
            {
                if (i == ID_COLUMN_INDEX) continue;
                writer.WriteInline(record[i], Styles.RED);
            }
            writer.WriteInline(record[ID_COLUMN_INDEX], Styles.RED);
            writer.WriteInline(string.Format("MISSING IN {0} FILE", fileLabel), Styles.RED);
            writer.NewRow();
        }

        public void Record(string[] comparedRecord, DiscrepancyType type)
        {
            writer.SetWorksheet("Discrepancies");

            if (type == DiscrepancyType.DISCREPANCY)
                rowsWithDiscrepanciesCount++;
            if (type == DiscrepancyType.ROUNDING)
                rowsWithRoundingCount++;
            
            for (int i = 0; i < comparedRecord.Length; i++)
            {
                if (i == ID_COLUMN_INDEX) continue;

                if (comparedRecord[i].IndexOf("|") >= 0 && comparedRecord[i].IndexOf("||") < 0)
                {
                    writer.WriteInline(comparedRecord[i], Styles.YELLOW);
                    NoteColumnDiscrepancy(i);
                } else if (comparedRecord[i].IndexOf("||") > 0)
                {
                    writer.WriteInline(comparedRecord[i].Replace("||", "|"), Styles.GRAY);
                    NoteColumnRounding(i);
                }
                else
                    writer.WriteInline(comparedRecord[i]);
            }

            string id = comparedRecord[ID_COLUMN_INDEX];
            if (id.IndexOf("|") >= 0)
                writer.WriteInline(id, Styles.GRAY);
            else
                writer.WriteInline(id);

            if (type == DiscrepancyType.DISCREPANCY) writer.WriteInline("DISCREPANCY", Styles.YELLOW);
            else if (type == DiscrepancyType.ROUNDING) writer.WriteInline("ROUNDING", Styles.GRAY);
            else writer.WriteInline("OK", Styles.GREEN);

            writer.NewRow();
        }

        public void WriteStatistics(TableData[] datas)
        {
            writer.SetWorksheet("Statistics");

            // General.
            writer.WriteInline("General information", Styles.BLUE);
            writer.NewRow();

            writer.WriteInline("Records with discrepancies");
            writer.Write(rowsWithDiscrepanciesCount);
            writer.NewRow();

            writer.WriteInline("Records with ONLY rounding needed");
            writer.Write(rowsWithRoundingCount);
            writer.NewRow();

            writer.WriteInline("Discrepancy values count");
            writer.Write(GetDiscrepancyCount());
            writer.NewRow();

            writer.WriteInline("Rounding values count");
            writer.Write(GetRoundingErrorCount());
            writer.NewRow();

            writer.NewRow();

            writer.WriteInline("Information by files", Styles.BLUE);
            writer.NewRow();

            // Master
            writer.WriteInline("Master file", Styles.GREEN);
            writer.NewRow();
            WritePerFileDetails(datas[0].recordCount, datas[0].columnCount, missingRowsInFirstFileCount);

            // Validated
            writer.WriteInline("Validated file", Styles.GREEN);
            writer.NewRow();
            WritePerFileDetails(datas[1].recordCount, datas[1].columnCount, missingRowsInSecondFileCount);

            writer.NewRow();

            // Discrepancy counts by column.
            writer.WriteInline("Discrepancy counts by column", Styles.BLUE);
            OutputByColumnData(columnDiscrepancyCounts);

            writer.NewRow();

            writer.WriteInline("Rounding count by column", Styles.BLUE);
            OutputByColumnData(columnRoundingDiscrepancyCounts);
        }

        private void OutputByColumnData(SortedDictionary<string, int> data)
        {
            writer.NewRow();
            foreach (KeyValuePair<string, int> pair in data)
            {
                writer.WriteInline(pair.Key);
                writer.Write(pair.Value);
                writer.NewRow();
            }
        }

        private void WriteHeader(string[] header, bool tip)
        {
            foreach (string field in header)
            {
                writer.WriteInline(field, Styles.BLUE);
            }
            writer.WriteInline("ROWS" + (tip ? " (M|V)" : string.Empty), Styles.BLUE);
            writer.WriteInline("DISCREPANCY TYPE", Styles.BLUE);
            writer.NewRow();
        }

        private void WritePerFileDetails(int rowCount, int columnCount, int missingRecordCount)
        {
            writer.WriteInline("Record count");
            writer.Write(rowCount);
            writer.NewRow();

            writer.WriteInline("Column count");
            writer.Write(columnCount);
            writer.NewRow();

            writer.WriteInline("Records missing");
            writer.Write(missingRecordCount);
            writer.NewRow();
        }

        private void NoteColumnDiscrepancy(int columnIndex)
        {
            string columnInOriginal = XLSXUtils.ColumnIndexToLetter(columnIndex);
            string column = string.Format("{0}", columnInOriginal);
            if (columnDiscrepancyCounts.ContainsKey(column))
            {
                columnDiscrepancyCounts[column] += 1;
            } else
            {
                columnDiscrepancyCounts[column] = 1;
            }
        }

        private void NoteColumnRounding(int columnIndex)
        {
            string columnInOriginal = XLSXUtils.ColumnIndexToLetter(columnIndex);
            string column = string.Format("{0}", columnInOriginal);
            if (columnRoundingDiscrepancyCounts.ContainsKey(column))
            {
                columnRoundingDiscrepancyCounts[column] += 1;
            }
            else
            {
                columnRoundingDiscrepancyCounts[column] = 1;
            }
        }

        public int GetDiscrepancyCount()
        {
            int result = 0;
            foreach (KeyValuePair<string, int> pair in columnDiscrepancyCounts)
            {
                result += pair.Value;
            }
            return result;
        }

        public int GetRoundingErrorCount()
        {
            int result = 0;
            foreach (KeyValuePair<string, int> pair in columnRoundingDiscrepancyCounts)
            {
                result += pair.Value;
            }
            return result;
        }
    }
}
