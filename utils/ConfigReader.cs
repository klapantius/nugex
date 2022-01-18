using System.Xml.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nugex.utils
{
    class ConfigReader
    {
        private readonly List<string> ConfigFiles = new()
        {
            @"%appdata%\NuGet\NuGet.Config"
        };

        private static XAttribute GetAttribute(string name, XElement element)
        {
            return element?.Attributes()
                .Where(a => a.Name.LocalName.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                .SingleOrDefault();
        }
        public List<FeedData> ReadSources(bool disabledToo = false)
        {
            var result = new List<FeedData>();
            foreach (var cfgFile in ConfigFiles)
            {
                var cfg = XDocument.Load(Environment.ExpandEnvironmentVariables(cfgFile));
                var packageSources = cfg
                    .Elements()?.First()?.Elements()?
                    .Where(e => e.Name.LocalName.Equals("packagesources", StringComparison.InvariantCultureIgnoreCase))
                    .SingleOrDefault();
                if (packageSources == default)
                {
                    Console.WriteLine($"{cfgFile} is wrong: could not identify exactly one <packageSources> element");
                    continue;
                }
                List<string> disabledSources = disabledToo ? new() :
                    cfg.Elements().First().Elements()
                        .Where(e => e.Name.LocalName.Equals("disabledPackageSources", StringComparison.InvariantCultureIgnoreCase))
                        .SingleOrDefault()?
                        .Elements()
                            .Where(e => GetAttribute("value", e).Value.ToLowerInvariant() == "true")
                            .Select(e => GetAttribute("key", e).Value)
                        .ToList();
                if (disabledSources == null) disabledSources = new();
                result.AddRange(packageSources
                    .Elements()
                    .Select(e => new FeedData
                    {
                        FeedName = GetAttribute("key", e)?.Value,
                        FeedUrl = GetAttribute("value", e)?.Value
                    })
                    .Where(e => e != null && !disabledSources.Contains(e.FeedName)));
            }
            return result.ToList();
        }
        public string GetUrlOfFeed(string feedName)
        {
            var sources = ReadSources();
            var entry = sources
                .SingleOrDefault(s => s.FeedName.Equals(feedName, StringComparison.InvariantCultureIgnoreCase));
            if (entry == default) throw new Exception($"Could not find feed alias \"{feedName}\"");
            return entry.FeedUrl;
        }
    }
}
