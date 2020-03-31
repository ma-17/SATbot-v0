using System;
using Google.Protobuf;
using Google.Rpc.Context;
using MongoDB.Bson;
using SATBot_v0.Models;

namespace SATBot_v0
{
    class Program
    {

        /*
         * @TODO:
         * 
         * - Write News Results Into news_info
         * - Write Sentiment Results Into sentiment_results
         * - Do Basic Company Lookup (search by sentiment: entity - type = organization)
         * - Write Company to stock_info
         * 
         * For Demo/Testing:
         * - Make sure 'live news updates' works, but for the actual demo/functionality, get all news by time period (Ex. last 24hrs)
         * 
         * Additional Considerations:
         * - Companies may trade under different names (ex. Google - Alphabet)
         * - Logging the industry? Possibly (consider: adding NLP category to stock_listing) - will have to figure out update queries for Mongo
         * - Associating other entity results with a stock (ex. tba)
         * 
         */

        static void Main(string[] args)
        {
            try
            {
                //Get MongoDB Connection
                var conn = new MongoConnection();


                //DB TESTING

                var testDoc = new BsonDocument
                {
                    { "student_id", 10011 },
                    { "scores", new BsonArray
                        {
                        new BsonDocument{ {"type", "exam"}, {"score", 88.12334193287023 } },
                        new BsonDocument{ {"type", "quiz"}, {"score", 74.92381029342834 } },
                        new BsonDocument{ {"type", "homework"}, {"score", 89.97929384290324 } },
                        new BsonDocument{ {"type", "homework"}, {"score", 82.12931030513218 } },
                        new BsonDocument{ {"timestamp", DateTime.Now.ToString() } }
                        }
                    },
                    { "class_id", 480 }
                };

                //  WRITE - implemented below
                //var result = conn.InsertDocument("SATbot", "news_info", testDoc);
                //var result2 = conn.InsertDocument("stock_listing", testDoc);
                //Console.WriteLine(result);

                //  READ
                //var latestDoc = conn.GetLast("SATbot", "news_info", "_id");
                //var latest2 = conn.GetLast("news_info"); //gets last entry by _id
                //var latest3 = conn.GetLast("news_info", "Date Retrieved"); //get last entry by criteria
                //Console.WriteLine(latestDoc);

                //  READ WITH FILTER
                conn.GetStocks("SecurityName", "Apple");
                conn.GetStocks("SecurityName", "Microsoft");

                //END DB TESTING

                //Environment Check
                Console.WriteLine();
                CheckApplicationEnvironment();
                
                //Retrieve Articles
                var articles = NewsAPIMethods.RetrieveNewsAsync().Result;
                Console.WriteLine("Retrieved articles maybe");
                Console.WriteLine(articles);

                Console.WriteLine("Articles:\n");

                //Write Articles to DB
                foreach (var article in articles)
                {
                    //Insert to DB
                    string result = NewsAPIMethods.InsertToDB(article, conn);
                    Console.WriteLine(result);
                    Console.WriteLine();
                }


                Console.WriteLine("---------------------------------------------------------------------------------------");
                Console.WriteLine("Entity Sentiment Analysis of the first article:\n");
                Console.WriteLine($"Title: {articles[0].Title}");
                Console.WriteLine($"Description: {articles[0].Description}\n");
                Console.WriteLine("Results:\n");

                var entitySentiment = NLPMethods.AnalyzeEntitySentimentAsync(articles[0].Description).Result;

                Console.WriteLine(entitySentiment);

                /*
                 * For each entity,
                 * If type = organization
                 * conn.GetStocks("SecurityName", entityValue);
                 * If list.empty
                 *      //write that it's empty into the db or ignore
                 *
                 * If !list.empty
                 *      //write into stock_info
                 */

                /*
                 * Test/Sample Article:
                 * Title: Google and Microsoft are working to make web forms more touch-friendly
                   Description: Google and Microsoft have redesigned native form controls -- buttons and various input elements you see on web forms -- to look more harmonious and be more touch-friendly. They spent the past year working together to design a new theme and make built-in form .
                 */

                
                var entitySentimentTest = NLPMethods.AnalyzeEntitySentimentAsync("Google and Microsoft have redesigned native form controls -- buttons and various input elements you see on web forms -- to look more harmonious and be more touch-friendly. They spent the past year working together to design a new theme and make built-in form.").Result;
                Console.WriteLine("Google and Microsoft are working to make web forms more touch-friendly");
                Console.WriteLine(entitySentimentTest);


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
