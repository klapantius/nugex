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

            var findings = Search(searchTerm, versionSpec);
            var w = findings.Max(f => f.PackageData.Identity.Id.Length); // find out the max length from all package names

            var knownFeeds = new ConfigReader().ReadSources();
            foreach (var feed in knownFeeds)
            {
                var feedName = feed.Item1;
                var packages = findings.Where(f => f.Feed.FeedName == feedName);
                if (packages.Any() || showAllFeeds) {
                    Console.WriteLine($"{Environment.NewLine}{$"---= {feedName} =".PadRight(w + 15, '-')}");
                }
                foreach (var package in packages.GroupBy(p => p.PackageData.Identity.Id))
                {
                    Console.Write("{0,-" + w + "} :  ", package.Key);
                    Console.WriteLine($"[{string.Join(", ", package.ToList().Select(vi => vi.VersionInfo.Version.ToString()))}]");
                }
            };

        }

        private static List<FeedWorker.SearchResult> Search(string packageName, string versionSpec)
        {
            ILogger logger = NullLogger.Instance;
            CancellationToken cancellationToken = CancellationToken.None;

            var knownFeeds = new ConfigReader().ReadSources();
            var feedCrawlers = knownFeeds.Select(feed => new FeedWorker(feed.Item1, feed.Item2)).ToList();
            var findings = new ConcurrentBag<FeedWorker.SearchResult>();
            Task.WaitAll(feedCrawlers.Select(async (fc) =>
            {
                var feedResult = await fc.Search(packageName, versionSpec, includePreRelease: true);
                feedResult.ToList().ForEach(r => findings.Add(r));
            }).ToArray());
            return findings.ToList();
        }

    }
}