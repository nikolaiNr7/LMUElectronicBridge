using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq; // using Newtonsoft.Json for JSON parsing already used in SimHub

namespace LMUElectronicBridge
{
    /// <summary>
    /// Container für die aus der API gelesenen Werte.
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
        // HttpClient als statische Instanz, um Socket-Erschöpfung in .NET 4.8 zu vermeiden
        private static readonly HttpClient _client = new HttpClient { Timeout = TimeSpan.FromMilliseconds(800) };
        private const string BaseUrl = "http://localhost:6397";

        public async Task<LmuDataModel> GetElectronicGarageValuesAsync()
        {
            try
            {
                // Abfrage der LMU Garage API
                string json = await _client.GetStringAsync(BaseUrl + "/rest/garage/getPlayerGarageData");

                // Parsing mit JObject (Newtonsoft)
                var root = JObject.Parse(json);
                var model = new LmuDataModel();

                // Extraktion der Integer-Werte
                model.ABS = GetValue(root, "VM_ANTILOCKBRAKESYSTEMMAP");
                model.TC_Main = GetValue(root, "VM_TRACTIONCONTROLMAP");
                model.TC_Slip = GetValue(root, "VM_TRACTIONCONTROLSLIPANGLEMAP");
                model.TC_Cut = GetValue(root, "VM_TRACTIONCONTROLPOWERCUTMAP");

                // Extraktion der String-Werte
                model.RegenLevel = GetStringValue(root, "VM_REGEN_LEVEL");
                model.BrakeMigration = GetStringValue(root, "VM_BRAKE_MIGRATION");
                model.ElectricMotorMap = GetStringValue(root, "VM_ELECTRIC_MOTOR_MAP");
                model.EngineMixture = GetStringValue(root, "VM_ENGINE_MIXTURE");

                model.IsAvailable = true;
                return model;
            }
            catch (Exception)
            {
                // Falls LMU nicht läuft oder die API nicht antwortet
                return new LmuDataModel { IsAvailable = false };
            }
        }

        /// <summary>
        /// Liest einen Integer aus dem "value"-Feld eines JSON-Objekts.
        /// Beispiel-Pfad im JSON: root["VM_TRACTIONCONTROLMAP"]["value"]
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
        /// Liest einen String aus dem "stringValue"-Feld.
        /// </summary>
        private string GetStringValue(JObject root, string key)
        {
            var token = root.SelectToken(key + ".stringValue");
            if (token != null && token.Type != JTokenType.Null)
            {
                string val = token.ToString();
                // LMU sendet oft "N/A" für nicht vorhandene Elektronik (z.B. GT3 ohne RegenLevel)
                if (val == "N/A" || val == "N/V") return null;
                return val;
            }
            return null;
        }
    }
}