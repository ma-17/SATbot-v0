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
                    string articleId = news["_id"].ToString();
                    string symbol = stockListing["Symbol"].AsString;
                    string name = stockListing["SecurityName"].AsString;
                    string title = news["Title"].AsString;
                    string descr = news["Description"].AsString;
                    BsonArray entitiesArray = sentimentResult["Entities"].AsBsonArray;
                    
                    var getDoc = from result in results
                                 where result.ArticleId == articleId
                                 select result;
                    Result r;
                    if (getDoc.Count() > 0)
                    {
                        r = getDoc.First();
                    } else
                    {
                        r = new Result();
                        r.ArticleId = articleId;
                        r.ArticleName = title;
                        r.ArticleDescription = descr;
                    }
                    
                    r.StockSymbols.Add(symbol);
                    r.CompanyNames.Add(name);

                    foreach (BsonDocument e in entitiesArray)
                    {
                        r.EntityValues.Add(e["Name"].AsString);
                        r.EntityTypes.Add(e["Type"].ToString());
                    }

                    if (getDoc.Count() == 0)
                    {
                        results.Add(r);
                    }

                } catch (Exception ex)
                {
                    Console.WriteLine("Unable to process document:" + info.ToString());
                    Console.WriteLine(ex.Message);
                }
            }

            ViewBag.Results = results;

            return View();
        }
    }
}