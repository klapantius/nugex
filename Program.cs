using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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

            SourceCacheContext cache = new SourceCacheContext();
            SourceRepository repository = Repository.Factory.GetCoreV3(feedUrl);

            var searcher = await repository.GetResourceAsync<PackageSearchResource>();
            var packages = await searcher.SearchAsync("microsoft", new SearchFilter(false), 0, 999, logger, cancellationToken);
            var releaseVersionRule = new Regex(@"^\d+\.\d+.\d+$");

            packages.ToList().ForEach(p =>
            {
                Console.WriteLine($"{p.Identity.Id} - {p.Identity.Version}");
            });
        }
    }
}
