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
    internal sealed class Scalper : IDisposable
    {
        static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private string API_KEY = "PKYTILJHP9H0FURKDZM1";

        private string API_SECRET = "U3BVINkOTMMENV37CNwT3RY0iyC89fHAS1OPd4H9";

        private string symbol = "SPY";

        private Decimal scale = 200;

        private AlpacaTradingClient alpacaTradingClient;

        private AlpacaDataClient alpacaDataClient;

        private Guid lastTradeId = Guid.NewGuid();

        public int MovingAverage = 50;

        private TradeLog tradeLog;
        public string TradeLogsBasePath = "";


        public async Task Run()
        {
            alpacaTradingClient = Environments.Paper.GetAlpacaTradingClient(new SecretKey(API_KEY, API_SECRET));

            alpacaDataClient = Environments.Paper.GetAlpacaDataClient(new SecretKey(API_KEY, API_SECRET));

            tradeLog = TradeLog.LoadTradeLoad(TradeLogsBasePath, symbol);
            if (tradeLog == null)
            {
                tradeLog = new TradeLog() { Symbol = symbol, BasePath = TradeLogsBasePath };
            }

            // First, cancel any existing orders so they don't impact our buying power.
            var orders = await alpacaTradingClient.ListOrdersAsync();
            foreach (var order in orders)
            {
                await alpacaTradingClient.DeleteOrderAsync(order.OrderId);
            }

            // Figure out when the market will close so we can prepare to sell beforehand.
            var calendars = (await alpacaTradingClient.ListCalendarAsync(DateTime.Today)).ToList();
            var calendarDate = calendars.First().TradingDate;
            var closingTime = calendars.First().TradingCloseTime;

            closingTime = new DateTime(calendarDate.Year, calendarDate.Month, calendarDate.Day, closingTime.Hour, closingTime.Minute, closingTime.Second);

            logger.Info("Waiting for market open...");
            await AwaitMarketOpen();
            logger.Info("Market opened.");

            TradeEntry trade = tradeLog.GetOpenTrade();
            
            // Check every minute for price updates.
            TimeSpan timeUntilClose = closingTime - DateTime.UtcNow;
            while (timeUntilClose.TotalMinutes > 15)
            {
                // Cancel old order if it's not already been filled.
                await alpacaTradingClient.DeleteOrderAsync(lastTradeId);

                // Get information about current account value.
                var account = await alpacaTradingClient.GetAccountAsync();
                Decimal buyingPower = account.BuyingPower;
                Decimal portfolioValue = account.Equity;

                // Get information about our existing position.
                int positionQuantity = 0;
                Decimal positionValue = 0;
                try
                {
                    var currentPosition = await alpacaTradingClient.GetPositionAsync(symbol);
                    positionQuantity = currentPosition.Quantity;
                    positionValue = currentPosition.MarketValue;
                }
                catch (Exception)
                {
                    // No position exists. This exception can be safely ignored.
                }

                var barSet = await alpacaDataClient.GetBarSetAsync(new BarSetRequest(symbol, TimeFrame.Minute) { Limit = MovingAverage});
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

                /*

                if (diff <= 0)
                {
                    // Above the 20 minute average - exit any existing long position.
                    if (positionQuantity > 0)
                    {
                        Console.WriteLine("Setting position to zero.");
                        await SubmitOrder(positionQuantity, currentPrice, OrderSide.Sell);
                    }
                    else
                    {
                        Console.WriteLine("No position to exit.");
                    }
                }
                else
                {
                    // Allocate a percent of our portfolio to this position.
                    Decimal portfolioShare = diff / currentPrice * scale;
                    Decimal targetPositionValue = portfolioValue * portfolioShare;
                    Decimal amountToAdd = targetPositionValue - positionValue;

                    if (amountToAdd > 0)
                    {
                        // Buy as many shares as we can without going over amountToAdd.

                        // Make sure we're not trying to buy more than we can.
                        if (amountToAdd > buyingPower)
                        {
                            amountToAdd = buyingPower;
                        }
                        int qtyToBuy = (int)(amountToAdd / currentPrice);

                        await SubmitOrder(qtyToBuy, currentPrice, OrderSide.Buy);
                    }
                    else
                    {
                        // Sell as many shares as we can without going under amountToAdd.

                        // Make sure we're not trying to sell more than we have.
                        amountToAdd *= -1;
                        int qtyToSell = (int)(amountToAdd / currentPrice);
                        if (qtyToSell > positionQuantity)
                        {
                            qtyToSell = positionQuantity;
                        }

                        await SubmitOrder(qtyToSell, currentPrice, OrderSide.Sell);
                    }
                }
                */
                // Wait another minute.
                Thread.Sleep(60000);
                timeUntilClose = closingTime - DateTime.UtcNow;
            }

            Console.WriteLine("Market nearing close; closing position.");
            await ClosePositionAtMarket();
        }

        private static decimal CalculateClosingSMA(IEnumerable<IAgg> barsNewestFirst, int days)
        {
            return barsNewestFirst.Take(days).Average(a => a.Close);
        }

        public void Dispose()
        {
            alpacaTradingClient?.Dispose();
            alpacaDataClient?.Dispose();
        }

        private async Task AwaitMarketOpen()
        {
            while (!(await alpacaTradingClient.GetClockAsync()).IsOpen)
            {
                await Task.Delay(60000);
            }
        }

        // Submit an order if quantity is not zero.
        private async Task SubmitOrder(int quantity, Decimal price, OrderSide side)
        {
            if (quantity == 0)
            {
                Console.WriteLine("No order necessary.");
                return;
            }
            Console.WriteLine($"Submitting {side} order for {quantity} shares at ${price}.");
            var order = await alpacaTradingClient.PostOrderAsync(symbol, quantity, side, OrderType.Limit, TimeInForce.Day, price);
            lastTradeId = order.OrderId;
        }

        private async Task ClosePositionAtMarket()
        {
            try
            {
                var positionQuantity = (await alpacaTradingClient.GetPositionAsync(symbol)).Quantity;
                await alpacaTradingClient.PostOrderAsync(symbol, positionQuantity, OrderSide.Sell, OrderType.Market, TimeInForce.Day);
            }
            catch (Exception)
            {
                // No position to exit.
            }
        }
    }
}
