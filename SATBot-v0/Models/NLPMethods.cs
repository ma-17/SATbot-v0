using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Cloud.Language.V1;
using Grpc.Core;
using MongoDB.Bson;
using NewsAPI.Models;
using Newtonsoft.Json;

namespace SATBot_v0.Models
{
    public class NLPMethods
    {
        /// <summary>
        /// Check if the environment variable has been set up for NLP
        /// </summary>
        /// <returns>True if the environment variable is already set up</returns>
        public static bool IsNLPEnvironmentReady()
        {
            string variable = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");

            if (string.IsNullOrEmpty(variable))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Set up the Environment Variable (Google_Application_Credentials) for NLP
        /// </summary>
        public static void SetNLPEnvironmentVariable()
        {
            // Check whether the environment variable exists.
            // If necessary, create it.
            if (!IsNLPEnvironmentReady())
            {
                if (string.IsNullOrEmpty(Resource.NLP_API_Key_Path))
                {
                    throw new Exception("Exception!: Your environment variable path has not been set up!\n" +
                                        "Please set up the path of Google NLP Credential json file in Resource.resx file.");
                }
                Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", Resource.NLP_API_Key_Path);
            }

            // Double-check the environment variable
            if (!IsNLPEnvironmentReady())
            {
                throw new Exception("Exception!: Your credential for Google NLP has not been set up.");
            }
        }

        /// <summary>
        /// Analyze the article sentiment
        /// </summary>
        /// <param name="articleId">ObjectId of the article</param>
        /// <param name="article">The article</param>
        /// <returns>A tuple contains sentiment result in BsonDocument and list of entities</returns>
        public static async Task<(BsonDocument SentimentBsonDocument, List<Entity> Entities)> AnalyzeSentimentAsync(BsonDocument articleBson, Article article)
        {
            //Get overall sentiment
            Console.WriteLine("Performing overall sentiment analysis...");
            var overallSentimentResponse = await NLPMethods.AnalyzeOverallSentimentAsync(article.Description);
            var overallSentiment = overallSentimentResponse.DocumentSentiment;

            // Get text categories
            Console.WriteLine("Classifying article...");
            var categories = await ClassifyArticleCategoriesAsync(article.Description);

            //Get Sentiment Entities
            Console.WriteLine("Performing entity sentiment analysis...");
            var entitySentiment = await NLPMethods.AnalyzeEntitySentimentAsync(article.Description);
            var entities = entitySentiment.Entities.ToList();

            // Build a complete sentiment result (overall sentiment and entity sentiment)
            BsonDocument sentimentResult = NLPMethods.BuildSentimentBsonDocument(articleBson, overallSentiment, entities, categories);

            return (sentimentResult, entities);
        }

        /// <summary>
        /// The overall document sentiment analysis
        /// </summary>
        /// <param name="text">The content that needs to be analyzed</param>
        /// <returns>Result of the analysis in AnalyzeSentimentResponse object</returns>
        public static async Task<AnalyzeSentimentResponse> AnalyzeOverallSentimentAsync(string text)
        {
            try
            {
                LanguageServiceClient client = LanguageServiceClient.Create();
                Document document = Document.FromPlainText(text);
                AnalyzeSentimentResponse response = await client.AnalyzeSentimentAsync(document);

                return response;
            }
            catch (Exception e)
            {
                throw new Exception("Exception at AnalyzeOverallSentimentAsync!: " + e.Message);
            }
        }

        /// <summary>
        /// The sentiment analysis of each and every entity in the text
        /// </summary>
        /// <param name="text">The content that needs to be analyzed</param>
        /// <returns>Result of the analysis in AnalyzeEntitySentimentResponse object</returns>
        public static async Task<AnalyzeEntitySentimentResponse> AnalyzeEntitySentimentAsync(string text)
        {
            try
            {
                LanguageServiceClient client = LanguageServiceClient.Create();
                Document document = Document.FromPlainText(text);
                AnalyzeEntitySentimentResponse response = await client.AnalyzeEntitySentimentAsync(document);

                return response;
            }
            catch (Exception e)
            {
                throw  new Exception("Exception at AnalyzeEntitySentimentAsync!: " + e.Message);
            }
        }

        /// <summary>
        /// Retrieve test categories
        /// </summary>
        /// <param name="text">The content that needs to be analyzed</param>
        /// <returns>List of categories of the content</returns>
        public static async Task<List<ClassificationCategory>> ClassifyArticleCategoriesAsync(string text)
        {
            try
            {
                var client = LanguageServiceClient.Create();
                var response = await client.ClassifyTextAsync(new Document()
                {
                    Content = text,
                    Type = Document.Types.Type.PlainText
                });

                var categories = response.Categories.ToList();

                return categories;
            }
            catch (Exception  e)
            {
                throw new Exception("Exception at ClassifyArticleCategoriesAsync!: " + e.Message);
            }
        }

        /// <summary>
        /// Build entity sentiment result in bson document format
        /// </summary>
        /// <param name="articleId">ObjectId of the article</param>
        /// <param name="entities">List of entities</param>
        /// <returns>Entity sentiment result in BsonDocument</returns>
        public static BsonDocument BuildSentimentBsonDocument(BsonDocument article, Sentiment overallSentiment, List<Entity> entities, List<ClassificationCategory> categories)
        {
            // Convert overall sentiment to BsonDocument
            var bsonOverallSentiment = overallSentiment.ToBsonDocument();

            // Convert list of entities -> JSON -> BsonDocument
            var jsonEntities = $"{{ Entities: {JsonConvert.SerializeObject(entities)} }}";
            var bsonEntities = BsonDocument.Parse(jsonEntities);

            // Convert list of categories -> JSON -> BsonDocument
            var jsonCategories = $"{{ Categories: {JsonConvert.SerializeObject(categories)} }}";
            var bsonCategories = BsonDocument.Parse(jsonCategories);

            //Create Sentiment Bson Doc
            BsonDocument sentimentDoc = new BsonDocument
            {
                { "News", article },
                { "OverallSentiment", bsonOverallSentiment },
            };
            sentimentDoc.AddRange(bsonEntities);
            sentimentDoc.AddRange(bsonCategories);
            sentimentDoc.Add("AnalyzedAt", DateTime.Now);

            return sentimentDoc;
        }

        /// <summary>
        /// Insert entity sentiment result into DB
        /// </summary>
        /// <param name="entitySentimentBsonDocument">Entity sentiment result in BsonDocument</param>
        /// <param name="conn">MongoConnection</param>
        /// <returns>ObjectId of the inserted entity sentiment result</returns>
        public static ObjectId InsertSentimentResult(BsonDocument sentimentBsonDocument, MongoConnection conn)
        {
            try
            {
                //Insert to DB
                var sentimentDocId = conn.InsertDocument("sentiment_results", sentimentBsonDocument);

                return sentimentDocId;
            }
            catch (Exception e)
            {
                throw new Exception("Exception at NLPMethods InsertEntitySentiment!: " + e.Message);
            }
        }
    }
}
