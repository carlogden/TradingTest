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
        public long VolumnAverage { get; set; }

        public long VolumnToday { get; protected set; }
        public List<IAgg> HistoricalData { get; set; }
        public IAgg TodayData { get; protected set; }
        public VolumeEstimate VolumeEstimate { get; protected set; }

        public void AddTick(IReadOnlyList<IAgg> data)
        {
            TodayData = data[0];
            Price = TodayData.Close;
            VolumnToday = TodayData.Volume;
            VolumeEstimate = GetVolumeEstimate(VolumnAverage, VolumnToday, MinutesOpen(DateTime.Now.AddHours(3)));
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
            DateTime marketOpen = new DateTime(asOfTime.Year, asOfTime.Month, asOfTime.Day, 9, 30, 0);
            double timeDif = asOfTime.Subtract(marketOpen).TotalMinutes;
            int timeDifSeconds = (int)Math.Round(timeDif);
            if (timeDifSeconds >= 0)
            {
                return timeDifSeconds;
            }
            return 0;
        }

        public void InitData(IReadOnlyList<IAgg> data)
        {
            var dataNotToday = data.Take(data.Count() - 1);
            HistoricalData = dataNotToday.ToList();
            VolumnAverage = (int) HistoricalData.Average(d => d.Volume);
        }


    }
}
