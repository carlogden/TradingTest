using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingTest
{
    public class VolumeEstimate
    {
        public long Volume{ get; set; }
        public double VolumePercent { get; set; }
        public double Confidence { get; set; }
    }
}
