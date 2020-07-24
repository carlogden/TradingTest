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
        public AlpacaTradingClient alpacaTradingClient { 
            get
            {
                if (_alpacaTradingClient == null)
                {
                    _alpacaTradingClient = Environments.Paper.GetAlpacaTradingClient(new SecretKey(API_KEY, API_SECRET));
                }
                return _alpacaTradingClient;
            }

            protected set { _alpacaTradingClient = value; }
        }

        private static AlpacaDataClient _alpacaDataClient;
        public AlpacaDataClient alpacaDataClient
        {
            get
            {
                if (_alpacaDataClient == null)
                {
                    _alpacaDataClient = Environments.Paper.GetAlpacaDataClient(new SecretKey(API_KEY, API_SECRET));
                }
                return _alpacaDataClient;
            }

            protected set { _alpacaDataClient = value; }
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
                await LoadTodaysBarAsync();                
                Thread.Sleep(60000);
                timeUntilClose = closingTime - DateTime.UtcNow;
            }            
        }

        public async Task LoadTodaysBarAsyncIfNeeded(int secondsOld = 60)
        {
            bool loadBars = false;
            if (StockDataModel.Stocks[0].TodayData == null)
            {
                loadBars = true;
            }
            if (IsMarketOpen())
            {

                DateTime lastUpdateTime = StockDataModel.Stocks[0].TodayData.Time;
                lastUpdateTime = lastUpdateTime.AddSeconds(secondsOld);
                if (lastUpdateTime < DateTime.Now)
                {
                    loadBars = true;
                }

            }
            if (loadBars)
            {
                await LoadTodaysBarAsync();
            }
        }

        public StockDataModelSummary GetSummary()
        {
            StockDataModelSummary summary = new StockDataModelSummary();
            var stock = GetStockOldestUpdated();
            summary.LastUpdated = stock.LastUpdated;
            summary.MinutesOpen = Stock.MinutesOpen(DateTime.Now);
            summary.EstimateConfidence = stock.VolumeEstimate.Confidence;
            summary.EstimateConfidenceFormated = stock.VolumeEstimate.ConfidenceFormated;
            return summary;
        }

        public bool IsMarketOpen()
        {
            return IsMarketOpen(DateTime.Now);
        }
        public static bool IsMarketOpen(DateTime timeToEvaluate)
        {
            if (timeToEvaluate.DayOfWeek == DayOfWeek.Saturday || timeToEvaluate.DayOfWeek == DayOfWeek.Sunday)
            {
                return false;
            }
            DateTime marketOpen = timeToEvaluate.Date.AddHours(6).AddSeconds(30);
            if(timeToEvaluate < marketOpen)
            {
                return false;
            }
            DateTime marketClose = timeToEvaluate.Date.AddHours(13);
            if(timeToEvaluate > marketClose)
            {
                return false;
            }
            return true;

        }

        public async Task LoadTodaysBarAsync()
        {
            var today = await GetBarsWithRetry(Symbols, 1);

            foreach (var stock in Stocks.OrderBy(s => s.Symbol))
            {
                var bars = today[stock.Symbol];
                stock.AddTick(bars[0]);
                logger.Info($"{stock.Symbol} price {stock.TodayData.Close}  avol[{InMillions(stock.VolumeAverage)}] vol[{InMillions(stock.VolumeToday)}] evol[{InMillions(stock.VolumeEstimate.Volume)}] per [{stock.VolumeEstimate.VolumePercent * 100}%]");
            }
            SaveModel(@"c:\temp\stockdatamodel_full.json");

        }

        public void RemoveStock(string ticker)
        {
            Stock stock = GetStockInModel(ticker);
            if (stock != null)
            {
                Stocks.Remove(stock);
                SaveModel();
            }
        }
        public void AddStock(string ticker)
        {
            if (ticker == null)
            {
                return;
            }
            if (!IsStockInModel(ticker))
            {
                Stocks.Add(new Stock { Symbol = ticker });
            }        
        }

        public async Task AddStockWithVerify(string ticker)
        {
            if (ticker == null)
            {
                return;
            }
            if (!IsStockInModel(ticker))
            {
                var asset = await GetAssetAsync(ticker);
                if (asset != null)
                {
                    AddStock(asset.Symbol);
                }
            }
        }

        public Task<IAsset> GetAssetAsync(string ticker)
        {
            return  alpacaTradingClient.GetAssetAsync(ticker.ToUpper());
        }

        public bool IsStockInModel(string ticker)
        {
            if (Stocks.Any(s => s.Symbol.Equals(ticker.ToUpper())))
            {
                return true;
            }
            return false;
        }
        public Stock GetStockInModel(string ticker)
        {
            return Stocks.FirstOrDefault(s => s.Symbol.Equals(ticker.ToUpper()));
        }

        public Stock GetStockOldestUpdated()
        {
            return Stocks.OrderByDescending(s => s.LastUpdated).FirstOrDefault(); ;
        }

        public async Task LoadHistoricalData()
        {
            if (Stocks.Count == 0)
            {
                LoadModel();
            }
            if (!IsHistoricalDataUpToDate())
            {
                if (Stocks.Count == 0)
                {
                    AddStock("SPY");
                    AddStock("BNTX");
                    AddStock("SPOT");
                }
                await LoadHistoricalDataForSymbols(Symbols);
                /*
                var historicalData = await alpacaDataClient.GetBarSetAsync(new BarSetRequest(Symbols, TimeFrame.Day) { Limit = DaysToAverage+1 });
                DateTime lastTradingDate = GetLastCompletedTradingDate();
                foreach (var stock in Stocks)
                {
                    var bars = historicalData[stock.Symbol].Where(d=> d.Time.Date <= lastTradingDate.Date);
                    stock.InitData(bars);
                }
                SaveModel(@"c:\temp\stockdatamodel_full.json");
                */
            }
        }

        public async Task AddStockAndLoad(string ticker)
        {
            if (!IsStockInModel(ticker))
            {
                await AddStockWithVerify(ticker);
                await LoadHistoricalDataForSymbols(new string[] { ticker });
                Stocks = Stocks.OrderBy(s => s.Symbol).ToList();
            }
        }

        private async Task LoadHistoricalDataForSymbols(IEnumerable<string> symbols)
        {
            var historicalData = await alpacaDataClient.GetBarSetAsync(new BarSetRequest(symbols, TimeFrame.Day) { Limit = DaysToAverage + 1 });
            DateTime lastTradingDate = GetLastCompletedTradingDate();
            foreach (var symbol in symbols)
            {
                Stock stock = Stocks.FirstOrDefault(s => s.Symbol.Equals(symbol));
                if (stock != null)
                {
                    var bars = historicalData[stock.Symbol].Where(d => d.Time.Date <= lastTradingDate.Date);
                    stock.InitData(bars);
                    var todayBar = historicalData[stock.Symbol].FirstOrDefault(d => d.Time.Date == lastTradingDate.Date);
                    if (todayBar != null)
                    {
                        stock.AddTick(todayBar);
                    }
                }
            }
            SaveModel(@"c:\temp\stockdatamodel_full.json");
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
            DateTime lastUpdateTime = StockDataModel.Stocks[0].HistoricalData.Last().Time;
            if (IsDateLastTradingDay(lastUpdateTime))
            {
                return true;
            }

            return false;
        }

        public bool IsDateLastTradingDay(DateTime dateToTest)
        {
            DateTime lastCompletedTradeDate = GetLastCompletedTradingDate();
            if(dateToTest.Date == lastCompletedTradeDate.Date)
            {
                return true;
            }
            return false;
        }

        public DateTime GetLastCompletedTradingDate()
        {
            return GetLastCompletedTradingDate(DateTime.Now);
        }
        public DateTime GetLastCompletedTradingDate(DateTime asOfDateTime)
        {
            DateTime lastTradingDate = asOfDateTime;
            if(lastTradingDate.Hour < 13)
            {
                lastTradingDate = lastTradingDate.AddDays(-1);
            }
            
            if(lastTradingDate.DayOfWeek == DayOfWeek.Saturday)
            {
                lastTradingDate = lastTradingDate.AddDays(-1);
            }

            if (lastTradingDate.DayOfWeek == DayOfWeek.Sunday)
            {
                lastTradingDate = lastTradingDate.AddDays(-2);
            }
            
            return lastTradingDate.Date;
        }

        public void LoadModel()
        {
            //string path = $"{DataPath}stockdatamodelcache.json";
            string path = $"{DataPath}stockdatamodel_full.json";
            
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                Stocks = JsonConvert.DeserializeObject<List<Stock>>(json).OrderBy(s=> s.Symbol).ToList();
            }
            else
            {
                //Stocks = new List<Stock>();
            }
        }
       

        public void SaveModel(string path=null)
        {
            string pathToWrite = path ?? $"{DataPath}stockdatamodel_full.json";
            string json = JsonConvert.SerializeObject(Stocks.OrderBy(s=> s.Symbol), Formatting.Indented);
            //string path = basePath.EndsWith("\\") ? basePath : basePath + "\\";
           // path += "stockdatamodel.json";
            File.WriteAllText(pathToWrite, json);
        }

        private string InMillions(long volume)
        {
            //number 24,601,236
            double volInMill = Math.Round(volume * 0.000001, 2);
            return volInMill.ToString() + "M";
        }

        public async Task<IReadOnlyDictionary<String, IReadOnlyList<IAgg>>> GetBarsWithRetry(IEnumerable<string> symbols,int? numberOfDays, int tries = 0)
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
