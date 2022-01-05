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
        internal static readonly string NugetOrgFeedUri = "https://api.nuget.org/v3/index.json";

        private static void Search()
        {
            var searchTerm = CmdLine.Parser.GetParam(_SEARCH_TERM_);
            if (string.IsNullOrWhiteSpace(searchTerm)) throw new Exception($"please use the --name parameter to specify a search term");
            var versionSpec = CmdLine.Parser.GetParam(_VSPEC_);
            var showAllFeeds = CmdLine.Parser.GetSwitch(_ALL_FEEDS_);

            var findings = Search(searchTerm, versionSpec).Result;
            var w = findings.Max(f => f.PackageData.Identity.Id.Length); // find out the max length from all package names

            var knownFeeds = new ConfigReader().ReadSources();
            foreach (var feed in knownFeeds)
            {
                var feedName = feed.Item1;
                var packages = findings.Where(f => f.Feed.FeedName == feedName);
                if (packages.Any() || showAllFeeds)
                {
                    Console.WriteLine($"{Environment.NewLine}{$"---= {feedName} =".PadRight(w + 15, '-')}");
                }
                foreach (var package in packages.GroupBy(p => p.PackageData.Identity.Id))
                {
                    Console.Write("{0,-" + w + "} :  ", package.Key);
                    Console.WriteLine($"[{string.Join(", ", package.ToList().Select(vi => vi.VersionInfo.Version.ToString()))}]");
                }
            };

        }

        private static async Task<List<FeedWorker.SearchResult>> Search(string packageName, string versionSpec, IEnumerable<Tuple<string, string>> knownFeeds = null)
        {
            ILogger logger = NullLogger.Instance;
            CancellationToken cancellationToken = CancellationToken.None;

            if (knownFeeds == null) knownFeeds = new ConfigReader().ReadSources();
            var feedCrawlers = knownFeeds.Select(feed => new FeedWorker(feed.Item1, feed.Item2)).ToList();
            var findings = new ConcurrentBag<FeedWorker.SearchResult>();
            await Task.WhenAll(feedCrawlers.Select(async (fc) =>
            {
                var feedResult = await fc.Search(packageName, versionSpec, includePreRelease: true);
                feedResult.ToList().ForEach(r => findings.Add(r));
            }).ToArray());
            return findings.ToList();
        }

        private static async Task<List<FeedWorker.SearchResult>> SearchOnNugetOrg(string packageName, string versionSpec)
            => await Search(packageName, versionSpec, new[]
                {
                    new Tuple<string, string>("nuget.org", NugetOrgFeedUri)
                });

        private static async Task<List<FeedWorker.SearchResult>> SearchInternal(string packageName, string versionSpec)
        {
            var knownFeeds = new ConfigReader().ReadSources();
            var internalFeeds = knownFeeds.Where(f => !f.Item2.Equals(NugetOrgFeedUri, StringComparison.InvariantCultureIgnoreCase));
            return await Search(packageName, versionSpec, internalFeeds);
        }

        private static string Exactly(string packageName) => $"^{packageName}$";

    }
}