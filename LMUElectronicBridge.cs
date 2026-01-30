using SimHub.Plugins;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Media;
using Newtonsoft.Json.Linq;

namespace LMUElectronicBridge
{
    [PluginName("LMU Electronic Bridge")]
    public class LMUElectronicBridge : IPlugin, IDataPlugin, IWPFSettingsV2, INotifyPropertyChanged
    {
        public PluginManager PluginManager { get; set; }
        public ImageSource PictureIcon => null;
        public string LeftMenuTitle => "LMU Electronics";
        public ElectronicSettings Settings { get; private set; }
        private LmuApiClient _apiClient = new LmuApiClient();
        private readonly List<Tuple<PropertyInfo, LmuPropertyAttribute>> _registeredProps = new List<Tuple<PropertyInfo, LmuPropertyAttribute>>();

        private static readonly Dictionary<string, string[]> Tables = new Dictionary<string, string[]>
        {
            { "BrakeMigration", new[] { "Disabled", "0.5% F", "1.0% F", "1.5% F", "2.0% F", "2.5% F" } },
            { "MotorMap", new[] { "Off", "10 kW", "20 kW", "30 kW", "40 kW", "50 kW" } },
            { "Regen", new[] { "Off", "17 kW", "34 kW", "51 kW", "68 kW", "85 kW", "102 kW", "119 kW", "136 kW", "153 kW", "170 kW" } },
            { "ARB", new[] { "Detached", "P1", "P2", "P3", "P4", "P5" } }
        };

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public void Init(PluginManager pluginManager)
        {
            PluginManager = pluginManager;
            Settings = this.ReadCommonSettings<ElectronicSettings>("ElectronicSettings", () => new ElectronicSettings());
            AutoRegister();
            this.AddAction("SyncFromGame", (a, b) => { _ = SyncAllFromLMU(); });
        }

        private void AutoRegister()
        {
            // Cache property infos and attributes to avoid repeated reflection calls at runtime
            foreach (var prop in typeof(ElectronicSettings).GetProperties())
            {
                var attr = prop.GetCustomAttribute<LmuPropertyAttribute>();
                if (attr == null) continue;
                string name = prop.Name;
                _registeredProps.Add(Tuple.Create(prop, attr));
                this.AttachDelegate(name, () => prop.GetValue(Settings));
                this.AttachDelegate(name + "_Str", () => typeof(ElectronicSettings).GetProperty(name + "_Str")?.GetValue(Settings));
                this.AddAction(name + "Increase", (a, b) => ChangeValue(prop, 1, attr.TableName));
                this.AddAction(name + "Decrease", (a, b) => ChangeValue(prop, -1, attr.TableName));
            }
        }

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

        private void UpdateStringProp(string baseName, int val, string tableName)
        {
            var strProp = typeof(ElectronicSettings).GetProperty(baseName + "_Str");
            if (tableName != null && Tables.ContainsKey(tableName))
                strProp?.SetValue(Settings, Lookup(Tables[tableName], val));
            else
                strProp?.SetValue(Settings, val.ToString());
        }

        public async Task SyncAllFromLMU()
        {
            JObject json = await _apiClient.GetRawGarageDataAsync();
            if (json == null) return;
            foreach (var tuple in _registeredProps)
            {
                var prop = tuple.Item1;
                var attr = tuple.Item2;
                var token = json.SelectToken(attr.JsonKey);
                if (token == null || !token.HasValues) continue;
                int val;
                int min;
                int max;
                // safe conversions with fallbacks
                if (!int.TryParse(token["value"]?.ToString(), out val)) continue;
                if (!int.TryParse(token["minValue"]?.ToString(), out min)) min = 0;
                if (!int.TryParse(token["maxValue"]?.ToString(), out max)) max = val + 1;
                max = Math.Max(min, max - 1);
                prop.SetValue(Settings, val);
                typeof(ElectronicSettings).GetProperty(prop.Name + "_Max")?.SetValue(Settings, max);
                typeof(ElectronicSettings).GetProperty(prop.Name + "_Min")?.SetValue(Settings, min);
                UpdateStringProp(prop.Name, val, attr.TableName);
            }
            // ElectronicSettings will notify property changes for individual props; notify that Settings collection state may have changed too
            OnPropertyChanged(nameof(Settings));
        }

        private string Lookup(string[] table, int index)
        {
            if (index < 0) return table[0];
            if (index >= table.Length) return table[table.Length - 1];
            return table[index];
        }

        public void DataUpdate(PluginManager pm, ref GameReaderCommon.GameData data) { /* Trigger Sync logic here */ }
        public void End(PluginManager pm) => this.SaveCommonSettings("ElectronicSettings", Settings);
        public System.Windows.Controls.Control GetWPFSettingsControl(PluginManager pm) => new SettingsControl(this);
    }
}