using System;
using Xunit;
using FluentAssertions;
using TradingTest;
using Alpaca.Markets;

namespace TradingTestTests
{
    public class Stock_Should
    {
        [Theory]
        [InlineData(10000, 5000, 0, 10000, 1.0, 0.0)]
        [InlineData(10000, 5000, 195, 10000, 1.0, 0.5)]
        [InlineData(10000, 5000, 390, 5000, 0.5, 1.0)]
        [InlineData(35140782, 22590093, 335, 26298914.24, 0.7484, 0.859)]
        [InlineData(10088921, 20843488, 335, 24265553.19, 2.4052, 0.859)]
        public void EstimateVolume_Should(int averageVolume, int volumeToday, int minutesFromOpen, int expectedEstimate, double expectedPercent, double expectedConfidence)
        {
            var volumeEstimate = Stock.GetVolumeEstimate(averageVolume, volumeToday, minutesFromOpen);
            volumeEstimate.Volume.Should().Be(expectedEstimate);
            volumeEstimate.VolumePercent.Should().Be(expectedPercent);
            volumeEstimate.Confidence.Should().Be(expectedConfidence);

        }
        [Theory]
        [InlineData(0, 0)]
        [InlineData(30, 0)]
        [InlineData(45, 1)]
        [InlineData(100, 2)]
        [InlineData(-50, 0)]
        public void GetMinutesFromOpen_Should(int secondsToAddTo, int expectedMinutes)
        {
            DateTime marketTime = new DateTime(2020, 7, 9, 9, 30, 0).AddSeconds(secondsToAddTo);
            //marketTime.AddSeconds(secondsToAddTo);

            Stock.MinutesOpen(marketTime).Should().Be(expectedMinutes);
        }

    }
}
