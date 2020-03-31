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
                conn.GetStocks("SecurityName", "Apple");

                //END DB TESTING
                
                
                
                Console.WriteLine($"{CheckApplicationEnvironment()}\n");

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
        public static string CheckApplicationEnvironment()
        {
            string message = "";

            if (!NewsAPIMethods.IsNewsEnvironmentReady())
            {
                message += "News API Key has not been set up.\n";
            }

            if (!NLPMethods.IsNLPEnvironmentReady())
            {
                message += "Google NLP environment variable has not been set up.\n" +
                           "Setting environment variable for Google Application Credential...\n";
                try
                {
                    NLPMethods.SetNLPEnvironmentVariable();
                }
                catch (Exception e)
                {
                    message += e.Message;
                }
            }

            if (message.Equals(""))
            {
                message = "Application environment is already set up!";
            }
            else
            {
                throw new Exception(message);
            }

            return message;
        }
    }
}
