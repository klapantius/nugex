using System.Collections.Generic;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using nugex.cmdline;
using nugex.utils;
using System.Collections.Concurrent;

namespace nugex
{
    partial class Program
    {
        private static void Search()
        {
            var searchTerm = CmdLine.Parser.GetParam(_SEARCH_TERM_);
            if (string.IsNullOrWhiteSpace(searchTerm)) throw new Exception($"please use the --name parameter to specify a search term");
            var versionSpec = CmdLine.Parser.GetParam(_VSPEC_);
            var showAllFeeds = CmdLine.Parser.GetSwitch(_ALL_FEEDS_);

            Search(searchTerm, versionSpec, showAllFeeds);
        }

        private static void Search(string packageName, string versionSpec, bool showAllFeeds = false)
        {
            ILogger logger = NullLogger.Instance;
            CancellationToken cancellationToken = CancellationToken.None;

            var knownFeeds = new ConfigReader().ReadSources();
            var feedCrawlers = knownFeeds.Select(feed => new FeedWorker(feed.Item1, feed.Item2)).ToList();
            var findings = new ConcurrentDictionary<string, IEnumerable<FeedWorker.SearchResult>>();
            Task.WaitAll(feedCrawlers.Select(async (fc) =>
            {
                findings[fc.FeedName] = await fc.Search(packageName, versionSpec, includePreRelease: true);
            }).ToArray());
            foreach (var finding in findings)
            {
                var feedName = finding.Key;
                var packages = finding.Value;
                if (packages.Any() || showAllFeeds) Console.WriteLine($"{Environment.NewLine}---= {feedName} =-------------");
                foreach (var package in packages.GroupBy(p => p.PackageData.Identity.Id))
                {
                    Console.Write($"{package.Key}: ");
                    Console.WriteLine($"[{string.Join(", ", package.ToList().Select(vi => vi.VersionInfo.Version.ToString()))}]");
                }
            };
        }

    }
}