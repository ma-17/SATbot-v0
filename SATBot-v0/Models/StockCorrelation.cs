﻿using Google.Cloud.Language.V1;
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

        public static List<StockListing> RunCorrelation(MongoConnection conn, Entity entity)
        {
            List<StockListing> nameMatch = new List<StockListing>();
            List<StockListing> relatedMatch = new List<StockListing>();

            //Check by organization name
            if (entity.Type == Entity.Types.Type.Organization)
            {                
                nameMatch = GetStocks(conn, "SecurityName", entity.Name, false);
            }

            //Check related entities
            relatedMatch = CheckRelatedEntities(conn, entity.Name);

            //Add all stocks to list
            List<StockListing> stocks = new List<StockListing>(nameMatch.Count + relatedMatch.Count);
            stocks.AddRange(nameMatch);
            stocks.AddRange(relatedMatch);

            return stocks;
        }

        public static void InsertToStockInfo(MongoConnection conn, BsonDocument sentimentResult, BsonDocument stockListing, string status, string tradeable, string currentPrice, string historicalData)
        {
            StockCorrelation stockCorrelation = new StockCorrelation();
            stockCorrelation.InitEmpty();

            stockCorrelation.SentimentResult = sentimentResult;
            stockCorrelation.StockListing = stockListing.ToBsonDocument();
            stockCorrelation.Status = status;
            stockCorrelation.Tradeable = tradeable;
            stockCorrelation.CurrentPrice = currentPrice;
            stockCorrelation.HistoricalData = historicalData;

            InsertToStockInfo(conn, stockCorrelation);
        }

        public static void InsertToStockInfo(MongoConnection conn, StockCorrelation correlation)
        {
            ObjectId stockCorrelationId = new ObjectId();
            stockCorrelationId = conn.InsertDocument("stock_info",
                correlation.GetBsonDocument());

            if (stockCorrelationId == null || stockCorrelationId.Equals(""))
            {
                Console.WriteLine("Stock Correlation: " +
                    "Insert Document returned empty? "
                    + stockCorrelationId);
            }
            else
            {
                Console.WriteLine("Stock Correlation: " +
                    "Successfully inserted stock correlation into stock_info at id: " +
                    stockCorrelationId);
            }
        }

        public static List<StockListing> CheckRelatedEntities(MongoConnection conn, string key)
        {
            List<StockListing> stocks = new List<StockListing>();
            List<string> symbols = new List<string>();

            //Get Stock Symbols
            List<BsonDocument> results = conn.GetFilterEq("related_entities", "keyword", key);
            foreach(BsonDocument doc in results)
            {
                var companies = doc["companies"].AsBsonArray;
                foreach(BsonDocument company in companies)
                {
                    symbols.Add(company["stockSymbol"].AsString);
                }
            }

            //Get Stocks
            foreach(string symbol in symbols)
            {
                foreach(StockListing stock in GetStocks(conn, "Symbol", symbol, true))
                {
                    stocks.Add(stock);
                }
            }

            return stocks;
        }

        public static List<StockListing> GetStocks(MongoConnection conn, string filterField, string filterCriteria, bool exact)
        {
            List<StockListing> stocks = new List<StockListing>();

            //Check if criteria is listed in ignore list
            if (IsIgnoredEntity(conn, filterCriteria))
            {
                return stocks;
            }

            //Get correlated stocks - based on organization entity partial match
            List<BsonDocument> results = exact ? 
                conn.GetFilterEq("stock_listing", filterField, filterCriteria) :
                conn.GetFilter("stock_listing", filterField, filterCriteria, false);
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
