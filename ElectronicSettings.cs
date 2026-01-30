using System;

namespace LMUElectronicBridge
{
    // -------------------------------------------------------------------------
    // CUSTOM ATTRIBUTES
    // -------------------------------------------------------------------------

    /// <summary>
    /// Links C# properties to specific LMU Garage API JSON keys and UI lookup tables.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class LmuPropertyAttribute : Attribute
    {
        public string JsonKey { get; }
        public string TableName { get; }

        public LmuPropertyAttribute(string jsonKey, string tableName = null)
        {
            JsonKey = jsonKey;
            TableName = tableName;
        }
    }

    // -------------------------------------------------------------------------
    // SETTINGS SCHEMA
    // -------------------------------------------------------------------------

    /// <summary>
    /// Data model representing the electronic state of the car.
    /// Values starting with -1 or "N/A" indicate the car/API has not been synced.
    /// </summary>
    public class ElectronicSettings
    {
        // ---- TRACTION CONTROL -----------------------------------------------

        [LmuProperty("VM_TRACTIONCONTROLMAP")]
        public int TC_Main { get; internal set; } = -1;
        public string TC_Main_Str { get; internal set; } = "N/A";
        public int TC_Main_Max { get; internal set; } = 0;
        public int TC_Main_Min { get; internal set; } = 0;

        [LmuProperty("VM_TRACTIONCONTROLPOWERCUTMAP")]
        public int TC_Cut { get; internal set; } = -1;
        public string TC_Cut_Str { get; internal set; } = "N/A";
        public int TC_Cut_Max { get; internal set; } = 0;
        public int TC_Cut_Min { get; internal set; } = 0;

        [LmuProperty("VM_TRACTIONCONTROLSLIPANGLEMAP")]
        public int TC_Slip { get; internal set; } = -1;
        public string TC_Slip_Str { get; internal set; } = "N/A";
        public int TC_Slip_Max { get; internal set; } = 0;
        public int TC_Slip_Min { get; internal set; } = 0;


        // ---- BRAKE SYSTEMS --------------------------------------------------

        [LmuProperty("VM_ANTILOCKBRAKESYSTEMMAP")]
        public int ABS { get; internal set; } = -1;
        public string ABS_Str { get; internal set; } = "N/A";
        public int ABS_Max { get; internal set; } = 0;
        public int ABS_Min { get; internal set; } = 0;

        [LmuProperty("VM_BRAKE_MIGRATION", "BrakeMigration")]
        public int BrakeMigration { get; internal set; } = -1;
        public string BrakeMigration_Str { get; internal set; } = "N/A";
        public int BrakeMigration_Max { get; internal set; } = 0;
        public int BrakeMigration_Min { get; internal set; } = 0;


        //---- HYBRID & POWERTRAIN --------------------------------------------

        [LmuProperty("VM_REGEN_LEVEL", "Regen")]
        public int Regen { get; internal set; } = -1;
        public string Regen_Str { get; internal set; } = "N/A";
        public int Regen_Max { get; internal set; } = 0;
        public int Regen_Min { get; internal set; } = 0;

        [LmuProperty("VM_ELECTRIC_MOTOR_MAP", "MotorMap")]
        public int MotorMap { get; internal set; } = -1;
        public string MotorMap_Str { get; internal set; } = "N/A";
        public int MotorMap_Max { get; internal set; } = 0;
        public int MotorMap_Min { get; internal set; } = 0;

        [LmuProperty("VM_ENGINE_MIXTURE", "EngineMixture")]
        public int Mixture { get; internal set; } = -1;
        public string Mixture_Str { get; internal set; } = "N/A";
        public int Mixture_Max { get; internal set; } = 0;
        public int Mixture_Min { get; internal set; } = 0;


        // ---- CHASSIS & SUSPENSION -------------------------------------------

        [LmuProperty("VM_FRONT_ANTISWAY", "ARB")]
        public int FrontARB { get; internal set; } = -1;
        public string FrontARB_Str { get; internal set; } = "N/A";
        public int FrontARB_Max { get; internal set; } = 0;
        public int FrontARB_Min { get; internal set; } = 0;

        [LmuProperty("VM_REAR_ANTISWAY", "ARB")]
        public int RearARB { get; internal set; } = -1;
        public string RearARB_Str { get; internal set; } = "N/A";
        public int RearARB_Max { get; internal set; } = 0;
        public int RearARB_Min { get; internal set; } = 0;


        // ---- GLOBAL PLUGIN SETTINGS -----------------------------------------

        /// <summary>
        /// Enable or disable the automatic background sync from LMU Garage API.
        /// </summary>
        public bool AutoSyncEnabled { get; set; } = true;

        /// <summary>
        /// Defines the verbosity of the SimHub log output for this plugin.
        /// </summary>
        public string DebugLogLevel { get; set; } = "Info";
    }
}