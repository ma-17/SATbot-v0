using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MongoDB.Bson;
using SATDash_v0.Models;

namespace SATDash_v0.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            DatabaseClient db = new DatabaseClient();
            var stockInfo = db.GetAllDocuments("stock_info");

            List<Result> results = new List<Result>();
            foreach(BsonDocument info in stockInfo)
            {
                BsonDocument stockListing = info["StockListing"].AsBsonDocument; 
                BsonDocument sentimentResult = info["SentimentResult"].AsBsonDocument; 
                BsonDocument news = sentimentResult["News"].AsBsonDocument;
                try
                {
                    string symbol = stockListing["Symbol"].AsString;
                    string name = stockListing["SecurityName"].AsString;
                    string title = news["Title"].AsString;
                    string descr = news["Description"].AsString;

                    List<string> entity = new List<string>();
                    List<string> type = new List<string>();

                    BsonArray entities = sentimentResult["Entities"].AsBsonArray;
                    foreach (BsonDocument e in entities)
                    {
                        entity.Add(e["Name"].AsString);
                        type.Add(e["Type"].ToString());
                    }

                    results.Add(new Result
                    {
                        StockSymbol = symbol,
                        CompanyName = name,
                        ArticleName = title,
                        ArticleDescription = descr,
                        EntityValue = entity,
                        EntityType = type
                    });
                } catch (Exception ex)
                {
                    Console.WriteLine("Unable to process document:" + info.ToString());
                }
            }

            ViewBag.Results = results;

            return View();
        }
    }
}