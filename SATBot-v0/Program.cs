using System;
using Google.Protobuf;
using Google.Rpc.Context;
using MongoDB.Bson;
using SATBot_v0.Models;

namespace SATBot_v0
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {

                //DB TESTING
                var conn = new MongoConnection();
                var testDoc = new BsonDocument
                {
                    { "student_id", 10006 },
                    { "scores", new BsonArray
                        {
                        new BsonDocument{ {"type", "exam"}, {"score", 88.12334193287023 } },
                        new BsonDocument{ {"type", "quiz"}, {"score", 74.92381029342834 } },
                        new BsonDocument{ {"type", "homework"}, {"score", 89.97929384290324 } },
                        new BsonDocument{ {"type", "homework"}, {"score", 82.12931030513218 } }
                        }
                    },
                    { "class_id", 480}
                };

                //  WRITE
                var result = conn.InsertDocument("SATbot", "news_info", testDoc);
                Console.WriteLine(result);

                //  READ
                var latestDoc = conn.GetLast("SATbot", "news_info", "_id");
                Console.WriteLine(latestDoc);

                //  READ WITH FILTER
                conn.GetStock("SecurityName", "Apple");


                //END DB TESTING


                Console.WriteLine();
                CheckApplicationEnvironment();

                var articles = NewsAPIMethods.RetrieveNewsAsync().Result;
                Console.WriteLine("Retrieved articles maybe");
                Console.WriteLine(articles);

                Console.WriteLine("Top headlines:\n");
                foreach (var article in articles)
                {
                    Console.WriteLine($"Title: {article.Title}\nDescription: {article.Description}\n\n");
                }

                Console.WriteLine("---------------------------------------------------------------------------------------");
                Console.WriteLine("Entity Sentiment Analysis of the first article:\n");
                Console.WriteLine($"Title: {articles[0].Title}");
                Console.WriteLine($"Description: {articles[0].Description}\n");
                Console.WriteLine("Results:\n");

                var entitySentiment = NLPMethods.AnalyzeEntitySentimentAsync(articles[0].Description).Result;

                Console.WriteLine(entitySentiment);

                Console.WriteLine();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        /// <summary>
        /// Check all application environment
        /// </summary>
        public static void CheckApplicationEnvironment()
        {
            Console.WriteLine("Checking NewsAPI environment");
            if (!NewsAPIMethods.IsNewsEnvironmentReady())
            {
                Console.WriteLine("News API Key has not been set up.\n");
            }
            else
            {
                Console.WriteLine("NewsAPI environment has been set up");
            }

            Console.WriteLine("Checking NLP environment");
            if (!NLPMethods.IsNLPEnvironmentReady())
            {
                Console.WriteLine("Google NLP environment variable has not been set up.\n" +
                           "Setting environment variable for Google Application Credential...\n");
                try
                {
                    NLPMethods.SetNLPEnvironmentVariable();
                    Console.WriteLine("Successfully set  up NLP environment variable");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
            else
            {
                Console.WriteLine("NLP environment has been set up");
            }
        }
    }
}
