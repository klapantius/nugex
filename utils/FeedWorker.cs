using System.Collections.Concurrent;
using System;
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

        public async Task<IEnumerable<SearchResult>> Search(
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
            var filter = new Regex(searchTerm, RegexOptions.IgnoreCase);
            packages = packages.Where(p => filter.IsMatch(p.Identity.Id));

            var conDict = new ConcurrentDictionary<SearchResult, byte>();
            var addVersions = new VersionCollectorFactory(versionSpec, this).Create();
            Task.WaitAll(packages.Select(async (pkgData) =>
            {
                var allVersions = await pkgData.GetVersionsAsync();
                addVersions(allVersions, pkgData, conDict);
            }).ToArray());
            return new HashSet<SearchResult>(conDict.Keys);
        }

        private class VersionCollectorFactory
        {
            private readonly string versionSpec;
            private readonly FeedWorker worker;

            public VersionCollectorFactory(string versionSpec, FeedWorker worker)
            {
                this.versionSpec = versionSpec;
                this.worker = worker;
            }

            public Action<IEnumerable<VersionInfo>, IPackageSearchMetadata, ConcurrentDictionary<SearchResult, byte>> Create()
            {
                Action<IEnumerable<VersionInfo>, IPackageSearchMetadata, ConcurrentDictionary<SearchResult, byte>> TakeLast = (versions, metaData, versionInfos) =>
                {
                    var x = new SearchResult
                    {
                        PackageData = metaData,
                        VersionInfo = versions.Last(),
                        Worker = worker
                    };
                    versionInfos[x] = 1;
                };
                Action<IEnumerable<VersionInfo>, IPackageSearchMetadata, ConcurrentDictionary<SearchResult, byte>> FindMatching = (versions, metaData, versionInfos) =>
                {
                    versions
                        .Where(v => Regex.IsMatch(v.Version.ToString(), versionSpec, RegexOptions.IgnoreCase))
                        .ToList()
                        .ForEach(v =>
                        {
                            var x = new SearchResult
                            {
                                PackageData = metaData,
                                VersionInfo = v,
                                Worker = worker
                            };
                            versionInfos[x] = 1;
                        });
                };
                return string.IsNullOrWhiteSpace(versionSpec) ? TakeLast : FindMatching;
            }
        }

    }
}
