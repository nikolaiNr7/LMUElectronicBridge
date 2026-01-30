using SimHub.Plugins;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Media;

namespace LMUElectronicBridge
{
    [PluginName("LMU Electronic Bridge")]
    [PluginAuthor("Nikolai Schlott")]
    public class LMUElectronicBridge : IPlugin, IDataPlugin, IWPFSettingsV2, INotifyPropertyChanged
    {
        public PluginManager PluginManager { get; set; }
        public ElectronicSettings Settings { get; private set; }
        private LmuApiClient _apiClient = new LmuApiClient();

        // --- LOOKUP TABLES ---
        private static readonly string[] BrakeMigrationTable = { "Disabled", "0.5% F", "1.0% F", "1.5% F", "2.0% F", "2.5% F" };
        private static readonly string[] MotorMapTable = { "Off", "10 kW", "20 kW", "30 kW", "40 kW", "50 kW" };
        private static readonly string[] RegenTable = { "Off", "17 kW", "34 kW", "51 kW", "68 kW", "85 kW", "102 kW", "119 kW", "136 kW", "153 kW", "170 kW" };

        public ImageSource PictureIcon => null;
        public string LeftMenuTitle => "LMU Electronics Bridge";

        private int lmuMaxValueOffset = 1;
        private bool _wasInGarageState = false;
        private string lastSessionType = null;
        private int lastLapCount = 0;

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public void Init(PluginManager pluginManager)
        {
            PluginManager = pluginManager;
            Settings = this.ReadCommonSettings<ElectronicSettings>("ElectronicSettings", () => new ElectronicSettings());

            RegisterDelegates();
            RegisterAllActions();

            this.AddAction("SyncFromGame", (a, b) => { _ = SyncAllFromLMU(); });
        }

        private void RegisterDelegates()
        {
            this.AttachDelegate("TC_Main", () => Settings.TC_Main);
            this.AttachDelegate("TC_Main_Str", () => Settings.TC_Main_Str);
            this.AttachDelegate("TC_Cut", () => Settings.TC_Cut);
            this.AttachDelegate("TC_Cut_Str", () => Settings.TC_Cut_Str);
            this.AttachDelegate("TC_Slip", () => Settings.TC_Slip);
            this.AttachDelegate("TC_Slip_Str", () => Settings.TC_Slip_Str);
            this.AttachDelegate("ABS", () => Settings.ABS);
            this.AttachDelegate("ABS_Str", () => Settings.ABS_Str);

            this.AttachDelegate("RegenLevel", () => Settings.RegenLevel);
            this.AttachDelegate("RegenLevel_Str", () => Settings.RegenLevel_Str);
            this.AttachDelegate("BrakeMigration", () => Settings.BrakeMigration);
            this.AttachDelegate("BrakeMigration_Str", () => Settings.BrakeMigration_Str);
            this.AttachDelegate("ElectricMotorMap", () => Settings.ElectricMotorMap);
            this.AttachDelegate("ElectricMotorMap_Str", () => Settings.ElectricMotorMap_Str);
            this.AttachDelegate("EngineMixture", () => Settings.EngineMixture);
            this.AttachDelegate("EngineMixture_Str", () => Settings.EngineMixture_Str);
        }

        private void RegisterAllActions()
        {
            RegisterControlActions("TC_Main", () => Settings.TC_Main, v => Settings.TC_Main = v, () => Settings.TC_Main_Min, () => Settings.TC_Main_Max);
            RegisterControlActions("TC_Cut", () => Settings.TC_Cut, v => Settings.TC_Cut = v, () => Settings.TC_Cut_Min, () => Settings.TC_Cut_Max);
            RegisterControlActions("TC_Slip", () => Settings.TC_Slip, v => Settings.TC_Slip = v, () => Settings.TC_Slip_Min, () => Settings.TC_Slip_Max);
            RegisterControlActions("ABS", () => Settings.ABS, v => Settings.ABS = v, () => Settings.ABS_Min, () => Settings.ABS_Max);

            // Manual Mapping Actions
            RegisterControlActions("Regen", () => Settings.RegenLevel, v => {
                Settings.RegenLevel = v; Settings.RegenLevel_Str = Lookup(RegenTable, v);
            }, () => Settings.Regen_Min, () => Settings.Regen_Max);

            RegisterControlActions("BrakeMigration", () => Settings.BrakeMigration, v => {
                Settings.BrakeMigration = v; Settings.BrakeMigration_Str = Lookup(BrakeMigrationTable, v);
            }, () => Settings.BrakeMigration_Min, () => Settings.BrakeMigration_Max);

            RegisterControlActions("MotorMap", () => Settings.ElectricMotorMap, v => {
                Settings.ElectricMotorMap = v; Settings.ElectricMotorMap_Str = Lookup(MotorMapTable, v);
            }, () => Settings.MotorMap_Min, () => Settings.MotorMap_Max);

            RegisterControlActions("Mixture", () => Settings.EngineMixture, v => Settings.EngineMixture = v, () => Settings.Mixture_Min, () => Settings.Mixture_Max);
        }

