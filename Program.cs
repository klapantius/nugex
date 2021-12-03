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
        static readonly string[] KnownFeeds = {
            "https://apollo.healthcare.siemens.com/tfs/IKM.TPC.Projects/_packaging/syngo-BuildTools/nuget/v3/index.json",
            "https://apollo.healthcare.siemens.com/tfs/IKM.TPC.Projects/_packaging/syngo-BuildIntegration-Extern/nuget/v3/index.json",
            "https://apollo.healthcare.siemens.com/tfs/IKM.TPC.Projects/_packaging/syngo-Extern/nuget/v3/index.json",
            "https://apollo.healthcare.siemens.com/tfs/IKM.TPC.Projects/_packaging/x-juba-upload-test/nuget/v3/index.json",
            "https://api.nuget.org/v3/index.json"
        };

        static void Main(string[] args)
        {
            ILogger logger = NullLogger.Instance;
            CancellationToken cancellationToken = CancellationToken.None;

            var feedCrowlers = KnownFeeds.Select(feedUrl => FeedCrawler(feedUrl)).ToArray();
            Task.WaitAll(feedCrowlers);
            feedCrowlers.ToList().ForEach(feed => {
                Console.WriteLine("--------");
                feed.Result.ForEach(item => Console.WriteLine($"{item.Identity.Id} - {item.Identity.Version.ToString()}"));
            });
        }

        static async Task<List<IPackageSearchMetadata>> FeedCrawler(string feedUrl)
        {
            SourceCacheContext cache = new SourceCacheContext();
            SourceRepository repository = Repository.Factory.GetCoreV3(feedUrl);
            var searcher = await repository.GetResourceAsync<PackageSearchResource>();
            var packages = await searcher.SearchAsync(@"sy.m", new SearchFilter(false), 0, 999, NullLogger.Instance, CancellationToken.None);
            return packages.ToList();
        }

    }
}
