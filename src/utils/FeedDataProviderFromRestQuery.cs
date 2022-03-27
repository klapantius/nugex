using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace nugex.utils
{
    /// <summary>
    /// combines the resulting list of a REST query for all available feeds
    /// with en-/disabled information from the nuget configuration
    /// </summary>
    public class FeedDataProviderFromRestQuery : IFeedDataProvider
    {
        public async Task<List<FeedData>> GetSources(bool disabledToo)
        {
            string response;
            using (var httpClient = new HttpClient())
            {
                //httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("nugex", "1.0.0"));
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                //string creds = string.Format("{0}:{1}", @"ad005\z001rybj", " Netudki989 ");
                byte[] bytes = Encoding.ASCII.GetBytes("c2obu2laifqhr6sogc6jsaucq5rvzpttzagw2wxbh7j2rxuj7s7q");
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "c2obu2laifqhr6sogc6jsaucq5rvzpttzagw2wxbh7j2rxuj7s7q");
                response = await httpClient.GetStringAsync(new Uri("https://apollo.healthcare.siemens.com/tfs/IKM.TPC.Projects"));
            }
            return null;
        }
    }
}
