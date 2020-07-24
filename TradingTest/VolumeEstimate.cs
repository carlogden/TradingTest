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
        public string VolumeFormated => Stock.FormatVolume(Volume);
        public double VolumePercent { get; set; }

        public string VolumePercentFormated => Stock.FormatPercent(VolumePercent);
        public double Confidence { get; set; }
        public string ConfidenceFormated => Stock.FormatPercent(Confidence);

        public string Classification => VolumeEstimate.ClassifyVolumenPercent(VolumePercent);

        public static string ClassifyVolumenPercent(double percent)
        {
            if (percent <= 0) return "No Data";

            if (percent < 0.6) return "Dryup";

            if (percent < 0.8) return "Light";

            if (percent < 1.2) return "Average";

            if (percent < 2) return "High";

            return "Extreme";
        }
    }
}
