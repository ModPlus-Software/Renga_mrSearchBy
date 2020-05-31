namespace mrSearchBy.Model
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using ModPlusAPI;
    using ModPlusAPI.Mvvm;
    using Renga;

    /// <summary>
    /// Единицы для получения расчетных характеристик
    /// </summary>
    public class QuantityMap : VmBase
    {
        private const string LangItem = "mrSearchBy";

        /// <summary>
        /// Initializes a new instance of the <see cref="QuantityMap"/> class.
        /// </summary>
        public QuantityMap()
        {
            var lengthUnits = Enum.GetValues(typeof(LengthUnit)).Cast<LengthUnit>().ToList();
            lengthUnits.Remove(LengthUnit.LengthUnit_Unknown);
            LengthUnits = lengthUnits;

            var areaUnits = Enum.GetValues(typeof(AreaUnit)).Cast<AreaUnit>().ToList();
            areaUnits.Remove(AreaUnit.AreaUnit_Unknown);
            AreaUnits = areaUnits;

            var massUnits = Enum.GetValues(typeof(MassUnit)).Cast<MassUnit>().ToList();
            massUnits.Remove(MassUnit.MassUnit_Unknown);
            MassUnits = massUnits;

            var volumeUnits = Enum.GetValues(typeof(VolumeUnit)).Cast<VolumeUnit>().ToList();
            volumeUnits.Remove(VolumeUnit.VolumeUnit_Unknown);
            VolumeUnits = volumeUnits;
        }

        /// <summary>
        /// Допустимые единицы
        /// </summary>
        public List<LengthUnit> LengthUnits { get; }

        /// <summary>
        /// Единицы площади
        /// </summary>
        public LengthUnit LengthUnit
        {
            get => Enum.TryParse(UserConfigFile.GetValue(LangItem, nameof(LengthUnit)), out LengthUnit lengthUnit)
                ? lengthUnit
                : LengthUnit.LengthUnit_Millimeters;
            set
            {
                UserConfigFile.SetValue(LangItem, nameof(LengthUnit), value.ToString(), true);
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Допустимые единицы площади
        /// </summary>
        public List<AreaUnit> AreaUnits { get; }

        /// <summary>
        /// Единицы площади
        /// </summary>
        public AreaUnit AreaUnit
        {
            get => Enum.TryParse(UserConfigFile.GetValue(LangItem, nameof(AreaUnit)), out AreaUnit areaUnit)
                ? areaUnit
                : AreaUnit.AreaUnit_Meters2;
            set
            {
                UserConfigFile.SetValue(LangItem, nameof(AreaUnit), value.ToString(), true);
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Допустимые единицы массы
        /// </summary>
        public List<MassUnit> MassUnits { get; }

        /// <summary>
        /// Единицы массы
        /// </summary>
        public MassUnit MassUnit
        {
            get => Enum.TryParse(UserConfigFile.GetValue(LangItem, nameof(MassUnit)), out MassUnit massUnit)
                ? massUnit
                : MassUnit.MassUnit_Kilograms;
            set
            {
                UserConfigFile.SetValue(LangItem, nameof(MassUnit), value.ToString(), true);
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Допустимые единицы объема
        /// </summary>
        public List<VolumeUnit> VolumeUnits { get; }

        /// <summary>
        /// Единицы объема
        /// </summary>
        public VolumeUnit VolumeUnit
        {
            get => Enum.TryParse(UserConfigFile.GetValue(LangItem, nameof(VolumeUnit)), out VolumeUnit volumeUnit)
                ? volumeUnit
                : VolumeUnit.VolumeUnit_Meters3;
            set
            {
                UserConfigFile.SetValue(LangItem, nameof(VolumeUnit), value.ToString(), true);
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Допустимые значения точности
        /// </summary>
        public List<double> Tolerances =>
            new List<double>
            {
                1,
                0.1,
                0.01,
                0.001,
                0.0001,
                0.00001,
                0.000001
            };

        /// <summary>
        /// Точность сравнения чисел
        /// </summary>
        public double Tolerance
        {
            get => ModPlusAPI.IO.String.TryToDouble(UserConfigFile.GetValue(LangItem, nameof(Tolerance)), out var d) ? d : 0.01;
            set
            {
                UserConfigFile.SetValue(LangItem, nameof(Tolerance), value.ToString(CultureInfo.InvariantCulture), true);
                OnPropertyChanged();
            }
        }
    }
}
