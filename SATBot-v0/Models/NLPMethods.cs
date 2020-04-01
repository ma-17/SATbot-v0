using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Google.Cloud.Language.V1;
using MongoDB.Bson;
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
        /// The overall document sentiment analysis
        /// </summary>
        /// <param name="text">The content that needs to be analyzed</param>
        /// <returns>Result of the analysis in AnalyzeSentimentResponse object</returns>
        public static async Task<AnalyzeSentimentResponse> AnalyzeSentimentAsync(string text)
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
                throw new Exception("Exception at AnalyzeSentimentAsync!: " + e.Message);
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
        /// Insert entity sentiment result into DB
        /// </summary>
        /// <param name="articleId">ObjectId of the article</param>
        /// <param name="entities">List of entities</param>
        /// <param name="conn">MongoConnection</param>
        /// <returns>ObjectId of the inserted entity sentiment result</returns>
        public static ObjectId InsertEntitySentiment(ObjectId articleId, List<Entity> entities, MongoConnection conn)
        {
            try
            {
                // Convert list of entities -> JSON -> BsonDocument
                var jsonEntities = $"{{ Entities: {JsonConvert.SerializeObject(entities)} }}";
                var bsonEntities = BsonDocument.Parse(jsonEntities);

                //Create Sentiment Bson Doc
                BsonDocument sentimentDoc = new BsonDocument
                {
                    { "NewsId", articleId },
                    { "NewsSentiment", "TBA" }
                };
                sentimentDoc.AddRange(bsonEntities);
                sentimentDoc.Add("AnalyzedAt", DateTime.Now);

                //Insert to DB
                var sentimentDocId = conn.InsertDocument("sentiment_results", sentimentDoc);

                return sentimentDocId;
            }
            catch (Exception e)
            {
                throw new Exception("Exception at NLPMethods InsertEntitySentiment!: " + e.Message);
            }
        }
    }
}
