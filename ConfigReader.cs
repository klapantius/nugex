using System.Xml.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nugex
{
    class ConfigReader
    {
        private readonly List<string> ConfigFiles = new List<string>() {
            @"%appdata%\NuGet\NuGet.Config"
        };

        private XAttribute GetAttribute(string name, XElement element)
        {
            return element?.Attributes()
                .Where(a => a.Name.LocalName.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                .SingleOrDefault();
        }
        public List<Tuple<string, string>> ReadSources()
        {
            var result = new List<Tuple<string, string>>();
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
                result.AddRange(packageSources
                    .Elements()
                    .Select(e => new Tuple<string, string>(
                        GetAttribute("key", e)?.Value,
                        GetAttribute("value", e)?.Value))
                    .Where(e => e != null));
            }
            return result.ToList();
        }
    }
}
