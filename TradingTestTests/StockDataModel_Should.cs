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
    }
}
