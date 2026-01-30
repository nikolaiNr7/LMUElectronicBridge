namespace LMUElectronicBridge
{
    public class ElectronicSettings
    {

        // Active runtime values
        public int TC_Main { get; set; } = 0;
        public int TC_Cut { get; set; } = 0;
        public int TC_Slip { get; set; } = 0;
        public int ABS { get; set; } = 0;

        // Limits
        public int TC_Main_Max { get; set; } = 11;
        public int TC_Cut_Max { get; set; } = 11;
        public int TC_Slip_Max { get; set; } = 11;
        public int ABS_Max { get; set; } = 9;
        public int MinValue { get; set; } = 0;
    }
}