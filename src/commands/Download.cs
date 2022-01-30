using NuGet.Common;
using NuGet.Protocol.Core.Types;
using NuGet.Protocol;
using NuGet.Versioning;
using nugex.cmdline;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using nugex.utils;
using System.Linq;

namespace nugex
{
    partial class Program
    {
        private static void Download()
        {
            var packageName = CmdLine.Parser.GetParam(_SEARCH_TERM_);
            if (string.IsNullOrWhiteSpace(packageName)) throw new ErrorMessage($"please use the {_SEARCH_TERM_} parameter to specify the package");
            var versionSpec = CmdLine.Parser.GetParam(_VSPEC_);
            if (string.IsNullOrWhiteSpace(versionSpec)) throw new ErrorMessage($"please use the {_VSPEC_} parameter to specify the version");
            var targetDirectory = EnsureDownloadFolder(CmdLine.Parser.GetParam(_TARGET_PATH_));

            var filePath = DownloadAsync(packageName, versionSpec, targetDirectory).Result;
            Console.WriteLine($"download completed: {filePath}");
        }

        private static string EnsureDownloadFolder(string targetDirectory)
        {
            if (string.IsNullOrWhiteSpace(targetDirectory))
            {
                targetDirectory = Environment.ExpandEnvironmentVariables($"%TEMP%\\{Guid.NewGuid()}");
                Directory.CreateDirectory(targetDirectory);
            }
            return targetDirectory;
        }

        private static async Task<string> DownloadAsync(string packageName, string version, string targetDirectory)
        {
            var source = FeedSelector.Find();
            var pkgInfo = await Search(packageName, version);
            if (!pkgInfo.Any(p => p.Feed.Url.Equals(source.Url)
                && p.VersionInfo.Version == NuGetVersion.Parse(version)))
            {
                var similar = pkgInfo.FirstOrDefault(p => p.Feed.Url.Equals(source.Url));
                throw new ErrorMessage($"{packageName} {version} is not available on {source.Name}{(similar!=default?" (but other versions)":"")}");
            }

            var cache = new SourceCacheContext();
            var repository = Repository.Factory.GetCoreV3(FeedSelector.Find().Url);
            var resource = await repository.GetResourceAsync<FindPackageByIdResource>();

            var fullPath = Path.Join(targetDirectory, $"{packageName}.{version}.nupkg");
            using var fileStream = new FileStream(fullPath, FileMode.CreateNew);
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