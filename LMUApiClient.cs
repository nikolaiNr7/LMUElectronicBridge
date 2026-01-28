using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq; // Using Newtonsoft.Json for JSON parsing already used in SimHub

namespace LMUElectronicBridge
{
    /// <summary>
    /// Container for the values read from the API.
    /// </summary>
    public class LmuDataModel
    {
        public int ABS { get; set; } = -1;
        public int TC_Main { get; set; } = -1;
        public int TC_Slip { get; set; } = -1;
        public int TC_Cut { get; set; } = -1;

        public string RegenLevel { get; set; }
        public string BrakeMigration { get; set; }
        public string ElectricMotorMap { get; set; }
        public string EngineMixture { get; set; }

        public bool IsAvailable { get; set; }
    }

    public class LmuApiClient
    {
        // HttpClient as a static instance to avoid socket exhaustion in .NET 4.8
        private static readonly HttpClient _client = new HttpClient { Timeout = TimeSpan.FromMilliseconds(800) };
        private const string BaseUrl = "http://localhost:6397";

        public async Task<LmuDataModel> GetElectronicGarageValuesAsync()
        {
            try
            {
                // Request data from the LMU Garage API
                string json = await _client.GetStringAsync(BaseUrl + "/rest/garage/getPlayerGarageData");

                // Parsing with JObject (Newtonsoft)
                var root = JObject.Parse(json);
                var model = new LmuDataModel();

                // Extraction of integer values
                model.ABS = GetValue(root, "VM_ANTILOCKBRAKESYSTEMMAP");
                model.TC_Main = GetValue(root, "VM_TRACTIONCONTROLMAP");
                model.TC_Slip = GetValue(root, "VM_TRACTIONCONTROLSLIPANGLEMAP");
                model.TC_Cut = GetValue(root, "VM_TRACTIONCONTROLPOWERCUTMAP");

                // Extraction of string values
                model.RegenLevel = GetStringValue(root, "VM_REGEN_LEVEL");
                model.BrakeMigration = GetStringValue(root, "VM_BRAKE_MIGRATION");
                model.ElectricMotorMap = GetStringValue(root, "VM_ELECTRIC_MOTOR_MAP");
                model.EngineMixture = GetStringValue(root, "VM_ENGINE_MIXTURE");

                model.IsAvailable = true;
                return model;
            }
            catch (Exception)
            {
                // Return unavailable model if LMU is not running or API does not respond
                return new LmuDataModel { IsAvailable = false };
            }
        }

        /// <summary>
        /// Reads an integer from the "value" field of a JSON object.
        /// Example path in JSON: root["VM_TRACTIONCONTROLMAP"]["value"]
        /// </summary>
        private int GetValue(JObject root, string key)
        {
            var token = root.SelectToken(key + ".value");
            if (token != null && token.Type != JTokenType.Null)
            {
                return token.Value<int>();
            }
            return -1;
        }

        /// <summary>
        /// Reads a string from the "stringValue" field.
        /// </summary>
        private string GetStringValue(JObject root, string key)
        {
            var token = root.SelectToken(key + ".stringValue");
            if (token != null && token.Type != JTokenType.Null)
            {
                string val = token.ToString();
                // LMU often sends "N/A" or "N/V" for non-existent electronics (e.g., GT3 without RegenLevel)
                if (val == "N/A" || val == "N/V") return null;
                return val;
            }
            return null;
        }
    }
}