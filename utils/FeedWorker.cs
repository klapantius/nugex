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

        public FeedWorker(string feedName, string feedUrl)
        {
            FeedName = feedName;
            FeedUrl = feedUrl;
        }

        public class SearchResult
        {
            public VersionInfo VersionInfo { get; set; }
            public IPackageSearchMetadata PackageData { get; set; }
            public FeedWorker Worker { get; set; }
        }

        public async Task<HashSet<SearchResult>> Search(
            string searchTerm,
            string versionSpec = null,
            bool includePreRelease = false)
        {
            SourceCacheContext cache = new SourceCacheContext();
            SourceRepository repository = Repository.Factory.GetCoreV3(FeedUrl);
            var searcher = await repository.GetResourceAsync<PackageSearchResource>();
            var packages = (await searcher.SearchAsync(
                searchTerm,
                new SearchFilter(includePrerelease: includePreRelease),
                0, 999,
                NullLogger.Instance, CancellationToken.None));
            var versionInfos = new HashSet<SearchResult>();
            Task.WaitAll(packages.Select(async (pkgData) =>
            {
                var allVersions = await pkgData.GetVersionsAsync();
                versionInfos.Add(new SearchResult
                {
                    PackageData = pkgData,
                    VersionInfo = allVersions.Last(),
                    Worker = this
                });
            }).ToArray());
            return versionInfos;
        }

        public async Task<IEnumerable<VersionInfo>> FindVersions(IPackageSearchMetadata package, string versionSpec)
        {
            var versions = await package.GetVersionsAsync();
            if (!versions.Any()) return new VersionInfo[0];
            var result = versions.Where(v => Regex.IsMatch(v.Version.ToString(), versionSpec, RegexOptions.IgnoreCase));
            return result;
        }

    }
}
