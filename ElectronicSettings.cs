namespace LMUElectronicBridge
{
    public class ElectronicSettings
    {
        // Strategy toggle
        public bool UseGameSync { get; set; } = true;

        // Active runtime values
        public int TC_Main { get; set; } = 0;
        public int TC_Cut { get; set; } = 0;
        public int TC_Slip { get; set; } = 0;
        public int ABS { get; set; } = 0;

        // Manual user values (used if UseGameSync = false)
        public int TC_Main_User { get; set; } = 0;
        public int TC_Cut_User { get; set; } = 0;
        public int TC_Slip_User { get; set; } = 0;
        public int ABS_User { get; set; } = 0;

        // Fallback property paths (used if API is offline)
        public string PropPath_TC_Main { get; set; } = "lmuDataPlugin.Redadeg.lmu.Extended.VM_TRACTIONCONTROLMAP";
        public string PropPath_TC_Cut { get; set; } = "lmuDataPlugin.Redadeg.lmu.Extended.VM_TRACTIONCONTROLPOWERCUTMAP";
        public string PropPath_TC_Slip { get; set; } = "lmuDataPlugin.Redadeg.lmu.Extended.VM_TRACTIONCONTROLSLIPANGLEMAP";
        public string PropPath_ABS { get; set; } = "lmuDataPlugin.Redadeg.lmu.Extended.VM_ANTILOCKBRAKESYSTEMMAP";

        // Limits
        public int TC_Main_Max { get; set; } = 11;
        public int TC_Cut_Max { get; set; } = 11;
        public int TC_Slip_Max { get; set; } = 11;
        public int ABS_Max { get; set; } = 9;
        public int MinValue { get; set; } = 0;
    }
}