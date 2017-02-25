using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheTool
{
    public interface DiscrepancyOutputter
    {
        void Header(string[] header);
        void Record(string[] comparedRecord, DiscrepancyType type);
        void MissingRecord(string[] matchedRecord, int recordIndex);
        void WriteStatistics(TableData[] datas);
        int GetDiscrepancyCount();
        int GetRoundingErrorCount();
        void Cleanup();
    }
}
