using System;
using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using NewsAPI;
using NewsAPI.Constants;
using NewsAPI.Models;
using MongoDB.Bson;

namespace SATBot_v0.Models
{
    public static class NewsAPIMethods
    {
        /// <summary>
        /// Check if the news api key has been set up
        /// </summary>
        /// <returns>Return True if the news api key is already set up. Otherwise, return False</returns>
        public static bool IsNewsEnvironmentReady()
        {
            if (string.IsNullOrEmpty(Resource.News_API_Key))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Retrieves the first 20 top headlines from NewsAPI
        /// </summary>
        /// <returns>List of articles</returns>
        public static async Task<List<Article>> RetrieveNewsAsync()
        {
            var newsApiClient = new NewsApiClient(Resource.News_API_Key);

            var response = await newsApiClient.GetEverythingAsync(new EverythingRequest()
            {
                Q = "*",
                From = new DateTime(2020, 03, 30),
                Language = Languages.EN,
                PageSize = 20
            });

            if (response.Status != Statuses.Ok)
            {
                throw new Exception(response.Error.Message);
            }
            return response.Articles;
        }

        public static string InsertToDB(Article article, MongoConnection conn)
        {
            //Convert to Bson
            var doc = article.ToBsonDocument();

            //Add Retrieved At
            DateTime now = DateTime.Now;
            doc.Add(new BsonElement("RetrievedAt", now));

            string title = article.Title;
            DateTime? publishedAt = article.PublishedAt;

            try
            {
                List<BsonDocument> results = conn.GetFilter("news_info", "Title", title);

                if (results.Count > 0)
                {
                    if (publishedAt != null)
                    {
                        foreach (BsonDocument result in results)
                        {
                            var publishedAtCurrent = new BsonDateTime((DateTime)publishedAt);
                            var publishedAtResult = result.GetValue("PublishedAt");

                            if (publishedAt == publishedAtResult)
                            {
                                return "Article: \"" + title + "\" exists in db with id: " + result.GetValue("_id");
                            }
                        }
                    }                    
                }

                //Insert to DB
                conn.InsertDocument("news_info", doc);

                //Retrieve last item and return _id
                List<BsonDocument> lastArticle = conn.GetFilter("news_info", "RetrievedAt", now.ToString());
                if (lastArticle.Count > 0)
                {
                    return lastArticle[0].GetValue("_id").ToString();
                }

            } catch (Exception ex)
            {
                return "Exception at NewsAPIMethods InsertToFB!: " + ex.Message;
            }

            return "Successfully added article to db";
        }
    }
}
