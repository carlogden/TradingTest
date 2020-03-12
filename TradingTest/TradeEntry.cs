using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingTest
{
    public class TradeEntry
    {
        public DateTime TradeTime;
        public string Symbol;
        public decimal OpenPrice;
        public decimal ClosePrice;
        public decimal SMAAtOpen;
        public decimal SMAAtClose;
        public decimal NetProfit => ClosePrice - OpenPrice;
        
        public TradeEntry(string symbol)
        {
            Symbol = symbol;
            TradeTime = new DateTime();
        }
    }
}
