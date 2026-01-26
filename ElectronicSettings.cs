namespace LMUElectronicBridge
{
    public class ElectronicSettings
    {
        // Current values (Runtime)
        public int TC_Main { get; set; } = 0;
        public int TC_Cut { get; set; } = 0;
        public int TC_Slip { get; set; } = 0;
        public int ABS { get; set; } = 0;

        // Manual Start/Override Values (User defined in UI)
        public int TC_Main_User { get; set; } = 0;
        public int TC_Cut_User { get; set; } = 0;
        public int TC_Slip_User { get; set; } = 0;
        public int ABS_User { get; set; } = 0;

        // Limits
        public int TC_Main_Max { get; set; } = 12;
        public int TC_Cut_Max { get; set; } = 12;
        public int TC_Slip_Max { get; set; } = 12;
        public int ABS_Max { get; set; } = 9;
        public int MinValue { get; set; } = 0;
    }
}