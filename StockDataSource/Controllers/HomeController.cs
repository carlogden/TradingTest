using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using StockDataSource.Models;
using TradingTest;
using NLog;

namespace StockDataSource.Controllers
{
    public class HomeController : Controller
    {
        static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly ILogger<HomeController> _logger;        

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }
        public async Task<IActionResult> StockDataRequest()
        {   //symbol=MSFT&target=price
            string symbol = Request.Query["symbol"];
            string target = Request.Query["target"];
            if (String.IsNullOrEmpty(symbol))
            {
                return Content("Error missing symbol");
            }
            if (String.IsNullOrEmpty(target))
            {
                return Content("Error missing target");
            }
            symbol = symbol.ToUpper();
            
            StockDataModel stockDataModel = new TradingTest.StockDataModel();
            
            await stockDataModel.LoadHistoricalData();
            await stockDataModel.LoadTodaysBarAsyncIfNeeded();
            
            var stock = stockDataModel.GetStockInModel(symbol);
            string result = "";
            if (stock == null)
            {
                await stockDataModel.AddStockAndLoad(symbol);
                stock = stockDataModel.GetStockInModel(symbol);
                if (stock == null)
                {
                    result = "STOCK_NOT_FOUND";
                    logger.Info($"{symbol} {target} {result}");
                    return Content(result);
                }                
            }
            if (target.Equals("price"))
            {
                result = stock.Price.ToString();
            }
            var remoteIpAddress = Request.HttpContext.Connection.RemoteIpAddress;
            logger.Info($"{symbol} {target} {result} {remoteIpAddress}");
            return Content(result);
            
            
        }
        public async Task<IActionResult> StockGridAsync()
        {
            StockDataModel stockDataModel = new TradingTest.StockDataModel();

            await stockDataModel.LoadHistoricalData();

            string action = (string)Request.Query["action"] ?? String.Empty;
            string symbol = Request.Query["symbol"];

            if (action.Equals("addsymbol") && symbol!=null){
                await stockDataModel.AddStockAndLoad(symbol);
            }

            if (action.Equals("removesymbol") && symbol != null)
            {
                stockDataModel.RemoveStock(symbol);
            }

            await stockDataModel.LoadTodaysBarAsyncIfNeeded();
            //ViewBag.Carl = "ogden1";
            ViewBag.stockDataModel = stockDataModel;
            ViewBag.Summary = stockDataModel.GetSummary();
            //ViewBag.stocks = StockDataModel.Stocks;
            ViewBag.Stocks = StockDataModel.Stocks;
            //StockGrid stockGrid = new StockGrid();
            //stockGrid.Stocks = StockDataModel.Stocks; 
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
