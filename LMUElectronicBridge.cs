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

            foreach (var prop in typeof(ElectronicSettings).GetProperties())
            {
                var attr = prop.GetCustomAttribute<LmuPropertyAttribute>();
                if (attr == null) continue;

                var token = json.SelectToken(attr.JsonKey);
                if (token != null && token.HasValues)
                {
                    int val = Convert.ToInt32(token["value"]);
                    int min = Convert.ToInt32(token["minValue"]);
                    int max = Convert.ToInt32(token["maxValue"]) - 1;

                    prop.SetValue(Settings, val);
                    typeof(ElectronicSettings).GetProperty(prop.Name + "_Max")?.SetValue(Settings, max);
                    typeof(ElectronicSettings).GetProperty(prop.Name + "_Min")?.SetValue(Settings, min);
                    UpdateStringProp(prop.Name, val, attr.TableName);
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
        /// Updates the associated string property for a setting based on a lookup table.
        /// </summary>
        private void UpdateStringProp(string baseName, int val, string tableName)
        {
            var strProp = typeof(ElectronicSettings).GetProperty(baseName + "_Str");

            // 1. Hole das dynamische Profil
            var profile = Settings.ActiveTeamProfile;
            IReadOnlyList<string> table = null;

            if (profile != null)
            {
                // 2. Weise die richtige Tabelle basierend auf dem tableName zu
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

            // 3. Apply lookup or fallback to ToString()
            if (table != null)
            {
                strProp?.SetValue(Settings, Lookup(table, val));
            }
            else
            {
                // Fallback without lookup table (e.g., for TC and ABS) use ToString() except for 0 = "Off"
                string fallbackValue = (val == 0) ? "Off" : val.ToString();
                strProp.SetValue(Settings, fallbackValue);
            }
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