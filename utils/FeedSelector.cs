using nugex.cmdline;
using System;
using System.Linq;

namespace nugex.utils
{
    class FeedSelector
    {
        public static string NugetOrgFeedUri => "https://api.nuget.org/v3/index.json";
        public static string DefaultFeedName => "nuget.org";

        /// <summary>
        /// Collects data for accessing of the specified feed.
        /// Takes value of '--source' if no name specified, falls back to nuget.org if no data available from command line.
        /// </summary>
        /// <param name="name">feed name from nuget.config</param>
        /// <returns>a value of type FeedData</returns>
        /// <exception cref="ArgumentException">if the (ex- or implicitly) specified name cannot be found in the configuration</exception>
        public static FeedData Find(string name = null)
        {
            if (string.IsNullOrEmpty(name))
            {
                name = CmdLine.Parser.GetParam(Program._SOURCE_FEED_);
            }
            if (string.IsNullOrEmpty(name))
            {
                name = DefaultFeedName;
            }
            if (name.Equals(DefaultFeedName)) return new FeedData { Name = DefaultFeedName, Url = NugetOrgFeedUri };
            var knownFeeds = new ConfigReader().ReadSources(disabledToo: true);
            var result = knownFeeds.FirstOrDefault(f => f.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
            if (result == default)
            {
                throw new ArgumentException($"could not find a feed with name like '{name}'. Use the 'nuget sources' command to see the available ones.");
            }
            return result;
        }
    }
}
