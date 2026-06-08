using GSharper.Collections;
using GSharper.Enums;
using GSharper.Models;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GSharper.Extensions
{
    public static class VisualStudioWorkspaceExtensions
    {
        private static ThreadDictionary<MetadataReference, SymbolProjectDefinition[]> _symbols = new ThreadDictionary<MetadataReference, SymbolProjectDefinition[]>();
        private static ThreadDictionary<MetadataReference, SymbolProjectDefinition[]> _symbolsITypeSymbol = new ThreadDictionary<MetadataReference, SymbolProjectDefinition[]>();
        private static ThreadDictionary<MetadataReference, SymbolProjectDefinition[]> _symbolsIEventSymbol = new ThreadDictionary<MetadataReference, SymbolProjectDefinition[]>();
        private static ThreadDictionary<MetadataReference, SymbolProjectDefinition[]> _symbolsIPropertySymbol = new ThreadDictionary<MetadataReference, SymbolProjectDefinition[]>();
        private static ThreadDictionary<MetadataReference, SymbolProjectDefinition[]> _symbolsIFieldSymbol = new ThreadDictionary<MetadataReference, SymbolProjectDefinition[]>();
        private static ThreadDictionary<MetadataReference, SymbolProjectDefinition[]> _symbolsIParameterSymbol = new ThreadDictionary<MetadataReference, SymbolProjectDefinition[]>();
        private static ThreadDictionary<MetadataReference, SymbolProjectDefinition[]> _symbolsIMethodSymbol = new ThreadDictionary<MetadataReference, SymbolProjectDefinition[]>();
        private static ThreadDictionary<MetadataReference, SymbolProjectDefinition[]> _symbolsILocalSymbol = new ThreadDictionary<MetadataReference, SymbolProjectDefinition[]>();

        /// <summary>
        /// Базовый метод для поиска символов
        /// </summary>
        /// <param name="_workspace"></param>
        /// <param name="isExternal">Искать внутри или во внешних библиотеках</param>
        /// <returns></returns>
        public static IEnumerable<SymbolProjectDefinition> GetSymbols(this VisualStudioWorkspace _workspace, bool isExternal, SymbolTypeInterface symbolType = SymbolTypeInterface.None)
        {            
            var _cancellationTokenSource = new CancellationTokenSource();
            try
            {
                var solution = _workspace.CurrentSolution;
                foreach (var project in solution.Projects)
                {
                    if (project.TryGetCompilation(out var compilation))
                    {
                        if (!isExternal)
                        {
                            var results = compilation.GetSymbolsWithName(name => true, SymbolFilter.All, _cancellationTokenSource.Token);
                            foreach (var result in results)
                            {
                                yield return new SymbolProjectDefinition(result, project, null, compilation);
                            }
                        }
                        else
                        {
                            foreach (var reference in compilation.References)
                            {
                                foreach (var symbol in _workspace.GetSymbolsFromReference(reference, project, compilation, symbolType))
                                {
                                    yield return symbol;
                                }
                            }
                        }
                    }
                }
            }
            finally
            {
                _cancellationTokenSource.Cancel();
            }
        }

        private static SymbolProjectDefinition[] GetSymbolsFromReference(this VisualStudioWorkspace _workspace, MetadataReference reference, Microsoft.CodeAnalysis.Project project, Compilation compilation, SymbolTypeInterface symbolType)
        {
            if (symbolType == SymbolTypeInterface.ITypeSymbol)
            {
                return _symbolsITypeSymbol.GetOrAdd(reference, (r) =>
                {
                    return _workspace.GetSymbolsFromReferenceDefault(r, project, compilation).Where(x => x.Symbol is ITypeSymbol).ToArray();
                });
            }

            if (symbolType == SymbolTypeInterface.IEventSymbol)
            {
                return _symbolsIEventSymbol.GetOrAdd(reference, (r) =>
                {
                    return _workspace.GetSymbolsFromReferenceDefault(r, project, compilation).Where(x => x.Symbol is IEventSymbol).ToArray();
                });
            }

            if (symbolType == SymbolTypeInterface.IPropertySymbol)
            {
                return _symbolsIPropertySymbol.GetOrAdd(reference, (r) =>
                {
                    return _workspace.GetSymbolsFromReferenceDefault(r, project, compilation).Where(x => x.Symbol is IPropertySymbol).ToArray();
                });
            }

            if (symbolType == SymbolTypeInterface.IFieldSymbol)
            {
                return _symbolsIFieldSymbol.GetOrAdd(reference, (r) =>
                {
                    return _workspace.GetSymbolsFromReferenceDefault(r, project, compilation).Where(x => x.Symbol is IFieldSymbol).ToArray();
                });
            }

            if (symbolType == SymbolTypeInterface.IParameterSymbol)
            {
                return _symbolsIParameterSymbol.GetOrAdd(reference, (r) =>
                {
                    return _workspace.GetSymbolsFromReferenceDefault(r, project, compilation).Where(x => x.Symbol is IParameterSymbol).ToArray();
                });
            }

            if (symbolType == SymbolTypeInterface.ILocalSymbol)
            {
                return _symbolsILocalSymbol.GetOrAdd(reference, (r) =>
                {
                    return _workspace.GetSymbolsFromReferenceDefault(r, project, compilation).Where(x => x.Symbol is ILocalSymbol).ToArray();
                });
            }

            if (symbolType == SymbolTypeInterface.IMethodSymbol)
            {
                return _symbolsIMethodSymbol.GetOrAdd(reference, (r) =>
                {
                    return _workspace.GetSymbolsFromReferenceDefault(r, project, compilation).Where(x => x.Symbol is IMethodSymbol).ToArray();
                });
            }

            return _workspace.GetSymbolsFromReferenceDefault(reference, project, compilation);
        }

        private static SymbolProjectDefinition[] GetSymbolsFromReferenceDefault(this VisualStudioWorkspace _workspace, MetadataReference reference, Microsoft.CodeAnalysis.Project project, Compilation compilation)
        {
            var symbols = _symbols.GetOrAdd(reference, (r) =>
            {
                var list = new List<SymbolProjectDefinition>();
                var assemblySymbol = compilation.GetAssemblyOrModuleSymbol(reference) as IAssemblySymbol;
                if (assemblySymbol != null)
                {
                    var globalNamespace = assemblySymbol.GlobalNamespace;
                    foreach (var symbol in ForeachSymbol(globalNamespace))
                    {
                        list.Add(new SymbolProjectDefinition(symbol, project, reference, compilation));
                    }
                }

                return list.ToArray();
            });


            return symbols;
        }

        /// <summary>
        /// Базовый метод для поиска символов
        /// </summary>
        /// <param name="_workspace"></param>
        /// <returns></returns>
        public static IEnumerable<SymbolProjectDefinition> GetSymbols(this VisualStudioWorkspace _workspace)
        {
            return _workspace.GetSymbols(false).Concat(_workspace.GetSymbols(true));
        }

        private static IEnumerable<ISymbol> ForeachSymbol(INamespaceSymbol namespaceSymbol)
        {
            if (namespaceSymbol != null)
            {
                foreach (var type in namespaceSymbol.GetTypeMembers())
                {
                    yield return type;

                    foreach (var method in type.GetMembers())
                    {
                        yield return method;
                    }
                }

                foreach (var nestedNamespace in namespaceSymbol.GetNamespaceMembers())
                {
                    foreach(var symbol in ForeachSymbol(nestedNamespace))
                    {
                        yield return symbol;
                    }
                }
            }
        }

        public static ISymbol SearchTypeByName(this VisualStudioWorkspace _workspace, string namespaceAndName)
        {
            ISymbol foundSymbol = _workspace.GetSymbols().FirstOrDefault(x =>
            {
                if (x.Symbol is ITypeSymbol _typeSymbol)
                {
                    if ($"{_typeSymbol.ContainingNamespace.Name}.{_typeSymbol.Name}" == namespaceAndName)
                    {
                        return true;
                    }
                }
                return false;
            })?.Symbol;

            return foundSymbol;
        }

        public static async Task<SymbolModel[]> SearchTypes(this VisualStudioWorkspace _workspace, bool isExternal, CancellationToken cancellationToken)
        {
            Func<ISymbol, SymbolModel> converter = (symbol) =>
            {
                TypeKind typeKind = TypeKind.Unknown;
                if (symbol is INamedTypeSymbol namedType)
                {
                    typeKind = namedType.TypeKind;
                }

                if(typeKind == TypeKind.Class || typeKind == TypeKind.Struct || typeKind == TypeKind.Enum || typeKind == TypeKind.Interface)
                {
                    return new SymbolModel(symbol);
                }

                return null;
            };

            return await _workspace.SearchSymbols(isExternal, converter, cancellationToken);
        }

        public static async Task<SymbolModel[]> SearchMethods(this VisualStudioWorkspace _workspace, bool isExternal, CancellationToken cancellationToken)
        {
            Func<ISymbol, SymbolModel> converter = (s) => 
            { 
                if(s is IMethodSymbol methodSymbol)
                {
                    if (methodSymbol.MethodKind == MethodKind.LocalFunction || methodSymbol.MethodKind == MethodKind.Ordinary)
                    {
                        return new SymbolModel(s);
                    }
                }

                return null;
            };

            return await _workspace.SearchSymbols(isExternal, converter, cancellationToken);
        }

        public static async Task<SymbolModel[]> SearchSymbols(this VisualStudioWorkspace _workspace, bool isExternal, Func<ISymbol, SymbolModel> converter, CancellationToken cancellationToken)
        {
            return _workspace.GetSymbols(isExternal).Select(x => converter(x.Symbol)).Where(x => x != null).ToArray();
        }
    }
}
