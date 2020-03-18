﻿using System;
using Google.Rpc.Context;
using SATBot_v0.Models;

namespace SATBot_v0
{
    class Program
    {
        static void Main(string[] args)
        {
            var articleResponse = NewsAPIMethods.RetrieveNewsAsync();

            try
            {
                var articles = articleResponse.Result;

                foreach (var article in articles)
                {
                    Console.WriteLine($"Title: {article.Title}\nDescription: {article.Description}\n");
                }
            }
            catch
            {
                Console.WriteLine("There is no top headlines");
            }
        }
    }
}
