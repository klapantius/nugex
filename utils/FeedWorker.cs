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
    class FeedData
    {
        public string FeedName { get; set; }
        public string FeedUrl { get; set; }

    }

    class FeedWorker
    {
        public FeedData FeedData { get; }
        public FeedWorker(string feedName, string feedUrl)
        {
            FeedData = new FeedData
            {
                FeedName = feedName,
                FeedUrl = feedUrl
            };
        }

        public class SearchResult
        {
            public VersionInfo VersionInfo { get; set; }
            public IPackageSearchMetadata PackageData { get; set; }
            public FeedData Feed { get; set; }
        }

        public async Task<IEnumerable<SearchResult>> Search(
            string searchTerm,
            string versionSpec = null,
            bool includePreRelease = false,
            bool strict = true)
        {
            //strict = !(!strict && versionSpec == null); // return latest only if allowed and a version specification passed
            SourceCacheContext cache = new SourceCacheContext();
            SourceRepository repository = Repository.Factory.GetCoreV3(FeedData.FeedUrl);
            var searcher = await repository.GetResourceAsync<PackageSearchResource>();
            // remove regex characters which may be added to make the search more specific
            var normalizedSearchTerm = new Regex(@"[\^\$]").Replace(searchTerm, "");
            var packages = (await searcher.SearchAsync(
                normalizedSearchTerm,
                new SearchFilter(includePrerelease: includePreRelease),
                0, 999,
                NullLogger.Instance, CancellationToken.None));
            var filter = new Regex(searchTerm, RegexOptions.IgnoreCase);
            packages = packages.Where(p => filter.IsMatch(p.Identity.Id));

            var conDict = new ConcurrentDictionary<SearchResult, byte>();
            var addVersions = new VersionCollectorFactory(versionSpec, FeedData, strict).Create();
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
            private readonly FeedData feedData;
            private readonly bool strict;

            public VersionCollectorFactory(string versionSpec, FeedData feedData, bool strict = true)
            {
                this.versionSpec = versionSpec;
                this.feedData = feedData;
                this.strict = strict;
            }

            public Action<IEnumerable<VersionInfo>, IPackageSearchMetadata, ConcurrentDictionary<SearchResult, byte>> Create()
            {
                void TakeLast(IEnumerable<VersionInfo> versions, IPackageSearchMetadata metaData, ConcurrentDictionary<SearchResult, byte> versionInfos)
                {
                    var x = new SearchResult
                    {
                        PackageData = metaData,
                        VersionInfo = versions.Last(),
                        Feed = feedData
                    };
                    versionInfos[x] = 1;
                }
                void FindMatching(IEnumerable<VersionInfo> versions, IPackageSearchMetadata metaData, ConcurrentDictionary<SearchResult, byte> versionInfos)
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
                                Feed = feedData
                            };
                            versionInfos[x] = 1;
                        });
                }
                void MatchingOrLatest(IEnumerable<VersionInfo> versions, IPackageSearchMetadata metaData, ConcurrentDictionary<SearchResult, byte> versionInfos)
                {
                    var oriCount = versionInfos.Count();
                    // first try to match with the specified version if any
                    if (!string.IsNullOrWhiteSpace(versionSpec)) FindMatching(versions, metaData, versionInfos);
                    // if not found then take latest
                    if (versionInfos.Count == oriCount) TakeLast(versions, metaData, versionInfos);
                }
                // either try to match and return latest as fallback if nothing SPECIFIED or try to match and return false if nothing FOUND
                return strict ? (!string.IsNullOrWhiteSpace(versionSpec) ? FindMatching : TakeLast)
                    : MatchingOrLatest;
            }
        }

    }
}
