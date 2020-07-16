using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Alpaca.Markets;
using Newtonsoft.Json;

namespace TradingTest
{
    public class StockBar
    {
        public Decimal Open { get; protected set; }

        public Decimal Close { get; set; }

        public Decimal Low { get; set; }

        public Decimal High { get; set; }

        public Int64 Volume { get; set; }

        public Int64 Ticks { get; set; }

        public Int32 ItemsInWindow { get; set; }

        [JsonIgnore]
        public DateTime Time { get; set; }
        

        public static StockBar LoadFromIAgg(IAgg barAgg)
        {
            StockBar newBar = new StockBar
            {
                Open = barAgg.Open,
                Close = barAgg.Close,
                High = barAgg.High,
                Low = barAgg.Low,
                Volume = barAgg.Volume,
                ItemsInWindow = barAgg.ItemsInWindow,
                Time = barAgg.Time,
                Ticks = barAgg.Time.Ticks
            };
            return newBar;
        }
        
        [OnDeserialized]
        internal void OnDeserializedMethod(StreamingContext context) => Time = new DateTime(Ticks);
        
    }
}
