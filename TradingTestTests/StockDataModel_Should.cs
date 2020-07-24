using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using FluentAssertions;
using TradingTest;
using Alpaca.Markets;

namespace TradingTestTests
{
    public class StockDataModel_Should
    {
        [Theory]
        [InlineData(null, 0,false)]
        [InlineData("SPY", 0,false)]
        [InlineData("SPY", 637278768000000000, true)]
        public void IsHistoricalDataUpToDate_Should(string ticker, Int64 ticksForHistoricalData, bool expectedValue)
        {
            StockDataModel stockDataModel = new StockDataModel();
            if (ticker != null)
            {
                List<Stock> stocks = new List<Stock>();
                Stock stock = new Stock { Symbol = ticker };
                if (ticksForHistoricalData > 0)
                {
                    DateTime barTime = new DateTime(ticksForHistoricalData);
                    StockBar bar = new StockBar { Time = barTime};
                    stock.HistoricalData.Add(bar);
                }
                stocks.Add(stock);
                stockDataModel.SetStockListForTesting(stocks);
            }

            stockDataModel.IsHistoricalDataUpToDate().Should().Be(expectedValue);
        }

        [Theory]
        [InlineData("07/22/2020 07:47:16", "07/21/2020")]
        [InlineData("07/22/2020 17:47:16", "07/22/2020")]
        [InlineData("07/19/2020 07:47:16", "07/17/2020")]
        [InlineData("07/18/2020 07:47:16", "07/17/2020")]
        public void GetLastCompletedTradingDate_Should(string asOfTestTimeSrc, string expectedDateSrc)
        {
            StockDataModel stockDataModel = new StockDataModel();
            DateTime asOfTestTime = DateTime.Parse(asOfTestTimeSrc);
            DateTime expectedDate = DateTime.Parse(expectedDateSrc);           

            var lastCompletedTradeDate = stockDataModel.GetLastCompletedTradingDate(asOfTestTime);
            lastCompletedTradeDate.Should().Be(expectedDate);
        }

        [Theory]
        [InlineData("07/18/2020 07:47:16", false)]
        [InlineData("07/19/2020 17:47:16", false)]
        [InlineData("07/22/2020 4:47:16", false)]
        [InlineData("07/22/2020 18:47:16", false)]
        [InlineData("07/22/2020 11:47:16", true)]
        public void IsMarketOpen_Should(string asOfTestTimeSrc, bool expectedResult)
        {
           // StockDataModel stockDataModel = new StockDataModel();
            DateTime asOfTestTime = DateTime.Parse(asOfTestTimeSrc);
            
            var lastCompletedTradeDate = StockDataModel.IsMarketOpen(asOfTestTime);
            lastCompletedTradeDate.Should().Be(expectedResult);
        }

        
    }
}
