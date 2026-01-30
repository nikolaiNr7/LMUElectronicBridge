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


        // Flags to avoid redundant syncs
        private bool _wasInGarageState = false;
        private int _lastIgnitionState = -1;
        private string lastSessionType = null;
        private int lastLapCount = 0;


        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Triggers the PropertyChanged event for UI data binding.
        /// </summary>
        /// <param name="name">The name of the property that changed.</param>
        public void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        /// <summary>
        /// Initializes the plugin, loads settings, and registers actions/properties in SimHub.
        /// </summary>
        /// <param name="pluginManager">The SimHub PluginManager instance.</param>
        public void Init(PluginManager pluginManager)
        {
            SimHub.Logging.Current.Info("Starting Plugin LMUElectronitBridge");

            PluginManager = pluginManager;
            Settings = this.ReadCommonSettings<ElectronicSettings>("ElectronicSettings", () => new ElectronicSettings());

            // Expose properties to SimHub (available in Dash Studio / Overlay editors)
            this.AttachDelegate("TC_Main", () => Settings.TC_Main);
            this.AttachDelegate("TC_Cut", () => Settings.TC_Cut);
            this.AttachDelegate("TC_Slip", () => Settings.TC_Slip);
            this.AttachDelegate("ABS", () => Settings.ABS);

            // Register Increase/Decrease actions for mapping to buttons/encoders
            RegisterControlActions("TC_Main", () => Settings.TC_Main, v => Settings.TC_Main = v, Settings.TC_Main_Max);
            RegisterControlActions("TC_Cut", () => Settings.TC_Cut, v => Settings.TC_Cut = v, Settings.TC_Cut_Max);
            RegisterControlActions("TC_Slip", () => Settings.TC_Slip, v => Settings.TC_Slip = v, Settings.TC_Slip_Max);
            RegisterControlActions("ABS", () => Settings.ABS, v => Settings.ABS = v, Settings.ABS_Max);

            // Action to manually trigger a full synchronization from the game
            this.AddAction("SyncAllFromGame", (a, b) => { _ = SyncAllFromLMU(); });
        }

        /// <summary>
        /// Helper method to register standard increase/decrease actions for electronic settings.
        /// </summary>
        /// <param name="name">The base name for the action.</param>
        /// <param name="getter">Function to retrieve the current value.</param>
        /// <param name="setter">Action to update the value.</param>
        /// <param name="maxValue">The upper limit for the setting.</param>
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
        /// Called during the SimHub game loop. Monitors for session resets or garage-to-track transitions.
        /// Retrieves electronic settings from LMU or applies manual values based on user preference.
        /// Requests a sync when:
        ///     New Session
        ///     Laps Reset to 0
        ///     Car was in Garage (Pit + Engine Off) and now Engine is On
        /// </summary>
        public void DataUpdate(PluginManager pluginManager, ref GameReaderCommon.GameData data)
        {
            // Ensure we only process data for Le Mans Ultimate
            if (data.NewData != null && (pluginManager.GameName == "LMU" || pluginManager.GameName == "LeMansUltimate"))
            {
                bool triggerSync = false;

                // --- 1. Debug Telemetry Monitoring ---
                // Log whenever ignition toggles to see the IsInPit status in the log file
                if (data.NewData.EngineIgnitionOn != _lastIgnitionState)
                {
                    _lastIgnitionState = data.NewData.EngineIgnitionOn;
                }

                // --- 2. Session/Lap Detection ---
                if (data.NewData.SessionTypeName != lastSessionType)
                {
                    lastSessionType = data.NewData.SessionTypeName;
                    triggerSync = true;
                }

                if (data.NewData.CurrentLap < lastLapCount && data.NewData.CurrentLap <= 1)
                {
                    triggerSync = true;
                }
                lastLapCount = data.NewData.CurrentLap;

                // --- 3. Garage Logic (Transition from Pit+Off to On) ---
                // Definition: Garage state is strictly "In Pits" AND "Engine Off"
                bool currentGarageState = (data.NewData.IsInPit == 1 && data.NewData.EngineIgnitionOn == 0);

                // TRIGGER: If we were in garage state and just turned the engine ON
                if (_wasInGarageState && data.NewData.EngineIgnitionOn == 1)
                {
                    triggerSync = true;
                }

                // Update the state tracker for the next frame
                _wasInGarageState = currentGarageState;

                // --- 4. Execution ---
                if (triggerSync)
                {
                    if (Settings.UseGameSync)
                    {
                        _ = SyncAllFromLMU();
                    }
                    else
                    {
                        ApplyManualValues();
                    }
                }
            }
        }

        /// <summary>
        /// Asynchronously fetches current garage data from LMU via the REST API.
        /// Falls back to internal SimHub properties if the API is offline.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task SyncAllFromLMU()
        {
            try
            {
                // 1. Attempt the data call
                var data = await _apiClient.GetElectronicGarageValuesAsync();

                // 2. Process data if the call succeeded and returned valid results
                if (data != null && data.IsAvailable)
                {
                    if (data.TC_Main != -1) Settings.TC_Main = data.TC_Main;
                    if (data.TC_Cut != -1) Settings.TC_Cut = data.TC_Cut;
                    if (data.TC_Slip != -1) Settings.TC_Slip = data.TC_Slip;
                    if (data.ABS != -1) Settings.ABS = data.ABS;

                    OnPropertyChanged(nameof(Settings));
                }
                else
                {
                    SimHub.Logging.Current.Warn("ElectronicBridge: API returned unavailable state. Using existing values.");
                }
            }
            catch (Exception ex)
            {
                // 3. Catch and log the specific error without crashing the plugin
                SimHub.Logging.Current.Error($"ElectronicBridge: Critical error during API Sync: {ex.Message}");
                // Optional: Revert to Manual Values as a safe fallback on error
                // ApplyManualValues();
            }


        /// <summary>
        /// Copies the user-defined manual values into the active runtime settings.
        /// </summary>
        public void ApplyManualValues()
        {
            Settings.TC_Main = Settings.TC_Main_User;
            Settings.TC_Cut = Settings.TC_Cut_User;
            Settings.TC_Slip = Settings.TC_Slip_User;
            Settings.ABS = Settings.ABS_User;
            OnPropertyChanged(nameof(Settings));
        }

        /// <summary>
        /// Tests if a specific SimHub property path exists and returns the current value.
        /// </summary>
        /// <param name="path">The full property path to test.</param>
        /// <param name="message">Out parameter containing a status message for the UI.</param>
        /// <returns>True if the property exists; otherwise false.</returns>
        public bool TestProperty(string path, out string message)
        {
            var val = PluginManager.GetPropertyValue(path);
            if (val != null) { message = $"Passed. Found value: {val}"; return true; }
            message = "Failed. Property name not found in SimHub.";
            return false;
        }

        /// <summary>
        /// Safely retrieves an integer value from a SimHub property path.
        /// </summary>
        /// <param name="prop">The property path.</param>
        /// <returns>The integer value, or 0 if the property is not found or invalid.</returns>
        private int GetSafeInt(string prop)
        {
            var val = PluginManager.GetPropertyValue(prop);
            try { return val != null ? Convert.ToInt32(val) : 0; }
            catch { return 0; }
        }

        /// <summary>
        /// Called when SimHub is closing. Saves the current plugin settings.
        /// </summary>
        /// <param name="pluginManager">The SimHub PluginManager instance.</param>
        public void End(PluginManager pluginManager) => this.SaveCommonSettings("ElectronicSettings", Settings);

        /// <summary>
        /// Returns the WPF user control for the plugin's settings menu.
        /// </summary>
        /// <param name="pluginManager">The SimHub PluginManager instance.</param>
        /// <returns>A new instance of SettingsControl.</returns>
        public System.Windows.Controls.Control GetWPFSettingsControl(PluginManager pluginManager) => new SettingsControl(this);
    }
}