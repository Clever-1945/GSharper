using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GSharper.Enums
{
    public enum SymbolEqualResult
    {
        None,

        /// <summary>
        /// Совпадение по подсловам, но без учета регистра
        /// </summary>
        SubWordIgnireCase,
        /// <summary>
        /// Совпадение по подсловам и с учетом регистра
        /// </summary>
        SubWordWithCase,

        /// <summary>
        /// Частичное совпадение но без учета регистра
        /// </summary>
        SubEqualIgnireCase,
        /// <summary>
        /// Частичное совпадение по слову
        /// </summary>
        SubEqualWithCase,

        /// <summary>
        /// Полное совпадение но без учета регистра
        /// </summary>
        FullEqualIgnireCase,
        /// <summary>
        /// Полное совпадение по слову
        /// </summary>
        FullEqualWithCase,
    }
}
