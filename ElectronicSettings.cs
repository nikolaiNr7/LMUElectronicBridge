using System;

namespace LMUElectronicBridge
{
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

    public class ElectronicSettings
    {
        [LmuProperty("VM_TRACTIONCONTROLMAP")]
        public int TC_Main { get; set; }
        public string TC_Main_Str { get; set; }
        public int TC_Main_Max { get; internal set; } = 10;
        public int TC_Main_Min { get; internal set; } = 0;

        [LmuProperty("VM_TRACTIONCONTROLPOWERCUTMAP")]
        public int TC_Cut { get; set; }
        public string TC_Cut_Str { get; set; }
        public int TC_Cut_Max { get; internal set; } = 10;
        public int TC_Cut_Min { get; internal set; } = 0;

        [LmuProperty("VM_TRACTIONCONTROLSLIPANGLEMAP")]
        public int TC_Slip { get; set; }
        public string TC_Slip_Str { get; set; }
        public int TC_Slip_Max { get; internal set; } = 10;
        public int TC_Slip_Min { get; internal set; } = 0;

        [LmuProperty("VM_ANTILOCKBRAKESYSTEMMAP")]
        public int ABS { get; set; }
        public string ABS_Str { get; set; }
        public int ABS_Max { get; internal set; } = 10;
        public int ABS_Min { get; internal set; } = 0;

        [LmuProperty("VM_REGEN_LEVEL", "Regen")]
        public int Regen { get; set; }
        public string Regen_Str { get; set; }
        public int Regen_Max { get; internal set; } = 10;
        public int Regen_Min { get; internal set; } = 0;

        [LmuProperty("VM_BRAKE_MIGRATION", "BrakeMigration")]
        public int BrakeMigration { get; set; }
        public string BrakeMigration_Str { get; set; }
        public int BrakeMigration_Max { get; internal set; } = 5;
        public int BrakeMigration_Min { get; internal set; } = 0;

        [LmuProperty("VM_ELECTRIC_MOTOR_MAP", "MotorMap")]
        public int MotorMap { get; set; }
        public string MotorMap_Str { get; set; }
        public int MotorMap_Max { get; internal set; } = 5;
        public int MotorMap_Min { get; internal set; } = 0;

        [LmuProperty("VM_ENGINE_MIXTURE", "EngineMixture")]
        public int Mixture { get; set; }
        public string Mixture_Str { get; set; }
        public int Mixture_Max { get; internal set; } = 2;
        public int Mixture_Min { get; internal set; } = 0;

        [LmuProperty("VM_FRONT_ANTISWAY", "ARB")]
        public int FrontARB { get; set; }
        public string FrontARB_Str { get; set; }
        public int FrontARB_Max { get; internal set; } = 5;
        public int FrontARB_Min { get; internal set; } = 0;

        [LmuProperty("VM_REAR_ANTISWAY", "ARB")]
        public int RearARB { get; set; }
        public string RearARB_Str { get; set; }
        public int RearARB_Max { get; internal set; } = 5;
        public int RearARB_Min { get; internal set; } = 0;
    }
}