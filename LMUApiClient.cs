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
        /// Returns null if the request failed or response cannot be parsed.
        /// </summary>
        public async Task<JObject> GetRawGarageDataAsync(System.Threading.CancellationToken cancellationToken = default)
        {
            var uri = new Uri(BaseUrl + "/rest/garage/getPlayerGarageData");
            try
            {
                using (var resp = await _client.GetAsync(uri, cancellationToken).ConfigureAwait(false))
                {
                    if (!resp.IsSuccessStatusCode)
                    {
                        SimHub.Logging.Current.Warn($"LMU API returned non-success status: {resp.StatusCode}");
                        return null;
                    }

                    var json = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                    if (string.IsNullOrWhiteSpace(json)) return null;
                    return JObject.Parse(json);
                }
            }
            catch (TaskCanceledException tex) when (!cancellationToken.IsCancellationRequested)
            {
                SimHub.Logging.Current.Warn($"LMU API request timed out: {tex.Message}");
                return null;
            }
            catch (HttpRequestException hex)
            {
                SimHub.Logging.Current.Error($"LMU API HTTP error: {hex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                SimHub.Logging.Current.Error($"LMU API Connection Failed: {ex.Message}");
                return null;
            }
        }
    }
}