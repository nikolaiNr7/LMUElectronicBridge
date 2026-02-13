using System;
using SimHub.Plugins;

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

        // ---- VERSION INFO ---------------------------------------------------
        /// <summary>
        /// Current plugin version.
        /// </summary>
        public string CurrentVersion { get; internal set; } = "1.1.2"; // Update this with each release

        /// <summary>
        /// Latest available version from GitHub.
        /// </summary>
        public string LatestVersion { get; internal set; } = "Checking...";

        /// <summary>
        /// Whether an update is available.
        /// </summary>
        public bool UpdateAvailable { get; internal set; } = false;

        //----- Constants for GitHub Integration -------------------------

        /// <summary>
        /// GitHub API endpoint for checking latest release version.
        /// </summary>
        public const string GITHUB_API_URL = "https://api.github.com/repos/nikolaiNr7/LMU-Electronic-Bridge-Release/releases/latest";

        /// <summary>
        /// GitHub releases page URL for users to download updates.
        /// </summary>
        public const string GITHUB_RELEASES_URL = "https://github.com/nikolaiNr7/LMU-Electronic-Bridge-Release/releases/latest";


        // ---- VEHICLE CLASS & TYPE --------------------------------------------
        public bool IsHypercar { get; internal set; } = false;

        // ---- TRACTION CONTROL -----------------------------------------------

        [LmuProperty("VM_TRACTIONCONTROLMAP")]
        public int TC_Main { get; internal set; } = -1;
        public string TC_Main_Str { get; internal set; } = "";
        public int TC_Main_Max { get; internal set; } = 0;
        public int TC_Main_Min { get; internal set; } = 0;

        [LmuProperty("VM_TRACTIONCONTROLPOWERCUTMAP")]
        public int TC_Cut { get; internal set; } = -1;
        public string TC_Cut_Str { get; internal set; } = "";
        public int TC_Cut_Max { get; internal set; } = 0;
        public int TC_Cut_Min { get; internal set; } = 0;

        [LmuProperty("VM_TRACTIONCONTROLSLIPANGLEMAP")]
        public int TC_Slip { get; internal set; } = -1;
        public string TC_Slip_Str { get; internal set; } = "";
        public int TC_Slip_Max { get; internal set; } = 0;
        public int TC_Slip_Min { get; internal set; } = 0;


        // ---- BRAKE SYSTEMS --------------------------------------------------

        [LmuProperty("VM_ANTILOCKBRAKESYSTEMMAP")]
        public int ABS { get; internal set; } = -1;
        public string ABS_Str { get; internal set; } = "";
        public int ABS_Max { get; internal set; } = 0;
        public int ABS_Min { get; internal set; } = 0;

        [LmuProperty("VM_BRAKE_MIGRATION", "BrakeMigration")]
        public int BrakeMigration { get; internal set; } = -1;
        public string BrakeMigration_Str { get; internal set; } = "";
        public int BrakeMigration_Max { get; internal set; } = 0;
        public int BrakeMigration_Min { get; internal set; } = 0;


        //---- HYBRID & POWERTRAIN --------------------------------------------

        [LmuProperty("VM_REGEN_LEVEL", "Regen")]
        public int Regen { get; internal set; } = -1;
        public string Regen_Str { get; internal set; } = "";
        public int Regen_Max { get; internal set; } = 0;
        public int Regen_Min { get; internal set; } = 0;

        [LmuProperty("VM_ELECTRIC_MOTOR_MAP", "MotorMap")]
        public int MotorMap { get; internal set; } = -1;
        public string MotorMap_Str { get; internal set; } = "";
        public int MotorMap_Max { get; internal set; } = 0;
        public int MotorMap_Min { get; internal set; } = 0;

        [LmuProperty("VM_ENGINE_MIXTURE", "EngineMixture")]
        public int Mixture { get; internal set; } = -1;
        public string Mixture_Str { get; internal set; } = "";
        public int Mixture_Max { get; internal set; } = 0;
        public int Mixture_Min { get; internal set; } = 0;


        // ---- CHASSIS & SUSPENSION -------------------------------------------

        [LmuProperty("VM_FRONT_ANTISWAY", "ARB")]
        public int FrontARB { get; internal set; } = -1;
        public string FrontARB_Str { get; internal set; } = "";
        public int FrontARB_Max { get; internal set; } = 0;
        public int FrontARB_Min { get; internal set; } = 0;

        [LmuProperty("VM_REAR_ANTISWAY", "ARB")]
        public int RearARB { get; internal set; } = -1;
        public string RearARB_Str { get; internal set; } = "";
        public int RearARB_Max { get; internal set; } = 0;
        public int RearARB_Min { get; internal set; } = 0;


        // ---- DAMAGE & WEAR --------------------------------------------------

        /// <summary>
        /// Front-left suspension damage/wear (0.0 = no damage, 1.0 = maximum damage).
        /// </summary>
        public double SuspensionDamage_FL { get; internal set; } = -1.0;

        /// <summary>
        /// Front-right suspension damage/wear (0.0 = no damage, 1.0 = maximum damage).
        /// </summary>
        public double SuspensionDamage_FR { get; internal set; } = -1.0;

        /// <summary>
        /// Rear-left suspension damage/wear (0.0 = no damage, 1.0 = maximum damage).
        /// </summary>
        public double SuspensionDamage_RL { get; internal set; } = -1.0;

        /// <summary>
        /// Rear-right suspension damage/wear (0.0 = no damage, 1.0 = maximum damage).
        /// </summary>
        public double SuspensionDamage_RR { get; internal set; } = -1.0;

        /// <summary>
        /// Average suspension damage across all four corners (0.0 = no damage, 1.0 = maximum damage).
        /// </summary>
        public double SuspensionDamage_Avg { get; internal set; } = -1.0;

        /// <summary>
        /// Aerodynamic damage (0.0 = no damage, 1.0 = maximum damage).
        /// </summary>
        public double AeroDamage { get; internal set; } = -1.0;


        // ---- TEAM INFO ------------------------------------------------------
        public string TeamName { get; internal set; } = "";
        public string VehicleName { get; internal set; } = "";

        // ---- LOOKUP PROFILES Current Team------------------------------------
        public TeamLookupProfile ActiveTeamProfile { get; internal set; }


    


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