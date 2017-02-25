using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheTool
{
    public abstract class Comparer
    {
        private const string TABLE_NAME_ONE = "table1";
        private const string TABLE_NAME_TWO = "table2";

        private SQLHashRecordMatcher matcher;
        private HashExporter[] exporters = new HashExporter[2];
        private TableData[] tableDatas = new TableData[2];
        protected RoundingSettings roundingSettings;
        protected ComparisonData comparisonData;
        private string connectionString;

        public Comparer(string connectionString, ComparisonData comparisonData, RoundingSettings roundingSettings)
        {
            this.connectionString = connectionString;
            this.comparisonData = comparisonData;
            this.roundingSettings = roundingSettings;

            matcher = new SQLHashRecordMatcher(
                new string[] {
                    TABLE_NAME_ONE,
                    TABLE_NAME_TWO
                },
                connectionString,
                comparisonData,
                roundingSettings);
        }

        public DiscrepancyType Compare(DiscrepancyOutputter outputter, out ComparisonResult result)
        {
            DiscrepancyType type;

            Extract();

            outputter.Header(tableDatas[0].header);
            type = matcher.Match(tableDatas, outputter);
            outputter.WriteStatistics(tableDatas);

            BuildComparisonResult(outputter, out result);
            
            return type;
        }

        private void BuildComparisonResult(DiscrepancyOutputter outputter, out ComparisonResult result)
        {
            result = new ComparisonResult();

            result.discrepancyCount = outputter.GetDiscrepancyCount();
            result.roundingErrorCount = outputter.GetRoundingErrorCount();

            // Create comment.
            StringBuilder commentBuilder = new StringBuilder();
            if (tableDatas[0].recordCount != tableDatas[1].recordCount || tableDatas[0].columnCount != tableDatas[1].columnCount)
            {
                commentBuilder.Append("Different");
                if (tableDatas[0].recordCount != tableDatas[1].recordCount)
                {
                    commentBuilder.Append(" row");
                    if (tableDatas[0].columnCount != tableDatas[1].columnCount)
                        commentBuilder.Append(" and");
                }
                if (tableDatas[0].columnCount != tableDatas[1].columnCount)
                    commentBuilder.Append(" column");
                commentBuilder.Append(" counts.");
            }
            result.comment = commentBuilder.ToString();
        }

        public void Cleanup()
        {
            matcher.Cleanup();

            foreach (HashExporter exporter in exporters)
                exporter.Cleanup();
        }

        protected abstract RecordReader[] InstantiateReaders();
        private void Extract()
        {
            RecordReader[] readers = InstantiateReaders();
            RecordWriter[] writers = new RecordWriter[]
            {
                new SQLRecordWriter(TABLE_NAME_ONE, connectionString),
                new SQLRecordWriter(TABLE_NAME_TWO, connectionString)
            };
            for (int i = 0; i < tableDatas.Length; i++)
            {
                exporters[i] = new HashExporter(
                    readers[i],
                    writers[i],
                    comparisonData);
                exporters[i].SetIdOffset(comparisonData.rowIgnores);
                exporters[i].Extract(out tableDatas[i]);
            }
        }
    }
}
