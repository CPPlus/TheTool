using DBTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheTool
{
    public class SQLRecordWriter : RecordWriter
    {
        private const int ID_COLUMN_INDEX = 1;

        private string destTable;
        private DB db;
        private bool tableIsCreated = false;
        private int fieldCount;

        public SQLRecordWriter(string destTable, string connectionString)
        {
            this.destTable = destTable;
            
            db = new SqlServerDB(connectionString);
            db.Connect();
        }

        public void CreateFields(int fieldCount)
        {
            CreateTable(fieldCount);
            this.fieldCount = fieldCount;
            tableIsCreated = true;
        }

        public void Write(string[] record)
        {
            StringBuilder query = new StringBuilder();
            query.Append("INSERT INTO ");
            query.Append(destTable);
            query.Append(" VALUES (");

            for (int i = 0; i < record.Length; i++)
            {
                if (i != ID_COLUMN_INDEX)
                    query.Append("'");
                if (record[i].IndexOf("'") >= 0)
                {
                    record[i] = record[i].Replace("'", "''");
                }
                query.Append(record[i]);
                if (i != ID_COLUMN_INDEX)
                    query.Append("'");

                if (i < record.Length - 1)
                    query.Append(",");
            }
            query.Append(");");

            db.Query(query.ToString());
        }

        public void Cleanup()
        {
            if (tableIsCreated)
                DeleteTable();

            db.Close();
        }

        private void CreateTable(int fieldCount)
        {
            StringBuilder query = new StringBuilder();
            query.Append("CREATE TABLE ");
            query.Append(destTable);
            query.Append("(");
            query.Append("F1 varchar(512),F2 int"); // Create hash and id columns.
            for (int i = 2; i < fieldCount; i++)
            {
                query.Append(",");
                query.Append(string.Format("F{0} varchar(512)", i + 1));
            }
            query.Append(");");
            query.Append("CREATE INDEX i1 ON " + destTable + " (F1)");
            db.Query(query.ToString());
        }

        private void DeleteTable()
        {
            string query = string.Format("DROP TABLE {0}", destTable);
            db.Query(query);
        }
    }
}
