using NuGet.Common;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using nugex.cmdline;
using nugex.utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace nugex
{
    partial class Program
    {
        // main idea from here: https://martinbjorkstrom.com/posts/2018-09-19-revisiting-nuget-client-libraries
        // some basic snippets can be found here: https://www.meziantou.net/exploring-the-nuget-client-libraries.htm

        private static void Explore()
        {
            var packageName = CmdLine.Parser.GetParam(_SEARCH_TERM_);
            if (string.IsNullOrWhiteSpace(packageName)) throw new Exception($"please use the {_SEARCH_TERM_} parameter to specify the package");
            var versionSpec = CmdLine.Parser.GetParam(_VSPEC_);

            var package = FindOnNugetOrg(packageName, versionSpec).Result;

            var fwSpec = CmdLine.Parser.GetParam(_FWSPEC_);
            if (string.IsNullOrWhiteSpace(fwSpec))
            {
                var supportedFrameworks = GetSupportedFrameworks(packageName, package.VersionInfo.Version.ToString()).Result;
                fwSpec = supportedFrameworks.First();
            }

            var packages = new HashSet<SourcePackageDependencyInfo>(PackageIdentityComparer.Default);
            using (var cacheContext = new SourceCacheContext())
            {
                GetPackageDependencies(
                    package.PackageData.Identity,
                    NuGetFramework.ParseFolder(fwSpec), cacheContext, NullLogger.Instance, packages).Wait();
            }

            packages.ToList().ForEach(i => Console.WriteLine($"{i.Id} {i.Version}"));
        }

        private static async Task GetPackageDependencies(PackageIdentity package,
            NuGetFramework framework,
            SourceCacheContext cacheContext,
            ILogger logger,
            ISet<SourcePackageDependencyInfo> availablePackages)
        {
            if (availablePackages.Contains(package)) return;

            var sourceRepository = Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");
            var dependencyInfoResource = await sourceRepository.GetResourceAsync<DependencyInfoResource>();
            var dependencyInfo = await dependencyInfoResource.ResolvePackage(
                package, framework, cacheContext, logger, CancellationToken.None);

            if (dependencyInfo == null) return;

            availablePackages.Add(dependencyInfo);
            // todo: use Tasks.WaitAll for faster progress
            foreach (var dependency in dependencyInfo.Dependencies)
            {
                await GetPackageDependencies(
                    new PackageIdentity(dependency.Id, dependency.VersionRange.MinVersion),
                    framework, cacheContext, logger, availablePackages);
            }
        }

        private static async Task<IEnumerable<string>> GetSupportedFrameworks(string packageName, string versionSpec)
        {
            var cache = new SourceCacheContext();
            var repository = Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");
            var resource = await repository.GetResourceAsync<FindPackageByIdResource>();

            using MemoryStream packageStream = new MemoryStream();
            await resource.CopyNupkgToStreamAsync(
                packageName,
                new NuGetVersion(versionSpec),
                packageStream,
                cache,
                NullLogger.Instance,
                CancellationToken.None);

            using var packageReader = new PackageArchiveReader(packageStream);
            var libs = packageReader.GetLibItems();

            return libs.Select(li => li.TargetFramework.GetShortFolderName()).ToList();
        }

        private static async Task<FeedWorker.SearchResult> FindOnNugetOrg(string packageName, string versionSpec)
        {
            var feed = new FeedWorker("nuget.org", "https://api.nuget.org/v3/index.json");
            var packages = (await feed.Search($"^{packageName}$", versionSpec, includePreRelease: true)).ToList();
            return packages.SingleOrDefault() ?? throw new Exception($"could not identify \"{packageName}\" \"{versionSpec}\". Use the 'search' command to find what you need.");
        }

    }
}
