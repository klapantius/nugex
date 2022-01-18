using System;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using nugex.cmdline;
using nugex.utils;

namespace nugex
{
    partial class Program
    {
        private static void Copy()
        {
            var packageName = CmdLine.Parser.GetParam(_SEARCH_TERM_);
            if (string.IsNullOrWhiteSpace(packageName)) throw new ErrorMessage($"please use the {_SEARCH_TERM_} parameter to specify the package");
            var versionSpec = CmdLine.Parser.GetParam(_VSPEC_);
            if (string.IsNullOrWhiteSpace(versionSpec)) throw new ErrorMessage($"please use the {_VSPEC_} parameter to specify the version");
            var targetFeed = CmdLine.Parser.GetParam(_TARGET_FEED_);
            if (string.IsNullOrWhiteSpace(targetFeed)) throw new ErrorMessage($"please use the {_TARGET_FEED_} parameter to specify the target feed");
            var apiKey = CmdLine.Parser.GetParam(_API_KEY_);

            CopyAsync(packageName, versionSpec, targetFeed, apiKey).Wait();
            System.Console.WriteLine($"transfer completed");
        }
        
        private static async Task CopyAsync(string packageName, string version, string targetFeed, string apiKey = "AzureDevOps")
        {
            if (string.IsNullOrEmpty(apiKey)) apiKey = "AzureDevOps";
            var transferFolder = EnsureDownloadFolder(null);
            var package = await DownloadAsync(packageName, version, transferFolder);

            var config = new ConfigReader();
            var url = config.GetUrlOfFeed(targetFeed);
            var repository = Repository.Factory.GetCoreV3(url);
            var uploader = await repository.GetResourceAsync<PackageUpdateResource>();

            await uploader.Push(
                new[] { package },
                symbolSource: null,
                timeoutInSecond: 4 * 15,
                disableBuffering: false,
                getApiKey: packageSource => apiKey,
                getSymbolApiKey: packageSource => null,
                noServiceEndpoint: false,
                skipDuplicate: false,
                symbolPackageUpdateResource: null,
                NullLogger.Instance);
        }

    }
}