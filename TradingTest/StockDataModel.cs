using Alpaca.Markets;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

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
            Stocks.Add(new Stock { Symbol = "SPY" });
            Stocks.Add(new Stock { Symbol = "AAPL" });
            Stocks.Add(new Stock { Symbol = "TWLO" });
            Stocks.Add(new Stock { Symbol = "TSLA" });

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
            while (timeUntilClose.TotalMinutes > 5)
            {
                var today = await GetBarsWithRetry(symbols, 1);

                foreach (var stock in Stocks)
                {
                    var bars = today[stock.Symbol];
                    stock.AddTick(bars);
                    logger.Info($"{stock.Symbol} price {stock.TodayData.Close} estimated volumne percent {stock.VolumeEstimate.VolumePercent *100}");
                }
                
                Thread.Sleep(60000);
                timeUntilClose = closingTime - DateTime.UtcNow;
            }

                /*
                while (timeUntilClose.TotalMinutes > 5)
                {
                    var barSet = await GetBarsWithRetry();
                    var bars = barSet[symbol].ToList();

                    var sma = CalculateClosingSMA(bars, MovingAverage);

                    Decimal avg = bars.Average(item => item.Close);
                    Decimal currentPrice = bars.Last().Close;
                    Decimal diff = avg - currentPrice;
                    string msg = $"{symbol} current price: {currentPrice} SMA: {sma} ";
                    var lastBarDate = bars.Last().Time.Date;
                    var currentDate = DateTime.Today;
                    if (lastBarDate == currentDate)
                    {
                        if (currentPrice > sma)
                        {
                            if (trade == null)
                            {
                                trade = new TradeEntry(symbol)
                                {
                                    OpenPrice = currentPrice,
                                    SMAAtOpen = sma                                
                                };
                                tradeLog.Trades.Add(trade);
                                tradeLog.SaveTradeLog();                            
                                logger.Info(msg + $"opening trade {currentPrice}");
                            }
                            else
                            {
                                logger.Info(msg + "holding, net open "+(currentPrice-trade.OpenPrice));
                            }


                            // Console.WriteLine(msg + "we are bullish");
                        }
                        else
                        {
                            if (trade != null)
                            {
                                trade.ClosePrice = currentPrice;
                                trade.SMAAtClose = sma;
                                trade.CloseTime = DateTime.Now;
                                tradeLog.NetProfit += trade.NetProfit;
                                tradeLog.SaveTradeLog();                           

                                logger.Info(msg + "we are bearish closing trade "+trade.NetProfit+" profit on trade, "+tradeLog.NetProfit+" profit on day");
                                trade = null;
                            }
                            else
                            {
                                logger.Info(msg + "we are bearish no open trade");
                            }


                        }
                    }
                    else
                    {
                        logger.Info("Barset is invalid");

                    }


                    // Wait another minute.
                 //   Thread.Sleep(60000);
                  //  timeUntilClose = closingTime - DateTime.UtcNow;
                }
                */
                // Console.WriteLine("Market nearing close; closing position.");
                //await ClosePositionAtMarket();            
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
