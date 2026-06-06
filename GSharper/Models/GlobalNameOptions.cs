namespace GSharper.Models
{
    public enum GlobalNameOptions
    {
        Default,
        /// <summary>
        /// Выдавать T если если тип это абстрактный генерик тип
        /// </summary>
        AliasIfGenericParameter,

        /// <summary>
        /// Выдавать T если если тип это любой генерик тип
        /// </summary>
        AliasIfGenericType,
    }
}
