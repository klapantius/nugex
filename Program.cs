using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace nugex
{
    class Program
    {
        static async Task Main(string[] args)
        {
            ILogger logger = NullLogger.Instance;
            CancellationToken cancellationToken = CancellationToken.None;

            var feedUrl = "https://apollo.healthcare.siemens.com/tfs/IKM.TPC.Projects/_packaging/syngo-BuildTools/nuget/v3/index.json";
            var packageName = "Siemens.TFS.Build.Core";
            
            SourceCacheContext cache = new SourceCacheContext();
            SourceRepository repository = Repository.Factory.GetCoreV3(feedUrl);
            FindPackageByIdResource resource = await repository.GetResourceAsync<FindPackageByIdResource>();

            IEnumerable<NuGetVersion> versions = await resource.GetAllVersionsAsync(
                packageName,
                cache,
                logger,
                cancellationToken);

            foreach (NuGetVersion version in versions)
            {
                Console.WriteLine($"{version.ToFullString()}");
            }
        }
    }
}
