using System.Collections.Generic;
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

}
