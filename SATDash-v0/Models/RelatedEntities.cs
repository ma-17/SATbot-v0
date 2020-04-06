using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SATDash_v0.Models
{
    public class RelatedEntities
    {
        public string Keyword { get; set; }
        public List<string> StockSymbols { get; set; }
        public List<int> Occurrences { get; set; }
        public List<bool> Verified { get; set; }

        public RelatedEntities()
        {
            StockSymbols = new List<string>();
            Occurrences = new List<int>();
            Verified = new List<bool>();
        }
    }
}