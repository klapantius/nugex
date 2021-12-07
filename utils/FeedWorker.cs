using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;

namespace nugex.utils
{
    class FeedWorker
    {
        public string FeedName { get; }
        public string FeedUrl { get; }
        public HashSet<IPackageSearchMetadata> Results { get; private set; } = new HashSet<IPackageSearchMetadata>();

        public FeedWorker(string feedName, string feedUrl)
        {
            FeedName = feedName;
            FeedUrl = feedUrl;
        }

        public async Task Search(string searchTerm, bool includePreRelease = false)
        {
            SourceCacheContext cache = new SourceCacheContext();
            SourceRepository repository = Repository.Factory.GetCoreV3(FeedUrl);
            var searcher = await repository.GetResourceAsync<PackageSearchResource>();
            Results.Clear();
            var r = (await searcher.SearchAsync(
                searchTerm,
                new SearchFilter(includePrerelease: includePreRelease),
                0, 999,
                NullLogger.Instance, CancellationToken.None));
            r.ToList().ForEach(i => Results.Add(i));
        }

        public async Task<IEnumerable<VersionInfo>> FindVersions(IPackageSearchMetadata package, string versionSpec) {
            var versions = await package.GetVersionsAsync();
            if (!versions.Any()) return new VersionInfo[0];
            var result = versions.Where(v => Regex.IsMatch(v.Version.ToString(), versionSpec, RegexOptions.IgnoreCase));
            return result;
        }
    }
}
