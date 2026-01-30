using SimHub.Plugins;
using System;
using System.Windows.Media;
using System.ComponentModel;
using System.Threading.Tasks;

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

        // Logic flags for session and garage detection
        private bool _wasInGarageState = false;
        private string lastSessionType = null;
        private int lastLapCount = 0;

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Triggers the PropertyChanged event for UI data binding.
        /// </summary>
        public void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        /// <summary>
        /// Initializes the plugin and registers all SimHub actions for button mapping.
        /// </summary>
        public void Init(PluginManager pluginManager)
        {
            SimHub.Logging.Current.Info("Starting Plugin LMUElectronicBridge");

            PluginManager = pluginManager;
            Settings = this.ReadCommonSettings<ElectronicSettings>("ElectronicSettings", () => new ElectronicSettings());

            // --- 1. PROPERTY DELEGATES ---
            // These allow Dash Studio and Overlays to display the values.
            this.AttachDelegate("TC_Main", () => Settings.TC_Main);
            this.AttachDelegate("TC_Cut", () => Settings.TC_Cut);
            this.AttachDelegate("TC_Slip", () => Settings.TC_Slip);
            this.AttachDelegate("ABS", () => Settings.ABS);

            // --- 2. ACTION BINDINGS ---
            // These allow the user to map buttons/encoders in the SimHub "Controls" menu.
            RegisterControlActions("TC_Main", () => Settings.TC_Main, v => Settings.TC_Main = v, Settings.TC_Main_Max);
            RegisterControlActions("TC_Cut", () => Settings.TC_Cut, v => Settings.TC_Cut = v, Settings.TC_Cut_Max);
            RegisterControlActions("TC_Slip", () => Settings.TC_Slip, v => Settings.TC_Slip = v, Settings.TC_Slip_Max);
            RegisterControlActions("ABS", () => Settings.ABS, v => Settings.ABS = v, Settings.ABS_Max);

            // Manual sync action (can be mapped to a button)
            this.AddAction("SyncFromGame", (a, b) => { _ = SyncAllFromLMU(); });
        }

        /// <summary>
        /// Registers Increase/Decrease actions that can be mapped to hardware buttons.
        /// </summary>
        private void RegisterControlActions(string name, Func<int> getter, Action<int> setter, int maxValue)
        {
            this.AddAction(name + "Increase", (a, b) => {
                if (getter() < maxValue) { setter(getter() + 1); OnPropertyChanged(nameof(Settings)); }
            });
            this.AddAction(name + "Decrease", (a, b) => {
                if (getter() > Settings.MinValue) { setter(getter() - 1); OnPropertyChanged(nameof(Settings)); }
            });
        }

        /// <summary>
        /// Main game loop. Monitors for session resets or garage-to-track transitions.
        /// </summary>
        public void DataUpdate(PluginManager pluginManager, ref GameReaderCommon.GameData data)
        {
            if (data.NewData != null && (pluginManager.GameName == "LMU" || pluginManager.GameName == "LeMansUltimate"))
            {
                bool triggerSync = false;

                // Detect New Session
                if (data.NewData.SessionTypeName != lastSessionType) { lastSessionType = data.NewData.SessionTypeName; triggerSync = true; }

                // Detect Lap Reset / Restart
                if (data.NewData.CurrentLap < lastLapCount && data.NewData.CurrentLap <= 1) triggerSync = true;
                lastLapCount = data.NewData.CurrentLap;

                // Detect Garage Exit (Ignition ON while in Pit)
                bool currentGarageState = (data.NewData.IsInPit == 1 && data.NewData.EngineIgnitionOn == 0);
                if (_wasInGarageState && data.NewData.EngineIgnitionOn == 1) triggerSync = true;
                _wasInGarageState = currentGarageState;

                if (triggerSync) _ = SyncAllFromLMU();
            }
        }

        /// <summary>
        /// Asynchronously pulls current garage settings from the LMU API.
        /// </summary>
        public async Task SyncAllFromLMU()
        {
            try
            {
                var data = await _apiClient.GetElectronicGarageValuesAsync();
                if (data != null && data.IsAvailable)
                {
                    if (data.TC_Main != -1) Settings.TC_Main = data.TC_Main;
                    if (data.TC_Cut != -1) Settings.TC_Cut = data.TC_Cut;
                    if (data.TC_Slip != -1) Settings.TC_Slip = data.TC_Slip;
                    if (data.ABS != -1) Settings.ABS = data.ABS;
                    OnPropertyChanged(nameof(Settings));
                }
            }
            catch (Exception ex) { SimHub.Logging.Current.Error($"Sync Error: {ex.Message}"); }
        }

        public void End(PluginManager pluginManager) => this.SaveCommonSettings("ElectronicSettings", Settings);
        public System.Windows.Controls.Control GetWPFSettingsControl(PluginManager pluginManager) => new SettingsControl(this);
    }
}