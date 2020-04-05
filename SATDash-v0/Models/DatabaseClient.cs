﻿using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SATDash_v0.Models
{
    public class DatabaseClient
    {
        //https://www.mongodb.com/blog/post/quick-start-c-sharp-and-mongodb--creating-documents
        //https://www.mongodb.com/blog/post/quick-start-c-and-mongodb--read-operations

        private string connectionString;
        private string databaseName;

        public DatabaseClient()
        {
            connectionString = Resource.DBConnectionString;
            databaseName = "SATbot";
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

        //Insert Document Into Collection
        public ObjectId InsertDocument(string collectionName, BsonDocument document)
        {
            try
            {
                var collection = GetCollection(collectionName);
                collection.InsertOne(document);

                // Get inserted doc's _id 
                // @QUESTION: How does this work???
                ObjectId id = document["_id"].AsObjectId;

                return id;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.StackTrace);
            }
        }

        public ObjectId InsertDocument(string dbName, string collectionName, BsonDocument document)
        {
            try
            {
                var collection = GetCollection(dbName, collectionName);
                collection.InsertOne(document);

                // Get inserted doc's _id
                ObjectId id = document["_id"].AsObjectId;

                return id;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.StackTrace);
            }
        }

        //Gets the first document in a collection
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

        //Gets all docs in a collection and returns as BSON doc list
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

        //Get last document by criteria (field name)
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

        //Get last document by _id
        public BsonDocument GetLast(string collectionName)
        {
            var sort = Builders<BsonDocument>.Sort.Descending("_id");

            var database = GetDatabase();
            var collection = database.GetCollection<BsonDocument>(collectionName);
            var docs = collection.Find(new BsonDocument()).Sort(sort);

            var latestDoc = docs.First<BsonDocument>();
            return latestDoc;
        }

        //Search and return list of all results (empty list if no results)
        public List<BsonDocument> GetFilter(string collectionName, string fieldName, string searchKey, bool ignoreCase)
        {
            var filter = ignoreCase ?
                Builders<BsonDocument>.Filter.Regex(fieldName, new BsonRegularExpression(".*" + searchKey + ".*", "i")) :
                Builders<BsonDocument>.Filter.Regex(fieldName, new BsonRegularExpression(".*" + searchKey + ".*"));
            var database = GetDatabase();
            var collection = database.GetCollection<BsonDocument>(collectionName);
            var results = collection.Find(filter);

            return results.ToList();
        }

        public List<BsonDocument> GetFilterEq(string collectionName, string fieldName, string searchKey)
        {
            var database = GetDatabase();
            var collection = database.GetCollection<BsonDocument>(collectionName);
            var filter = Builders<BsonDocument>.Filter.Eq(fieldName, searchKey);

            var results = collection.Find(filter);
            return results.ToList();
        }

        public List<BsonDocument> GetById(string collectionName, ObjectId id)
        {
            var database = GetDatabase();
            var collection = database.GetCollection<BsonDocument>(collectionName);
            var filter = Builders<BsonDocument>.Filter.Eq("_id", id);

            var results = collection.Find(filter);
            return results.ToList();
        }
    }
}