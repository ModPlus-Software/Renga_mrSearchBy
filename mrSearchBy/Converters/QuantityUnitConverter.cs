namespace mrSearchBy.Converters
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Windows.Data;
    using ModPlusAPI;
    using Renga;

    /// <summary>
    /// Quantity unit display converter
    /// </summary>
    public class QuantityUnitConverter : IValueConverter
    {
        private const string LangItem = "mrSearchBy";

        private readonly Dictionary<string, string> _dictionary;

        /// <summary>
        /// Initializes a new instance of the <see cref="QuantityUnitConverter"/> class.
        /// </summary>
        public QuantityUnitConverter()
        {
            _dictionary = new Dictionary<string, string>
            {
                { Language.GetItem(LangItem, "q1"), LengthUnit.LengthUnit_Millimeters.ToString() },
                { Language.GetItem(LangItem, "q2"), LengthUnit.LengthUnit_Centimeters.ToString() },
                { Language.GetItem(LangItem, "q3"), LengthUnit.LengthUnit_Meters.ToString() },
                { Language.GetItem(LangItem, "q4"), LengthUnit.LengthUnit_Inches.ToString() },
                { Language.GetItem(LangItem, "q5"), AreaUnit.AreaUnit_Millimeters2.ToString() },
                { Language.GetItem(LangItem, "q6"), AreaUnit.AreaUnit_Centimeters2.ToString() },
                { Language.GetItem(LangItem, "q7"), AreaUnit.AreaUnit_Meters2.ToString() },
                { Language.GetItem(LangItem, "q8"), MassUnit.MassUnit_Grams.ToString() },
                { Language.GetItem(LangItem, "q9"), MassUnit.MassUnit_Kilograms.ToString() },
                { Language.GetItem(LangItem, "q10"), VolumeUnit.VolumeUnit_Millimeters3.ToString() },
                { Language.GetItem(LangItem, "q11"), VolumeUnit.VolumeUnit_Centimeters3.ToString() },
                { Language.GetItem(LangItem, "q12"), VolumeUnit.VolumeUnit_Meters3.ToString() },
            };
        }

        /// <inheritdoc />
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is LengthUnit lengthUnit)
            {
                return _dictionary.FirstOrDefault(d => Enum.TryParse(d.Value, out LengthUnit l) && l == lengthUnit).Key;
            }

            if (value is AreaUnit areaUnit)
            {
                return _dictionary.FirstOrDefault(d => Enum.TryParse(d.Value, out AreaUnit a) && a == areaUnit).Key;
            }

            if (value is MassUnit massUnit)
            {
                return _dictionary.FirstOrDefault(d => Enum.TryParse(d.Value, out MassUnit m) && m == massUnit).Key;
            }

            if (value is VolumeUnit volumeUnit)
            {
                return _dictionary.FirstOrDefault(d => Enum.TryParse(d.Value, out VolumeUnit v) && v == volumeUnit).Key;
            }

            return value;
        }

        /// <inheritdoc />
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string s && _dictionary.TryGetValue(s, out var e))
            {
                return e;
            }

            return value;
        }
    }
}
