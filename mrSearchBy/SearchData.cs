namespace mrSearchBy
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using Enums;
    using Model;
    using ModPlus.Helpers;
    using Renga;

    /// <summary>
    /// Объект, инкапсулирующий в себе работу с поисковым значением
    /// </summary>
    public class SearchData
    {
        private readonly double _tolerance;
        private readonly MatchType _matchType;
        private readonly List<Value> _searchStringValues;
        private readonly List<Value> _searchDoubleValues;
        private readonly List<Value> _searchIntValues;
        private readonly List<Value> _searchBoolValues;
        private readonly LengthUnit _lengthUnit;
        private readonly MassUnit _massUnit;
        private readonly AreaUnit _areaUnit;
        private readonly VolumeUnit _volumeUnit;

        // Поисковые значение без операторов и управляющих символов
        private readonly List<string> _searchValues;

        /// <summary>
        /// Initializes a new instance of the <see cref="SearchData"/> class.
        /// </summary>
        /// <param name="searchValue">Исходная поисковая строка</param>
        /// <param name="matchType">Тип совпадения для строковых значений</param>
        /// <param name="quantityMap">Единицы получения величин</param>
        public SearchData(string searchValue, MatchType matchType, QuantityMap quantityMap)
        {
            _matchType = matchType;
            _lengthUnit = quantityMap.LengthUnit;
            _massUnit = quantityMap.MassUnit;
            _areaUnit = quantityMap.AreaUnit;
            _volumeUnit = quantityMap.VolumeUnit;
            _tolerance = quantityMap.Tolerance;
            _searchBoolValues = new List<Value>();
            _searchDoubleValues = new List<Value>();
            _searchIntValues = new List<Value>();
            _searchStringValues = new List<Value>();

            _searchValues = new List<string>();

            foreach (var str in searchValue.Split('*'))
            {
                var s = str;
                var name = string.Empty;
                if (s.Contains("="))
                {
                    var split = s.Split('=');
                    name = split[0];
                    s = split[1];
                }

                var op = Operator.Equal;
                if (str.StartsWith("!="))
                {
                    op = Operator.NotEqual;
                    s = str.Substring(2);
                }
                else if (str.StartsWith("<"))
                {
                    op = Operator.Less;
                    s = str.Substring(1);
                }
                else if (str.StartsWith(">"))
                {
                    op = Operator.More;
                    s = str.Substring(1);
                }

                var addString = true;
                if (int.TryParse(s, out var i))
                {
                    _searchIntValues.Add(new Value(op, s, i, name, matchType));
                    addString = false;
                }

                if (double.TryParse(s, out var d))
                {
                    _searchDoubleValues.Add(new Value(op, s, d, name, matchType));
                    addString = false;
                }

                if (bool.TryParse(s, out var b))
                {
                    _searchBoolValues.Add(new Value(op, s, b, name, matchType));
                    addString = false;
                }

                if (addString)
                    _searchStringValues.Add(new Value(op, s, name, matchType));

                _searchValues.Add(s);
            }
        }

        /// <summary>
        /// Оператор
        /// </summary>
        private enum Operator
        {
            /// <summary>
            /// Равно
            /// </summary>
            Equal,

            /// <summary>
            /// Не равно
            /// </summary>
            NotEqual,

            /// <summary>
            /// Больше
            /// </summary>
            More,

            /// <summary>
            /// Меньше
            /// </summary>
            Less
        }

        /// <summary>
        /// Совпадают ли все найденные совпавшие значения со списком поисковых значений
        /// </summary>
        /// <param name="matchStringValues">Совпавшие значения</param>
        /// <returns></returns>
        public bool IsMatchAll(List<string> matchStringValues)
        {
            var set = new HashSet<string>(_searchValues);
            set.ExceptWith(matchStringValues);
            return !set.Any();
        }

        /// <summary>
        /// Совпадает ли свойство с поисковым значением
        /// </summary>
        /// <param name="property">Свойство</param>
        /// <param name="matchValue">Совпавшее строковое значение из исходной поисковой строки, без учета операторов</param>
        public bool IsMatch(IProperty property, out string matchValue)
        {
            matchValue = string.Empty;
            if (property.Type != PropertyType.PropertyType_String && !property.HasValue())
                return false;

            switch (property.Type)
            {
                case PropertyType.PropertyType_Double:
                    return IsMatch(property.GetDoubleValue(), property.Name, out matchValue);
                case PropertyType.PropertyType_String:
                    return IsMatch(property.GetStringValue()?.ToUpper() ?? string.Empty, property.Name, out matchValue);
                case PropertyType.PropertyType_Angle:
                    return IsMatch(property.GetAngleValue(AngleUnit.AngleUnit_Degrees), property.Name, out matchValue);
                case PropertyType.PropertyType_Boolean:
                    return IsMatch(property.GetBooleanValue(), property.Name, out matchValue);
                case PropertyType.PropertyType_Area:
                    return IsMatch(property.GetAreaValue(_areaUnit), property.Name, out matchValue);
                case PropertyType.PropertyType_Integer:
                    return IsMatch(property.GetIntegerValue(), property.Name, out matchValue);
                case PropertyType.PropertyType_Length:
                    return IsMatch(property.GetLengthValue(_lengthUnit), property.Name, out matchValue);
                case PropertyType.PropertyType_Logical:
                    return IsMatch(ModPlusAPI.Language.GetItem("RengaDlls", property.GetLogicalValue().ToString()), property.Name, out matchValue);
                case PropertyType.PropertyType_Mass:
                    return IsMatch(property.GetMassValue(_massUnit), property.Name, out matchValue);
                case PropertyType.PropertyType_Volume:
                    return IsMatch(property.GetVolumeValue(_volumeUnit), property.Name, out matchValue);
                case PropertyType.PropertyType_Enumeration:
                    return IsMatch(property.GetEnumerationValue() ?? string.Empty, property.Name, out matchValue);
                default:
                    return false;
            }
        }

        /// <summary>
        /// Совпадает ли параметр с поисковым значением
        /// </summary>
        /// <param name="parameter">Параметр</param>
        /// <param name="matchValue">Совпавшее строковое значение из исходной поисковой строки, без учета операторов</param>
        public bool IsMatch(IParameter parameter, out string matchValue)
        {
            switch (parameter.ValueType)
            {
                case ParameterValueType.ParameterValueType_Int:
                    return IsMatch(parameter.GetIntValue(), parameter.Definition.Name, out matchValue);
                case ParameterValueType.ParameterValueType_Double:
                    return IsMatch(parameter.GetDoubleValue(), parameter.Definition.Name, out matchValue);
                case ParameterValueType.ParameterValueType_Bool:
                    return IsMatch(parameter.GetBoolValue(), parameter.Definition.Name, out matchValue);
                case ParameterValueType.ParameterValueType_String:
                    return IsMatch(parameter.GetStringValue()?.ToUpper() ?? string.Empty, parameter.Definition.Name, out matchValue);
                default:
                    matchValue = string.Empty;
                    return false;
            }
        }

        /// <summary>
        /// Совпадает ли расчетная характеристика с поисковым значением
        /// </summary>
        /// <param name="quantity">Величина</param>
        /// <param name="name">Имя расчетной характеристики</param>
        /// <param name="matchValue">Совпавшее строковое значение из исходной поисковой строки, без учета операторов</param>
        public bool IsMatch(IQuantity quantity, string name, out string matchValue)
        {
            switch (quantity.Type)
            {
                case QuantityType.QuantityType_Area:
                    return IsMatch(quantity.AsArea(_areaUnit), name, out matchValue);
                case QuantityType.QuantityType_Count:
                    return IsMatch(quantity.AsCount(), name, out matchValue);
                case QuantityType.QuantityType_Length:
                    return IsMatch(quantity.AsLength(_lengthUnit), name, out matchValue);
                case QuantityType.QuantityType_Mass:
                    return IsMatch(quantity.AsMass(_massUnit), name, out matchValue);
                case QuantityType.QuantityType_Volume:
                    return IsMatch(quantity.AsVolume(_volumeUnit), name, out matchValue);
                default:
                    matchValue = string.Empty;
                    return false;
            }
        }

        private bool IsMatch(int intValue, string name, out string matchValue)
        {
            foreach (var value in _searchIntValues)
            {
                matchValue = value.StringValue;
                if (value.Operator == Operator.Equal && value.IntValue == intValue && value.IsMatchName(name))
                    return true;
                if (value.Operator == Operator.NotEqual && value.IntValue != intValue && value.IsMatchName(name))
                    return true;
                if (value.Operator == Operator.More && intValue > value.IntValue && value.IsMatchName(name))
                    return true;
                if (value.Operator == Operator.Less && intValue < value.IntValue && value.IsMatchName(name))
                    return true;
            }

            matchValue = string.Empty;
            return false;
        }

        private bool IsMatch(double doubleValue, string name, out string matchValue)
        {
            foreach (var value in _searchDoubleValues)
            {
                matchValue = value.StringValue;
                if (value.Operator == Operator.Equal && 
                    Math.Abs(value.DoubleValue - doubleValue) < _tolerance &&
                    value.IsMatchName(name))
                    return true;
                if (value.Operator == Operator.NotEqual &&
                    Math.Abs(value.DoubleValue - doubleValue) > _tolerance &&
                    value.IsMatchName(name))
                    return true;
                if (value.Operator == Operator.More &&
                    doubleValue > value.DoubleValue &&
                    value.IsMatchName(name))
                    return true;
                if (value.Operator == Operator.Less &&
                    doubleValue < value.DoubleValue &&
                    value.IsMatchName(name))
                    return true;
            }

            matchValue = string.Empty;
            return false;
        }

        private bool IsMatch(string stringValue, string name, out string matchValue)
        {
            foreach (var value in _searchStringValues)
            {
                matchValue = value.StringValue;
                if (value.Operator == Operator.Equal &&
                    ((_matchType == MatchType.Equal && value.StringValue == stringValue) ||
                     (_matchType == MatchType.Contain && stringValue.Contains(value.StringValue))) &&
                    value.IsMatchName(name))
                    return true;
                if (value.Operator == Operator.NotEqual && value.StringValue != stringValue && value.IsMatchName(name))
                    return true;
            }

            matchValue = string.Empty;
            return false;
        }

        private bool IsMatch(bool boolValue, string name, out string matchValue)
        {
            foreach (var value in _searchBoolValues)
            {
                matchValue = value.StringValue;
                if (value.BoolValue == boolValue && value.IsMatchName(name))
                    return true;
            }

            matchValue = string.Empty;
            return false;
        }

        private class Value
        {
            private readonly MatchType _matchType;

            public Value(Operator op, string stringValue, string name, MatchType matchType)
            {
                _matchType = matchType;
                Operator = op;
                StringValue = stringValue;
                Name = name.ToUpper();
            }

            public Value(Operator op, string stringValue, double doubleValue, string name, MatchType matchType)
                : this(op, stringValue, name, matchType)
            {
                DoubleValue = doubleValue;
            }

            public Value(Operator op, string stringValue, int intValue, string name, MatchType matchType)
                : this(op, stringValue, name, matchType)
            {
                IntValue = intValue;
            }

            public Value(Operator op, string stringValue, bool boolValue, string name, MatchType matchType)
                : this(op, stringValue, name, matchType)
            {
                BoolValue = boolValue;
            }

            public string Name { get; }

            public Operator Operator { get; }

            public string StringValue { get; }

            public double DoubleValue { get; }

            public int IntValue { get; }

            public bool BoolValue { get; }

            public bool IsMatchName(string name)
            {
                if (string.IsNullOrEmpty(Name))
                    return true;

                if (_matchType == MatchType.Equal && name.ToUpper() == Name)
                    return true;
                
                if (_matchType == MatchType.Contain && name.ToUpper().Contains(Name))
                    return true;

                return false;
            }
        }
    }
}