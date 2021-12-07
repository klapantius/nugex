using System.Collections.Generic;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using nugex.cmdline;
using nugex.utils;
using NuGet.Protocol.Core.Types;

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
            var findings = new Dictionary<string, HashSet<FeedWorker.SearchResult>>();
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
            var findings = new Dictionary<string, HashSet<FeedWorker.SearchResult>>();
            Task.WaitAll(feedCrawlers.Select(async (fc) =>
            {
                findings[fc.FeedName] = new HashSet<FeedWorker.SearchResult>(
                    await fc.Search(packageName, versionSpec, includePreRelease: true));
            }).ToArray());
            foreach (var finding in findings)
            {
                var feedName = finding.Key;
                var packages = finding.Value;
                var first = true;
                foreach (var package in packages)
                {
                    var versions = package.Worker.FindVersions(package.PackageData, versionSpec).Result;
                    if (first) Console.WriteLine($"{Environment.NewLine}---= {package.Worker.FeedName} =-------------");
                    if (!versions.Any()) Console.WriteLine($"{package.PackageData.Identity.Id} - package exists, but no matching version could be identified");
                    else Console.WriteLine($"{package.PackageData.Identity.Id}:");
                    versions.ToList().ForEach(vi => Console.WriteLine($"  {vi.Version.ToString()}"));
                    first = false;
                }
            };
        }

    }
}