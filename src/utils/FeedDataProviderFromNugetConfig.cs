using NuGet.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace nugex.utils
{
    /// <summary>
    /// wrapper for the Configuration API of the Nuget library
    /// </summary>
    public class FeedDataProviderFromNugetConfig : IFeedDataProvider
    {
        public async Task<List<FeedData>> GetSources(bool disabledToo)
        {
            var settings = Settings.LoadDefaultSettings(null);
            var psp = new PackageSourceProvider(settings);
            var availableSources = psp.LoadPackageSources()
                .Where(source => disabledToo || source.IsEnabled);
            return availableSources
                .Select(i => new FeedData { Name = i.Name, Url = i.Source })
                .ToList();
        }
    }
}
