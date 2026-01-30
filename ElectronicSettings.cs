using System;

namespace LMUElectronicBridge
{
    public class ElectronicSettings
    {
        // --- ACTIVE RUNTIME VALUES ---
        // These are the values shown in the UI and updated by buttons/API
        public int TC_Main { get; set; } = 0;
        public int TC_Cut { get; set; } = 0;
        public int TC_Slip { get; set; } = 0;
        public int ABS { get; set; } = 0;

        // --- CALIBRATION LIMITS ---
        // NOT SAVED: Setter is internal. SimHub ignores these entirely.
        // They will always reset to these defaults when SimHub starts.
        public int TC_Main_Max { get; internal set; } = 11;
        public int TC_Cut_Max { get; internal set; } = 11;
        public int TC_Slip_Max { get; internal set; } = 11;
        public int ABS_Max { get; internal set; } = 9;
        public int MinValue { get; internal set; } = 0;
    }
}