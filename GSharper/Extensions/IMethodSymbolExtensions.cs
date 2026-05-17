using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GSharper.Extensions
{
    public static class IMethodSymbolExtensions
    {
        /// <summary> Получить все базовые типы текущего символа </summary>
        /// <param name="typeSymbol"></param>
        /// <returns></returns>
        public static IEnumerable<IMethodSymbol> GetBaseSymbols(this IMethodSymbol methodSymbol)
        {
            foreach(var baseSymbol in methodSymbol.ContainingType.GetBaseSymbols())
            {
                foreach(var member in baseSymbol.GetMembers())
                {
                    if (member is IMethodSymbol method)
                    {
                        if (IsMethodImplementingInterface(methodSymbol, method))
                        {
                            yield return method;
                        }
                        else if (IsMethodOverridingBase(methodSymbol, method))
                        {
                            yield return method;
                        }
                    }
                }
            }
        }

        /// <summary> Сопоставить 2 функции из интерфейса и из реализации </summary>
        /// <param name="classMethod"></param>
        /// <param name="interfaceMethod"></param>
        /// <returns></returns>
        private static bool IsMethodImplementingInterface(IMethodSymbol classMethod, IMethodSymbol interfaceMethod)
        {
            if (SymbolEqualityComparer.Default.Equals(classMethod.OriginalDefinition, interfaceMethod.OriginalDefinition))
            {
                return true;
            }
            INamedTypeSymbol classType = classMethod.ContainingType;

            ISymbol implementation = classType.FindImplementationForInterfaceMember(interfaceMethod.OriginalDefinition);
            return SymbolEqualityComparer.Default.Equals(classMethod.OriginalDefinition, implementation?.OriginalDefinition);
        }

        private static bool IsMethodOverridingBase(IMethodSymbol derivedMethod, IMethodSymbol baseMethod)
        {
            var currentBase = derivedMethod.OriginalDefinition;
            var targetBase = baseMethod.OriginalDefinition;
            while (currentBase != null)
            {
                if (SymbolEqualityComparer.Default.Equals(currentBase, targetBase))
                {
                    return true;
                }
                currentBase = currentBase.OverriddenMethod?.OriginalDefinition;
            }

            return false;
        }

        /// <summary> Найти все имплементации символа </summary>
        /// <param name="typeSymbol"></param>
        /// <param name="inSolution"></param>
        /// <returns></returns>
        public static IEnumerable<IMethodSymbol> GetImplementations(this IMethodSymbol methodSymbol, bool isExternal)
        {
            var isInterface = methodSymbol.ContainingType.TypeKind == TypeKind.Interface;
            foreach (var implementation in methodSymbol.ContainingType.GetImplementations(isExternal))
            {
                if (isInterface)
                {
                    foreach (var member in implementation.GetMembers())
                    {
                        if (member is IMethodSymbol method)
                        {
                            if (IsMethodImplementingInterface(method, methodSymbol))
                            {
                                yield return method;
                                break;
                            }
                        }
                    }
                }
                else
                {
                    foreach (var member in implementation.GetMembers())
                    {
                        if (member is IMethodSymbol method)
                        {
                            if (IsMethodOverridingBase(method, methodSymbol))
                            {
                                yield return method;
                                break;
                            }
                        }
                    }
                }
            }
        }

        /// <summary> Найти все имплементации символа </summary>
        /// <param name="typeSymbol"></param>
        /// <param name="inSolution"></param>
        /// <returns></returns>
        public static IEnumerable<IMethodSymbol> GetImplementations(this IMethodSymbol methodSymbol)
        {
            return methodSymbol.GetImplementations(false).Concat(methodSymbol.GetImplementations(true));
        }
    }
}
