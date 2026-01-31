// -------------------------------------------------------------------------
// LMU Electronic Bridge for SimHub
// Developed by: [Nikolai Schlott]
// License: CC BY-NC 4.0 (Attribution-NonCommercial)
// -------------------------------------------------------------------------

using Newtonsoft.Json.Linq;
using SimHub.Plugins;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Markup;
using System.Windows.Media;

namespace LMUElectronicBridge
{
    [PluginName("LMU Electronic Bridge")]
    public class LMUElectronicBridge : IPlugin, IDataPlugin, IWPFSettingsV2, INotifyPropertyChanged
    {
        //----- Properties & Members -----------------------------------------
        public PluginManager PluginManager { get; set; }
        public ImageSource PictureIcon => null;
        public string LeftMenuTitle => "LMU Electronics Bridge";
        public ElectronicSettings Settings { get; private set; }

        private LmuApiClient _apiClient = new LmuApiClient();


        //----- Constants ------------------------------------------------------
        private const int LMU_MAX_VALUE_OFFSET = 1; // LMU API max values are exclusive, so we need to subtract 1

        //----- State Tracking for resync get Garage Values ------------------
        private string lastSessionType = "";
        private double lastLapCount = 0;
        private bool _wasInGarageState = false;
        private bool _firstLoadSyncDone = false;

        //----- Events -------------------------------------------------------
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        //----- Initialize ---------------------------------------------------
        /// <summary>
        /// Instance of the plugin at startup.
        /// </summary>
        public void Init(PluginManager pluginManager)
        {
            PluginManager = pluginManager;
            Settings = this.ReadCommonSettings<ElectronicSettings>("ElectronicSettings", () => new ElectronicSettings());

            // Automatically register all properties and actions based on ElectronicSettings
            AutoRegister();

            // Register the manual sync action
            this.AddAction("SyncFromGame", (a, b) => { _ = SyncAllFromLMU(); });
        }

        //########### Core Data Loop #####################################################

        /// <summary>
        /// Method called at every SimHub data refresh.
        /// // Triggers sync from LMU Garage API based on game state changes.
        /// State changes monitored:
        ///     Session Type Change
        ///     Garage Exit 
        ///     Lap Reset (Teleport to pits or Restart)
        /// </summary>
        public void DataUpdate(PluginManager pluginManager, ref GameReaderCommon.GameData data)
        {
            // 1. Basic Check: Is the game running?
            bool isLmu = data.NewData != null && (pluginManager.GameName == "LMU" || pluginManager.GameName == "LeMansUltimate");
            if (isLmu)
            {

                // check is hypercar to enable/disable certain settings
                Settings.IsHypercar = (data.NewData.CarClass == "LMH" || data.NewData.CarClass == "LMDh" || data.NewData.CarClass == "Hypercar" || data.NewData.CarClass == "Hyper");


                // FORCE SYNC ON FIRST LOAD
                // This covers the "SimHub started late" scenario
                if (!_firstLoadSyncDone)
                {
                    _firstLoadSyncDone = true;
                    SimHub.Logging.Current.Info("LMU Bridge: Initial sync on plugin load.");
                    _ = SyncAllFromLMU();
                }

                bool triggerSync = false;

                // Trigger: Session Change
                if (data.NewData.SessionTypeName != lastSessionType)
                {
                    lastSessionType = data.NewData.SessionTypeName;
                    triggerSync = true;
                }

                // Trigger: Lap Reset (Teleport to pits or Restart)
                if (data.NewData.CurrentLap < lastLapCount && data.NewData.CurrentLap <= 1) triggerSync = true;
                lastLapCount = data.NewData.CurrentLap;

                // Trigger: Garage Exit (Ignition on while in pits)
                bool currentGarageState = (data.NewData.IsInPit == 1 && data.NewData.EngineIgnitionOn == 0);
                if (_wasInGarageState && data.NewData.EngineIgnitionOn == 1) triggerSync = true;
                _wasInGarageState = currentGarageState;

                if (triggerSync) _ = SyncAllFromLMU();
            }
            else
            {
                // Reset the flag if the game is closed, so it's ready for the next launch
                _firstLoadSyncDone = false;
            }
        }

        //########### APY Sync Logic ##################################################################


