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
        /// <returns>True if the news api key is already set up</returns>
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
                From = DateTime.Now.AddDays(-1),
                Language = Languages.EN,
                PageSize = 20
            });

            if (response.Status != Statuses.Ok)
            {
                throw new Exception(response.Error.Message);
            }
            return response.Articles;
        }

        /// <summary>
        /// Check if the article is inserted into the database
        /// </summary>
        /// <param name="article">The article needs to be checked</param>
        /// <param name="conn">MongoConnection</param>
        /// <returns>True if the article is already inserted</returns>
        public static bool IsInserted(Article article, MongoConnection conn, out ObjectId articleId)
        {
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
                            var publishedAtResult = result.GetValue("PublishedAt");

                            if (publishedAt == publishedAtResult)
                            {
                                //Console.WriteLine("Article: \"" + title + "\" exists in db with id: " + result.GetValue("_id"));
                                articleId = result.GetValue("_id").AsObjectId;
                                return true;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            articleId = new ObjectId();
            return false;
        }

        /// <summary>
        /// Insert article into db
        /// </summary>
        /// <param name="article">The article</param>
        /// <param name="conn">MongoConnection</param>
        /// <returns>ObjectId of the inserted article</returns>
        public static ObjectId InsertArticle(Article article, MongoConnection conn)
        {
            //Convert to Bson
            var doc = article.ToBsonDocument();

            //Add Retrieved At
            DateTime now = DateTime.Now;
            doc.Add(new BsonElement("RetrievedAt", now));

            try
            {
                //Insert to DB
                ObjectId id = conn.InsertDocument("news_info", doc);

                //@TODO: TO BE TESTED - not 100% sure it's working properly.
                //Retrieve last item and return _id
                /*List<BsonDocument> lastArticle = conn.GetFilter("news_info", "RetrievedAt", now.ToString());
                if (lastArticle.Count > 0)
                {
                    return lastArticle[0].GetValue("_id").ToString();
                } else
                {
                    return conn.GetLast("news_info").GetValue("_id").ToString();
                }*/
                return id;
            } catch (Exception ex)
            {
                throw new Exception("Exception at NewsAPIMethods InsertArticle!: " + ex.Message);
            }
        }
    }
}
