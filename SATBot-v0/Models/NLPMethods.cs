using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Google.Cloud.Language.V1;

namespace SATBot_v0.Models
{
    public class NLPMethods
    {
        /// <summary>
        /// Check if the environment variable has been set up for NLP
        /// </summary>
        /// <returns>Return True if the environment variable is already set up. Otherwise, return False</returns>
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
                    throw new Exception("Your environment variable path has not been set up!\n" +
                                        "Please set up the path of Google NLP Credential json file in Resource.resx file.");
                }
                Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", Resource.NLP_API_Key_Path);
            }

            // Double-check the environment variable
            if (!IsNLPEnvironmentReady())
            {
                throw new Exception("Your credential for Google NLP has not been set up!");
            }
        }

        /// <summary>
        /// The overall document sentiment analysis
        /// </summary>
        /// <param name="text">Serves as the content that needs to be analyzed</param>
        /// <returns>Result of the analysis in AnalyzeSentimentResponse object</returns>
        public static async Task<AnalyzeSentimentResponse> AnalyzeSentimentAsync(string text)
        {
            LanguageServiceClient client = LanguageServiceClient.Create();
            Document document = Document.FromPlainText(text);
            AnalyzeSentimentResponse response = await client.AnalyzeSentimentAsync(document);

            return response;
        }

        /// <summary>
        /// The sentiment analysis of each and every entity in the text
        /// </summary>
        /// <param name="text">Serves as the content that needs to be analyzed</param>
        /// <returns>Result of the analysis in AnalyzeEntitySentimentResponse object</returns>
        public static async Task<AnalyzeEntitySentimentResponse> AnalyzeEntitySentimentAsync(string text)
        {
            LanguageServiceClient client = LanguageServiceClient.Create();
            Document document = Document.FromPlainText(text);
            AnalyzeEntitySentimentResponse response = await client.AnalyzeEntitySentimentAsync(document);

            return response;
        }
    }
}
