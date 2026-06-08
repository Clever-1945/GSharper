using Microsoft.CodeAnalysis;
using GSharper.Models;
using System.Collections.Generic;
using System.Linq;
using GSharper.Assistants;

namespace GSharper.Extensions
{
    public static class ITypeSymbolExtensions
    {
        /// <summary> Получить все базовые типы текущего символа </summary>
        /// <param name="typeSymbol"></param>
        /// <returns></returns>
        public static IEnumerable<ITypeSymbol> GetBaseSymbols(this ITypeSymbol typeSymbol)
        {
            var baseType = typeSymbol;
            do
            {
                if (baseType?.Interfaces != null)
                {
                    foreach (var _interface in baseType.Interfaces)
                    {
                        yield return _interface;
                    }
                }

                baseType = baseType?.BaseType as ITypeSymbol;
                if (baseType != null)
                {
                    yield return baseType;
                }
            } while (baseType != null);
        }

        /// <summary> Найти все имплементации символа </summary>
        /// <param name="typeSymbol"></param>
        /// <param name="inSolution"></param>
        /// <returns></returns>
        public static IEnumerable<ITypeSymbol> GetImplementations(this ITypeSymbol typeSymbol, bool isExternal)
        {
            foreach(var s in Assistant.GetWorkspace().GetSymbols(isExternal, Enums.SymbolTypeInterface.ITypeSymbol))
            {
                if (s.Symbol is ITypeSymbol _typeSymbol)
                {
                    if (_typeSymbol.GetBaseSymbols().Any(x => x.IsEqualTo(typeSymbol)))
                    {
                        yield return _typeSymbol;
                    }
                }
            }
        }

        /// <summary> Найти все имплементации символа </summary>
        /// <param name="typeSymbol"></param>
        /// <param name="inSolution"></param>
        /// <returns></returns>
        public static IEnumerable<ITypeSymbol> GetImplementations(this ITypeSymbol typeSymbol)
        {
            return typeSymbol.GetImplementations(false).Concat(typeSymbol.GetImplementations(true));
        }

        /// <summary>
        /// Проверяет, подходит ли метод расширения для вызова у указанного типа.
        /// </summary>
        /// <param name="methodSymbol">Метод, который мы проверяем.</param>
        /// <param name="receiverType">Тип, у которого мы хотим вызвать этот метод.</param>
        /// <returns>True, если метод расширения подходит для данного типа.</returns>
        public static bool IsExtensionMethod(this ITypeSymbol receiverType, IMethodSymbol methodSymbol)
        {
            if (methodSymbol == null || receiverType == null)
                return false;

            if (!methodSymbol.IsExtensionMethod)
                return false;

            if (methodSymbol.Parameters.Length < 1)
                return false;

            try
            {
                IMethodSymbol reducedMethod = methodSymbol.ReduceExtensionMethod(receiverType);
                return reducedMethod != null;
            }
            catch
            {
                return false;
            }
        }
    }
}
