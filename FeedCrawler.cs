using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;

namespace nugex
{
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
