using NUnit.Framework;
using nugex.utils;

namespace nugex.tests
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Test1()
        {
            var sut = new FeedWorker() { Feed = new FeedData { Name = "foo", Url = "https://foo" } };

            Assert.Pass();
        }
    }
}