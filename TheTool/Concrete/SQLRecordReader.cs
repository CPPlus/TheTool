using DBTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Spreadsheet;

namespace TheTool
{
    class SQLRecordReader : RecordReader
    {
        private DB db;
        private Recordset recordset;
        private bool hasReadHeader = false;

        public SQLRecordReader(string connectionString, string query)
        {
            db = new SqlServerDB(connectionString);
            db.Connect();
            recordset = db.QueryResult(query);
        }

        public bool ReadNextRecord(out string[] record)
        {
            record = new string[recordset.GetFieldCount()];
            if (hasReadHeader)
            {
                bool hasRead = recordset.Read();
                if (hasRead)
                {
                    for (int i = 0; i < record.Length; i++)
                    {
                        record[i] = recordset.GetString(i);
                    }
                }

                return hasRead;
            } else
            {
                for (int i = 0; i < record.Length; i++)
                {
                    record[i] = recordset.GetFieldName(i);
                }
                hasReadHeader = true;
                return true;
            }
        }

        public void Close()
        {
            recordset.Close();
            db.Close();
        }
    }
}
