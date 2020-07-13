using Alpaca.Markets;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.IO;

namespace TradingTest
{
    // This version of the mean reversion example algorithm uses only API features which
    // are available to users with a free account that can only be used for paper trading.
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal sealed class StockDataModel : IDisposable
    {
        static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private string API_KEY = "PKYTILJHP9H0FURKDZM1";

        private string API_SECRET = "U3BVINkOTMMENV37CNwT3RY0iyC89fHAS1OPd4H9";

        private List<Stock> Stocks = new List<Stock>();

        //private Decimal scale = 200;

        private AlpacaTradingClient alpacaTradingClient;

        private AlpacaDataClient alpacaDataClient;

        private Guid lastTradeId = Guid.NewGuid();

        public int MovingAverage = 50;


        public async Task Run()
        {
            
            Stocks.Add(new Stock { Symbol = "AAPL" });            
            Stocks.Add(new Stock { Symbol = "SPY" });
            Stocks.Add(new Stock { Symbol = "TSLA" });
            Stocks.Add(new Stock { Symbol = "TWLO" });
            Stocks.Add(new Stock { Symbol = "ZS" });
            Stocks.Add(new Stock { Symbol = "CRWD" });
            Stocks.Add(new Stock { Symbol = "FTNT" });
            
            alpacaTradingClient = Environments.Paper.GetAlpacaTradingClient(new SecretKey(API_KEY, API_SECRET));

            alpacaDataClient = Environments.Paper.GetAlpacaDataClient(new SecretKey(API_KEY, API_SECRET));

            // Figure out when the market will close so we can prepare to sell beforehand.
            var calendars = (await alpacaTradingClient.ListCalendarAsync(DateTime.Today)).ToList();
            var calendarDate = calendars.First().TradingDate;
            var closingTime = calendars.First().TradingCloseTime;

            closingTime = new DateTime(calendarDate.Year, calendarDate.Month, calendarDate.Day, closingTime.Hour, closingTime.Minute, closingTime.Second);

            TimeSpan timeUntilClose = closingTime - DateTime.UtcNow;
            IEnumerable<string> symbols = Stocks.Select(s => s.Symbol);
            //var historicalData = await GetBarsWithRetry(symbols, 20);
            var historicalData = await alpacaDataClient.GetBarSetAsync(new BarSetRequest(symbols, TimeFrame.Day) { Limit = 21 });

            foreach (var stock in Stocks)
            {
                var bars = historicalData[stock.Symbol];
                stock.InitData(bars);
            }
            SaveModel(@"c:\temp\stockdatamodelcache.json");

            logger.Info("After");
            /*
            var today = await GetBarsWithRetry(symbols, 1);

            foreach (var stock in Stocks)
            {
                var bars = today[stock.Symbol];
                stock.AddTick(bars);
            }
            logger.Info("After");
            */
            while (timeUntilClose.TotalMinutes > 1)
            {
                var today = await GetBarsWithRetry(symbols, 1);

                foreach (var stock in Stocks.OrderBy(s => s.Symbol))
                {
                    var bars = today[stock.Symbol];
                    stock.AddTick(bars);
                    logger.Info($"{stock.Symbol} price {stock.TodayData.Close}  avol[{InMillions(stock.VolumnAverage)}] vol[{InMillions(stock.VolumnToday)}] evol[{InMillions(stock.VolumeEstimate.Volume)}] per [{stock.VolumeEstimate.VolumePercent * 100}%]");
                }
                SaveModel(@"c:\temp\stockdatamodel_full.json");
                Thread.Sleep(60000);
                timeUntilClose = closingTime - DateTime.UtcNow;
            }            
        }

        public void SaveModel(string path)
        {
            string json = JsonConvert.SerializeObject(Stocks, Formatting.Indented);
            //string path = basePath.EndsWith("\\") ? basePath : basePath + "\\";
           // path += "stockdatamodel.json";
            File.WriteAllText($"{path}", json);
        }

        private string InMillions(long volume)
        {
            //number 24,601,236
            double volInMill = Math.Round(volume * 0.000001, 2);
            return volInMill.ToString() + "M";
        }

        private async Task<IReadOnlyDictionary<String, IReadOnlyList<IAgg>>> GetBarsWithRetry(IEnumerable<string> symbols,int? numberOfDays, int tries = 0)
        {

            var barSet = await alpacaDataClient.GetBarSetAsync(new BarSetRequest(symbols, TimeFrame.Day) { Limit = numberOfDays });

            var bars = barSet.ElementAt(0).Value.ToList();

            var lastBar = bars.Last();
            if (IsBarForDate(lastBar, DateTime.Today))
            {
                return barSet;
            }
            else
            {
                if(tries < 10)
                {
                    logger.Info($"bars are invalid retrying getbars [{tries + 1}]");
                    await Task.Delay(5000);
                    return await GetBarsWithRetry(symbols,numberOfDays,tries+1);
                }
                else
                {
                    logger.Error($"maximum retres hit, giving up");
                    return barSet;
                }
            }


        }

        private bool IsBarForDate(IAgg bar, DateTime date)
        {
            var barDate = bar.Time.Date;
            if (barDate == date)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        
                

        public void Dispose()
        {
            alpacaTradingClient?.Dispose();
            alpacaDataClient?.Dispose();
        }

        
    }
}
