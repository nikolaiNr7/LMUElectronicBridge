using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace LMUElectronicBridge
{
    /// <summary>
    /// Handles HTTP communication with the Le Mans Ultimate REST API.
    /// </summary>
    public class LmuApiClient
    {
        //----- Properties & Members -----------------------------------------
        private static readonly HttpClient _client = new HttpClient { Timeout = TimeSpan.FromMilliseconds(2000) };
        private const string BaseUrl = "http://localhost:6397";

        //----- API Methods --------------------------------------------------

        /// <summary>
        /// Fetches the full garage JSON from LMU.
        /// </summary>
        /// <returns>A JObject containing the garage data, or null if the request fails.</returns>
        public async Task<JObject> GetRawGarageDataAsync()
        {
            try
            {
                // Requesting player garage data from the local LMU web server
                string json = await _client.GetStringAsync(BaseUrl + "/rest/garage/getPlayerGarageData");
                return JObject.Parse(json);
            }
            catch (Exception ex)
            {
                // Logging error to SimHub's internal logger for troubleshooting
                SimHub.Logging.Current.Error($"LMU API Connection Failed: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Fetches vehicle status data from the TireManagement endpoint.
        /// Includes team information, suspension wear, and aerodynamic damage.
        /// </summary>
        /// <returns>A JObject containing teamInfo, wearables.suspension, and wearables.body.aero, or null if the request fails.</returns>
        public async Task<JObject> GetVehicleStatusDataAsync()
        {
            try
            {
                string json = await _client.GetStringAsync(BaseUrl + "/rest/garage/UIScreen/TireManagement");
                JObject root = JObject.Parse(json);

                // Extract the relevant data sections
                var vehicleStatusData = new JObject
                {
                    ["teamInfo"] = root["teamInfo"],
                    ["suspensionDamage"] = root["wearables"]?["suspension"],
                    ["aeroDamage"] = root["wearables"]?["body"]?["aero"]
                };

                return vehicleStatusData;
            }
            catch (Exception ex)
            {
                SimHub.Logging.Current.Error($"LMU Vehicle Status API Connection Failed: {ex.Message}");
                return null;
            }
        }
    }
}