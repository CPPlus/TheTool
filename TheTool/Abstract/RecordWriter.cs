using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheTool
{
    public interface RecordWriter
    {
        void CreateFields(int fieldCount);
        void Write(string[] record);
        void Cleanup();
    }
}
