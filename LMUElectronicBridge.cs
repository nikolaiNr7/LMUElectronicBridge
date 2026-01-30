using LMUElectronicBridge.LMUElectronicBridge;
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

        public ImageSource PictureIcon => null;
        public string LeftMenuTitle => "LMU Electronics Bridge";

        private int lmuMaxValueOffset = 1; // LMU Max values are 1 higher than actual usable max (e.g., 11 means 0-10)
        private bool _wasInGarageState = false;
        private string lastSessionType = null;
        private int lastLapCount = 0;

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        /// <summary>
        /// Initializes the plugin and registers all SimHub actions and properties.
        /// </summary>
        public void Init(PluginManager pluginManager)
        {
            PluginManager = pluginManager;
            Settings = this.ReadCommonSettings<ElectronicSettings>("ElectronicSettings", () => new ElectronicSettings());

            // --- 1. PROPERTY DELEGATES (For Dashboards/UI) ---
            // These allow you to use [LMUElectronicBridge.TC_Main_Str] in Dash Studio
            RegisterDelegates();

            // --- 2. ACTION BINDINGS (For Wheel Buttons) ---
            RegisterAllActions();

            this.AddAction("SyncFromGame", (a, b) => { _ = SyncAllFromLMU(); });
        }

        private void RegisterDelegates()
        {
            // Primary Electronics
            this.AttachDelegate("TC_Main", () => Settings.TC_Main);
            this.AttachDelegate("TC_Main_Str", () => Settings.TC_Main_Str);
            this.AttachDelegate("TC_Cut", () => Settings.TC_Cut);
            this.AttachDelegate("TC_Cut_Str", () => Settings.TC_Cut_Str);
            this.AttachDelegate("TC_Slip", () => Settings.TC_Slip);
            this.AttachDelegate("TC_Slip_Str", () => Settings.TC_Slip_Str);
            this.AttachDelegate("ABS", () => Settings.ABS);
            this.AttachDelegate("ABS_Str", () => Settings.ABS_Str);

            // MGU & Engine
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

            RegisterControlActions("Regen", () => Settings.RegenLevel, v => Settings.RegenLevel = v, () => Settings.Regen_Min, () => Settings.Regen_Max);
            RegisterControlActions("Migration", () => Settings.BrakeMigration, v => Settings.BrakeMigration = v, () => Settings.Migration_Min, () => Settings.Migration_Max);
            RegisterControlActions("MotorMap", () => Settings.ElectricMotorMap, v => Settings.ElectricMotorMap = v, () => Settings.MotorMap_Min, () => Settings.MotorMap_Max);
            RegisterControlActions("Mixture", () => Settings.EngineMixture, v => Settings.EngineMixture = v, () => Settings.Mixture_Min, () => Settings.Mixture_Max);
        }

        private void RegisterControlActions(string name, Func<int> getter, Action<int> setter, Func<int> minGetter, Func<int> maxGetter)
        {
            this.AddAction(name + "Increase", (a, b) => {
                int current = getter();
                int max = maxGetter();
                if (current + 1 <= max)
                {
                    setter(current + 1);
                    OnPropertyChanged(nameof(Settings));
                }
            });

            this.AddAction(name + "Decrease", (a, b) => {
                int current = getter();
                int min = minGetter();
                if (current - 1 >= min)
                {
                    setter(current - 1);
                    OnPropertyChanged(nameof(Settings));
                }
            });
        }

        /// <summary>
        /// Main Sync Logic: Fetches raw data from API and maps it to Plugin Settings.
        /// </summary>
        public async Task SyncAllFromLMU()
        {
            try
            {
                var data = await _apiClient.GetElectronicGarageValuesAsync();
                if (data != null && data.IsAvailable)
                {
                    // Local helper for mapping Raw JSON objects to Settings
                    void Map(GarageValue raw, Action<int> valSet, Action<string> strSet, Action<int> minSet, Action<int> maxSet)
                    {
                        if (raw.value != -1)
                        {
                            minSet(raw.minValue);
                            maxSet(raw.maxValue - lmuMaxValueOffset); // Adjust for the 12->11 offset of simhub Max values
                            valSet(raw.value);
                            strSet(raw.stringValue);
                        }
                    }

                    Map(data.TC_Main_Raw, v => Settings.TC_Main = v, s => Settings.TC_Main_Str = s, min => Settings.TC_Main_Min = min, max => Settings.TC_Main_Max = max);
                    Map(data.TC_Cut_Raw, v => Settings.TC_Cut = v, s => Settings.TC_Cut_Str = s, min => Settings.TC_Cut_Min = min, max => Settings.TC_Cut_Max = max);
                    Map(data.TC_Slip_Raw, v => Settings.TC_Slip = v, s => Settings.TC_Slip_Str = s, min => Settings.TC_Slip_Min = min, max => Settings.TC_Slip_Max = max);
                    Map(data.ABS_Raw, v => Settings.ABS = v, s => Settings.ABS_Str = s, min => Settings.ABS_Min = min, max => Settings.ABS_Max = max);

                    Map(data.Regen_Raw, v => Settings.RegenLevel = v, s => Settings.RegenLevel_Str = s, min => Settings.Regen_Min = min, max => Settings.Regen_Max = max);
                    Map(data.Migration_Raw, v => Settings.BrakeMigration = v, s => Settings.BrakeMigration_Str = s, min => Settings.Migration_Min = min, max => Settings.Migration_Max = max);
                    Map(data.MotorMap_Raw, v => Settings.ElectricMotorMap = v, s => Settings.ElectricMotorMap_Str = s, min => Settings.MotorMap_Min = min, max => Settings.MotorMap_Max = max);
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

                // Session Change
                if (data.NewData.SessionTypeName != lastSessionType)
                {
                    lastSessionType = data.NewData.SessionTypeName;
                    triggerSync = true;
                }

                // Restart / New Lap
                if (data.NewData.CurrentLap < lastLapCount && data.NewData.CurrentLap <= 1) triggerSync = true;
                lastLapCount = data.NewData.CurrentLap;

                // Garage Exit
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