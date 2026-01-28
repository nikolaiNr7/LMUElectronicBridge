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
        public void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public void Init(PluginManager pluginManager)
        {
            PluginManager = pluginManager;
            Settings = this.ReadCommonSettings<ElectronicSettings>("ElectronicSettings", () => new ElectronicSettings());

            this.AttachDelegate("TC_Main", () => Settings.TC_Main);
            this.AttachDelegate("TC_Cut", () => Settings.TC_Cut);
            this.AttachDelegate("TC_Slip", () => Settings.TC_Slip);
            this.AttachDelegate("ABS", () => Settings.ABS);

            RegisterControlActions("TC_Main", () => Settings.TC_Main, v => Settings.TC_Main = v, Settings.TC_Main_Max);
            RegisterControlActions("TC_Cut", () => Settings.TC_Cut, v => Settings.TC_Cut = v, Settings.TC_Cut_Max);
            RegisterControlActions("TC_Slip", () => Settings.TC_Slip, v => Settings.TC_Slip = v, Settings.TC_Slip_Max);
            RegisterControlActions("ABS", () => Settings.ABS, v => Settings.ABS = v, Settings.ABS_Max);

            this.AddAction("SyncAllFromGame", (a, b) => { _ = SyncAllFromLMU(); });
        }

        private void RegisterControlActions(string name, Func<int> getter, Action<int> setter, int maxValue)
        {
            this.AddAction(name + "Increase", (a, b) => {
                if (getter() < maxValue) { setter(getter() + 1); OnPropertyChanged(nameof(Settings)); }
            });
            this.AddAction(name + "Decrease", (a, b) => {
                if (getter() > Settings.MinValue) { setter(getter() - 1); OnPropertyChanged(nameof(Settings)); }
            });
        }

        public void DataUpdate(PluginManager pluginManager, ref GameReaderCommon.GameData data)
        {
            if (data.NewData != null && (pluginManager.GameName == "LMU" || pluginManager.GameName == "LeMansUltimate"))
            {
                bool sessionTrigger = false;
                if (data.NewData.SessionTypeName != lastSessionType) { lastSessionType = data.NewData.SessionTypeName; sessionTrigger = true; }
                if (data.NewData.CurrentLap < lastLapCount && data.NewData.CurrentLap <= 1) { sessionTrigger = true; }
                lastLapCount = data.NewData.CurrentLap;

                if (sessionTrigger)
                {
                    if (Settings.UseGameSync) _ = SyncAllFromLMU();
                    else ApplyManualValues();
                }
            }
        }

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
                // Fallback auf Properties, wenn API nicht erreichbar
                SyncFromPropertiesFallback();
            }
        }

        private void SyncFromPropertiesFallback()
        {
            Settings.TC_Main = GetSafeInt(Settings.PropPath_TC_Main);
            Settings.TC_Cut = GetSafeInt(Settings.PropPath_TC_Cut);
            Settings.TC_Slip = GetSafeInt(Settings.PropPath_TC_Slip);
            Settings.ABS = GetSafeInt(Settings.PropPath_ABS);
            OnPropertyChanged(nameof(Settings));
        }

        public void ApplyManualValues()
        {
            Settings.TC_Main = Settings.TC_Main_User;
            Settings.TC_Cut = Settings.TC_Cut_User;
            Settings.TC_Slip = Settings.TC_Slip_User;
            Settings.ABS = Settings.ABS_User;
            OnPropertyChanged(nameof(Settings));
        }

        public bool TestProperty(string path, out string message)
        {
            var val = PluginManager.GetPropertyValue(path);
            if (val != null) { message = $"Passed. Found value: {val}"; return true; }
            message = "Failed. Property name not found in SimHub.";
            return false;
        }

        private int GetSafeInt(string prop)
        {
            var val = PluginManager.GetPropertyValue(prop);
            return val != null ? Convert.ToInt32(val) : 0;
        }

        public void End(PluginManager pluginManager) => this.SaveCommonSettings("ElectronicSettings", Settings);
        public System.Windows.Controls.Control GetWPFSettingsControl(PluginManager pluginManager) => new SettingsControl(this);
    }
}