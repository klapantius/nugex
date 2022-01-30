using Moq;
using NuGet.Common;
using NuGet.Frameworks;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace utils
{
    public class ExploreTests
    {
        [SetUp]
        public void Setup()
        {
        }

        public class TestDependencyInfoResource : INuGetResource
        {
            public Mock<DependencyInfoResource> myDependencyInfoResource;
            public TestDependencyInfoResource(IEnumerable<SourcePackageDependencyInfo> dependencySetup)
            {
                myDependencyInfoResource = new Mock<DependencyInfoResource>();
                dependencySetup.ToList().ForEach(i =>
                {
                    myDependencyInfoResource
                        .Setup(x => x.ResolvePackage(
                            It.Is<PackageIdentity>(p => p.Id == i.Id),
                            It.IsAny<NuGetFramework>(),
                            It.IsAny<SourceCacheContext>(),
                            It.IsAny<ILogger>(),
                            It.IsAny<CancellationToken>()))
                        .Returns(Task.FromResult(i));
                });
            }
        }

        public class TestDependencyInfoResourceV3Provider : ResourceProvider
        {
            public TestDependencyInfoResourceV3Provider(TestDependencyInfoResource tdir)
                : base(typeof(DependencyInfoResource), nameof(TestDependencyInfoResourceV3Provider), NuGetResourceProviderPositions.Last)
            {
                Tdir = tdir;
            }

            public TestDependencyInfoResource Tdir { get; }

            public override Task<Tuple<bool, INuGetResource>> TryCreate(SourceRepository source, CancellationToken token)
            {
                return Task.FromResult(new Tuple<bool, INuGetResource>(Tdir != null, Tdir.myDependencyInfoResource.Object));
            }
        }

        public class FakeFactory : Repository.ProviderFactory
        {
            public TestDependencyInfoResource Tdir { get; }

            public Mock<DependencyInfoResource> myDependencyInfoResource;

            public FakeFactory(TestDependencyInfoResource tdir)
            {
                FactoryExtensionsV3.GetCoreV3(this);
                Tdir = tdir;
            }

            public static IEnumerable<Lazy<INuGetResourceProvider>> GetProviders()
            {
                return Repository.Provider.GetCoreV3();
            }

            public override IEnumerable<Lazy<INuGetResourceProvider>> GetCoreV3()
            {
                yield return new Lazy<INuGetResourceProvider>(() => new TestDependencyInfoResourceV3Provider(Tdir));
            }
        }

        [Test]
        public void FollowsTransitiveDependencies()
        {
            var tdir = new TestDependencyInfoResource(
                new List<SourcePackageDependencyInfo> {
                    new SourcePackageDependencyInfo("foo", NuGetVersion.Parse("1.0.0"), new[]
                    {
                        new PackageDependency("bar"),
                        new PackageDependency("baz"),
                    }, false, null),
                    new SourcePackageDependencyInfo("bar", NuGetVersion.Parse("1.0.0"), new[]
                    {
                        new PackageDependency("baz")
                    }, false, null),
                    new SourcePackageDependencyInfo("baz", NuGetVersion.Parse("1.0.0"), new PackageDependency[0], false, null)
            });
            var ff = new FakeFactory(tdir);
            Repository.Provider = ff;

            var packages = new HashSet<SourcePackageDependencyInfo>(PackageIdentityComparer.Default);
            _ = nugex.Program.GetPackageDependencies(
                new PackageIdentity("foo", NuGetVersion.Parse("1.0.0")),
                NuGetFramework.Parse("net5"), "yyy", null, null, packages);

            tdir.myDependencyInfoResource.Verify(x => x.ResolvePackage(
                    It.IsAny<PackageIdentity>(),
                    It.IsAny<NuGetFramework>(),
                    It.IsAny<SourceCacheContext>(),
                    It.IsAny<ILogger>(),
                    It.IsAny<CancellationToken>()
                ),
                Times.Exactly(4));
        }

    }
}