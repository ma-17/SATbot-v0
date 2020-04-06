using Google.Cloud.Language.V1;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public static List<StockListing> RunCorrelation(MongoConnection conn, List<Entity> entities)
        {
            List<StockListing> nameMatch = new List<StockListing>();
            List<StockListing> relatedMatch = new List<StockListing>();

            foreach (Entity entity in entities)
            {
                //Check related entities
                List<StockListing> allMatches = CheckRelatedEntities(conn, entity.Name);

                foreach (StockListing match in allMatches)
                {
                    var lookup = from stock in relatedMatch where stock.Symbol == match.Symbol select stock;
                    if (lookup.Count() == 0)
                    {
                        relatedMatch.Add(match);
                    }
                }

                //Check by organization name
                if (entity.Type == Entity.Types.Type.Organization)
                {
                    nameMatch = GetStocks(conn, "SecurityName", entity.Name, false);
                    if (nameMatch.Count > 0)
                    {
                        //Add/update entity in the related_entities table
                        ProcessAllEntities(conn, entity, nameMatch, entities);
                    }
                }
            }

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
                conn.GetFilterWord("stock_listing", filterField, filterCriteria, false);
            foreach (BsonDocument doc in results)
            {
                var stockListing = BsonSerializer.Deserialize<StockListing>(doc);
                stocks.Add(stockListing);
            }

            //@TODO: Add a field to stock_listing for # of article mentions, so we can track related_entities hit rate by %

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

        public static void ProcessAllEntities(MongoConnection conn, Entity nameMatch, List<StockListing> stocks, List<Entity> entities)
        {
            try {
                foreach (StockListing stock in stocks)
                {
                    foreach (Entity entity in entities)
                    {
                        if (entity.Name != nameMatch.Name)
                        {
                            List<BsonDocument> lookup = conn.GetFilterEq("related_entities", "keyword", entity.Name);

                            if (lookup.Count > 0)
                            {
                                if (lookup.Count > 1) { Console.WriteLine($"Found multiple related_entities entries for '{entity.Name}', please check the database and combine duplicate entries into a single entry."); }

                                BsonDocument relatedEntity = lookup[0];
                                BsonArray companies = relatedEntity["companies"].AsBsonArray;

                                if (relatedEntity.ToString().Contains(stock.Symbol))
                                {
                                    foreach (BsonDocument company in companies)
                                    {
                                        if (company["stockSymbol"].ToString() == stock.Symbol)
                                        {
                                            int occurrences = company["occurrences"].ToInt32() + 1;

                                            // Update occurrences of the company
                                            var objectId = relatedEntity["_id"].AsObjectId;
                                            Dictionary<string, object> conditions = new Dictionary<string, object>
                                            {
                                                { "_id", objectId },
                                                {"companies.stockSymbol", stock.Symbol}
                                            };
                                            conn.UpdateDocument("related_entities", conditions, "companies.$.occurrences", occurrences);
                                        }
                                    }
                                }
                                else
                                {
                                    var newCompany = BuildCompanyBsonDocument(stock.Symbol);

                                    // Add the company into companies of the entity
                                    var objectId = relatedEntity["_id"].AsObjectId;
                                    Dictionary<string, object> conditions = new Dictionary<string, object>
                                    {
                                        {"_id", objectId}
                                    };
                                    conn.AddDocumentToArray("related_entities", conditions, "companies", newCompany);
                                }
                            }
                            else
                            {
                                var newRelatedEntity = BuildRelatedEntityBsonDocument(entity.Name, stock.Symbol);
                                conn.InsertDocument("related_entities", newRelatedEntity);
                            }
                        }
                    }
                }
            } catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine(ex.Message);
            }
        }

        public static BsonDocument BuildCompanyBsonDocument(string stockSymbol)
        {
            BsonDocument company = new BsonDocument();

            company.Add("stockSymbol", stockSymbol);
            company.Add("occurrences", 1);
            company.Add("verified", false);

            return company;
        }

        public static BsonDocument BuildRelatedEntityBsonDocument(string entity, string stockSymbol)
        {
            BsonDocument relatedEntity = new BsonDocument();
            relatedEntity.Add("keyword", entity);

            BsonArray companies = new BsonArray();
            var company = BuildCompanyBsonDocument(stockSymbol);

            companies.Add(company);
            relatedEntity.Add("companies", companies);

            return relatedEntity;
        }
    }
}
