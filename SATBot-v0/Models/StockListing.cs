using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace SATBot_v0.Models
{
    public class StockListing
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string _id { get; set; }
        public string Symbol { get; set; }
        public string SecurityName { get; set; }
        public string MarketCategory { get; set; }
        public string TestIssue { get; set; }
        public string FinancialStatus { get; set; }
        public string RoundLotSize { get; set; }
        public string ETF { get; set; }
        public string NextShares { get; set; }
        public string Exchange { get; set; }
    }
}
