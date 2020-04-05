using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SATDash_v0.Models
{
    public class Result
    {
        public string ArticleName { get; set; }
        public string ArticleDescription { get; set; }
        public List<string> EntityType { get; set; }
        public List<string> EntityValue { get; set; }
        public string StockSymbol { get; set; }
        public string CompanyName { get; set; }
    }
}