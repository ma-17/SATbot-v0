using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace SATBot_v0.Models
{
    class StockCorrelation
    {
        //@TODO: All member variables that we haven't 'officially retrieved' are currently strings

        public string _id { get; set; }
        public BsonDocument SentimentResult { get; set; }
        public BsonDocument StockListing { get; set; }
        public string Status { get; set; }
        public string Tradeable { get; set; }
        public string CurrentPrice { get; set; }
        public string HistoricalData { get; set; }

        public StockCorrelation() { }

        public void InitEmpty()
        {
            SentimentResult = new BsonDocument();
            StockListing = new BsonDocument();
            Status = "";
            Tradeable = "";
            CurrentPrice = "";
            HistoricalData = "";
        }

        public BsonDocument GetBsonDocument()
        {
            BsonDocument doc = new BsonDocument();

            doc.Add("SentimentResult", SentimentResult);
            doc.Add("StockListing", StockListing);
            doc.Add(new BsonElement("Status", Status));
            doc.Add(new BsonElement("Tradeable", Tradeable));
            doc.Add(new BsonElement("CurrentPrice", CurrentPrice));
            doc.Add(new BsonElement("HistoricalData", HistoricalData));
            doc.Add(new BsonElement("RetrievedAt", DateTime.Now));

            return doc;
        }

        public static List<StockListing> GetStocks(MongoConnection conn, string filterField, string filterCriteria)
        {
            List<StockListing> stocks = new List<StockListing>();

            //Check if criteria is listed in ignore list
            if (IsIgnoredEntity(conn, filterCriteria))
            {
                return stocks;
            }

            //Get correlated stocks - based on organization entity partial match
            List<BsonDocument> results = conn.GetFilter("stock_listing", filterField, filterCriteria, false);
            foreach (BsonDocument doc in results)
            {
                var stockListing = BsonSerializer.Deserialize<StockListing>(doc);
                stocks.Add(stockListing);
            }

            return stocks;
        }

        public static bool IsIgnoredEntity(MongoConnection conn, string searchKey)
        {
            bool ignore = false;

            List<BsonDocument> results = conn.GetFilterEq("ignored_entities", "keyword", searchKey);

            if (results.Count > 0)
            {
                ignore = true;
            }

            return ignore;
        }
    }
}
