using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace TradingTest
{
    public class TradeLog
    {
        public List<TradeEntry> Trades;
        public DateTime LogDate;
        public string Symbol;
        public decimal NetProfit;
        public string BasePath="";

        public TradeLog()
        {
            Trades = new List<TradeEntry>();
            LogDate = DateTime.Today;
            NetProfit = 0.0m;
            BasePath = "";
        }

        public TradeEntry GetOpenTrade()
        {
            if (Trades.Count > 0)
            {
                var lastTrade = Trades.Last();
                if (lastTrade.ClosePrice == 0)
                {
                    return lastTrade;
                }
            }
            return null;
        }
        public static TradeLog LoadTradeLoad(string basePath, string symbol)
        {
            string path = basePath.EndsWith("\\") ? basePath : basePath + "\\";
            path += symbol + "-" + DateTime.Today.ToString("yyyyMMdd") + ".log";
            if (File.Exists(path))
            {
                var jsonString = File.ReadAllText(path);
                TradeLog tradeLog = JsonConvert.DeserializeObject<TradeLog>(jsonString);
                return tradeLog;
            }
            return null;
        }
        public void SaveTradeLog()
        {
            SaveTradeLog(BasePath);
        }
        public void SaveTradeLog(string basePath)
        {
            string json = JsonConvert.SerializeObject(this,Formatting.Indented);
            string path = basePath.EndsWith("\\") ? basePath : basePath + "\\";
            path += Symbol + "-" + LogDate.ToString("yyyyMMdd") + ".log";
            File.WriteAllText($"{path}",json);            
        }
        
    }
}
