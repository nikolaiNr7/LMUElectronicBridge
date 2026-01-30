using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace LMUElectronicBridge
{
    public class LmuApiClient
    {
        private static readonly HttpClient _client = new HttpClient { Timeout = TimeSpan.FromMilliseconds(2000) };
        private const string BaseUrl = "http://localhost:6397";

        /// <summary>
        /// Fetches the full garage JSON from LMU.
        /// </summary>
        public async Task<JObject> GetRawGarageDataAsync()
        {
            try
            {
                string json = await _client.GetStringAsync(BaseUrl + "/rest/garage/getPlayerGarageData");
                return JObject.Parse(json);
            }
            catch (Exception ex)
            {
                SimHub.Logging.Current.Error($"LMU API Connection Failed: {ex.Message}");
                return null;
            }
        }
    }
}