        private string Lookup(string[] table, int index)
        {
            if (index < 0) return table[0];
            if (index >= table.Length) return table[table.Length - 1];
            return table[index];
        }

        private void RegisterControlActions(string name, Func<int> getter, Action<int> setter, Func<int> minGetter, Func<int> maxGetter)
        {
            this.AddAction(name + "Increase", (a, b) => {
                int current = getter();
                if (current + 1 <= maxGetter()) { setter(current + 1); OnPropertyChanged(nameof(Settings)); }
            });
            this.AddAction(name + "Decrease", (a, b) => {
                int current = getter();
                if (current - 1 >= minGetter()) { setter(current - 1); OnPropertyChanged(nameof(Settings)); }
            });
        }

        public async Task SyncAllFromLMU()
        {
            try
            {
                var data = await _apiClient.GetElectronicGarageValuesAsync();
                if (data != null && data.IsAvailable)
                {
                    void Map(GarageValue raw, Action<int> valSet, Action<string> strSet, Action<int> minSet, Action<int> maxSet, string[] customTable = null)
                    {
                        if (raw.value != -1)
                        {
                            minSet(raw.minValue);
                            maxSet(raw.maxValue - lmuMaxValueOffset);
                            valSet(raw.value);
                            // If table exists use it, otherwise strictly default to value string
                            strSet(customTable != null ? Lookup(customTable, raw.value) : raw.value.ToString());
                        }
                    }

                    Map(data.TC_Main_Raw, v => Settings.TC_Main = v, s => Settings.TC_Main_Str = s, min => Settings.TC_Main_Min = min, max => Settings.TC_Main_Max = max);
                    Map(data.TC_Cut_Raw, v => Settings.TC_Cut = v, s => Settings.TC_Cut_Str = s, min => Settings.TC_Cut_Min = min, max => Settings.TC_Cut_Max = max);
                    Map(data.TC_Slip_Raw, v => Settings.TC_Slip = v, s => Settings.TC_Slip_Str = s, min => Settings.TC_Slip_Min = min, max => Settings.TC_Slip_Max = max);
                    Map(data.ABS_Raw, v => Settings.ABS = v, s => Settings.ABS_Str = s, min => Settings.ABS_Min = min, max => Settings.ABS_Max = max);

                    // Systems with Custom Lookups
                    Map(data.Regen_Raw, v => Settings.RegenLevel = v, s => { }, min => Settings.Regen_Min = min, max => Settings.Regen_Max = max, RegenTable);
                    Map(data.BrakeMigration_Raw,
                         v => Settings.BrakeMigration = v,
                         s => Settings.BrakeMigration_Str = s, // Ensure the string is actually set
                         min => Settings.BrakeMigration_Min = min,
                         max => Settings.BrakeMigration_Max = max,
                         BrakeMigrationTable);
                    Map(data.MotorMap_Raw, v => Settings.ElectricMotorMap = v, s => { }, min => Settings.MotorMap_Min = min, max => Settings.MotorMap_Max = max, MotorMapTable);

                    Map(data.Mixture_Raw, v => Settings.EngineMixture = v, s => Settings.EngineMixture_Str = s, min => Settings.Mixture_Min = min, max => Settings.Mixture_Max = max);

                    OnPropertyChanged(nameof(Settings));
                }
            }
            catch (Exception ex) { SimHub.Logging.Current.Error($"Sync Error: {ex.Message}"); }
        }

        public void DataUpdate(PluginManager pluginManager, ref GameReaderCommon.GameData data)
        {
            if (data.NewData != null && (pluginManager.GameName == "LMU" || pluginManager.GameName == "LeMansUltimate"))
            {
                bool triggerSync = false;
                if (data.NewData.SessionTypeName != lastSessionType) { lastSessionType = data.NewData.SessionTypeName; triggerSync = true; }
                if (data.NewData.CurrentLap < lastLapCount && data.NewData.CurrentLap <= 1) triggerSync = true;
                lastLapCount = data.NewData.CurrentLap;

                bool currentGarageState = (data.NewData.IsInPit == 1 && data.NewData.EngineIgnitionOn == 0);
                if (_wasInGarageState && data.NewData.EngineIgnitionOn == 1) triggerSync = true;
                _wasInGarageState = currentGarageState;

                if (triggerSync) _ = SyncAllFromLMU();
            }
        }

        public void End(PluginManager pluginManager) => this.SaveCommonSettings("ElectronicSettings", Settings);
        public System.Windows.Controls.Control GetWPFSettingsControl(PluginManager pluginManager) => new SettingsControl(this);
    }
}