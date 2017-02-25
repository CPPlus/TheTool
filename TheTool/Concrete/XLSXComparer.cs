using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheTool
{
    public class XLSXComparer : Comparer
    {
        private string[] filePaths;
        private string[] sheetNames;
        private ComparisonData data;

        public XLSXComparer(string[] filePaths, string[] sheetNames, string connectionString, ComparisonData data, RoundingSettings settings) : base(connectionString, data, settings)
        {
            this.filePaths = filePaths;
            this.sheetNames = sheetNames;
            this.data = data;
        }

        protected override RecordReader[] InstantiateReaders()
        {
            XLSXRecordReader[] readers = new XLSXRecordReader[2];

            for (int i = 0; i < readers.Length; i++)
                readers[i] = new XLSXRecordReader(filePaths[i], sheetNames[i], data.manualCalculation, data.rowIgnores);

            return readers;
        }
    }
}
