namespace mrSearchBy
{
    using System.Collections.Generic;
    using ModPlusAPI.Interfaces;

    /// <inheritdoc/>
    public class ModPlusConnector : IModPlusFunctionForRenga
    {
        private static ModPlusConnector _instance;

        /// <summary>
        /// Singleton instance
        /// </summary>
        public static ModPlusConnector Instance => _instance ?? (_instance = new ModPlusConnector());

        /// <inheritdoc/>
        public SupportedProduct SupportedProduct => SupportedProduct.Renga;

        /// <inheritdoc/>
        public string Name => "mrSearchBy";

        /// <inheritdoc/>
        public RengaProduct RengaProduct => RengaProduct.Any;

        /// <inheritdoc/>
        public FunctionUILocation UiLocation => FunctionUILocation.PrimaryPanel;

        /// <inheritdoc/>
        public ContextMenuShowCase ContextMenuShowCase => ContextMenuShowCase.Scene;

        /// <inheritdoc/>
        public List<ViewType> ViewTypes => new List<ViewType>();

        /// <inheritdoc/>
        public bool IsAddingToMenuBySelf => false;

        /// <inheritdoc/>
        public string LName => "Поиск по условию";

        /// <inheritdoc/>
        public string Description => "Поиск объектов по различным условиям";

        /// <inheritdoc/>
        public string Author => "Пекшев Александр aka Modis";

        /// <inheritdoc/>
        public string Price => "0";

        /// <inheritdoc/>
        public string FullDescription => "Плагин позволяет производить поиск по свойствам, параметрам и величинам среди всех объектов модели или объектов указанного типа. Найденные объекты выделяются в модели. Присутствует возможность в строке поиска задавать несколько величины для поиска, указывать вариант работы «И-ИЛИ», а также использовать операторы «не равно», «меньше», «больше»";
    }
}
