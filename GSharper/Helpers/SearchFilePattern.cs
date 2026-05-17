using GSharper.Enums;
using GSharper.Interfaces;
using GSharper.Models;
using System;
using System.Linq;

namespace GSharper.Helpers
{
    public class SearchFilePattern: ISearchPattern
    {
        private bool _isEmptySearch;
        private string[] _segments;

        /// <summary>
        /// Установить текст для поиска
        /// </summary>
        /// <param name="text"></param>
        public void SetSearchText(string text)
        {
            _isEmptySearch = String.IsNullOrWhiteSpace(text);
            if (!_isEmptySearch)
            {
                _segments = text
                    .Split(new char[] { '\\', '/' })
                    .Select(x => x.Trim())
                    .Where(x => !String.IsNullOrWhiteSpace(x))
                    .ToArray();

                if (_segments.Length < 1)
                {
                    _isEmptySearch = true;
                }
            }
        }

        /// <summary> Удовлетворяет ли символ поискомому паттерну и вернуть вес символа </summary>
        /// <param name="symbolModel"></param>
        /// <returns></returns>
        public int IsEquals(SymbolModel symbolModel)
        {
            if (_isEmptySearch)
                return 0;
            string fullName = symbolModel.GetProjectFile()?.FullName;

            if (String.IsNullOrWhiteSpace(fullName))
                return -1;

            var result = IsEquals(fullName);
            if (result.Length < 1)
                return -1;

            int weight = 0;
            for (int i = 0; i < result.Length; i++)
            {
                weight += (int)result[i];
            }

            return weight;
        }

        public SymbolEqualResult[] IsEquals(string fullName)
        {
            if (_isEmptySearch)
                return Array.Empty<SymbolEqualResult>();

            if (String.IsNullOrWhiteSpace(fullName))
                return Array.Empty<SymbolEqualResult>();

            var fullNameSegments = fullName
                    .Split(new char[] { '\\', '/' })
                    .Select(x => x.Trim())
                    .Where(x => !String.IsNullOrWhiteSpace(x))
                    .ToArray();

            if (fullNameSegments.Length < _segments.Length)
                return Array.Empty<SymbolEqualResult>();

            var results = new SymbolEqualResult[_segments.Length];
            for (int i = 0; i < _segments.Length; i++)
            {
                var segment = _segments[_segments.Length - i - 1];
                var fullNameSegment = fullNameSegments[fullNameSegments.Length - i - 1];

                var symbolPattern = new SearchSymbolPattern();
                symbolPattern.SetSearchText(segment);
                var result = symbolPattern.IsEquals(fullNameSegment);
                if (result == SymbolEqualResult.None)
                    return Array.Empty<SymbolEqualResult>();

                results[i] = result;
            }

            return results;
        }
    }
}
