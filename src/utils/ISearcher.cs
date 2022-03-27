using System.Collections.Generic;
using System.Threading.Tasks;

namespace nugex.utils
{
    public interface ISearcher
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="packageName">search term: partial package name, with optional trailing '^' and/or closing '$' char</param>
        /// <param name="versionSpec">version number regex pattern</param>
        /// <param name="knownFeeds">list of package sources to be considered - all enabled sources will be taken if no one is specified</param>
        /// <param name="strict">TODO: describe this parameter</param>
        /// <returns></returns>
        Task<List<FeedWorker.SearchResult>> RunAsync(string packageName, string versionSpec, IEnumerable<FeedData> knownFeeds = null, bool strict = true);
    }
}