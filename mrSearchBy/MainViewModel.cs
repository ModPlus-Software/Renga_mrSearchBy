namespace mrSearchBy
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Windows.Input;
    using Enums;
    using Model;
    using ModPlusAPI;
    using ModPlusAPI.Mvvm;
    using ModPlusAPI.Windows;

    /// <summary>
    /// Главная модель представления
    /// </summary>
    public class MainViewModel : VmBase
    {
        private const string LangItem = "mrSearchBy";
        private readonly Renga.Application _rengaApplication;
        private string _searchValue;
        private bool _isEnableSearch;
        private string _searchStatus;
        private readonly List<Guid> _quantityIds;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainViewModel"/> class.
        /// </summary>
        public MainViewModel()
        {
            _rengaApplication = new Renga.Application();
            QuantityMap = new QuantityMap();

            var types = new List<LocalizableObjectType>
            {
                new LocalizableObjectType(Guid.Empty)
            };
            types.AddRange(typeof(Renga.ObjectTypes)
                .GetProperties()
                .Select(p => (Guid)p.GetValue(null))
                .Select(guid => new LocalizableObjectType(guid)));
            ObjectTypes = types;

            _quantityIds = new List<Guid>(typeof(Renga.QuantityIds).GetProperties().Select(p => (Guid)p.GetValue(null)));
        }

        /// <summary>
        /// Значение для поиска
        /// </summary>
        public string SearchValue
        {
            get => _searchValue;
            set
            {
                if (_searchValue == value)
                    return;
                _searchValue = value;
                OnPropertyChanged();
                IsEnableSearch = !string.IsNullOrEmpty(value);
            }
        }

        /// <summary>
        /// Тип совпадения
        /// </summary>
        public MatchType MatchType
        {
            get => Enum.TryParse(UserConfigFile.GetValue(LangItem, nameof(MatchType)), out MatchType matchType)
                ? matchType 
                : MatchType.Contain;
            set
            {
                UserConfigFile.SetValue(LangItem, nameof(MatchType), value.ToString(), true);
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Типы объектов Renga
        /// </summary>
        public List<LocalizableObjectType> ObjectTypes { get; }

        /// <summary>
        /// Тип объекта для фильтрации. Null - любой тип объекта
        /// </summary>
        public LocalizableObjectType ObjectTypeFilter
        {
            get =>
                Guid.TryParse(UserConfigFile.GetValue(LangItem, nameof(ObjectTypeFilter)), out var g)
                    ? ObjectTypes.FirstOrDefault(t => t.ObjectType == g)
                    : ObjectTypes.FirstOrDefault(t => t.ObjectType == Guid.Empty);
            set
            {
                UserConfigFile.SetValue(LangItem, nameof(ObjectTypeFilter), value.ObjectType.ToString(), true);
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Доступность кнопки поиска
        /// </summary>
        public bool IsEnableSearch
        {
            get => _isEnableSearch;
            set
            {
                if (_isEnableSearch == value)
                    return;
                _isEnableSearch = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Статус поиска
        /// </summary>
        public string SearchStatus
        {
            get => _searchStatus;
            set
            {
                if (_searchStatus == value)
                    return;
                _searchStatus = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Поиск по свойствам
        /// </summary>
        public bool SearchByProperties
        {
            get => !bool.TryParse(UserConfigFile.GetValue(LangItem, nameof(SearchByProperties)), out var b) || b;
            set
            {
                UserConfigFile.SetValue(LangItem, nameof(SearchByProperties), value.ToString(), true);
                OnPropertyChanged();
                if (!value && !SearchByParameters && !SearchByQuantities)
                    SearchByParameters = true;
            }
        }

        /// <summary>
        /// Поиск по параметрам
        /// </summary>
        public bool SearchByParameters
        {
            get => !bool.TryParse(UserConfigFile.GetValue(LangItem, nameof(SearchByParameters)), out var b) || b;
            set
            {
                UserConfigFile.SetValue(LangItem, nameof(SearchByParameters), value.ToString(), true);
                OnPropertyChanged();
                if (!value && !SearchByQuantities && !SearchByProperties)
                    SearchByQuantities = true;
            }
        }

        /// <summary>
        /// Поиск по расчетным характеристикам
        /// </summary>
        public bool SearchByQuantities
        {
            get => !bool.TryParse(UserConfigFile.GetValue(LangItem, nameof(SearchByQuantities)), out var b) || b;
            set
            {
                UserConfigFile.SetValue(LangItem, nameof(SearchByQuantities), value.ToString(), true);
                OnPropertyChanged();
                if (!value && !SearchByParameters && !SearchByProperties)
                    SearchByProperties = true;
            }
        }

        /// <summary>
        /// Единицы для получения расчетных характеристик
        /// </summary>
        public QuantityMap QuantityMap { get; }

        /// <summary>
        /// Сбрасывать текущий выбор, если ничего не найдено
        /// </summary>
        public bool UnselectIfNotFound
        {
            get => bool.TryParse(UserConfigFile.GetValue(LangItem, nameof(UnselectIfNotFound)), out var b) && b;
            set
            {
                UserConfigFile.SetValue(LangItem, nameof(UnselectIfNotFound), value.ToString(), true);
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Команда запуска поиска
        /// </summary>
        public ICommand SearchCommand => new RelayCommandWithoutParameter(Search);

        private void Search()
        {
            try
            {
                var and = false;
                var searchValue = SearchValue.ToUpper();
                if (searchValue.StartsWith("AND"))
                {
                    and = true;
                    searchValue = searchValue.Substring(3).ToUpper();
                }
                else
                {
                    searchValue = searchValue.ToUpper();
                }

                var searchData = new SearchData(searchValue, MatchType, QuantityMap);

                var matchObjects = new List<Renga.IModelObject>();

                var objects = _rengaApplication.Project.Model.GetObjects();
                for (var i = 0; i < objects.Count; i++)
                {
                    var modelObject = objects.GetByIndex(i);

                    Debug.Print(
                        $"Model object: {ModPlus.Helpers.Localization.RengaObjectType(modelObject.ObjectType)}");

                    if (ObjectTypeFilter.ObjectType != Guid.Empty &&
                        modelObject.ObjectType != ObjectTypeFilter.ObjectType)
                        continue;

                    var searchNext = true;

                    var matchValues = new List<string>();
                    if (SearchByProperties)
                    {
                        var propertyContainer = modelObject.GetProperties();
                        var guidCollection = propertyContainer.GetIds();
                        for (var j = 0; j < guidCollection.Count; j++)
                        {
                            var property = propertyContainer.Get(guidCollection.Get(j));
                            if (!searchData.IsMatch(property, out var matchValue))
                                continue;

                            if (and)
                            {
                                matchValues.Add(matchValue);
                            }
                            else
                            {
                                matchObjects.Add(modelObject);
                                searchNext = false;
                                break;
                            }
                        }
                    }

                    if (SearchByParameters && searchNext)
                    {
                        var parameterContainer = modelObject.GetParameters();
                        var guidCollection = parameterContainer.GetIds();

                        for (var j = 0; j < guidCollection.Count; j++)
                        {
                            var parameter = parameterContainer.Get(guidCollection.Get(j));
                            if (!searchData.IsMatch(parameter, out var matchValue)) 
                                continue;

                            if (and)
                            {
                                matchValues.Add(matchValue);
                            }
                            else
                            {
                                matchObjects.Add(modelObject);
                                searchNext = false;
                                break;
                            }
                        }
                    }

                    if (SearchByQuantities && searchNext)
                    {
                        var quantityContainer = modelObject.GetQuantities();
                        foreach (var quantityId in _quantityIds)
                        {
                            var quantity = quantityContainer.Get(quantityId);
                            if (quantity == null)
                                continue;
                            if (!searchData.IsMatch(quantity, out var matchValue)) 
                                continue;

                            if (and)
                            {
                                matchValues.Add(matchValue);
                            }
                            else
                            {
                                matchObjects.Add(modelObject);
                                break;
                            }
                        }
                    }

                    if (and && searchData.IsMatchAll(matchValues))
                    {
                        matchObjects.Add(modelObject);
                    }
                }

                var selection = _rengaApplication.Selection;

                if (matchObjects.Any())
                {
                    // Найдено {0} объектов
                    SearchStatus = string.Format(Language.GetItem(LangItem, "h11"), matchObjects.Count);
                    selection.SetSelectedObjects(matchObjects.Select(o => o.Id).ToArray());
                }
                else
                {
                    // Ничего не найдено
                    SearchStatus = Language.GetItem(LangItem, "h12");
                    if (UnselectIfNotFound)
                        selection.SetSelectedObjects(new int[] { });
                }
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
            }
        }

        /// <summary>
        /// Объект, инкапсулирующий в себе работу с поисковым значением
        /// </summary>
        internal class SearchData
        {
            private readonly double _tolerance;
            private readonly MatchType _matchType;
            private readonly List<Value> _searchStringValues;
            private readonly List<Value> _searchDoubleValues;
            private readonly List<Value> _searchIntValues;
            private readonly List<Value> _searchBoolValues;
            private readonly Renga.LengthUnit _lengthUnit;
            private readonly Renga.MassUnit _massUnit;
            private readonly Renga.AreaUnit _areaUnit;
            private readonly Renga.VolumeUnit _volumeUnit;

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
                        _searchIntValues.Add(new Value(op, s, i));
                        addString = false;
                    }

                    if (double.TryParse(s, out var d))
                    {
                        _searchDoubleValues.Add(new Value(op, s, d));
                        addString = false;
                    }

                    if (bool.TryParse(s, out var b))
                    {
                        _searchBoolValues.Add(new Value(op, s, b));
                        addString = false;
                    }

                    if (addString)
                        _searchStringValues.Add(new Value(op, s));

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
            public bool IsMatch(Renga.IProperty property, out string matchValue)
            {
                matchValue = string.Empty;
                if (!property.HasValue())
                    return false;

                if (property.Type == Renga.PropertyType.PropertyType_Double)
                {
                    var doubleValue = property.GetDoubleValue();
                    Debug.Print($"Double property {property.Name}: {doubleValue}");
                    return IsMatch(doubleValue, out matchValue);
                }

                if (property.Type == Renga.PropertyType.PropertyType_String)
                {
                    var stringValue = property.GetStringValue().ToUpper();
                    Debug.Print($"String property {property.Name}: {stringValue}");
                    return IsMatch(stringValue, out matchValue);
                }

                return false;
            }

            /// <summary>
            /// Совпадает ли параметр с поисковым значением
            /// </summary>
            /// <param name="parameter">Параметр</param>
            /// <param name="matchValue">Совпавшее строковое значение из исходной поисковой строки, без учета операторов</param>
            public bool IsMatch(Renga.IParameter parameter, out string matchValue)
            {
                if (parameter.ValueType == Renga.ParameterValueType.ParameterValueType_Int)
                {
                    var intValue = parameter.GetIntValue();
                    Debug.Print($"Int parameter {parameter.Definition.Name}: {intValue}");
                    return IsMatch(intValue, out matchValue);
                }

                if (parameter.ValueType == Renga.ParameterValueType.ParameterValueType_Double)
                {
                    var doubleValue = parameter.GetDoubleValue();
                    Debug.Print($"Double parameter {parameter.Definition.Name}: {doubleValue}");
                    return IsMatch(doubleValue, out matchValue);
                }

                if (parameter.ValueType == Renga.ParameterValueType.ParameterValueType_Bool)
                {
                    var boolValue = parameter.GetBoolValue();
                    Debug.Print($"Bool parameter {parameter.Definition.Name}: {boolValue}");
                    return IsMatch(boolValue, out matchValue);
                }

                if (parameter.ValueType == Renga.ParameterValueType.ParameterValueType_String)
                {
                    var stringValue = parameter.GetStringValue();
                    Debug.Print($"String parameter {parameter.Definition.Name}: {stringValue}");
                    return IsMatch(stringValue, out matchValue);
                }

                matchValue = string.Empty;
                return false;
            }

            /// <summary>
            /// Совпадает ли расчетная характеристика с поисковым значением
            /// </summary>
            /// <param name="quantity">Величина</param>
            /// <param name="matchValue">Совпавшее строковое значение из исходной поисковой строки, без учета операторов</param>
            public bool IsMatch(Renga.IQuantity quantity, out string matchValue)
            {
                if (quantity.Type == Renga.QuantityType.QuantityType_Area)
                {
                    var area = quantity.AsArea(_areaUnit);
                    Debug.Print($"Area quantity {area}");
                    return IsMatch(area, out matchValue);
                }

                if (quantity.Type == Renga.QuantityType.QuantityType_Count)
                {
                    var count = quantity.AsCount();
                    Debug.Print($"Count quantity {count}");
                    return IsMatch(count, out matchValue);
                }

                if (quantity.Type == Renga.QuantityType.QuantityType_Length)
                {
                    var length = quantity.AsLength(_lengthUnit);
                    Debug.Print($"Length quantity {length}");
                    return IsMatch(length, out matchValue);
                }

                if (quantity.Type == Renga.QuantityType.QuantityType_Mass)
                {
                    var mass = quantity.AsMass(_massUnit);
                    Debug.Print($"Mass quantity {mass}");
                    return IsMatch(mass, out matchValue);
                }

                if (quantity.Type == Renga.QuantityType.QuantityType_Volume)
                {
                    var volume = quantity.AsVolume(_volumeUnit);
                    Debug.Print($"Volume quantity {volume}");
                    return IsMatch(volume, out matchValue);
                }

                matchValue = string.Empty;
                return false;
            }

            private bool IsMatch(int intValue, out string matchValue)
            {
                foreach (var value in _searchIntValues)
                {
                    matchValue = value.StringValue;
                    if (value.Operator == Operator.Equal && value.IntValue == intValue)
                        return true;
                    if (value.Operator == Operator.NotEqual && value.IntValue != intValue)
                        return true;
                    if (value.Operator == Operator.More && intValue > value.IntValue)
                        return true;
                    if (value.Operator == Operator.Less && intValue < value.IntValue)
                        return true;
                }

                matchValue = string.Empty;
                return false;
            }

            private bool IsMatch(double doubleValue, out string matchValue)
            {
                foreach (var value in _searchDoubleValues)
                {
                    matchValue = value.StringValue;
                    if (value.Operator == Operator.Equal && Math.Abs(value.DoubleValue - doubleValue) < _tolerance)
                        return true;
                    if (value.Operator == Operator.NotEqual && Math.Abs(value.DoubleValue - doubleValue) > _tolerance)
                        return true;
                    if (value.Operator == Operator.More && doubleValue > value.DoubleValue)
                        return true;
                    if (value.Operator == Operator.Less && doubleValue < value.DoubleValue)
                        return true;
                }

                matchValue = string.Empty;
                return false;
            }

            private bool IsMatch(string stringValue, out string matchValue)
            {
                foreach (var value in _searchStringValues)
                {
                    matchValue = value.StringValue;
                    if (value.Operator == Operator.Equal &&
                        ((_matchType == MatchType.Equal && value.StringValue == stringValue) ||
                         (_matchType == MatchType.Contain && stringValue.Contains(value.StringValue))))
                        return true;
                    if (value.Operator == Operator.NotEqual && value.StringValue != stringValue)
                        return true;
                }

                matchValue = string.Empty;
                return false;
            }

            private bool IsMatch(bool boolValue, out string matchValue)
            {
                foreach (var value in _searchBoolValues)
                {
                    matchValue = value.StringValue;
                    if (value.BoolValue == boolValue)
                        return true;
                }

                matchValue = string.Empty;
                return false;
            }

            private class Value
            {
                public Value(Operator op, string stringValue)
                {
                    Operator = op;
                    StringValue = stringValue;
                }

                public Value(Operator op, string stringValue, double doubleValue)
                    : this(op, stringValue)
                {
                    DoubleValue = doubleValue;
                }

                public Value(Operator op, string stringValue, int intValue) 
                    : this(op, stringValue)
                {
                    IntValue = intValue;
                }

                public Value(Operator op, string stringValue, bool boolValue)
                    : this(op, stringValue)
                {
                    BoolValue = boolValue;
                }

                public Operator Operator { get; }

                public string StringValue { get; }

                public double DoubleValue { get; }

                public int IntValue { get; }

                public bool BoolValue { get; }
            }
        }
    }
}
