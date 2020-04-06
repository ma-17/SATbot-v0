using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MongoDB.Bson;
using SATDash_v0.Models;

namespace SATDash_v0.Controllers
{
    public class RelatedEntitiesController : Controller
    {
        // GET: RelatedEntities
        public ActionResult Index()
        {
            DatabaseClient db = new DatabaseClient();
            var REInfo = db.GetAllDocuments("related_entities");

            List<RelatedEntities> results = new List<RelatedEntities>();

            foreach (BsonDocument info in REInfo)
            {
                RelatedEntities relatedEntities = new RelatedEntities();
                try
                {
                    relatedEntities.Keyword = info["keyword"].AsString;
                    BsonArray companies = info["companies"].AsBsonArray;
                    foreach(BsonDocument c in companies)
                    {
                        relatedEntities.StockSymbols.Add(c["stockSymbol"].AsString);
                        relatedEntities.Occurrences.Add(c["occurrences"].AsInt32);
                        relatedEntities.Verified.Add(c["verified"].AsBoolean);
                    }
                } catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }

                results.Add(relatedEntities);
            }

            ViewBag.Results = results;
            return View();
        }
    }
}