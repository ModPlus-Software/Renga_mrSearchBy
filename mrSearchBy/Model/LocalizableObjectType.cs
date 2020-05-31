namespace mrSearchBy.Model
{
    using System;
    using ModPlusAPI;

    /// <summary>
    /// Тип объекта Renga с локализованным именем для отображения
    /// </summary>
    public class LocalizableObjectType
    {
        private const string LangItem = "mrSearchBy";

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalizableObjectType"/> class.
        /// </summary>
        /// <param name="objectType"><see cref="Guid"/> типа объекта Renga. Guid.Empty - для типа "Все"</param>
        public LocalizableObjectType(Guid objectType)
        {
            DisplayName = objectType == Guid.Empty 
                ? Language.GetItem(LangItem, "all")
                : ModPlus.Helpers.Localization.RengaObjectType(objectType);
            ObjectType = objectType;
        }

        /// <summary>
        /// Отображаемое имя
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Тип объекта Renga. Guid.Empty соответствует значению "Все"
        /// </summary>
        public Guid ObjectType { get; }
    }
}
