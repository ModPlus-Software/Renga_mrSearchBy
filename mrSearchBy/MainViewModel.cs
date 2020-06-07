namespace mrSearchBy
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Windows;
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
        private List<Renga.IModelObject> _matchObjects;

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

            ShowButtonVisibility = _rengaApplication.ActiveView.Type == Renga.ViewType.ViewType_View3D
                ? Visibility.Visible
                : Visibility.Collapsed;
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
                SearchStatus = string.Empty;
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
        /// Доступность кнопки "Показать"
        /// </summary>
        public bool IsEnableShow => _matchObjects != null && _matchObjects.Any();

        /// <summary>
        /// Видимость кнопки "Показать"
        /// </summary>
        public Visibility ShowButtonVisibility { get; }

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

        /// <summary>
        /// Команда "Показать найденные объекты"
        /// </summary>
        public ICommand ShowCommand => new RelayCommandWithoutParameter(() =>
        {
            try
            {
                if (_rengaApplication.ActiveView.Type != Renga.ViewType.ViewType_View3D || !_matchObjects.Any())
                    return;

                var objectIds = _matchObjects.Select(o => o.Id).ToArray();
                _rengaApplication.Selection.SetSelectedObjects(objectIds);
                (_rengaApplication.ActiveView as Renga.IModelView)?.ShowObjects(objectIds);
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
            }
        });

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

                _matchObjects = new List<Renga.IModelObject>();

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
                                _matchObjects.Add(modelObject);
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
                                _matchObjects.Add(modelObject);
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
                            if (!searchData.IsMatch(quantity, null, out var matchValue)) 
                                continue;

                            if (and)
                            {
                                matchValues.Add(matchValue);
                            }
                            else
                            {
                                _matchObjects.Add(modelObject);
                                break;
                            }
                        }
                    }

                    if (and && searchData.IsMatchAll(matchValues))
                    {
                        _matchObjects.Add(modelObject);
                    }
                }

                var selection = _rengaApplication.Selection;

                if (_matchObjects.Any())
                {
                    // Найдено {0} объектов
                    SearchStatus = string.Format(Language.GetItem(LangItem, "h11"), _matchObjects.Count);
                    selection.SetSelectedObjects(_matchObjects.Select(o => o.Id).ToArray());
                }
                else
                {
                    // Ничего не найдено
                    SearchStatus = Language.GetItem(LangItem, "h12");
                    if (UnselectIfNotFound)
                        selection.SetSelectedObjects(new int[] { });
                }

                OnPropertyChanged(nameof(IsEnableShow));
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
            }
        }
    }
}
