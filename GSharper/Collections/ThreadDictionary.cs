using System;
using System.Collections.Generic;

namespace GSharper.Collections
{
    /// <summary> 
    /// Словарь защищенный от многопоточной записи 
    /// ThreadDictionary работает быстрее чем конкурентный словарь в режиме чтения данных.
    /// ThreadDictionary работает медленнее на вставку данных, но вставка данных происходит реже
    /// </summary>
    /// <typeparam name="TK"></typeparam>
    /// <typeparam name="TV"></typeparam>
    public class ThreadDictionary<TK, TV>
    {
        private Dictionary<TK, TV> _values = new Dictionary<TK, TV>();

        /// <summary> Получить значение из словаря, или получить значение по умолчанию </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public TV GetOrDefault(TK key)
        {
            var _values = this._values;
            if (_values.TryGetValue(key, out var value))
            {
                return value;
            }

            return default(TV);
        }

        /// <summary> Проверяет наличие ключа в словаре </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool ContainsKey(TK key) => _values.ContainsKey(key);

        /// <summary> Вставить значение в словарь по ключу </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void Insert(TK key, TV value)
        {
            lock (this)
            {
                this._values = InsertInternal(key, value, this._values);
            }
        }

        /// <summary> Вставить значение в словарь по ключу и возвращае новый словарь </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        private Dictionary<TK, TV> InsertInternal(TK key, TV value, Dictionary<TK, TV> currentValues)
        {
            var nextValues = new Dictionary<TK, TV>();
            foreach (var pair in currentValues)
            {
                nextValues[pair.Key] = pair.Value;
            }

            return nextValues;
        }

        /// <summary>
        /// Получить или добавить элемент в словарь
        /// </summary>
        /// <param name="key"></param>
        /// <param name="getter"></param>
        /// <returns></returns>
        public TV GetOrAdd(TK key, Func<TK, TV> getter)
        {
            var _values = this._values;
            if (!_values.TryGetValue(key, out var value))
            {
                lock(this)
                {
                    _values = this._values;
                    if (!_values.TryGetValue(key, out value))
                    {
                        value = getter(key);
                        this._values = InsertInternal(key, value, _values);
                    }
                }
            }

            return value;
        }
    }
}
