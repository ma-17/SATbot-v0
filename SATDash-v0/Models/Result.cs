using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SATDash_v0.Models
{
    public class Result
    {
        public ObjectId ArticleId { get; set; }
        public string ArticleName { get; set; }
        public string ArticleDescription { get; set; }
        public List<string> EntityTypes { get; set; }
        public List<string> EntityValues { get; set; }
        public List<string> StockSymbols { get; set; }
        public List<string> CompanyNames { get; set; }

        public Result()
        {
            EntityTypes = new List<string>();
            EntityValues = new List<string>();
            StockSymbols = new List<string>();
            CompanyNames = new List<string>();
        }
    }
}