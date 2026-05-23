using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;

namespace GSharper.Extensions
{
    public static class IPropertySymbolExtensions
    {
        /// <summary> Получить все базовые типы текущего символа </summary>
        /// <param name="propertySymbol"></param>
        /// <returns></returns>
        public static IEnumerable<IPropertySymbol> GetBaseSymbols(this IPropertySymbol propertySymbol)
        {
            foreach (var baseSymbol in propertySymbol.ContainingType.GetBaseSymbols())
            {
                foreach (var member in baseSymbol.GetMembers())
                {
                    if (member is IPropertySymbol property)
                    {
                        if (IsPropertyImplementingInterface(propertySymbol, property))
                        {
                            yield return property;
                        }
                        else if (IsPropertyOverridingBase(propertySymbol, property))
                        {
                            yield return property;
                        }
                    }
                }
            }
        }

        /// <summary> Сопоставить 2 функции из интерфейса и из реализации </summary>
        /// <param name="classProperty"></param>
        /// <param name="interfaceProperty"></param>
        /// <returns></returns>
        private static bool IsPropertyImplementingInterface(IPropertySymbol classProperty, IPropertySymbol interfaceProperty)
        {
            if (SymbolEqualityComparer.Default.Equals(classProperty.OriginalDefinition, interfaceProperty.OriginalDefinition))
            {
                return true;
            }
            INamedTypeSymbol classType = classProperty.ContainingType;

            ISymbol implementation = classType.FindImplementationForInterfaceMember(interfaceProperty.OriginalDefinition);
            return SymbolEqualityComparer.Default.Equals(classProperty.OriginalDefinition, implementation?.OriginalDefinition);
        }

        private static bool IsPropertyOverridingBase(IPropertySymbol derivedProperty, IPropertySymbol baseProperty)
        {
            var currentBase = derivedProperty.OriginalDefinition;
            var targetBase = baseProperty.OriginalDefinition;
            while (currentBase != null)
            {
                if (SymbolEqualityComparer.Default.Equals(currentBase, targetBase))
                {
                    return true;
                }
                currentBase = currentBase.OverriddenProperty?.OriginalDefinition;
            }

            return false;
        }

        /// <summary> Найти все имплементации символа </summary>
        /// <param name="propertySymbol"></param>
        /// <param name="isExternal"></param>
        /// <returns></returns>
        public static IEnumerable<IPropertySymbol> GetImplementations(this IPropertySymbol propertySymbol, bool isExternal)
        {
            var isInterface = propertySymbol.ContainingType.TypeKind == TypeKind.Interface;
            foreach (var implementation in propertySymbol.ContainingType.GetImplementations(isExternal))
            {
                if (isInterface)
                {
                    foreach (var member in implementation.GetMembers())
                    {
                        if (member is IPropertySymbol property)
                        {
                            if (IsPropertyImplementingInterface(property, propertySymbol))
                            {
                                yield return property;
                                break;
                            }
                        }
                    }
                }
                else
                {
                    foreach (var member in implementation.GetMembers())
                    {
                        if (member is IPropertySymbol property)
                        {
                            if (IsPropertyOverridingBase(property, propertySymbol))
                            {
                                yield return property;
                                break;
                            }
                        }
                    }
                }
            }
        }

        /// <summary> Найти все имплементации символа </summary>
        /// <param name="propertySymbol"></param>
        /// <returns></returns>
        public static IEnumerable<IPropertySymbol> GetImplementations(this IPropertySymbol propertySymbol)
        {
            return propertySymbol.GetImplementations(false).Concat(propertySymbol.GetImplementations(true));
        }
    }
}
