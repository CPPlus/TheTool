using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheTool
{
    public class RoundingSettings
    {
        public bool smartRounding;
        public bool smartRoundingFormatting;
        public decimal delta;

        public RoundingSettings()
        {
            smartRounding = true;
            smartRoundingFormatting = true;
            delta = 0.01m;
        }
        
    }
}
