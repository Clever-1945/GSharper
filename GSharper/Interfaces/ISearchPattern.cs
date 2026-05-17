using GSharper.Enums;
using GSharper.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GSharper.Interfaces
{
    /// <summary> Паттерн поиска символов </summary>
    public interface ISearchPattern
    {
        /// <summary> Удовлетворяет ли символ поискомому паттерну и вернуть вес символа </summary>
        /// <param name="symbolModel"></param>
        /// <returns></returns>
        int IsEquals(SymbolModel symbolModel);

        /// <summary>
        /// Установить текст для поиска
        /// </summary>
        /// <param name="text"></param>
        void SetSearchText(string text);
    }
}
