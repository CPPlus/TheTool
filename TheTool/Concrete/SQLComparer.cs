using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheTool
{
    public class SQLComparer : Comparer
    {
        private string connStr1;
        private string connStr2;
        private string query1;
        private string query2;

        public SQLComparer(
            string connectionString, 
            string connStr1,
            string query1, 
            string connStr2, 
            string query2,
            ComparisonData data,
            RoundingSettings settings) : base(connectionString, data, settings)
        {
            this.connStr1 = connStr1;
            this.query1 = query1;
            this.connStr2 = connStr2;
            this.query2 = query2;
        }

        protected override RecordReader[] InstantiateReaders()
        {
            SQLRecordReader[] readers = new SQLRecordReader[]
            {
                new SQLRecordReader(connStr1, query1),
                new SQLRecordReader(connStr2, query2)
            };
            return readers;
        }
    }
}
