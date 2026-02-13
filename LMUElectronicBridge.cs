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
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
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

        private readonly LmuApiClient _apiClient = new LmuApiClient();

        // Reflection Cache to boost performance
        private readonly Dictionary<string, PropertyInfo> _propCache = new Dictionary<string, PropertyInfo>();

        //----- Constants ------------------------------------------------------
        private const int LMU_MAX_VALUE_OFFSET = 1;
        private const string STATUS_NA = "N/A";
        private const string STATUS_LINKED = "Linked";


        //----- State Tracking for resync get Garage Values ------------------
        private string lastSessionType = "";
        private double lastLapCount = 0;
        private bool _wasInGarageState = false;
        private bool _firstLoadSyncDone = false;
        private bool _raceLoadSyncDone = false;
        private bool _isTcSlipLinked = false;

        //----- Events -------------------------------------------------------
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Triggers the PropertyChanged event for a given property name.
        /// </summary>
        /// <param name="name">The name of the property that changed.</param>
        public void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        //----- Initialize ---------------------------------------------------

        /// <summary>
        /// Initializes the plugin, loads settings, and sets up reflection caching.
        /// </summary>
        /// <param name="pluginManager">The SimHub PluginManager instance.</param>
        public void Init(PluginManager pluginManager)
        {
            PluginManager = pluginManager;
            Settings = this.ReadCommonSettings<ElectronicSettings>("ElectronicSettings", () => new ElectronicSettings());

            // Cache properties once at startup to avoid expensive reflection later
            foreach (var prop in typeof(ElectronicSettings).GetProperties())
            {
                _propCache[prop.Name] = prop;
            }

            AutoRegister();
            this.AddAction("SyncFromGame", (a, b) => { _ = SyncAllFromLMU(); });

            // Check for updates on startup
            _ = CheckForUpdatesAsync();
        }

        //########### Core Data Loop #####################################################

        /// <summary>
        /// Core update loop called by SimHub. Monitors game state to trigger API synchronizations.
        /// </summary>
        /// <param name="pluginManager">The SimHub PluginManager instance.</param>
        /// <param name="data">The current game data state.</param>
        public void DataUpdate(PluginManager pluginManager, ref GameReaderCommon.GameData data)
        {
            if (data.NewData == null || !(pluginManager.GameName == "LMU" || pluginManager.GameName == "LeMansUltimate"))
            {
                ResetState();
                return;
            }

            // Update Car Class Status
            Settings.IsHypercar = IsHypercarClass(data.NewData.CarClass);

            // Continuously update vehicle status (team info and damage) from the API
            _ = UpdateVehicleStatusAsync();

            bool triggerSync = false;

            // 1. First Load Sync
            if (!_firstLoadSyncDone)
            {
                _firstLoadSyncDone = true;
                triggerSync = true;
            }

            // 2. Initial Data Sync
            if (string.IsNullOrEmpty(lastSessionType) && !string.IsNullOrEmpty(data.NewData.SessionTypeName))
                triggerSync = true;

            // 3. Session Change Trigger
            if (data.NewData.SessionTypeName != lastSessionType)
            {
                lastSessionType = data.NewData.SessionTypeName;
                _raceLoadSyncDone = false;
                triggerSync = true;
            }

            // 4. Race Specific Sync (on Ignition)
            if (data.NewData.SessionTypeName == "Race" && !_raceLoadSyncDone && data.NewData.EngineIgnitionOn == 1)
            {
                _raceLoadSyncDone = true;
                triggerSync = true;
            }

            // 5. Lap Reset Trigger
            if (data.NewData.CurrentLap < lastLapCount && data.NewData.CurrentLap <= 1)
            {
                _raceLoadSyncDone = false;
                triggerSync = true;
            }
            lastLapCount = data.NewData.CurrentLap;

            // 6. Garage Exit Trigger
            bool currentGarageState = (data.NewData.IsInPit == 1 && data.NewData.EngineIgnitionOn == 0);
            if (_wasInGarageState && data.NewData.EngineIgnitionOn == 1) triggerSync = true;
            _wasInGarageState = currentGarageState;

            if (triggerSync) _ = SyncAllFromLMU();
        }


        //########### API Sync Logic ##################################################################

        /// <summary>
        /// Asynchronously updates vehicle status data from the API.
        /// Includes team information, suspension damage, and aerodynamic damage.
        /// Called continuously during DataUpdate to keep real-time values current.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task UpdateVehicleStatusAsync()
        {
            JObject vehicleData = await _apiClient.GetVehicleStatusDataAsync();
            if (vehicleData == null) return;

            // Parse suspension damage array [FL, FR, RL, RR]
            JArray suspensionArray = vehicleData["suspensionDamage"] as JArray;
            if (suspensionArray != null && suspensionArray.Count == 4)
            {
                Settings.SuspensionDamage_FL = suspensionArray[0]?.Value<double>() ?? -1.0;
                Settings.SuspensionDamage_FR = suspensionArray[1]?.Value<double>() ?? -1.0;
                Settings.SuspensionDamage_RL = suspensionArray[2]?.Value<double>() ?? -1.0;
                Settings.SuspensionDamage_RR = suspensionArray[3]?.Value<double>() ?? -1.0;

                // Calculate average suspension damage
                Settings.SuspensionDamage_Avg = (Settings.SuspensionDamage_FL +
                                                  Settings.SuspensionDamage_FR +
                                                  Settings.SuspensionDamage_RL +
                                                  Settings.SuspensionDamage_RR) / 4.0;
            }

            // Parse aero damage
            Settings.AeroDamage = vehicleData["aeroDamage"]?.Value<double>() ?? -1.0;

            // Parse team information
            JToken teamData = vehicleData["teamInfo"];
            if (teamData != null)
            {
                Settings.TeamName = teamData["teamName"]?.ToString() ?? STATUS_NA;
                Settings.VehicleName = teamData["vehicleName"]?.ToString() ?? STATUS_NA;
            }

            // Notify SimHub of property changes
            OnPropertyChanged(nameof(Settings));
        }

        /// <summary>
        /// Asynchronously fetches garage data from the LMU API and updates electronic settings.
        /// Team info and damage data are handled by UpdateDamageDataAsync() in the main loop.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task SyncAllFromLMU()
        {
            // Fetch garage data for electronic settings only
            JObject json = await _apiClient.GetRawGarageDataAsync();
            if (json == null) return;

            // Update team lookup profile if team name is available
            if (!string.IsNullOrEmpty(Settings.TeamName) && Settings.TeamName != STATUS_NA)
            {
                Settings.ActiveTeamProfile = new TeamLookupProfile(Settings.VehicleName);
            }

            foreach (var prop in _propCache.Values)
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

                    // Update Numerics
                    prop.SetValue(Settings, val);
                    GetCachedProp(prop.Name + "_Max")?.SetValue(Settings, max);
                    GetCachedProp(prop.Name + "_Min")?.SetValue(Settings, min);

                    // Logic Check for TC Linking
                    if (prop.Name == "TC_Slip")
                        _isTcSlipLinked = apiString != null && apiString.Contains(STATUS_LINKED);

                    // Centralized String Update
                    UpdateStringLogic(prop.Name, val, max, min, apiString, attr.TableName, true);
                }
            }
            OnPropertyChanged(nameof(Settings));
        }

        /// <summary>
        /// Centralized decision engine for setting the display string based on API state and local lookups.
        /// </summary>
        /// <param name="baseName">The base name of the electronic property.</param>
        /// <param name="val">The current numeric value.</param>
        /// <param name="max">The maximum allowed value.</param>
        /// <param name="min">The minimum allowed value.</param>
        /// <param name="apiString">The string value provided by the API.</param>
        /// <param name="tableName">The lookup table name defined in the attribute.</param>
        /// <param name="isFullSync">Whether this is a full API sync or a manual button adjustment.</param>
        private void UpdateStringLogic(string baseName, int val, int max, int min, string apiString, string tableName, bool isFullSync)
        {
            var strProp = GetCachedProp(baseName + "_Str");
            if (strProp == null) return;

            // 1. Block manual updates if already N/A or Linked
            if (!isFullSync)
            {
                string current = strProp.GetValue(Settings)?.ToString();
                if (current == STATUS_NA || current == STATUS_LINKED) return;
            }

            // 2. Priority: N/A check
            if (max <= min || apiString == STATUS_NA)
            {
                strProp.SetValue(Settings, STATUS_NA);
                return;
            }

            // 3. Priority: if API explicitly provides "Linked" status (currently only for TC Slip)
            if (!string.IsNullOrEmpty(apiString) && apiString.Contains(STATUS_LINKED))
            {
                strProp.SetValue(Settings, apiString);
                return;
            }

            // 4. Fallback: Custom Lookups
            UpdateStringProp(baseName, val, tableName);
        }

        private async Task CheckForUpdatesAsync()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "LMU-Electronic-Bridge");
                    client.Timeout = TimeSpan.FromSeconds(5);

                    string response = await client.GetStringAsync(ElectronicSettings.GITHUB_API_URL);

                    // Parse JSON response
                    JObject release = JObject.Parse(response);
                    string latestVersion = release["tag_name"]?.ToString().TrimStart('v'); // Remove 'v' prefix if present

                    Settings.LatestVersion = latestVersion ?? "Unknown";

                    // Compare versions
                    if (!string.IsNullOrEmpty(latestVersion) && IsNewerVersion(latestVersion, Settings.CurrentVersion))
                    {
                        Settings.UpdateAvailable = true;
                        SimHub.Logging.Current.Info($"LMU Electronic Bridge: Update available! Current: {Settings.CurrentVersion}, Latest: {latestVersion}");
                    }
                    else
                    {
                        Settings.UpdateAvailable = false;
                        SimHub.Logging.Current.Info($"LMU Electronic Bridge: You are using the latest version ({Settings.CurrentVersion})");
                    }
                }
            }
            catch (Exception ex)
            {
                Settings.LatestVersion = "Check Failed";
                SimHub.Logging.Current.Error($"LMU Electronic Bridge: Version check failed: {ex.Message}");
            }

            OnPropertyChanged(nameof(Settings));
        }

        /// <summary>
        /// Compares two semantic version strings (e.g., "1.2.3" vs "1.2.4").
        /// </summary>
        /// <param name="latest">The latest version string.</param>
        /// <param name="current">The current version string.</param>
        /// <returns>True if latest is newer than current.</returns>
        private bool IsNewerVersion(string latest, string current)
        {
            try
            {
                var latestParts = latest.Split('.').Select(int.Parse).ToArray();
                var currentParts = current.Split('.').Select(int.Parse).ToArray();

                for (int i = 0; i < Math.Min(latestParts.Length, currentParts.Length); i++)
                {
                    if (latestParts[i] > currentParts[i]) return true;
                    if (latestParts[i] < currentParts[i]) return false;
                }

                return latestParts.Length > currentParts.Length;
            }
            catch
            {
                return false;
            }
        }

        //########### Helper Functions ##################################################################

        /// <summary>
        /// Resets the internal state trackers, usually when the game is closed.
        /// </summary>
        private void ResetState()
        {
            _firstLoadSyncDone = false;
            _raceLoadSyncDone = false;
            lastSessionType = null;
            lastLapCount = 0;
        }

        /// <summary>
        /// Determines if the current car class belongs to the Hypercar category.
        /// </summary>
        /// <param name="carClass">The car class string from SimHub.</param>
        /// <returns>True if the class is Hypercar, otherwise false.</returns>
        private bool IsHypercarClass(string carClass)
        {
            return carClass == "LMH" || carClass == "LMDh" || carClass == "Hypercar" || carClass == "Hyper";
        }

        /// <summary>
        /// Retrieves a PropertyInfo object from the internal cache.
        /// </summary>
        /// <param name="name">The name of the property.</param>
        /// <returns>The PropertyInfo if found, otherwise null.</returns>
        private PropertyInfo GetCachedProp(string name) => _propCache.TryGetValue(name, out var p) ? p : null;

        /// <summary>
        /// Automatically registers SimHub properties and actions based on the ElectronicSettings class.
        /// </summary>
        private void AutoRegister()
        {
            // Register team information properties
            this.AttachDelegate("teamInfo.teamName", () => Settings.TeamName);
            this.AttachDelegate("teamInfo.vehicleName", () => Settings.VehicleName);

            // Register damage and wear properties
            this.AttachDelegate("Damage.SuspensionDamageFrontLeft", () => Settings.SuspensionDamage_FL);
            this.AttachDelegate("Damage.SuspensionDamageFrontRight", () => Settings.SuspensionDamage_FR);
            this.AttachDelegate("Damage.SuspensionDamageRearLeft", () => Settings.SuspensionDamage_RL);
            this.AttachDelegate("Damage.SuspensionDamageRearRight", () => Settings.SuspensionDamage_RR);
            this.AttachDelegate("Damage.SuspensionDamageAverage", () => Settings.SuspensionDamage_Avg);
            this.AttachDelegate("Damage.AeroDamage", () => Settings.AeroDamage);

            // Register electronic settings properties and actions
            foreach (var prop in _propCache.Values)
            {
                var attr = prop.GetCustomAttribute<LmuPropertyAttribute>();
                if (attr == null) continue;

                string name = prop.Name;
                this.AttachDelegate(name, () => prop.GetValue(Settings));
                this.AttachDelegate(name + "_Str", () => GetCachedProp(name + "_Str")?.GetValue(Settings));

                this.AddAction(name + "Increase", (a, b) => ChangeValue(prop, 1, attr.TableName));
                this.AddAction(name + "Decrease", (a, b) => ChangeValue(prop, -1, attr.TableName));
            }
        }

        /// <summary>
        /// Adjusts the numeric value of a property, handles clamping, and updates linked TC properties.
        /// </summary>
        /// <param name="prop">The property to change.</param>
        /// <param name="delta">The amount to change (e.g., +1 or -1).</param>
        /// <param name="tableName">The lookup table name for string conversion.</param>
        private void ChangeValue(PropertyInfo prop, int delta, string tableName)
        {
            int current = (int)prop.GetValue(Settings);
            int max = (int)(GetCachedProp(prop.Name + "_Max")?.GetValue(Settings) ?? 10);
            int min = (int)(GetCachedProp(prop.Name + "_Min")?.GetValue(Settings) ?? 0);

            // Logic-based guard (ChangeValue only happens via buttons, so isFullSync = false)
            UpdateStringLogic(prop.Name, current, max, min, null, tableName, false);

            // Re-check string after logic guard to see if we should proceed
            string currentStr = GetCachedProp(prop.Name + "_Str")?.GetValue(Settings)?.ToString();
            if (currentStr == STATUS_NA || (prop.Name == "TC_Slip" && currentStr == STATUS_LINKED)) return;

            int newValue = Math.Max(min, Math.Min(max, current + delta));
            prop.SetValue(Settings, newValue);

            // Handle TC Main -> Slip Slave Link
            if (prop.Name == "TC_Main" && _isTcSlipLinked)
            {
                GetCachedProp("TC_Slip")?.SetValue(Settings, newValue);
                GetCachedProp("TC_Slip_Str")?.SetValue(Settings, STATUS_LINKED);
            }

            UpdateStringProp(prop.Name, newValue, tableName);
            OnPropertyChanged(nameof(Settings));
        }

        /// <summary>
        /// Updates the string representation property using the active team lookup profile.
        /// </summary>
        /// <param name="baseName">The base name of the electronic property.</param>
        /// <param name="val">The current numeric value.</param>
        /// <param name="tableName">The lookup table category (e.g., "ARB", "Regen").</param>
        private void UpdateStringProp(string baseName, int val, string tableName)
        {
            var profile = Settings.ActiveTeamProfile;
            IReadOnlyList<string> table = null;

            if (profile != null && tableName != null)
            {
                switch (tableName)
                {
                    case "ARB": table = baseName.Contains("Front") ? profile.FrontARB : profile.RearARB; break;
                    case "Regen": table = profile.RegenLevels; break;
                    case "MotorMap": table = profile.ElectronicMotorMaps; break;
                    case "BrakeMigration": table = profile.BrakeMigration; break;
                    case "EngineMixture": table = profile.EngineMixture; break;
                }
            }

            string displayValue = table != null ? Lookup(table, val) : (val == 0 ? "Off" : val.ToString());
            GetCachedProp(baseName + "_Str")?.SetValue(Settings, displayValue);
        }

        /// <summary>
        /// Performs a safe index-based lookup in a string list.
        /// </summary>
        /// <param name="table">The list of strings to look up from.</param>
        /// <param name="index">The desired index.</param>
        /// <returns>The string at the clamped index, or "N/A" if the table is empty.</returns>
        private string Lookup(IReadOnlyList<string> table, int index)
        {
            if (table == null || table.Count == 0) return STATUS_NA;
            return table[Math.Max(0, Math.Min(index, table.Count - 1))];
        }

        //########### WPF Settings and Termination Interface #####################################################

        /// <summary>
        /// Called by SimHub when the plugin is being shut down. Saves settings.
        /// </summary>
        /// <param name="pm">The SimHub PluginManager instance.</param>
        public void End(PluginManager pm) => this.SaveCommonSettings("ElectronicSettings", Settings);

        /// <summary>
        /// Returns the WPF control for the plugin settings menu in SimHub.
        /// </summary>
        /// <param name="pm">The SimHub PluginManager instance.</param>
        /// <returns>A WPF Control instance.</returns>
        public System.Windows.Controls.Control GetWPFSettingsControl(PluginManager pm) => new SettingsControl(this);
    }
}