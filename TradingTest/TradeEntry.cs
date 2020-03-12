using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingTest
{
    public class TradeEntry
    {
        public string Symbol;
        public DateTime OpenTime;        
        public decimal OpenPrice;
        public decimal SMAAtOpen;

        public DateTime CloseTime;
        public decimal ClosePrice;        
        public decimal SMAAtClose;
        public decimal NetProfit => ClosePrice - OpenPrice;
        
        public TradeEntry(string symbol)
        {
            Symbol = symbol;
            OpenTime = DateTime.Now;
        }
    }
}
