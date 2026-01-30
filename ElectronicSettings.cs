using System;

namespace LMUElectronicBridge
{
    public class ElectronicSettings
    {
        // --- PERSISTENT VALUES ---
        public int TC_Main { get; set; } = 0;
        public string TC_Main_Str { get; set; } = "0";
        public int TC_Cut { get; set; } = 0;
        public string TC_Cut_Str { get; set; } = "0";
        public int TC_Slip { get; set; } = 0;
        public string TC_Slip_Str { get; set; } = "0";
        public int ABS { get; set; } = 0;
        public string ABS_Str { get; set; } = "0";

        // Brake Migration
        public int BrakeMigration { get; set; } = 0;
        public string BrakeMigration_Str { get; set; } = "Disabled";

        // MGU & Engine
        public int RegenLevel { get; set; } = 0;
        public string RegenLevel_Str { get; set; } = "0";
        public int ElectricMotorMap { get; set; } = 0;
        public string ElectricMotorMap_Str { get; set; } = "0";
        public int EngineMixture { get; set; } = 0;
        public string EngineMixture_Str { get; set; } = "0";

        // --- ARB Values ---
        public int FrontARB { get; set; } = 0;
        public string FrontARB_Str { get; set; } = "Detached";
        public int RearARB { get; set; } = 0;
        public string RearARB_Str { get; set; } = "Detached";



        // --- LIVE LIMITS (NOT SAVED) ---
        public int TC_Main_Max { get; internal set; } = 11;
        public int TC_Main_Min { get; internal set; } = 0;
        public int TC_Cut_Max { get; internal set; } = 11;
        public int TC_Cut_Min { get; internal set; } = 0;
        public int TC_Slip_Max { get; internal set; } = 11;
        public int TC_Slip_Min { get; internal set; } = 0;
        public int ABS_Max { get; internal set; } = 9;
        public int ABS_Min { get; internal set; } = 0;

        // Engine & MGU
        public int Regen_Max { get; internal set; } = 10;
        public int Regen_Min { get; internal set; } = 0;
        public int MotorMap_Max { get; internal set; } = 5;
        public int MotorMap_Min { get; internal set; } = 0;
        public int Mixture_Max { get; internal set; } = 10;
        public int Mixture_Min { get; internal set; } = 0;

        // Brake Migration
        public int BrakeMigration_Max { get; internal set; } = 5;
        public int BrakeMigration_Min { get; internal set; } = 0;

        // --- ARB Limits ---
        public int FrontARB_Max { get; internal set; } = 5;
        public int FrontARB_Min { get; internal set; } = 0;
        public int RearARB_Max { get; internal set; } = 5;
        public int RearARB_Min { get; internal set; } = 0;
    }
}