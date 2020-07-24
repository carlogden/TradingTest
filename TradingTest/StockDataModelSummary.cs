using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingTest
{
    public class StockDataModelSummary
    {
        public DateTime LastUpdated;
        public int MinutesOpen;
        public bool MarketOpen;
        public double EstimateConfidence;
        public string EstimateConfidenceFormated;
    }
}
