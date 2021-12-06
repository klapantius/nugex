using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;

namespace nugex
{
    class Program
    {
        private static List<string> Args;

        static void Main(string[] args)
        {
            Args = new List<string>(args);
            ILogger logger = NullLogger.Instance;
            CancellationToken cancellationToken = CancellationToken.None;

            var searchTerm = Param("name");
            if (string.IsNullOrWhiteSpace(searchTerm)) throw new Exception($"please use the --name parameter to specify a search term");

            var knownFeeds = new ConfigReader().ReadSources();
            var feedCrowlers = knownFeeds.Select(feed => new FeedCrawler(feed.Item1, feed.Item2)).ToList();
            Task.WaitAll(feedCrowlers.Select(fc => fc.Search(searchTerm)).ToArray());
            feedCrowlers.ToList().ForEach(crawler =>
            {
                Console.WriteLine($"{Environment.NewLine}---= {crawler.FeedName} =-------------");
                crawler.Results.ToList().ForEach(item => Console.WriteLine($"{item.Identity.Id} - {item.Identity.Version.ToString()}"));
            });
        }

        static string Param(string name)
        {
            var arg = Args
                .LastOrDefault(a => a.Trim(new[] { '/', '-' }).ToLowerInvariant().Equals(name, StringComparison.InvariantCultureIgnoreCase));
            if (arg == default) return default;
            var idx = Args.IndexOf(arg) + 1;
            if (idx >= Args.Count) return default;
            return Args[idx];
        }
    }
}
