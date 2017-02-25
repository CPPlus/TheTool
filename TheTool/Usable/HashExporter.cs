using DocumentFormat.OpenXml.Spreadsheet;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using XLSXTools;

namespace TheTool
{
    public class HashExporter
    {
        private const int ADDITIONAL_COLUMNS_COUNT = 2;

        private RecordReader reader;
        private RecordWriter writer;
        private ComparisonData data;

        private int[] keyIndices;
        private MD5 md5;
        private int id = 1;
        private int idOffset = 0;

        public HashExporter(RecordReader reader, RecordWriter writer, ComparisonData data)
        {
            this.reader = reader;
            this.data = data;
            TurnKeyColumnsToIndices(data.keyColumns);

            md5 = MD5.Create();
            this.writer = writer;
        }

        public void SetIdOffset(int offset)
        {
            idOffset = offset;
        }

        public void Extract(out TableData data)
        {
            data = new TableData();
            string[] record;

            // Skip records.
            for (int i = 0; i < idOffset - 1; i++)
                reader.ReadNextRecord(out record);
            
            int recordCounter = 0;
            while (reader.ReadNextRecord(out record))
            {
                // Skip header and record column count.
                if (recordCounter == 0)
                {
                    data.header = record;
                    recordCounter++;

                    int columnCount = record.Length;
                    writer.CreateFields(columnCount + ADDITIONAL_COLUMNS_COUNT);
                    data.columnCount = columnCount;

                    continue;
                }

                string[] hashedRecord = BuildHashedRecord(record);
                writer.Write(hashedRecord.ToArray());
                recordCounter++;
                if (recordCounter % 1000 == 0)
                    Console.WriteLine("Extracted {0} rows.", recordCounter);
            }
            reader.Close();

            data.recordCount = recordCounter;
        }

        public void Cleanup()
        {
            md5.Clear();
            writer.Cleanup();
        }

        private string[] BuildHashedRecord(string[] record)
        {
            List<string> hashedRecord = new List<string>();
            
            // Compute hash.
            string hash = ComputeHash(record);
            hashedRecord.Add(hash);

            // Add id.
            hashedRecord.Add(((id++) + idOffset).ToString());

            // Write the record.
            foreach (string field in record)
            {
                string newField = field;

                decimal number;
                if (decimal.TryParse(field, NumberStyles.Float, CultureInfo.InvariantCulture, out number))
                {
                    newField = decimal.Round(number, 15).ToString("0.###############");
                }
                    
                hashedRecord.Add(newField);
            }

            return hashedRecord.ToArray();
        }

        private string ComputeHash(string[] record)
        {
            // Concatenate key fields.
            StringBuilder keyFields = new StringBuilder();
            for (int i = 0; i < record.Length; i++)
            {
                int columnIndex = i + 1;
                if (IsKeyIndex(columnIndex))
                {
                    string value = record[i];
                    value = FilterValue(value);
                    value = FilterCharacters(value);
                    // value = value.ToLower();
                    keyFields.Append(value);
                }  
            }
            
            // Compute hash.
            byte[] inputBytes = Encoding.Unicode.GetBytes(keyFields.ToString());
            byte[] hashBytes = md5.ComputeHash(inputBytes);
            StringBuilder hash = new StringBuilder();
            for (int i = 0; i < hashBytes.Length; i++)
            {
                hash.Append(hashBytes[i].ToString("X2"));
            }
            return hash.ToString();
        }

        private string FilterCharacters(string value)
        {
            foreach (string substr in data.substringsToNeglect)
            {
                value = value.Replace(substr, string.Empty);
            }
            return value;
        }

        private string FilterValue(string value)
        {
            foreach (string neglectedValue in data.valuesToNeglect)
            {
                if (value.Equals(neglectedValue))
                    return string.Empty;
            }
            return value;
        }

        private bool IsKeyIndex(int index)
        {
            foreach (int keyIndex in keyIndices)
                if (index == keyIndex)
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
    }
}
