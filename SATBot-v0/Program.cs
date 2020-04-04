using System;
using System.Collections.Generic;
using System.Linq;
using Google.Cloud.Language.V1;
using Google.Protobuf;
using Google.Rpc.Context;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using NewsAPI.Models;
using Newtonsoft.Json;
using SATBot_v0.Models;

namespace SATBot_v0
{
    class Program
    {

        /*
         * @TODO:
         * 
         * - Write News Results Into news_info - DONE! woo!
         * - Write Sentiment Results Into sentiment_results - Done! yayyyyy!
         * - Do Basic Company Lookup (search by sentiment: entity - type = organization) - Done!
         * - Write Company to stock_info - DONE! :D
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
                /*
                 * Program Flow:
                 * =============
                 * 
                 * 1) Grab News
                 * 2) For Each Article:
                 *     3) Write to Database (news_info)
                 *     4) Analyse Sentiment
                 *     5) Write Sentiment to Database (sentiment_results)
                 *     6) Lookup Company - 
                 *          6.1) By Entity Where Type = Organization:
                                 * For each entity,
                                 * If type = organization
                                 * conn.GetStocks("SecurityName", entityValue);
                                 * If list.empty
                                 *      //write that it's empty into the db or ignore
                                 *
                                 * If !list.empty
                                 *      //write into stock_info
                 * 7) Write Company to Database (stock_info)
                 * 
                 */


                //Get MongoDB Connection
                var conn = new MongoConnection();


                /*
                 * DB TESTING

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

                  WRITE - implemented below
                var result = conn.InsertDocument("SATbot", "news_info", testDoc);
                var result2 = conn.InsertDocument("stock_listing", testDoc);
                Console.WriteLine(result);

                  READ
                var latestDoc = conn.GetLast("SATbot", "news_info", "_id");
                var latest2 = conn.GetLast("news_info"); //gets last entry by _id
                var latest3 = conn.GetLast("news_info", "Date Retrieved"); //get last entry by criteria
                Console.WriteLine(latestDoc);

                  READ WITH FILTER
                conn.GetStocks("SecurityName", "Apple");
                conn.GetStocks("SecurityName", "Microsoft");
                conn.GetStocks("SecurityName", "Hathaway");

                END DB TESTING
                */

                //Environment Check
                Console.WriteLine();
                CheckApplicationEnvironment();
                
                Console.WriteLine();
                Console.WriteLine("-------------------------------------------------------------------------------");
                Console.WriteLine();

                //Retrieve Articles
                Console.WriteLine("Retrieving articles...");
                var articles = NewsAPIMethods.RetrieveNewsAsync().Result;

                Console.WriteLine();
                Console.WriteLine("Processing articles...\n");
                //Write Articles to DB
                foreach (var article in articles)
                {
                    // Check if the article is already inserted (skip the insertion if it is)
                    BsonDocument articleBson;
                    var articleId = new ObjectId();

                    //@QUESTION: How does putting a blank object id in the param check whether it's been inserted already?
                    bool isInserted = NewsAPIMethods.IsInserted(article, conn, out articleId);

                    if (!isInserted)
                    {
                        Console.WriteLine();
                        //Insert the article into DB
                        articleBson = NewsAPIMethods.InsertArticle(article, conn);
                        
                        //Null check
                        if (articleBson == null)
                        {
                            throw new Exception("NewsAPIMethods.InsertArticle returned a null object for: " 
                                + article.Title + "\n");
                        }

                        articleId = new ObjectId(articleBson.GetValue("_id", "No id found??").ToString());
                        Console.WriteLine($"Article: {article.Title} is sucessfully inserted at _id: {articleId}");

                        // Perform sentiment analysis
                        Console.WriteLine("Analyzing article sentiment...");
                        var sentimentResponse = NLPMethods.AnalyzeSentimentAsync(articleBson, article);
                        var sentimentResult = sentimentResponse.Result;
                        var sentimentDoc = sentimentResult.SentimentBsonDocument;

                        // Insert sentiment results to DB
                        var sentimentResultId = NLPMethods.InsertSentimentResult(sentimentDoc, conn);
                        Console.WriteLine($"Sucessfully inserted sentiment result of article (_id: {articleId}) at _id: {sentimentResultId}");

                        //Correlate entities and stocks
                        Console.WriteLine("Correlating entities and stocks...");
                        var entities = sentimentResult.Entities;
                        foreach (var entity in entities)
                        {
                            if (entity.Type == Entity.Types.Type.Organization)
                            {
                                Console.WriteLine($"Entity Name: {entity.Name}");

                                //Get stock info by entity's name
                                //var stocks = conn.GetStocks("SecurityName", entity.Name);
                                var stocks = StockCorrelation.GetStocks(conn, "SecurityName", entity.Name);
                                if (stocks.Count > 0)
                                {
                                    foreach (var stock in stocks)
                                    {
                                        Console.WriteLine($"Symbol: {stock.Symbol} | Security Name: {stock.SecurityName} | Company Name: {stock.CompanyName}");

                                        //@TODO: Insert stock info into DB

                                        StockCorrelation stockCorrelation = new StockCorrelation();
                                        stockCorrelation.InitEmpty();

                                        stockCorrelation.SentimentResult = sentimentDoc;     
                                        stockCorrelation.StockListing = stock.ToBsonDocument();

                                        ObjectId stockCorrelationId = new ObjectId();

                                        stockCorrelationId = conn.InsertDocument("stock_info", 
                                            stockCorrelation.GetBsonDocument());

                                        if (stockCorrelationId == null || stockCorrelationId.Equals(""))
                                        {
                                            Console.WriteLine("Stock Correlation: " +
                                                "Insert Document returned empty? "
                                                + stockCorrelationId);
                                        } else
                                        {
                                            Console.WriteLine("Stock Correlation: " +
                                                "Successfully inserted stock correlation into stock_info at id: " +
                                                stockCorrelationId);
                                        }                                        
                                    }
                                }
                                else
                                {
                                    Console.WriteLine("Could not find any related stocks");
                                }
                            }
                        }
                        Console.WriteLine();
                    }
                    else
                    {
                        Console.WriteLine($"Article: {article.Title} already exists with _id: {articleId}");
                    }
                }

                Console.WriteLine();
                Console.WriteLine("-------------------------------------------------------------------------------");
                Console.WriteLine();
                Console.WriteLine("Demo Entity Sentiment: ");
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
                Console.WriteLine(e.StackTrace);
            }
        }

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
