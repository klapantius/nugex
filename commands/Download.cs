using NuGet.Common;
using NuGet.Protocol.Core.Types;
using NuGet.Protocol;
using NuGet.Versioning;
using nugex.cmdline;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace nugex
{
    partial class Program
    {
        private static void Download() {
            var packageName = CmdLine.Parser.GetParam(_SEARCH_TERM_);
            if (string.IsNullOrWhiteSpace(packageName)) throw new Exception($"please use the {_SEARCH_TERM_} parameter to specify the package");
            var versionSpec = CmdLine.Parser.GetParam(_VSPEC_);
            if (string.IsNullOrWhiteSpace(versionSpec)) throw new Exception($"please use the {_VSPEC_} parameter to specify the version");
            var targetDirectory = CmdLine.Parser.GetParam(_TARGET_PATH_);
            if (string.IsNullOrWhiteSpace(targetDirectory)) {
                targetDirectory = Environment.ExpandEnvironmentVariables($"%TEMP%\\{Guid.NewGuid()}");
                Directory.CreateDirectory(targetDirectory);
            }

            var filePath = DownloadAsync(packageName, versionSpec, targetDirectory).Result;
            System.Console.WriteLine($"download completed: {filePath}");
        }

        private static async Task<string> DownloadAsync(string packageName, string version, string targetDirectory)
        {
            var cache = new SourceCacheContext();
            var repository = Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");
            var resource = await repository.GetResourceAsync<FindPackageByIdResource>();

            var fullPath = Path.Join(targetDirectory, $"{packageName}.{version}.nupkg");
            using FileStream fileStream = new FileStream(fullPath, FileMode.CreateNew);
            await resource.CopyNupkgToStreamAsync(
                packageName,
                new NuGetVersion(version),
                fileStream,
                cache,
                NullLogger.Instance,
                CancellationToken.None);
            return fullPath;
        }
    }
}