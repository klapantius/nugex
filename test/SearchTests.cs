using Moq;
using nugex.cmdline;
using nugex.utils;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace utils
{
    class SearchTests
    {
        [Test]
        public void CanUseRegexAsVersionSpecification()
        {
            var usedVersionSpec = string.Empty;
            var fakeSearcher = new Mock<ISearcher>();
            fakeSearcher
                .Setup(s => s.RunAsync(It.IsAny<string>(), It.IsAny<string>(), null, It.IsAny<bool>()))
                .Callback<string, string, IEnumerable<FeedData>, bool>((name, vspec, x, y) =>
                {
                    usedVersionSpec = vspec;
                })
                .Returns(Task.FromResult(new List<FeedWorker.SearchResult>()));
            SearcherFactory.Mock = fakeSearcher.Object;
            nugex.Program.InitCommands();
            var pattern = @"1\..*\.3";
            CmdLine.Parser.Parse(
                "search",
                $"--{nugex.Program._SEARCH_TERM_}", "foo",
                $"--{nugex.Program._VPATTERN_}", pattern);
            nugex.Program.Search();
            Assert.AreEqual(1, fakeSearcher.Invocations.Count, "unexpected number of invocations on Searcher object");
            Assert.AreEqual(pattern, usedVersionSpec, "unexpected version specification for Searcher");
        }

        [Test]
        public void ConvertsExactVersionSpecificationToPattern()
        {
            var usedVersionSpec = string.Empty;
            var fakeSearcher = new Mock<ISearcher>();
            fakeSearcher
                .Setup(s => s.RunAsync(It.IsAny<string>(), It.IsAny<string>(), null, It.IsAny<bool>()))
                .Callback<string, string, IEnumerable<FeedData>, bool>((name, vspec, x, y) =>
                {
                    usedVersionSpec = vspec;
                })
                .Returns(Task.FromResult(new List<FeedWorker.SearchResult>()));
            SearcherFactory.Mock = fakeSearcher.Object;
            nugex.Program.InitCommands();
            var pattern = @"1.2.3";
            CmdLine.Parser.Parse(
                "search",
                $"--{nugex.Program._SEARCH_TERM_}", "foo",
                $"--{nugex.Program._VSPEC_}", pattern);
            nugex.Program.Search();
            Assert.AreEqual(1, fakeSearcher.Invocations.Count, "unexpected number of invocations on Searcher object");
            Assert.AreEqual($"^{pattern}$", usedVersionSpec, "unexpected version specification for Searcher");
        }

        [Test]
        public void SearchesForAllVersionsIfNoneSpecified()
        {
            var usedVersionSpec = string.Empty;
            var fakeSearcher = new Mock<ISearcher>();
            fakeSearcher
                .Setup(s => s.RunAsync(It.IsAny<string>(), It.IsAny<string>(), null, It.IsAny<bool>()))
                .Callback<string, string, IEnumerable<FeedData>, bool>((name, vspec, x, y) =>
                {
                    usedVersionSpec = vspec;
                })
                .Returns(Task.FromResult(new List<FeedWorker.SearchResult>()));
            SearcherFactory.Mock = fakeSearcher.Object;
            nugex.Program.InitCommands();
            var pattern = @"^.*$";
            CmdLine.Parser.Parse(
                "search",
                $"--{nugex.Program._SEARCH_TERM_}", "foo");
            nugex.Program.Search();
            Assert.AreEqual(1, fakeSearcher.Invocations.Count, "unexpected number of invocations on Searcher object");
            Assert.AreEqual(pattern, usedVersionSpec, "unexpected version specification for Searcher");
        }

    }
}
