using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using nugex.cmdline;
using nugex.utils;

namespace nugex
{
    partial class Program
    {
        private static void Search()
        {
            var searchTerm = CmdLine.Parser.GetParam(_SEARCH_TERM_);
            if (string.IsNullOrWhiteSpace(searchTerm)) throw new Exception($"please use the --name parameter to specify a search term");
            var showAllFeeds = CmdLine.Parser.GetSwitch(_ALL_FEEDS_);
            var includePreRelease = CmdLine.Parser.GetSwitch(_PREREL_);

            Search(searchTerm, showAllFeeds, includePreRelease);
        }

        private static void Search(string searchTerm, bool showAllFeeds, bool includePreRelease)
        {
            ILogger logger = NullLogger.Instance;
            CancellationToken cancellationToken = CancellationToken.None;

            var knownFeeds = new ConfigReader().ReadSources();
            var feedCrawlers = knownFeeds.Select(feed => new FeedWorker(feed.Item1, feed.Item2)).ToList();
            Task.WaitAll(feedCrawlers.Select(fc => fc.Search(searchTerm, includePreRelease)).ToArray());
            feedCrawlers.ToList().ForEach(crawler =>
            {
                if (!crawler.Results.Any() && !showAllFeeds) return;
                Console.WriteLine($"{Environment.NewLine}---= {crawler.FeedName} =-------------");
                crawler.Results.ToList().ForEach(item => Console.WriteLine($"{item.Identity.Id} - {item.Identity.Version.ToString()}"));
            });
        }

        private static void SearchVersions()
        {
            var searchTerm = CmdLine.Parser.GetParam(_SEARCH_TERM_);
            if (string.IsNullOrWhiteSpace(searchTerm)) throw new Exception($"please use the --{_SEARCH_TERM_} parameter to specify a search term");
            var versionSpec = CmdLine.Parser.GetParam(_VSPEC_);
            if (string.IsNullOrWhiteSpace(versionSpec)) throw new Exception($"please use the --{_VSPEC_} parameter to specify a search term");

            SearchVersions(searchTerm, versionSpec);
        }

        private static void SearchVersions(string packageName, string versionSpec)
        {
            ILogger logger = NullLogger.Instance;
            CancellationToken cancellationToken = CancellationToken.None;

            var knownFeeds = new ConfigReader().ReadSources();
            var feedCrawlers = knownFeeds.Select(feed => new FeedWorker(feed.Item1, feed.Item2)).ToList();
            Task.WaitAll(feedCrawlers.Select(fc => fc.Search(packageName, includePreRelease: true)).ToArray());
            feedCrawlers.ToList().ForEach(crawler =>
            {
                if (!crawler.Results.Any()) return;
                var first = true;
                foreach (var package in crawler.Results)
                {
                    var versions = crawler.FindVersions(package, versionSpec).Result;
                    if (first) Console.WriteLine($"{Environment.NewLine}---= {crawler.FeedName} =-------------");
                    if (!versions.Any()) Console.WriteLine($"{package.Identity.Id} - package exists, but no matching version could be identified");
                    else Console.WriteLine($"{package.Identity.Id}:");
                    versions.ToList().ForEach(vi => Console.WriteLine($"  {vi.Version.ToString()}"));
                    first = false;
                }
            });
        }

    }
}