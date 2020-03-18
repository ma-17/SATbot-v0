using System;
using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using NewsAPI;
using NewsAPI.Constants;
using NewsAPI.Models;

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

            var response = await newsApiClient.GetTopHeadlinesAsync(new TopHeadlinesRequest()
            {
                Language = Languages.EN,
                PageSize = 20
            });

            if (response.Status != Statuses.Ok)
            {
                throw new Exception(response.Error.Message);
            }
            return response.Articles;
        }
    }
}
