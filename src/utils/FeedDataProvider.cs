using NuGet.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace nugex.utils
{
    interface IFeedDataProvider
    {
        /// <summary>
        /// retrives feed name/url pairs from the implemented source
        /// </summary>
        /// <param name="disabledToo">option to query also disabled feeds
        /// if the implemented source can be configured in this regard</param>
        /// <returns>List of FeedData records</returns>
        Task<List<FeedData>> GetSources(bool disabledToo);
    }

    /// <summary>
    /// obsolete: self-made nuget configuration reader
    /// </summary>
    public class FeedDataProviderFromConfiguration : IFeedDataProvider
    {
        public async Task<List<FeedData>> GetSources(bool disabledToo = false)
        {
            return await Task.Run(() => new ConfigReader().ReadSources(disabledToo));
        }
    }

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

    /// <summary>
    /// combines the resulting list of a REST query for all available feeds
    /// with en-/disabled information from the nuget configuration
    /// </summary>
    public class FeedDataProviderFromRestQuery : IFeedDataProvider
    {
        public async Task<List<FeedData>> GetSources(bool disabledToo)
        {
            throw new NotImplementedException();
        }
    }
}
