using System;
using System.Collections.Generic;
using System.Text;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace SATBot_v0.Models
{
    class MongoConnection
    {
        //https://www.mongodb.com/blog/post/quick-start-c-sharp-and-mongodb--creating-documents
        //https://www.mongodb.com/blog/post/quick-start-c-and-mongodb--read-operations

        private string connectionString;
        private string databaseName;

        public MongoConnection()
        {
            connectionString = Resource.DBConnectionString;
            databaseName = "SATbot";

            var client = new MongoClient(connectionString);
            var database = client.GetDatabase("SATbot");

        }

        public MongoClient GetClient()
        {
            var client = new MongoClient(connectionString);
            return client;
        }

        public IMongoDatabase GetDatabase()
        {
            var client = GetClient();
            var database = client.GetDatabase(databaseName);
            return database;
        }

        public IMongoDatabase GetDatabase(string dbName)
        {
            var client = GetClient();
            var database = client.GetDatabase(dbName);
            return database;
        }

        public IMongoCollection<BsonDocument> GetCollection(string dbName, string collectionName)
        {
            var database = GetDatabase(dbName);
            var collection = database.GetCollection<BsonDocument>(collectionName);
            return collection;
        }

        public IMongoCollection<BsonDocument> GetCollection(string collectionName)
        {
            var database = GetDatabase();
            var collection = database.GetCollection<BsonDocument>(collectionName);
            return collection;
        }

        public string InsertDocument(string collectionName, BsonDocument document)
        {
            try
            {
                var collection = GetCollection(collectionName);
                collection.InsertOne(document);
                return "Insert success!";

            } catch (Exception ex)
            {
                return "Exception!: " + ex.StackTrace;
            }
        }
        

        public string InsertDocument(string dbName, string collectionName, BsonDocument document)
        {
            try
            {
                var collection = GetCollection(dbName, collectionName);
                collection.InsertOne(document);
                return "Insert success!";

            } catch (Exception ex)
            {
                return "Exception!: " + ex.StackTrace;
            }
        }

        public BsonDocument GetFirstDocument(string collectionName)
        {
            var database = GetDatabase();
            var collection = database.GetCollection<BsonDocument>(collectionName);

            var doc = collection.Find(new BsonDocument()).FirstOrDefault();

            return doc;
        }

        public BsonDocument GetFirstDocument(string dbName, string collectionName)
        {
            var database = GetDatabase(dbName);
            var collection = database.GetCollection<BsonDocument>(collectionName);

            var doc = collection.Find(new BsonDocument()).FirstOrDefault();

            return doc;
        }

        public List<BsonDocument> GetAllDocuments(string collectionName)
        {
            var database = GetDatabase();
            var collection = database.GetCollection<BsonDocument>(collectionName);

            var doc = collection.Find(new BsonDocument()).ToList();

            return doc;
        }

        public List<BsonDocument> GetAllDocuments(string dbName, string collectionName)
        {
            var database = GetDatabase(dbName);
            var collection = database.GetCollection<BsonDocument>(collectionName);

            var doc = collection.Find(new BsonDocument()).ToList();

            return doc;
        }

        public BsonDocument GetLast(string dbName, string collectionName, string sortCriteria)
        {
            var sort = Builders<BsonDocument>.Sort.Descending(sortCriteria);


            var database = GetDatabase(dbName);
            var collection = database.GetCollection<BsonDocument>(collectionName);
            var docs = collection.Find(new BsonDocument()).Sort(sort);

            var latestDoc = docs.First<BsonDocument>();
            return latestDoc;
        }

        public BsonDocument GetLast(string collectionName, string sortCriteria)
        {
            var sort = Builders<BsonDocument>.Sort.Descending(sortCriteria);


            var database = GetDatabase();
            var collection = database.GetCollection<BsonDocument>(collectionName);
            var docs = collection.Find(new BsonDocument()).Sort(sort);

            var latestDoc = docs.First<BsonDocument>();
            return latestDoc;
        }

        public BsonDocument GetLast(string collectionName)
        {
            var sort = Builders<BsonDocument>.Sort.Descending("_id");


            var database = GetDatabase();
            var collection = database.GetCollection<BsonDocument>(collectionName);
            var docs = collection.Find(new BsonDocument()).Sort(sort);

            var latestDoc = docs.First<BsonDocument>();
            return latestDoc;
        }

        public BsonDocument GetFilter(string collectionName, string filterField, string filterCriteria)
        {
            var filter = Builders<BsonDocument>.Filter.Regex(filterField, new BsonRegularExpression(".*" + filterCriteria + ".*", "i"));
            var database = GetDatabase();
            var collection = database.GetCollection<BsonDocument>(collectionName);
            var results = collection.Find(filter);
            
            foreach(BsonDocument doc in results.ToList())
            {
                Console.WriteLine(doc);
            }

            return null;
        }

        public BsonDocument GetStock(string filterField, string filterCriteria)
        {
            var filter = Builders<BsonDocument>.Filter.Regex(filterField, new BsonRegularExpression(".*" + filterCriteria + ".*", "i"));
            var database = GetDatabase();
            var collection = database.GetCollection<BsonDocument>("stock_listing");
            var results = collection.Find(filter);
            
            foreach(BsonDocument doc in results.ToList())
            {
                var stockListing = BsonSerializer.Deserialize<StockListing>(doc);
                Console.WriteLine(stockListing.Symbol + " " + stockListing.SecurityName);
            }

            return null;
        }



    }
}
