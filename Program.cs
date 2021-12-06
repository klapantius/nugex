using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;

namespace nugex
{
    class Program
    {
        static void Main(string[] args)
        {
            ILogger logger = NullLogger.Instance;
            CancellationToken cancellationToken = CancellationToken.None;

            var knownFeeds = new ConfigReader().ReadSources();
            var feedCrowlers = knownFeeds.Select(feed => new FeedCrawler(feed.Item1, feed.Item2)).ToList();
            Task.WaitAll(feedCrowlers.Select(fc => fc.Search("sy.")).ToArray());
            feedCrowlers.ToList().ForEach(crawler =>
            {
                Console.WriteLine($"{Environment.NewLine}---= {crawler.FeedName} =-------------");
                crawler.Results.ToList().ForEach(item => Console.WriteLine($"{item.Identity.Id} - {item.Identity.Version.ToString()}"));
            });
        }

    }
}
