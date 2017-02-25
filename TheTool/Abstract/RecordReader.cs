using DocumentFormat.OpenXml.Spreadsheet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheTool
{
    public interface RecordReader
    {
        bool ReadNextRecord(out string[] record);
        void Close();
    }
}
