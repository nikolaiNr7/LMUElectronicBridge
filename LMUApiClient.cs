using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace LMUElectronicBridge
{
    public class GarageValue
    {
        public int value { get; set; } = -1;
        public string stringValue { get; set; } = null;
        public int minValue { get; set; } = 0;
        public int maxValue { get; set; } = 0;
    }

    public class LmuDataModel
    {
        public GarageValue ABS_Raw { get; set; } = new GarageValue();
        public GarageValue TC_Main_Raw { get; set; } = new GarageValue();
        public GarageValue TC_Slip_Raw { get; set; } = new GarageValue();
        public GarageValue TC_Cut_Raw { get; set; } = new GarageValue();

        // MGU & Engine Blocks
        public GarageValue Regen_Raw { get; set; } = new GarageValue();
        public GarageValue Migration_Raw { get; set; } = new GarageValue();
        public GarageValue MotorMap_Raw { get; set; } = new GarageValue();
        public GarageValue Mixture_Raw { get; set; } = new GarageValue();

        public bool IsAvailable { get; set; }
    }

    public class LmuApiClient
    {
        private static readonly HttpClient _client = new HttpClient { Timeout = TimeSpan.FromMilliseconds(1500) };
        private const string BaseUrl = "http://localhost:6397";

        public async Task<LmuDataModel> GetElectronicGarageValuesAsync()
        {
            try
            {
                string json = await _client.GetStringAsync(BaseUrl + "/rest/garage/getPlayerGarageData");
                var root = JObject.Parse(json);
                var model = new LmuDataModel { IsAvailable = true };

                model.ABS_Raw = GetGarageValue(root, "VM_ANTILOCKBRAKESYSTEMMAP");
                model.TC_Main_Raw = GetGarageValue(root, "VM_TRACTIONCONTROLMAP");
                model.TC_Slip_Raw = GetGarageValue(root, "VM_TRACTIONCONTROLSLIPANGLEMAP");
                model.TC_Cut_Raw = GetGarageValue(root, "VM_TRACTIONCONTROLPOWERCUTMAP");
                model.Regen_Raw = GetGarageValue(root, "VM_REGEN_LEVEL");
                model.Migration_Raw = GetGarageValue(root, "VM_BRAKE_MIGRATION");
                model.MotorMap_Raw = GetGarageValue(root, "VM_ELECTRIC_MOTOR_MAP");
                model.Mixture_Raw = GetGarageValue(root, "VM_ENGINE_MIXTURE");

                return model;
            }
            catch { return new LmuDataModel { IsAvailable = false }; }
        }

        private GarageValue GetGarageValue(JObject root, string key)
        {
            var token = root.SelectToken(key);
            return (token != null && token.HasValues) ? token.ToObject<GarageValue>() : new GarageValue();
        }
    }
}