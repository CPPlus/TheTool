using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheTool
{
    public class ComparisonData
    {
        public string[] keyColumns;
        public string[] ignoredColumns;
        public string[] additionalKeyColumns;
        public int rowIgnores;
        public bool manualCalculation;

        public string[] valuesToNeglect;
        public string[] substringsToNeglect;

        public ComparisonData()
        {
            keyColumns = new string[] { };
            additionalKeyColumns = new string[] { };
            ignoredColumns = new string[] { };
            rowIgnores = 0;
            manualCalculation = false;
            valuesToNeglect = new string[] { };
            substringsToNeglect = new string[] { };
        }
    }
}
