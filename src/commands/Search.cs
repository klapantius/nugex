using System.Collections.Generic;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using nugex.cmdline;
using nugex.utils;
using System.Collections.Concurrent;
using NuGet.Versioning;

namespace nugex
{
    partial class Program
    {
        internal static void Search()
        {
            var searchTerm = CmdLine.Parser.GetParam(_SEARCH_TERM_);
            if (string.IsNullOrWhiteSpace(searchTerm)) throw new ErrorMessage($"please use the --name parameter to specify a search term");
            var versionSpec = CmdLine.Parser.GetParam(_VPATTERN_);
            if (versionSpec == default)
            {
                versionSpec = @$"^{CmdLine.Parser.GetParam(_VSPEC_) ?? ".*"}$";
            }
            var showAllFeeds = CmdLine.Parser.GetSwitch(_ALL_FEEDS_);

            var searcher = SearcherFactory.Create();
            var findings = searcher.RunAsync(searchTerm, versionSpec).Result;
            if (!findings.Any())
            {
                Console.WriteLine("Could not find any package according to these parameters.");
                return;
            }
            var w = findings.Max(f => f.PackageData.Identity.Id.Length); // find out the max length from all package names

            var anyVersionSpecified = CmdLine.Parser.GetParam(_VPATTERN_) != default || CmdLine.Parser.GetParam(_VSPEC_) != null;
            // todo: replace ConfigReader
            var knownFeeds = new ConfigReader().ReadSources(CmdLine.Parser.GetSwitch(_CONSIDER_DISABLED_FEEDS_));
            foreach (var feed in knownFeeds)
            {
                var feedName = feed.Name;
                var packagesFromFeed = findings.Where(f => f.Feed.Name == feedName);
                if (packagesFromFeed.Any() || showAllFeeds)
                {
                    Console.WriteLine($"{Environment.NewLine}{$"---= {feedName} =".PadRight(w + 15, '-')}");
                }
                foreach (var package in packagesFromFeed.GroupBy(p => p.PackageData.Identity.Id))
                {
                    Console.Write("{0,-" + w + "} :  ", package.Key);
                    var versionsFound = package
                        .OrderByDescending(pv => pv.VersionInfo.Version)
                        .Select(vi => vi.VersionInfo.Version)
                        .ToList();
                    var versionsToPrint = anyVersionSpecified ? versionsFound : new List<NuGetVersion>() { versionsFound.First() };
                    if (!anyVersionSpecified && versionsFound.First().IsPrerelease)
                    {
                        versionsToPrint.AddRange(versionsFound.SkipWhile(v => v.IsPrerelease).Take(1));
                    }
                    Console.Write("[");
                    for (int i = 0; i < versionsToPrint.Count; i++)
                    {
                        var version = versionsToPrint[i];
                        var oriColor = Console.ForegroundColor;
                        Console.ForegroundColor = version.IsPrerelease ? ConsoleColor.Gray : ConsoleColor.Yellow;
                        Console.Write(version);
                        Console.ForegroundColor = oriColor;
                        if (i < versionsToPrint.Count - 1) Console.Write(", ");
                    }
                    Console.WriteLine("]");
                }
            };
        }

        private static async Task<List<FeedWorker.SearchResult>> Search(
            string packageName,
            string versionSpec,
            IEnumerable<FeedData> knownFeeds = null,
            bool strict = true)
        {
            ILogger logger = NullLogger.Instance;
            CancellationToken cancellationToken = CancellationToken.None;

            //if (knownFeeds == null) knownFeeds = new ConfigReader().ReadSources();
            if (knownFeeds == null) knownFeeds = await Task.Run(() => new FeedDataProviderFromNugetConfig().GetSources(true));
            var fwf = new FeedWorkerFactory();
            var feedCrawlers = knownFeeds.Select(feed => fwf.Create(feed.Name, feed.Url)).ToList();
            var findings = new ConcurrentBag<FeedWorker.SearchResult>();
            await Task.WhenAll(feedCrawlers.Select(async (fc) =>
            {
                var feedResult = await fc.Search(packageName, versionSpec, includePreRelease: true, strict);
                feedResult.ToList().ForEach(r => findings.Add(r));
            }).ToArray());
            return findings.ToList();
        }

        public static List<FeedData> InternalFeeds(bool considerDisabledFeeds = true)
        {
            var knownFeeds = new ConfigReader().ReadSources(considerDisabledFeeds);
            return knownFeeds
                .Where(f => !f.Url.Equals(FeedSelector.NugetOrgFeedUri, StringComparison.InvariantCultureIgnoreCase))
                .ToList();
        }
        private static async Task<List<FeedWorker.SearchResult>> SearchInternal(string packageName, string versionSpec, bool strict = true, bool considerDisabledFeeds = true)
        {
            var searcher = SearcherFactory.Create();
            return await searcher.RunAsync(packageName, versionSpec, InternalFeeds(considerDisabledFeeds), strict);
        }

        private static string Exactly(string packageName) => $"^{packageName}$";

    }
}