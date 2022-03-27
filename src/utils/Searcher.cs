using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using System.Collections.Concurrent;

namespace nugex.utils
{
    public class Searcher : ISearcher
    {
        public async Task<List<FeedWorker.SearchResult>> RunAsync(
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
    }
}