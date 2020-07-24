using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Alpaca.Markets;
using UsageExamples;


namespace TradingTest
{
    class Program
    {
        static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        static async Task Main(string[] args)
        {
            string API_KEY = "PKYTILJHP9H0FURKDZM1";
            string API_SECRET = "U3BVINkOTMMENV37CNwT3RY0iyC89fHAS1OPd4H9";
            string API_URL = "https://paper-api.alpaca.markets";

            logger.Info("Log initalized");

            /*
            var alpaca = new Alpaca.Markets.RestClient(API_KEY, API_SECRET, API_URL);
            var account = alpaca.GetAccountAsync().Result;

            Console.Out.WriteLine( $"Account is {account.Status} buying power {account.BuyingPower.ToString("c")} {account.Currency}");

            var asset = alpaca.GetAssetAsync("SPY").Result;

            Console.Out.WriteLine($"Asset {asset.Symbol} is tradable {asset.IsTradable} on the {asset.Exchange} exchange");

            asset = alpaca.GetAssetAsync("AMZN").Result;

            Console.Out.WriteLine($"Asset {asset.Symbol} is tradable {asset.IsTradable} on the {asset.Exchange} exchange");

            var barsSets = alpaca.GetBarSetAsync(new String[] { "SPY","AMZN" }, TimeFrame.Day, 200).Result;
            */
            /*
            foreach(var barsSet in barsSets)
            {
                DisplayBarsFacts(barsSet);
            }
            */

            // await RunStockDataModel();
            /* 
             var scalper = new Scalper()
             {
                 TradeLogsBasePath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)
             };
             scalper.MovingAverage = 50;

             await scalper.Run();
             */

            await RunTask();
            //var meanRevision = new MeanReversionPaperOnly();
            //await meanRevision.Run();
        }

        private static async Task RunTask()
        {
            StockDataModel stockDataModel = new StockDataModel();
            var alpaca = stockDataModel.alpacaTradingClient;
            var account = alpaca.GetAccountAsync().Result;
            string[] symbols = { "SPY" ,"BNTX"};
            //{d7947b70-cf3c-4209-b072-4689e7a635de}

            //var createResults = alpaca.CreateWatchListAsync("carl", symbols).Result;
            //var watch = alpaca.GetWatchListByNameAsync("carl").Result;
            //var updated = alpaca.UpdateWatchListByIdAsync(watch.WatchListId,"carl",symbols).Result;
            //var watchList = alpaca.ListWatchListsAsync().Result;
            
            //Console.Out.WriteLine($"Account is {account.Status} buying power {account.BuyingPower.ToString("c")} {account.Currency}");

            var asset = alpaca.GetAssetAsync("SPY").Result;
            
            int numberOfDays = 10;
            var barSet = await stockDataModel.alpacaDataClient.GetBarSetAsync(new BarSetRequest(symbols, TimeFrame.Hour) { Limit = numberOfDays });
            
            /*
            Console.Out.WriteLine($"Asset {asset.Symbol} is tradable {asset.IsTradable} on the {asset.Exchange} exchange");

            asset = alpaca.GetAssetAsync("AMZN").Result;

            Console.Out.WriteLine($"Asset {asset.Symbol} is tradable {asset.IsTradable} on the {asset.Exchange} exchange");
            */
            //var barsSets = alpaca.GetBarSetAsync(new String[] { "SPY", "AMZN" }, TimeFrame.Day, 200).Result;
            /*
            foreach(var barsSet in barsSets)
            {
                DisplayBarsFacts(barsSet);
            }
            */
        }

        private static async Task RunStockDataModel()
        {
            var stockData = new StockDataModel();
            //await stockData.Run();
            stockData.AddStock("SPY");
            stockData.AddStock("QQQ");
            await stockData.LoadHistoricalData();
        }

        //private static void DisplayBarsFacts(List<IAgg> bars)
        private static void DisplayBarsFacts(KeyValuePair<string, IEnumerable<IAgg>> barPackage)
        {
            var barsNewestFirst = barPackage.Value.Reverse();
            foreach (var bar in barsNewestFirst.Take(5))
            {
                Console.Out.WriteLine($"{barPackage.Key} Closing price {bar.Close} on {bar.Time.ToShortDateString()}");
            }
            int[] smaLines = { 200, 100, 50, 20, 10, 5 };
            foreach (var sma in smaLines)
            {
                Console.Out.WriteLine($"{barPackage.Key} {sma} day SMA {CalculateClosingSMA(barsNewestFirst, sma)}");
            }

        }
        private static decimal CalculateClosingSMA(IEnumerable<IAgg> barsNewestFirst, int days)
        {
            return barsNewestFirst.Take(days).Average(a=> a.Close);
        }
    }
}
