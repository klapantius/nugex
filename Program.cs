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
            var showAllFeeds = Switch("show-all-feeds");
            var includePreRelease = Switch("include-pre-release");

            var knownFeeds = new ConfigReader().ReadSources();
            var feedCrowlers = knownFeeds.Select(feed => new FeedCrawler(feed.Item1, feed.Item2)).ToList();
            Task.WaitAll(feedCrowlers.Select(fc => fc.Search(searchTerm, includePreRelease)).ToArray());
            feedCrowlers.ToList().ForEach(crawler =>
            {
                if (!crawler.Results.Any() && !showAllFeeds) return;
                Console.WriteLine($"{Environment.NewLine}---= {crawler.FeedName} =-------------");
                crawler.Results.ToList().ForEach(item => Console.WriteLine($"{item.Identity.Id} - {item.Identity.Version.ToString()}"));
            });
        }

        static int FindOption(string name)
        {
            var arg = Args
                .LastOrDefault(a => a.Trim(new[] { '/', '-' }).ToLowerInvariant().Equals(name, StringComparison.InvariantCultureIgnoreCase));
            if (arg == default) return -1;
            return Args.IndexOf(arg);
        }

        static string Param(string name)
        {
            var valueIdx = FindOption(name) + 1;
            if (valueIdx == 0 || valueIdx >= Args.Count) return default;
            return Args[valueIdx];
        }
        
        static bool Switch(string name) => FindOption(name) >= 0;

    }
}
