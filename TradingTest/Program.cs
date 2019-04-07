using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Alpaca.Markets;


namespace TradingTest
{
    class Program
    {
        static void Main(string[] args)
        {
            string API_KEY = "PKGTE1QBWLC5KS2I2DEG";
            string API_SECRET = "DMcv5Yxk6UtuogCXIOYBxUYS2foP/Zaos1WvzHu4";
            string API_URL = "https://paper-api.alpaca.markets";

            var alpaca = new Alpaca.Markets.RestClient(API_KEY, API_SECRET, API_URL);
            var account = alpaca.GetAccountAsync().Result;

            Console.Out.WriteLine( $"Account is {account.Status} buying power {account.BuyingPower.ToString("c")} {account.Currency}");

            var asset = alpaca.GetAssetAsync("SPY").Result;

            Console.Out.WriteLine($"Asset {asset.Symbol} is tradable {asset.IsTradable} on the {asset.Exchange} exchange");

            asset = alpaca.GetAssetAsync("AMZN").Result;

            Console.Out.WriteLine($"Asset {asset.Symbol} is tradable {asset.IsTradable} on the {asset.Exchange} exchange");

            var barsSets = alpaca.GetBarSetAsync(new String[] { "SPY","AMZN" }, TimeFrame.Day, 200).Result;
            foreach(var barsSet in barsSets)
            {
                DisplayBarsFacts(barsSet);
            }
            //var barsArray = barsSets.ToArray();

            //var bars = barsSets["SPY"].ToList();
            //DisplayBarsFacts(barsArray[0]);

            //var sma200 = bars.Average(a => a.c);



            var test = new MeanReversionPaperOnly();
            //test.Run();
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
