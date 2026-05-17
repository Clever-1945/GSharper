using GSharper.Enums;
using GSharper.Interfaces;
using GSharper.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GSharper.Helpers
{
    public class SearchSymbolPattern: ISearchPattern
    {
        private string _search;
        private bool _isEmptySearch;

        /// <summary>
        /// Установить текст для поиска
        /// </summary>
        /// <param name="text"></param>
        public void SetSearchText(string text)
        {
            _search = text;
            _isEmptySearch = String.IsNullOrWhiteSpace(text);
        }

        /// <summary>
        /// Текст по патерну равен искомой строке с фильтром?
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public SymbolEqualResult IsEquals(string text)
        {
            if (_isEmptySearch)
                return SymbolEqualResult.None;

            if(String.IsNullOrWhiteSpace(text))
                return SymbolEqualResult.None;

            if(_search == text)
                return SymbolEqualResult.FullEqualWithCase;

            if (String.Equals(text, _search, StringComparison.OrdinalIgnoreCase))
                return SymbolEqualResult.FullEqualIgnireCase;

            if (text.Contains(_search))
                return SymbolEqualResult.SubEqualWithCase;

            if (text.Contains(_search, StringComparison.OrdinalIgnoreCase))
                return SymbolEqualResult.SubEqualIgnireCase;

            var words = GetWords(_search).ToArray();

            int indexPrevious = 0;
            bool isBreak = false;
            foreach (string word in words)
            {
                var index = text.IndexOf(word, indexPrevious);
                if (index < 0)
                {
                    isBreak = true;
                    break;
                }

                indexPrevious = index;
            }
            if (!isBreak)
            {
                return SymbolEqualResult.SubWordWithCase;
            }

            indexPrevious = 0;
            foreach (string word in words)
            {
                var index = text.IndexOf(word, indexPrevious, StringComparison.OrdinalIgnoreCase);
                if (index < 0)
                    return SymbolEqualResult.None;

                indexPrevious = index;
            }

            return SymbolEqualResult.SubWordIgnireCase;
        }

        private static IEnumerable<string> GetWords(string text)
        {
            var builder = new StringBuilder();
            for (int i = 0; i < text.Length; i++)
            {
                var ch = text[i];
                if (char.IsUpper(ch) || char.IsWhiteSpace(ch))
                {
                    var word = builder.ToString().Trim();
                    if (!String.IsNullOrEmpty(word))
                    {
                        yield return word;
                    }
                    builder.Clear();
                }

                builder.Append(ch);
            }

            var lastWord = builder.ToString().Trim();
            if (!String.IsNullOrEmpty(lastWord))
            {
                yield return lastWord;
            }
        }

        /// <summary> Удовлетворяет ли символ поискомому паттерну и вернуть вес символа </summary>
        /// <param name="symbolModel"></param>
        /// <returns></returns>
        public int IsEquals(SymbolModel symbolModel)
        {
            if (_isEmptySearch)
                return 0;

            string text = symbolModel.Symbol?.Name;
            if (String.IsNullOrWhiteSpace(text))
                return -1;

            var result = IsEquals(text);
            return result == SymbolEqualResult.None ? -1 : (int)result;
        }
    }
}
