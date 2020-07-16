using Alpaca.Markets;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.IO;
using Newtonsoft.Json.Converters;

namespace TradingTest
{
    public class StockDataModel : IDisposable
    {
        static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private const string API_KEY = "PKYTILJHP9H0FURKDZM1";

        private const string API_SECRET = "U3BVINkOTMMENV37CNwT3RY0iyC89fHAS1OPd4H9";

        public static List<Stock> Stocks { get; protected set; }
        private static string DataPath = @"c:\temp\";
        public const int DaysToAverage = 20;
        
        public IEnumerable<string> Symbols
        {
            get => Stocks.Select(s => s.Symbol);
        }

        private static AlpacaTradingClient _alpacaTradingClient;
        private AlpacaTradingClient alpacaTradingClient { 
            get
            {
                if (_alpacaTradingClient == null)
                {
                    _alpacaTradingClient = Environments.Paper.GetAlpacaTradingClient(new SecretKey(API_KEY, API_SECRET));
                }
                return _alpacaTradingClient;
            }

            set { _alpacaTradingClient = value; }
        }

        private static AlpacaDataClient _alpacaDataClient;
        private AlpacaDataClient alpacaDataClient
        {
            get
            {
                if (_alpacaDataClient == null)
                {
                    _alpacaDataClient = Environments.Paper.GetAlpacaDataClient(new SecretKey(API_KEY, API_SECRET));
                }
                return _alpacaDataClient;
            }

            set { _alpacaDataClient = value; }
        }

        


        private Guid lastTradeId = Guid.NewGuid();

        public int MovingAverage = 50;

        public StockDataModel()
        {
            if (Stocks == null)
            {
                Stocks = new List<Stock>();
                //alpacaTradingClient = Environments.Paper.GetAlpacaTradingClient(new SecretKey(API_KEY, API_SECRET));
                //alpacaDataClient = Environments.Paper.GetAlpacaDataClient(new SecretKey(API_KEY, API_SECRET));
            }
        }


        public async Task Run()
        {
            
            Stocks.Add(new Stock { Symbol = "AAPL" });            
            Stocks.Add(new Stock { Symbol = "SPY" });
            Stocks.Add(new Stock { Symbol = "TSLA" });
            Stocks.Add(new Stock { Symbol = "TWLO" });
            Stocks.Add(new Stock { Symbol = "ZS" });
            Stocks.Add(new Stock { Symbol = "CRWD" });
            Stocks.Add(new Stock { Symbol = "FTNT" });
            
            //alpacaTradingClient = Environments.Paper.GetAlpacaTradingClient(new SecretKey(API_KEY, API_SECRET));

            //alpacaDataClient = Environments.Paper.GetAlpacaDataClient(new SecretKey(API_KEY, API_SECRET));

            // Figure out when the market will close so we can prepare to sell beforehand.
            var calendars = (await alpacaTradingClient.ListCalendarAsync(DateTime.Today)).ToList();
            var calendarDate = calendars.First().TradingDate;
            var closingTime = calendars.First().TradingCloseTime;

            closingTime = new DateTime(calendarDate.Year, calendarDate.Month, calendarDate.Day, closingTime.Hour, closingTime.Minute, closingTime.Second);

            TimeSpan timeUntilClose = closingTime - DateTime.UtcNow;
            

            logger.Info("After");
           
            while (timeUntilClose.TotalMinutes > 1)
            {
                var today = await GetBarsWithRetry(Symbols, 1);

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

        public void AddStock(string ticker)
        {
            if (!Stocks.Any(s => s.Symbol.Equals(ticker)))
            {
                Stocks.Add(new Stock { Symbol = ticker });
            }
        }

        public async Task LoadHistoricalData()
        {
            LoadModel();
            if (!IsHistoricalDataUpToDate())
            {
                var adc = alpacaDataClient;
                var historicalData = await alpacaDataClient.GetBarSetAsync(new BarSetRequest(Symbols, TimeFrame.Day) { Limit = DaysToAverage+1 });

                foreach (var stock in Stocks)
                {
                    var bars = historicalData[stock.Symbol];
                    stock.InitData(bars);
                }
                SaveModel(@"c:\temp\stockdatamodelcache.json");
            }
        }

        public void SetStockListForTesting(List<Stock> stocks)
        {
            Stocks = stocks;
        }

        public bool IsHistoricalDataUpToDate()
        {
            if (Stocks.Count() == 0)
            {
                return false;
            }
            if (Stocks[0].HistoricalData == null || Stocks[0].HistoricalData.Count() == 0)
            {
                return false;
            }
            
            return true;
        }

        public void LoadModel()
        {
            string path = $"{DataPath}stockdatamodelcache.json";
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                Stocks = JsonConvert.DeserializeObject<List<Stock>>(json);
            }
            else
            {
                Stocks = new List<Stock>();
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
