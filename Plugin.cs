using SimHub.Plugins;
using System;
using System.Windows.Media;
using System.ComponentModel;

namespace LMUElectronicBridge
{
    [PluginName("LMU Electronic Bridge")]
    [PluginAuthor("Nikolai Schlott")]
    [PluginDescription("Syncs In-Car electronics for LMU on session changes or lap resets.")]
    public class LMUElectronicBridge : IPlugin, IWPFSettingsV2, INotifyPropertyChanged
    {
        public PluginManager PluginManager { get; set; }
        public ElectronicSettings Settings { get; private set; }

        private string lastSessionType = null;
        private int lastLapCount = 0;

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public ImageSource PictureIcon => null;
        public string LeftMenuTitle => "LMU Bridge";

        public void Init(PluginManager pluginManager)
        {
            PluginManager = pluginManager;
            Settings = this.ReadCommonSettings<ElectronicSettings>("ElectronicSettings", () => new ElectronicSettings());

            // Expose properties to Dash Studio
            this.AttachDelegate("TC_Main", () => Settings.TC_Main);
            this.AttachDelegate("TC_Cut", () => Settings.TC_Cut);
            this.AttachDelegate("TC_Slip", () => Settings.TC_Slip);
            this.AttachDelegate("ABS", () => Settings.ABS);

            // Register Increase/Decrease Actions
            RegisterControlActions("TC_Main", () => Settings.TC_Main, v => Settings.TC_Main = v, Settings.TC_Main_Max);
            RegisterControlActions("TC_Cut", () => Settings.TC_Cut, v => Settings.TC_Cut = v, Settings.TC_Cut_Max);
            RegisterControlActions("TC_Slip", () => Settings.TC_Slip, v => Settings.TC_Slip = v, Settings.TC_Slip_Max);
            RegisterControlActions("ABS", () => Settings.ABS, v => Settings.ABS = v, Settings.ABS_Max);

            // Action for Wheel Button Binding
            this.AddAction("SyncAllFromGame", (a, b) => SyncAllFromLMU());
        }

        private void RegisterControlActions(string name, Func<int> getter, Action<int> setter, int maxValue)
        {
            this.AddAction(name + "Increase", (a, b) => {
                if (getter() < maxValue) { setter(getter() + 1); OnPropertyChanged(name); }
            });
            this.AddAction(name + "Decrease", (a, b) => {
                if (getter() > Settings.MinValue) { setter(getter() - 1); OnPropertyChanged(name); }
            });
        }

        public void DataUpdate(PluginManager pluginManager, ref GameReaderCommon.GameData data)
        {
            if (pluginManager.GameName == "LMU" || pluginManager.GameName == "LeMansUltimate")
            {
                if (data.NewData != null)
                {
                    bool shouldSync = false;

                    // 1. Detect Session Change (e.g. Practice -> Qualifying)
                    if (data.NewData.SessionTypeName != lastSessionType)
                    {
                        lastSessionType = data.NewData.SessionTypeName;
                        shouldSync = true;
                    }

                    // 2. Detect Lap Reset (Laps go back to 0 or 1 from a higher number)
                    if (data.NewData.CurrentLap < lastLapCount && data.NewData.CurrentLap <= 1)
                    {
                        shouldSync = true;
                    }
                    lastLapCount = data.NewData.CurrentLap;

                    if (shouldSync) SyncAllFromLMU();
                }
            }
        }

        public void SyncAllFromLMU()
        {
            Settings.TC_Main = GetSafeInt("lmuDataPlugin.Redadeg.lmu.Extended.VM_TRACTIONCONTROLMAP");
            Settings.TC_Cut = GetSafeInt("lmuDataPlugin.Redadeg.lmu.Extended.VM_TRACTIONCONTROLPOWERCUTMAP");
            Settings.TC_Slip = GetSafeInt("lmuDataPlugin.Redadeg.lmu.Extended.VM_TRACTIONCONTROLSLIPANGLEMAP");
            Settings.ABS = GetSafeInt("lmuDataPlugin.Redadeg.lmu.Extended.VM_ANTILOCKBRAKESYSTEMMAP");
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