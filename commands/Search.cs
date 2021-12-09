using System.Collections.Generic;
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
            var findings = new Dictionary<string, IEnumerable<FeedWorker.SearchResult>>();
            Task.WaitAll(feedCrawlers.Select(async (fc) => findings[fc.FeedName] = await fc.Search(searchTerm, null, includePreRelease)).ToArray());
            foreach (var finding in findings)
            {
                var feedName = finding.Key;
                var packages = finding.Value;
                if (!packages.Any() && !showAllFeeds) continue;
                Console.WriteLine($"{Environment.NewLine}---= {feedName} =-------------");
                packages.ToList().ForEach(item => Console.WriteLine($"{item.PackageData?.Identity.Id ?? "oups"} - {item.VersionInfo?.Version.ToString() ?? "pups"}"));
            }
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
            var findings = new Dictionary<string, IEnumerable<FeedWorker.SearchResult>>();
            Task.WaitAll(feedCrawlers.Select(async (fc) =>
            {
                // todo: use ConcurrentDictionary here
                findings[fc.FeedName] = await fc.Search(packageName, versionSpec, includePreRelease: true);
            }).ToArray());
            foreach (var finding in findings)
            {
                var feedName = finding.Key;
                var packages = finding.Value;
                var first = true;
                foreach (var package in packages.GroupBy(p => p.PackageData.Identity.Id))
                {
                    if (first) Console.WriteLine($"{Environment.NewLine}---= {feedName} =-------------");
                    // if (!package.Any()) Console.WriteLine($"{package.Key} - package exists, but no matching version could be identified");
                    else Console.WriteLine($"{package.Key}:");
                    package.ToList().ForEach(vi => Console.WriteLine($"  {vi.VersionInfo.Version.ToString()}"));
                    first = false;
                }
            };
        }

    }
}