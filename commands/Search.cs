using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;

namespace nugex
{
    partial class Program
    {
        private static void Search()
        {
            ILogger logger = NullLogger.Instance;
            CancellationToken cancellationToken = CancellationToken.None;

            var searchTerm = CmdLine.GetParam("name");
            if (string.IsNullOrWhiteSpace(searchTerm)) throw new Exception($"please use the --name parameter to specify a search term");
            var showAllFeeds = CmdLine.GetSwitch("show-all-feeds");
            var includePreRelease = CmdLine.GetSwitch("include-pre-release");

            var knownFeeds = new ConfigReader().ReadSources();
            var feedCrawlers = knownFeeds.Select(feed => new FeedCrawler(feed.Item1, feed.Item2)).ToList();
            Task.WaitAll(feedCrawlers.Select(fc => fc.Search(searchTerm, includePreRelease)).ToArray());
            feedCrawlers.ToList().ForEach(crawler =>
            {
                if (!crawler.Results.Any() && !showAllFeeds) return;
                Console.WriteLine($"{Environment.NewLine}---= {crawler.FeedName} =-------------");
                crawler.Results.ToList().ForEach(item => Console.WriteLine($"{item.Identity.Id} - {item.Identity.Version.ToString()}"));
            });
        }
    }
}