using System.Xml.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;

namespace nugex
{
    class Program
    {
        static void Main(string[] args)
        {
            ILogger logger = NullLogger.Instance;
            CancellationToken cancellationToken = CancellationToken.None;

            var knownFeeds = new ConfigReader().ReadSources();
            var feedCrowlers = knownFeeds.Select(feed => new FeedCrawler(feed.Item1, feed.Item2)).ToList();
            Task.WaitAll(feedCrowlers.Select(fc => fc.Search("sy.")).ToArray());
            feedCrowlers.ToList().ForEach(crawler =>
            {
                Console.WriteLine($"{Environment.NewLine}---= {crawler.FeedName} =-------------");
                crawler.Results.ToList().ForEach(item => Console.WriteLine($"{item.Identity.Id} - {item.Identity.Version.ToString()}"));
            });
        }

    }

    class ConfigReader
    {
        private readonly List<string> ConfigFiles = new List<string>() {
            @"%appdata%\NuGet\NuGet.Config"
        };

        private XAttribute GetAttribute(string name, XElement element)
        {
            return element?.Attributes()
                .Where(a => a.Name.LocalName.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                .SingleOrDefault();
        }
        public List<Tuple<string, string>> ReadSources()
        {
            var result = new List<Tuple<string, string>>();
            foreach (var cfgFile in ConfigFiles)
            {
                var cfg = XDocument.Load(Environment.ExpandEnvironmentVariables(cfgFile));
                var packageSources = cfg
                    .Elements()?.First()?.Elements()?
                    .Where(e => e.Name.LocalName.Equals("packagesources", StringComparison.InvariantCultureIgnoreCase))
                    .SingleOrDefault();
                if (packageSources == default)
                {
                    Console.WriteLine($"{cfgFile} is wrong: could not identify exactly one <packageSources> element");
                    continue;
                }
                result.AddRange(packageSources
                    .Elements()
                    .Select(e => new Tuple<string, string>(
                        GetAttribute("key", e)?.Value,
                        GetAttribute("value", e)?.Value))
                    .Where(e => e != null));
            }
            return result.ToList();
        }
    }
    class FeedCrawler
    {
        public string FeedName { get; }
        public string FeedUrl { get; }
        public HashSet<IPackageSearchMetadata> Results { get; private set; } = new HashSet<IPackageSearchMetadata>();

        public FeedCrawler(string feedName, string feedUrl)
        {
            FeedName = feedName;
            FeedUrl = feedUrl;
        }

        public async Task Search(string searchTerm)
        {
            SourceCacheContext cache = new SourceCacheContext();
            SourceRepository repository = Repository.Factory.GetCoreV3(FeedUrl);
            var searcher = await repository.GetResourceAsync<PackageSearchResource>();
            Results.Clear();
            var r = (await searcher.SearchAsync(
                searchTerm,
                new SearchFilter(includePrerelease: false),
                0, 999,
                NullLogger.Instance, CancellationToken.None));
            r.ToList().ForEach(i => Results.Add(i));
        }
    }
}
