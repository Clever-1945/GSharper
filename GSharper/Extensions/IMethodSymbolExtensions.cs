using GSharper.Assistants;
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
        /// <param name="methodSymbol"></param>
        /// <param name="isExternal"></param>
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
        /// <param name="methodSymbol"></param>
        /// <returns></returns>
        public static IEnumerable<IMethodSymbol> GetImplementations(this IMethodSymbol methodSymbol)
        {
            return methodSymbol.GetImplementations(false).Concat(methodSymbol.GetImplementations(true));
        }

        /// <summary> Вернуть тип, которму принадлежит функция. Если функция считается ресширением, то вернуть тип расширения </summary>
        /// <param name="method"></param>
        /// <returns></returns>
        public static ITypeSymbol GetTargetType(this IMethodSymbol method)
        {
            if (method.IsExtensionMethod && method.Parameters.Length > 0)
            {
                return method.Parameters[0].Type;
            }
            return method.ContainingType;
        }

        /// <summary> Сравнивает две функции и вернуть флаг: две функции считаются перегрузкой ? </summary>
        /// <param name="method1"></param>
        /// <param name="method2"></param>
        /// <returns></returns>
        private static bool IsMethodsOverloadsWithExtensions(IMethodSymbol method1, IMethodSymbol method2)
        {
            if (method1.Name != method2.Name)
            {
                return false;
            }

            if (SymbolEqualityComparer.Default.Equals(method1, method2))
            {
                return false;
            }

            ITypeSymbol targetType1 = GetTargetType(method1);
            ITypeSymbol targetType2 = GetTargetType(method2);

            if (!SymbolEqualityComparer.Default.Equals(targetType1, targetType2))
            {
                return false;
            }

            return true;
        }

        /// <summary> Вернуть все методы, которые считаются перегрузками </summary>
        /// <param name="methodSymbol"></param>
        /// <returns></returns>
        public static IEnumerable<IMethodSymbol> GetOverloadingMethods(this IMethodSymbol methodSymbol, bool isExternal)
        {
            foreach(var symbol in Assistant.GetWorkspace().GetSymbols(isExternal))
            {
                if (symbol.Symbol is IMethodSymbol checkMethodSymbol)
                {
                    if (IsMethodsOverloadsWithExtensions(checkMethodSymbol, methodSymbol))
                    {
                        yield return checkMethodSymbol;
                    }
                }
            }
        }

        /// <summary> Вернуть все методы, которые считаются перегрузками </summary>
        /// <param name="methodSymbol"></param>
        /// <returns></returns>
        public static IEnumerable<IMethodSymbol> GetOverloadingMethods(this IMethodSymbol methodSymbol)
        {
            return methodSymbol.GetOverloadingMethods(false).Concat(methodSymbol.GetOverloadingMethods(true));
        }
    }
}
