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

            // select the source feed
            var sourceFeed = FeedSelector.Find();

            // resolve the versionSpec
            var package = Search(Exactly(packageName), versionSpec, new[] { sourceFeed }).Result
                .SingleOrDefault()
                ?? throw new Exception($"could not identify \"{packageName}\" \"{versionSpec}\". Use the 'search' command to find what you need.");

            // assumed it makes no difference for our purpose which framework we take
            var fwSpec = CmdLine.Parser.GetParam(_FWSPEC_);
            if (string.IsNullOrWhiteSpace(fwSpec))
            {
                var supportedFrameworks = GetSupportedFrameworks(packageName, package.VersionInfo.Version.ToString(), sourceFeed.Name).Result;
                fwSpec = supportedFrameworks.First();
            }

            // collect all needed packages
            var packages = new HashSet<SourcePackageDependencyInfo>(PackageIdentityComparer.Default);
            using (var cacheContext = new SourceCacheContext())
            {
                GetPackageDependencies(
                    new PackageIdentity(package.PackageData.Identity.Id, package.VersionInfo.Version),
                    NuGetFramework.ParseFolder(fwSpec),
                    sourceFeed.Url,
                    cacheContext, NullLogger.Instance,
                    packages).Wait();
            }

            packages.ToList().ForEach(i => Console.WriteLine($"{i.Id} {i.Version}"));

            // evaluate the internal availability of each package found
            Console.WriteLine("\ninternal package availablility");
            var internalResults = EvaluateInternalAvailability(packages, CmdLine.Parser.GetSwitch(_CONSIDER_DISABLED_FEEDS_)).Result;
            var noExactMatches = CmdLine.Parser.GetSwitch(_NO_EXACT_);
            var noPartialMatches = CmdLine.Parser.GetSwitch(_NO_PARTIALS_);
            var noMissings = CmdLine.Parser.GetSwitch(_NO_MISSINGS_);
            var oriColor = Console.ForegroundColor;
            internalResults.OrderBy(p => p.identity).ToList().ForEach(p =>
            {
                Console.Write($"{p.identity} - ");
                if (p.NotFoundAtAll)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write($"couldn't be found internally");
                }
                else
                {
                    if (!noExactMatches && p.exactlyMatching.Any())
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write($"Found on {string.Join(", ", p.exactlyMatching.Select(x => x.Feed.Name))}. ");
                    }
                    if (!noPartialMatches && p.nameOnlyMatching.Any())
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.Write(string.Join(", ", p.nameOnlyMatching.Select(r => $"{r.VersionInfo.Version} on {r.Feed.Name}")));
                        Console.Write(". ");
                    }
                    if (!noMissings && p.notFound.Any())
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write($"Not found on {string.Join(", ", p.notFound)}. ");
                    }
                }
                Console.WriteLine();
                Console.ForegroundColor = oriColor;
            });
        }

        /// <summary>
        /// walks along the dependency tree of the specified package and returns the list of all needed packages
        /// </summary>
        /// <param name="package">the root of the dependency tree</param>
        /// <param name="framework">the API needs this, use any framework supported by the package</param>
        /// <param name="availablePackages">a container for the result</param>
        /// <returns>nothing, but it populates the result into the object specified in the last parameter</returns>
        public static async Task GetPackageDependencies(PackageIdentity package,
            NuGetFramework framework,
            string sourceFeed,
            SourceCacheContext cacheContext,
            ILogger logger,
            ISet<SourcePackageDependencyInfo> availablePackages)
        {
            if (availablePackages.Contains(package)) return;

            var sourceRepository = Repository.Factory.GetCoreV3(sourceFeed);
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
                    framework, sourceFeed, cacheContext, logger, availablePackages);
            }
        }

        /// <summary>
        /// internal availability record of a package
        /// </summary>
        internal class InternalResult
        {
            /// <summary>
            /// package name and version
            /// </summary>
            public string identity;
            /// <summary>
            /// findings which are completely satisfy the query
            /// </summary>
            public List<FeedWorker.SearchResult> exactlyMatching;
            /// <summary>
            /// findings from feeds which don't host the asked version of the package, but some other one(s)
            /// </summary>
            public List<FeedWorker.SearchResult> nameOnlyMatching;
            /// <summary>
            /// feeds don't host the asked package at all
            /// </summary>
            public List<string> notFound;
            /// <summary>
            /// true if the asked package is not available internally
            /// </summary>
            public bool NotFoundAtAll => !exactlyMatching.Any() && !nameOnlyMatching.Any();
        }

        /// <summary>
        /// an optional step after <see cref="GetPackageDependencies"/> to check the internal availability of the resulted packages
        /// </summary>
        /// <param name="packages"></param>
        /// <returns>a list of <see cref="InternalResult"/></returns>
        public static async Task<List<InternalResult>> EvaluateInternalAvailability(ISet<SourcePackageDependencyInfo> packages, bool considerDisabledFeeds)
        {
            var internalFeeds = InternalFeeds(considerDisabledFeeds).Select(f => f.Name).ToList();
            var tasks = packages.Select(async (p) =>
            {
                var result = await SearchInternal(Exactly(p.Id), Exactly(p.Version.ToString()), strict: false, considerDisabledFeeds);
                if (!result.Any()) result = await SearchInternal(Exactly(p.Id), null, considerDisabledFeeds);
                var finalResult = new InternalResult
                {
                    identity = $"{p.Id} {p.Version}",
                    exactlyMatching = result.Where(r => r.VersionInfo.Version == p.Version)?.ToList(),
                    nameOnlyMatching = result.Where(r => r.VersionInfo.Version != p.Version)?.ToList()
                };
                finalResult.notFound = internalFeeds
                    .Except(
                        finalResult.exactlyMatching.Select(r => r.Feed.Name)
                            .Union(
                        finalResult.nameOnlyMatching.Select(r => r.Feed.Name)))
                    .ToList();
                return finalResult;
            }).ToArray();
            await Task.WhenAll(tasks);
            return tasks.Select(t => t.Result).ToList();
        }

        private static async Task<IEnumerable<string>> GetSupportedFrameworks(string packageName, string versionSpec, string sourceFeed)
        {
            var cache = new SourceCacheContext();
            var repository = Repository.Factory.GetCoreV3(sourceFeed);
            var resource = await repository.GetResourceAsync<FindPackageByIdResource>();

            using MemoryStream packageStream = new();
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

    }
}