        /// <summary>
        /// Fetches all electronic data from the LMU API and updates settings.
        /// </summary>
        public async Task SyncAllFromLMU()
        {
            // ---  Team Info Sync ---
            JToken teamData = await _apiClient.GetTeamInfoAsync();
            if (teamData != null)
            {
                string teamName = teamData["teamName"]?.ToString() ?? "N/A";
                Settings.TeamName = teamName;
                Settings.VehicleName = teamData["vehicleName"]?.ToString() ?? "N/A";

                // Initialize the team profile for dynamic lookups
                Settings.ActiveTeamProfile = new TeamLookupProfile(teamName);
            }
            // ---  Garage Settings Sync ---
            JObject json = await _apiClient.GetRawGarageDataAsync();
            if (json == null) return;

            // Parse and update each electronic setting
            // --- Garage Settings Sync ---
            foreach (var prop in typeof(ElectronicSettings).GetProperties())
            {


                var attr = prop.GetCustomAttribute<LmuPropertyAttribute>();
                if (attr == null) continue;

                var token = json.SelectToken(attr.JsonKey);
                if (token != null && token.HasValues)
                {
                    int val = Convert.ToInt32(token["value"]);
                    int min = Convert.ToInt32(token["minValue"]);
                    int max = Convert.ToInt32(token["maxValue"]) - LMU_MAX_VALUE_OFFSET;
                    string apiString = token["stringValue"]?.ToString();

                    // SEt numeric value, min, max
                    prop.SetValue(Settings, val);
                    typeof(ElectronicSettings).GetProperty(prop.Name + "_Max")?.SetValue(Settings, max);
                    typeof(ElectronicSettings).GetProperty(prop.Name + "_Min")?.SetValue(Settings, min);

                    // --- STRING LOGIK ---
                    var strProp = typeof(ElectronicSettings).GetProperty(prop.Name + "_Str");

                    // 2. String-Logik mit Range-Check Safeguard
                    // Wenn Max nicht größer als Min ist, existiert das Feature für dieses Auto faktisch nicht.
                    if (max <= min || apiString == "N/A")
                    {
                        typeof(ElectronicSettings).GetProperty(prop.Name + "_Str")?.SetValue(Settings, "N/A");
                    }
                    else
                    {
                        // Feature aktiv (Hybrid), aber API liefert keinen Text -> Nutze Lookups
                        UpdateStringProp(prop.Name, val, attr.TableName);
                    }
                }

            }
        
            OnPropertyChanged(nameof(Settings));
        }

        //########### Helper Functions ##################################################################

        /// <summary>
        /// Automatically registers settings properties as SimHub properties and actions.
        /// </summary>
        private void AutoRegister()
        {
            // Manually register Team Info properties
            this.AttachDelegate("teamInfo.teamName", () => Settings.TeamName);
            this.AttachDelegate("teamInfo.vehicleName", () => Settings.VehicleName);

            // Register properties and actions based on ElectronicSettings properties
            foreach (var prop in typeof(ElectronicSettings).GetProperties())
            {
                var attr = prop.GetCustomAttribute<LmuPropertyAttribute>();
                if (attr == null) continue;

                string name = prop.Name;
                this.AttachDelegate(name, () => prop.GetValue(Settings));
                this.AttachDelegate(name + "_Str", () => typeof(ElectronicSettings).GetProperty(name + "_Str")?.GetValue(Settings));

                this.AddAction(name + "Increase", (a, b) => ChangeValue(prop, 1, attr.TableName));
                this.AddAction(name + "Decrease", (a, b) => ChangeValue(prop, -1, attr.TableName));
            }
        }

        /// <summary>
        /// Adjusts a value locally and updates its string representation.
        /// </summary>
        private void ChangeValue(PropertyInfo prop, int delta, string tableName)
        {
            int current = (int)prop.GetValue(Settings);
            int max = (int)(typeof(ElectronicSettings).GetProperty(prop.Name + "_Max")?.GetValue(Settings) ?? 10);
            int min = (int)(typeof(ElectronicSettings).GetProperty(prop.Name + "_Min")?.GetValue(Settings) ?? 0);

            int newValue = Math.Max(min, Math.Min(max, current + delta));
            prop.SetValue(Settings, newValue);

            UpdateStringProp(prop.Name, newValue, tableName);
            OnPropertyChanged(nameof(Settings));
        }

        /// <summary>
        /// Updates the string representation of a setting.
        /// Respects API "N/A" status and applies team-specific lookups.
        /// </summary>
        private void UpdateStringProp(string baseName, int val, string tableName)
        {
            var strProp = typeof(ElectronicSettings).GetProperty(baseName + "_Str");
            if (strProp == null) return;

            // Check current string value from Settings
            // If the API previously set this to "N/A", this functionalty is for the current car not available
            object currentObj = strProp.GetValue(Settings);
            string currentStr = currentObj != null ? currentObj.ToString() : string.Empty;

            IReadOnlyList<string> table = null;
            var profile = Settings.ActiveTeamProfile;

            // Map table names to the active team profile lists
            if (profile != null && tableName != null)
            {
                switch (tableName)
                {
                    case "ARB":
                        table = baseName.Contains("Front") ? profile.FrontARB : profile.RearARB;
                        break;
                    case "Regen":
                        table = profile.RegenLevels;
                        break;
                    case "MotorMap":
                        table = profile.ElectronicMotorMaps;
                        break;
                    case "BrakeMigration":
                        table = profile.BrakeMigration;
                        break;
                    case "EngineMixture":
                        table = profile.EngineMixture;
                        break;
                }
            }

            // Determine the final display string
            string displayValue;
            if (table != null)
            {
                // Use team-specific lookup table
                displayValue = Lookup(table, val);
            }
            else
            {
                // Fallback: Use "Off" for 0, otherwise show raw number
                displayValue = (val == 0) ? "Off" : val.ToString();
            }

            strProp.SetValue(Settings, displayValue);
        }


        /// <summary>
        /// Safely retrieves a string from a lookup table based on index.
        /// Returns "N/A" if table is null/empty, or the clamped value.
        /// </summary>
        private string Lookup(IReadOnlyList<string> table, int index)
        {
            if (table == null || table.Count == 0) return "N/A";

            // Safe clamping of index
            int safeIndex = Math.Max(0, Math.Min(index, table.Count - 1));
            return table[safeIndex];
        }

        //########### WPF Settings and Termination Interface #####################################################
        public void End(PluginManager pm) => this.SaveCommonSettings("ElectronicSettings", Settings);

        public System.Windows.Controls.Control GetWPFSettingsControl(PluginManager pm) => new SettingsControl(this);
    }
}