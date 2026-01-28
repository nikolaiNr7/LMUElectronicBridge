using SimHub.Plugins;
using System;
using System.Windows.Media;
using System.ComponentModel;
using System.Threading.Tasks;

namespace LMUElectronicBridge
{
    [PluginName("LMU Electronic Bridge")]
    [PluginAuthor("Nikolai Schlott")]
    public class LMUElectronicBridge : IPlugin, IWPFSettingsV2, INotifyPropertyChanged
    {
        public PluginManager PluginManager { get; set; }
        public ElectronicSettings Settings { get; private set; }
        private LmuApiClient _apiClient = new LmuApiClient();

        public ImageSource PictureIcon => null;
        public string LeftMenuTitle => "LMU Electronics Bridge";

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
            SimHub.Logging.Current.Info("ElectronicBridge Initializing -------------------------------------------------");

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
        /// Called during the SimHub game loop. Handles automatic synchronization on session or lap changes.
        /// </summary>
        /// <param name="pluginManager">The SimHub PluginManager instance.</param>
        /// <param name="data">The current game telemetry data.</param>
        public void DataUpdate(PluginManager pluginManager, ref GameReaderCommon.GameData data)
        {
            if (data.NewData != null && (pluginManager.GameName == "LMU" || pluginManager.GameName == "LeMansUltimate"))
            {
                bool sessionTrigger = false;

                // Trigger sync if the session type (Practice/Qualy/Race) changes
                if (data.NewData.SessionTypeName != lastSessionType) { lastSessionType = data.NewData.SessionTypeName; sessionTrigger = true; }

                // Trigger sync if the lap resets (lap 0 or 1 usually indicates a fresh start)
                if (data.NewData.CurrentLap < lastLapCount && data.NewData.CurrentLap <= 1) { sessionTrigger = true; }
                lastLapCount = data.NewData.CurrentLap;

                if (sessionTrigger)
                {
                    if (Settings.UseGameSync) _ = SyncAllFromLMU();
                    else ApplyManualValues();
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
            var data = await _apiClient.GetElectronicGarageValuesAsync();

            if (data.IsAvailable)
            {
                if (data.TC_Main != -1) Settings.TC_Main = data.TC_Main;
                if (data.TC_Cut != -1) Settings.TC_Cut = data.TC_Cut;
                if (data.TC_Slip != -1) Settings.TC_Slip = data.TC_Slip;
                if (data.ABS != -1) Settings.ABS = data.ABS;
                OnPropertyChanged(nameof(Settings));
            }
            else
            {
                SyncFromPropertiesFallback();
            }
        }

        /// <summary>
        /// Updates the active settings by reading specific SimHub properties (e.g., from other plugins).
        /// </summary>
        private void SyncFromPropertiesFallback()
        {
            Settings.TC_Main = GetSafeInt(Settings.PropPath_TC_Main);
            Settings.TC_Cut = GetSafeInt(Settings.PropPath_TC_Cut);
            Settings.TC_Slip = GetSafeInt(Settings.PropPath_TC_Slip);
            Settings.ABS = GetSafeInt(Settings.PropPath_ABS);
            OnPropertyChanged(nameof(Settings));
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