using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace LMUElectronicBridge
{
    /// <summary>
    /// Handles HTTP communication with the Le Mans Ultimate REST API.
    /// Includes a built-in Circuit Breaker to prevent performance degradation and log spam during API outages.
    /// </summary>
    public class LmuApiClient
    {
        //----- Network Members -----------------------------------------------
        private static readonly HttpClient _client = new HttpClient { Timeout = TimeSpan.FromMilliseconds(2000) };
        private const string BaseUrl = "http://localhost:6397";

        //----- Circuit Breaker State -----------------------------------------
        private DateTime _lastFailureTime = DateTime.MinValue;
        private int _consecutiveFailures = 0;
        private readonly TimeSpan _coolDownPeriod = TimeSpan.FromSeconds(5);
        private bool _wasDown = false;

        /// <summary>
        /// Gets a value indicating whether the API is currently in a "cooling off" period after a failure.
        /// Use this in the main loop to skip API calls when the game is in menus or closed.
        /// </summary>
        public bool IsInCooldown => (DateTime.Now - _lastFailureTime) < _coolDownPeriod;

        //----- Internal Helper ----------------------------------------------

        /// <summary>
        /// Executes a GET request with error handling and circuit breaker logic.
        /// </summary>
        /// <param name="endpoint">The API endpoint (e.g., "/rest/garage/...")</param>
        /// <returns>The raw JSON string result, or null if the request fails or is in cooldown.</returns>
        private async Task<string> GetAsyncSafe(string endpoint)
        {
            // Tier 1: Circuit Breaker - Don't even try if we are cooling down
            if (IsInCooldown) return null;

            try
            {
                string response = await _client.GetStringAsync(BaseUrl + endpoint);

                // Recovery Logic: If we were previously down, log the restoration
                if (_wasDown)
                {
                    SimHub.Logging.Current.Info("[LMU Electronic Bridge] LMU API: Connection restored.");
                    _wasDown = false;
                }

                _consecutiveFailures = 0;
                return response;
            }
            catch (Exception ex)
            {
                _consecutiveFailures++;
                _lastFailureTime = DateTime.Now;
                _wasDown = true;

                // Tier 2: Throttled Logging - Only log warnings for the first 3 failures
                if (_consecutiveFailures <= 3)
                {
                    SimHub.Logging.Current.Warn($"[LMU Electronic Bridge]LMU API: Request to {endpoint} failed (Attempt {_consecutiveFailures}).\n Make sure LMU Game is running. Also you are in an active Session not in the Menu.\n Error: {ex.Message}");
                }

                return null;
            }
        }

        //----- Public API Methods -------------------------------------------

        /// <summary>
        /// Fetches the full garage JSON from LMU containing electronic settings.
        /// </summary>
        /// <returns>A JObject containing the garage data, or null if the request fails.</returns>
        public async Task<JObject> GetRawGarageDataAsync()
        {
           
            try
            {
                string json = await GetAsyncSafe("/rest/garage/getPlayerGarageData");
                return json != null ? JObject.Parse(json) : null;
            }
            catch (Exception ex)
            {
                SimHub.Logging.Current.Error($"[LMU Electronic Bridge] LMU API: Failed: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Fetches vehicle status data from the TireManagement endpoint.
        /// Extracts team information, suspension wear, and aerodynamic damage.
        /// </summary>
        /// <returns>A JObject containing teamInfo, suspension, and aero damage, or null if the request fails.</returns>
        public async Task<JObject> GetVehicleStatusDataAsync()
        {
            try
            {
                string json = await GetAsyncSafe("/rest/garage/UIScreen/TireManagement");
                if (json == null) return null;
                JObject root = JObject.Parse(json);

                // We reconstruct a smaller JObject to keep memory usage low and clarify the schema
                return new JObject
                {
                    ["teamInfo"] = root["teamInfo"],
                    ["suspensionDamage"] = root["wearables"]?["suspension"],
                    ["aeroDamage"] = root["wearables"]?["body"]?["aero"]
                };
            }
            catch (Exception ex)
            {
                SimHub.Logging.Current.Error($"[LMU Electronic Bridge] LMU API: Failed : {ex.Message}");
                return null;
            }
        }
    }
}