using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace LMUElectronicBridge
{
    public class LmuDataModel
    {
        public int ABS { get; set; } = -1;
        public int TC_Main { get; set; } = -1;
        public int TC_Slip { get; set; } = -1;
        public int TC_Cut { get; set; } = -1;

        // Platzhalter für spätere Erweiterungen
        public string EngineMixture { get; set; }
        public string RegenLevel { get; set; }
        public string BrakeMigration { get; set; }
        public string ElectricMotorMap { get; set; }
  

        public bool IsAvailable { get; set; }
    }

    public class LmuApiClient
    {
        private static readonly HttpClient _client = new HttpClient { Timeout = TimeSpan.FromMilliseconds(800) };
        private const string BaseUrl = "http://localhost:6397";

        public async Task<LmuDataModel> GetElectronicGarageValuesAsync()
        {
            try
            {
                string json = await _client.GetStringAsync($"{BaseUrl}/rest/garage/getPlayerGarageData");
                using (JsonDocument doc = JsonDocument.Parse(json))
                {
                    JsonElement root = doc.RootElement;

                    return new LmuDataModel
                    {
                        ABS = GetValue(root, "VM_ANTILOCKBRAKESYSTEMMAP"),
                        TC_Main = GetValue(root, "VM_TRACTIONCONTROLMAP"),
                        TC_Slip = GetValue(root, "VM_TRACTIONCONTROLSLIPANGLEMAP"),
                        TC_Cut = GetValue(root, "VM_TRACTIONCONTROLPOWERCUTMAP"),

                        RegenLevel = GetStringValue(root, "VM_REGEN_LEVEL"),
                        BrakeMigration = GetStringValue(root, "VM_BRAKE_MIGRATION"),
                        ElectricMotorMap = GetStringValue(root, "VM_ELECTRIC_MOTOR_MAP"),
                        EngineMixture = GetStringValue(root, "VM_ENGINE_MIXTURE"),

                        IsAvailable = true
                    };
                }
            }
            catch
            {
                return new LmuDataModel { IsAvailable = false };
            }
        }

        private int GetValue(JsonElement root, string key)
        {
            if (root.TryGetProperty(key, out JsonElement element) && element.TryGetProperty("value", out JsonElement valProp))
            {
                return valProp.GetInt32();
            }
            return -1;
        }

        private string GetStringValue(JsonElement root, string key)
        {
            if (root.TryGetProperty(key, out JsonElement element) && element.TryGetProperty("stringValue", out JsonElement strProp))
            {
                string val = strProp.GetString();
                return (val == "N/A" || val == "N/V") ? null : val;
            }
            return null;
        }
    }
}