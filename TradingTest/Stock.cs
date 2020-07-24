using Alpaca.Markets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingTest
{
    public class Stock
    {
        public string Symbol { get; set; }
        public decimal Price { get; set; }
        public string PriceFormated => Stock.FormatMoney(Price);

        public long VolumeAverage { get; set; }
        public string VolumeAverageFormated => Stock.FormatVolume(VolumeAverage);

        public DateTime LastUpdated { get; set; }

        public long VolumeToday { get; set; }
        public string VolumeTodayFormated => Stock.FormatVolume(VolumeToday);
        //public List<JsonBarAgg> HistoricalData { get; set; } //{Alpaca.Markets.JsonBarAgg}
        public List<StockBar> HistoricalData { get; set; } = new List<StockBar>();
        public StockBar TodayData { get; set; }
        public VolumeEstimate VolumeEstimate { get; set; }


        public static string FormatVolume(long volume)
        {
            double volInMillions = Math.Round((double)volume * 0.000001,2);
            return $"{volInMillions}M";            
        }

        public static string FormatPercent(double percent)
        {
            double volInMillions = Math.Round(percent * 100.0, 2);
            return $"{volInMillions}%";
        }
        public static string FormatMoney(decimal percent)
        {
            return percent.ToString("C");
        }

        public void AddTick(IAgg data)
        {
            TodayData = StockBar.LoadFromIAgg(data);
            TodayData.Time = DateTime.Now;
            LastUpdated = TodayData.Time;
            Price = TodayData.Close;
            VolumeToday = TodayData.Volume;
            VolumeEstimate = GetVolumeEstimate(VolumeAverage, VolumeToday, MinutesOpen(DateTime.Now));
        }
        
        public static VolumeEstimate GetVolumeEstimate(long averageVolume, long volumeToday, int minutesOpen)
        {
            if (minutesOpen <= 0)
            {
                return new VolumeEstimate
                {
                    Volume = averageVolume,
                    VolumePercent = 1.0,
                    Confidence = 0
                };
            }

            double averageVolumePerMintueToday = (double) volumeToday / minutesOpen;
            double estimatedVolumeToday = averageVolumePerMintueToday * 390;
            VolumeEstimate volumeEstimate = new VolumeEstimate
            {
                Volume = (int)estimatedVolumeToday,
                VolumePercent = Math.Round(estimatedVolumeToday / averageVolume,4),
                Confidence = Math.Round((double)minutesOpen / 390,4)
            };
            return volumeEstimate;
        }

        public static int MinutesOpen(DateTime asOfTime)
        {
            DateTime marketOpen = new DateTime(asOfTime.Year, asOfTime.Month, asOfTime.Day, 6, 30, 0);
            double timeDif = asOfTime.Subtract(marketOpen).TotalMinutes;
            int timeDifSeconds = (int)Math.Round(timeDif);
            if (timeDifSeconds >= 0)
            {
                return timeDifSeconds;
            }
            return 0;
        }

        public void InitData(IEnumerable<IAgg> data)
        {
            //var dataNotToday = data.Take(data.Count() - 1);
            HistoricalData = new List<StockBar>();
            foreach(var barAgg in data)
            {
                HistoricalData.Add(StockBar.LoadFromIAgg(barAgg));
            }
            VolumeAverage = (int) HistoricalData.Average(d => d.Volume);
        }


    }
}
