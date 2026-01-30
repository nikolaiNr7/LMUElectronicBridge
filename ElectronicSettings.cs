using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

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

    public class ElectronicSettings : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        private bool SetField<T>(ref T field, T value, [CallerMemberName] string name = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(name);
            return true;
        }

        [LmuProperty("VM_TRACTIONCONTROLMAP")]
        private int _tc_main;
        public int TC_Main { get => _tc_main; set => SetField(ref _tc_main, value); }
        private string _tc_main_str;
        public string TC_Main_Str { get => _tc_main_str; set => SetField(ref _tc_main_str, value); }
        public int TC_Main_Max { get; internal set; } = 10;
        public int TC_Main_Min { get; internal set; } = 0;

        [LmuProperty("VM_TRACTIONCONTROLPOWERCUTMAP")]
        private int _tc_cut;
        public int TC_Cut { get => _tc_cut; set => SetField(ref _tc_cut, value); }
        private string _tc_cut_str;
        public string TC_Cut_Str { get => _tc_cut_str; set => SetField(ref _tc_cut_str, value); }
        public int TC_Cut_Max { get; internal set; } = 10;
        public int TC_Cut_Min { get; internal set; } = 0;

        [LmuProperty("VM_TRACTIONCONTROLSLIPANGLEMAP")]
        private int _tc_slip;
        public int TC_Slip { get => _tc_slip; set => SetField(ref _tc_slip, value); }
        private string _tc_slip_str;
        public string TC_Slip_Str { get => _tc_slip_str; set => SetField(ref _tc_slip_str, value); }
        public int TC_Slip_Max { get; internal set; } = 10;
        public int TC_Slip_Min { get; internal set; } = 0;

        [LmuProperty("VM_ANTILOCKBRAKESYSTEMMAP")]
        private int _abs;
        public int ABS { get => _abs; set => SetField(ref _abs, value); }
        private string _abs_str;
        public string ABS_Str { get => _abs_str; set => SetField(ref _abs_str, value); }
        public int ABS_Max { get; internal set; } = 10;
        public int ABS_Min { get; internal set; } = 0;

        [LmuProperty("VM_REGEN_LEVEL", "Regen")]
        private int _regen;
        public int Regen { get => _regen; set => SetField(ref _regen, value); }
        private string _regen_str;
        public string Regen_Str { get => _regen_str; set => SetField(ref _regen_str, value); }
        public int Regen_Max { get; internal set; } = 10;
        public int Regen_Min { get; internal set; } = 0;

        [LmuProperty("VM_BRAKE_MIGRATION", "BrakeMigration")]
        private int _brakeMigration;
        public int BrakeMigration { get => _brakeMigration; set => SetField(ref _brakeMigration, value); }
        private string _brakeMigration_str;
        public string BrakeMigration_Str { get => _brakeMigration_str; set => SetField(ref _brakeMigration_str, value); }
        public int BrakeMigration_Max { get; internal set; } = 5;
        public int BrakeMigration_Min { get; internal set; } = 0;

        [LmuProperty("VM_ELECTRIC_MOTOR_MAP", "MotorMap")]
        private int _motorMap;
        public int MotorMap { get => _motorMap; set => SetField(ref _motorMap, value); }
        private string _motorMap_str;
        public string MotorMap_Str { get => _motorMap_str; set => SetField(ref _motorMap_str, value); }
        public int MotorMap_Max { get; internal set; } = 5;
        public int MotorMap_Min { get; internal set; } = 0;

        [LmuProperty("VM_MIXTURE_MAP")]
        private int _mixture;
        public int Mixture { get => _mixture; set => SetField(ref _mixture, value); }
        private string _mixture_str;
        public string Mixture_Str { get => _mixture_str; set => SetField(ref _mixture_str, value); }
        public int Mixture_Max { get; internal set; } = 5;
        public int Mixture_Min { get; internal set; } = 0;

        [LmuProperty("VM_FRONT_ANTISWAY", "ARB")]
        private int _frontArb;
        public int FrontARB { get => _frontArb; set => SetField(ref _frontArb, value); }
        private string _frontArb_str;
        public string FrontARB_Str { get => _frontArb_str; set => SetField(ref _frontArb_str, value); }
        public int FrontARB_Max { get; internal set; } = 5;
        public int FrontARB_Min { get; internal set; } = 0;

        [LmuProperty("VM_REAR_ANTISWAY", "ARB")]
        private int _rearArb;
        public int RearARB { get => _rearArb; set => SetField(ref _rearArb, value); }
        private string _rearArb_str;
        public string RearARB_Str { get => _rearArb_str; set => SetField(ref _rearArb_str, value); }
        public int RearARB_Max { get; internal set; } = 5;
        public int RearARB_Min { get; internal set; } = 0;
    }
}