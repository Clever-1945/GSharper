using Microsoft.CodeAnalysis;
using GSharper.Dialogs;
using GSharper.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;

namespace GSharper.Extensions
{
    public static class ISymbolExtensions
    {
        public static string GetResourceForName(this ISymbol symbol)
        {
            INamedTypeSymbol namedTypeSymbol = symbol as INamedTypeSymbol;
            if (namedTypeSymbol != null)
            {
                if (namedTypeSymbol.TypeKind == TypeKind.Class)
                {
                    return "GSharper.Resources.Class.png";
                }
                else if (namedTypeSymbol.TypeKind == TypeKind.Interface)
                {
                    return "GSharper.Resources.Interface.png";
                }
                else if (namedTypeSymbol.TypeKind == TypeKind.Struct)
                {
                    return "GSharper.Resources.Structure.png";
                }
                else if (namedTypeSymbol.TypeKind == TypeKind.Enum)
                {
                    return "GSharper.Resources.Enumeration.png";
                }
            }

            IMethodSymbol methodSymbol = symbol as IMethodSymbol;
            if (methodSymbol != null)
            {
                return "GSharper.Resources.Method.png";
            }

            if (symbol is ILocalSymbol)
            {
                return "GSharper.Resources.LocalVariable.png";
            }

            if (symbol is IPropertySymbol)
            {
                return "GSharper.Resources.OverlayProperty.png";
            }

            if(symbol is IParameterSymbol)
            {
                return "GSharper.Resources.FieldSnippet.png";
            }

            return null;
        }

        public static bool IsKeyword(this ISymbol symbol)
        {
            var typeSymbol = (symbol as ITypeSymbol);
            return typeSymbol != null && typeSymbol.SpecialType != SpecialType.None;
        }

        public static IEnumerable<Inline> CreateInline(this ISymbol symbol, bool createLink = false)
        {
            var _typeSymbol = symbol as ITypeSymbol;
            var _methodSymbol = symbol as IMethodSymbol;
            var _parameterSymbol = symbol as IParameterSymbol;
            var _propertySymbol = symbol as IPropertySymbol;
            var _fieldSymbol = symbol as IFieldSymbol;
            var _localSymbol = symbol as ILocalSymbol;

            if (_typeSymbol != null)
            {
                return _typeSymbol.CreateInline(createLink: createLink);
            }
            else if (_methodSymbol != null)
            {
                return _methodSymbol.CreateInline(createLink: createLink);
            }
            else if (_parameterSymbol != null)
            {
                return _parameterSymbol.CreateInline(createLink: createLink);
            }
            else if (_propertySymbol != null)
            {
                return _propertySymbol.CreateInline(createLink: createLink);
            }
            else if (_fieldSymbol != null)
            {
                return _fieldSymbol.CreateInline(createLink: createLink);
            }
            else if (_localSymbol != null)
            {
                return _localSymbol.CreateInline(createLink: createLink);
            }
            else
            {
                if (symbol != null)
                {
                    return new Inline[]
                    {
                        new Run("Неизвестный тип: " + symbol?.Name)
                        {
                            FontWeight = FontWeights.Bold,
                            Foreground = Assistant.TextFormatting.IdentifierProperties.ForegroundBrush
                        }
                    };
                }

                return Array.Empty<Inline>();
            }
        }

        public static IEnumerable<Inline> CreateInline(this ILocalSymbol localSymbol, bool createLink = false)
        {
            if (localSymbol == null)
                yield break;

            foreach (var element in localSymbol.Type.CreateInline(setNameSpace: false, createLink: createLink))
            {
                yield return element;
            }

            yield return new Run(" ");

            foreach (var element in localSymbol.ContainingType.CreateInline(setNameSpace: false, createLink: createLink))
            {
                yield return element;
            }

            yield return new Run("." + localSymbol.Name)
            {
                FontWeight = FontWeights.Bold,
                Foreground = Assistant.TextFormatting.LocalProperties.ForegroundBrush
            }.CreateLink(localSymbol, createLink);
        }

        public static IEnumerable<Inline> CreateInline(this IFieldSymbol fieldSymbol, bool createLink = false)
        {
            if (fieldSymbol == null)
                yield break;

            foreach (var element in fieldSymbol.Type.CreateInline(setNameSpace: false, createLink: createLink))
            {
                yield return element;
            }

            yield return new Run(" ");

            foreach (var element in fieldSymbol.ContainingType.CreateInline(setNameSpace: false, createLink: createLink))
            {
                yield return element;
            }

            yield return new Run("." + fieldSymbol.Name)
            {
                FontWeight = FontWeights.Bold,
                Foreground = Assistant.TextFormatting.FieldProperties.ForegroundBrush
            }.CreateLink(fieldSymbol, createLink);
        }

        public static IEnumerable<Inline> CreateInline(this IPropertySymbol propertySymbol, bool createLink = false)
        {
            if (propertySymbol == null)
                yield break;

            foreach (var element in propertySymbol.Type.CreateInline(setNameSpace: false, createLink: createLink))
            {
                yield return element;
            }

            yield return new Run(" ");

            foreach (var element in propertySymbol.ContainingType.CreateInline(setNameSpace: false, createLink: createLink))
            {
                yield return element;
            }

            yield return new Run("." + propertySymbol.Name)
            {
                FontWeight = FontWeights.Bold,
                Foreground = Assistant.TextFormatting.PropertyProperties.ForegroundBrush
            }.CreateLink(propertySymbol, createLink);
        }

        public static IEnumerable<Inline> CreateInline(this IParameterSymbol parameterSymbol, bool createLink = false)
        {
            if (parameterSymbol == null)
                yield break;

            foreach (var element in parameterSymbol.Type.CreateInline(setNameSpace: false, createLink: createLink))
            {
                yield return element;
            }

            yield return new Run(" ");

            foreach (var element in parameterSymbol.ContainingType.CreateInline(setNameSpace: false, createLink: createLink))
            {
                yield return element;
            }

            yield return new Run("." + parameterSymbol.Name)
            {
                FontWeight = FontWeights.Bold,
                Foreground = Assistant.TextFormatting.ParameterProperties.ForegroundBrush
            }.CreateLink(parameterSymbol, createLink);
        }

        private static IEnumerable<Inline> CreateGenericInline(this ITypeSymbol typeSymbol, bool setNameSpace = true, bool createLink = false)
        {
            var typeArguments = (typeSymbol as INamedTypeSymbol)?.TypeArguments.Cast<ITypeSymbol>().ToArray() ?? Array.Empty<ITypeSymbol>();

            if (typeArguments.Length > 0)
            {
                yield return new Run("<")
                {
                    FontWeight = FontWeights.Bold,
                    Foreground = Assistant.TextFormatting.IdentifierProperties.ForegroundBrush
                };

                for (int i = 0; i < typeArguments.Length; i++)
                {
                    var typeArgument = typeArguments[i];
                    foreach (var elemet in typeArgument.CreateInline(setNameSpace: setNameSpace, createLink: createLink))
                    {
                        yield return elemet;
                    }

                    if (i < (typeArguments.Length - 1))
                    {
                        yield return new Run(", ")
                        {
                            FontWeight = FontWeights.Bold,
                            Foreground = Assistant.TextFormatting.IdentifierProperties.ForegroundBrush
                        };
                    }
                }

                yield return new Run(">")
                {
                    FontWeight = FontWeights.Bold,
                    Foreground = Assistant.TextFormatting.IdentifierProperties.ForegroundBrush
                };
            }
        }

        public static IEnumerable<Inline> CreateInline(this ITypeSymbol typeSymbol, bool setNameSpace = true, bool createLink = false)
        {
            if (typeSymbol == null)
            {
                yield break;
            }

            var nameSpace = typeSymbol.ContainingNamespace?.Name;

            if (!String.IsNullOrWhiteSpace(nameSpace) && setNameSpace)
            {
                yield return new Run(nameSpace + ".");
            }

            var shortFormat = new SymbolDisplayFormat(
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameOnly,
                genericsOptions: SymbolDisplayGenericsOptions.None,
                miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

            var typeSymbolName = typeSymbol.ToDisplayString(NullableFlowState.MaybeNull, shortFormat);

            if (typeSymbol.TypeKind == TypeKind.Interface)
            {
                yield return new Run(typeSymbolName)
                {
                    FontWeight = FontWeights.Bold,
                    Foreground = Assistant.TextFormatting.InterfaceProperties.ForegroundBrush
                }.CreateLink(typeSymbol, createLink);

                foreach (var e in typeSymbol.CreateGenericInline(setNameSpace: setNameSpace, createLink: createLink))
                {
                    yield return e;
                }
            }
            else if (typeSymbol.TypeKind == TypeKind.Enum)
            {
                yield return new Run(typeSymbolName)
                {
                    FontWeight = FontWeights.Bold,
                    Foreground = Assistant.TextFormatting.EnumProperties.ForegroundBrush
                }.CreateLink(typeSymbol, createLink);
            }
            else if (typeSymbol.IsKeyword())
            {
                yield return new Run(typeSymbolName)
                {
                    FontWeight = FontWeights.Bold,
                    Foreground = Assistant.TextFormatting.KeywordProperties.ForegroundBrush
                }.CreateLink(typeSymbol, createLink);

                foreach (var e in typeSymbol.CreateGenericInline(setNameSpace: setNameSpace, createLink: createLink))
                {
                    yield return e;
                }
            }
            else if (typeSymbol.TypeKind == TypeKind.Struct)
            {
                yield return new Run(typeSymbolName)
                {
                    FontWeight = FontWeights.Bold,
                    Foreground = Assistant.TextFormatting.ClassProperties.ForegroundBrush
                }.CreateLink(typeSymbol, createLink);

                foreach (var e in typeSymbol.CreateGenericInline(setNameSpace: setNameSpace, createLink: createLink))
                {
                    yield return e;
                }
            }
            else if (typeSymbol.TypeKind == TypeKind.Class)
            {
                yield return new Run(typeSymbolName)
                {
                    FontWeight = FontWeights.Bold,
                    Foreground = Assistant.TextFormatting.ClassProperties.ForegroundBrush,
                }.CreateLink(typeSymbol, createLink);

                foreach (var e in typeSymbol.CreateGenericInline(setNameSpace: setNameSpace, createLink: createLink))
                {
                    yield return e;
                }
            }
            else if (typeSymbol.TypeKind == TypeKind.Delegate)
            {
                yield return new Run(typeSymbolName)
                {
                    FontWeight = FontWeights.Bold,
                    Foreground = Assistant.TextFormatting.DelegateProperties.ForegroundBrush,
                }.CreateLink(typeSymbol, createLink);

                foreach (var e in typeSymbol.CreateGenericInline(setNameSpace: setNameSpace, createLink: createLink))
                {
                    yield return e;
                }
            }
            else if (typeSymbol.TypeKind == TypeKind.Array)
            {
                if (typeSymbol is IArrayTypeSymbol arraySymbol)
                {
                    ITypeSymbol elementType = arraySymbol.ElementType;
                    foreach (var e in elementType.CreateInline(false, createLink: createLink))
                    {
                        yield return e;
                    }
                    yield return new Run("[]")
                    {
                        FontWeight = FontWeights.Bold,
                        Foreground = Assistant.TextFormatting.IdentifierProperties.ForegroundBrush
                    };

                    foreach (var e in typeSymbol.CreateGenericInline(setNameSpace: setNameSpace, createLink: createLink))
                    {
                        yield return e;
                    }
                }
                else
                {
                    yield return new Run(typeSymbolName)
                    {
                        FontWeight = FontWeights.Bold,
                        Foreground = Assistant.TextFormatting.IdentifierProperties.ForegroundBrush
                    }.CreateLink(typeSymbol, createLink);
                }
            }
            else if (typeSymbol.TypeKind == TypeKind.TypeParameter)
            {
                yield return new Run(typeSymbolName)
                {
                    FontWeight = FontWeights.Bold,
                    Foreground = Assistant.TextFormatting.TypeParameterProperties.ForegroundBrush
                }.CreateLink(typeSymbol, createLink);
            }
            else
            {
                yield return new Run(typeSymbolName)
                {
                    Foreground = Assistant.TextFormatting.IdentifierProperties.ForegroundBrush
                }.CreateLink(typeSymbol, createLink);
            }
        }

        public static IEnumerable<Inline> CreateInline(this IMethodSymbol methodSymbol, bool createLink = false)
        {
            if (methodSymbol == null)
            {
                yield break;
            }

            foreach(var e in methodSymbol.ReturnType.CreateInline(setNameSpace: false, createLink: createLink))
            {
                yield return e;
            }

            if (methodSymbol.ReturnType != null)
            {
                yield return new Run(" ")
                {
                    Foreground = Assistant.TextFormatting.IdentifierProperties.ForegroundBrush
                };
            }

            INamedTypeSymbol containingClass = methodSymbol.ContainingType;
            if (containingClass != null)
            {
                foreach (var e in containingClass.CreateInline(setNameSpace: false, createLink: createLink))
                {
                    yield return e;
                }

                yield return new Run(".")
                {
                    Foreground = Assistant.TextFormatting.IdentifierProperties.ForegroundBrush
                };
            }

            yield return new Run(methodSymbol.Name)
            {
                FontWeight = FontWeights.Bold,
                Foreground = Assistant.TextFormatting.MethodProperties.ForegroundBrush
            }.CreateLink(methodSymbol, createLink);

            yield return new Run("(")
            {
                FontWeight = FontWeights.Bold,
                Foreground = Assistant.TextFormatting.IdentifierProperties.ForegroundBrush
            };

            var parameters = methodSymbol.Parameters.Cast<IParameterSymbol>().ToArray();
            for (int i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                foreach (var e in parameter.Type.CreateInline(setNameSpace: false, createLink: createLink))
                {
                    yield return e;
                }

                yield return new Run(" ");
                yield return new Run(parameter.Name)
                {
                    Foreground = Assistant.TextFormatting.IdentifierProperties.ForegroundBrush
                };

                if (i < (parameters.Length - 1))
                {
                    yield return new Run(", ")
                    {
                        FontWeight = FontWeights.Bold,
                        Foreground = Assistant.TextFormatting.IdentifierProperties.ForegroundBrush
                    };
                }
            }

            yield return new Run(")")
            {
                FontWeight = FontWeights.Bold,
                Foreground = Assistant.TextFormatting.IdentifierProperties.ForegroundBrush
            };
        }

        /// <summary> Сравнить 2 типизированных символа, с учетом дженерик типа </summary>
        /// <param name="symbolLeft"></param>
        /// <param name="symbolRight"></param>
        /// <returns></returns>
        public static bool IsEqualTo(this ISymbol symbolLeft, ISymbol symbolRight)
        {
            if (symbolLeft == null || symbolRight == null)
                return false;
            return symbolLeft == symbolRight || SymbolEqualityComparer.Default.Equals(symbolLeft.OriginalDefinition, symbolRight.OriginalDefinition);
        }

        /// <summary> Поиск проекта, которому принадлежит символ </summary>
        /// <param name="symbol"></param>
        /// <returns></returns>
        public static Microsoft.CodeAnalysis.Project GetProject(this ISymbol symbol, bool isExternal)
        {
            var project = Assistant.GetWorkspace().GetSymbols(isExternal).FirstOrDefault(x => x.Symbol.IsEqualTo(symbol));
            return project?.Project;
        }

        /// <summary>
        /// Поиск проекта, которому принадлежит символ.
        /// Сперва воиск в решении, вотом во внешних библиотеках
        /// </summary>
        /// <param name="symbol"></param>
        /// <returns></returns>
        public static Microsoft.CodeAnalysis.Project GetProject(this ISymbol symbol)
        {
            var project = symbol.GetProject(false) ?? symbol.GetProject(true);
            return project;
        }

        public static async Task<bool> TryGoToDefinitionAsync(this ISymbol symbol)
        {
            var project = symbol.GetProject();
            if (project == null)
                return false;
            return await Assistant.GetWorkspace().TryGoToDefinitionAsync(symbol, project, default);
        }

        public static Diagnostic[] GetDiagnostics(this ISymbol symbol)
        {
            if (symbol == null)
                return Array.Empty<Diagnostic>();

            var project = symbol.GetProject();
            if (project == null)
                return Array.Empty<Diagnostic>();

            if (project.TryGetCompilation(out var compilation))
            {
                if (symbol == null || compilation == null)
                    return Array.Empty<Diagnostic>();

                List<Diagnostic> listDiagnostic = new List<Diagnostic>();
                foreach (var location in symbol.Locations.Where(loc => loc.IsInSource))
                {
                    var syntaxTree = location.SourceTree;
                    if (syntaxTree == null) 
                        continue;

                    var semanticModel = compilation.GetSemanticModel(syntaxTree);
                    var diagnostics = semanticModel.GetDiagnostics(location.SourceSpan);

                    foreach (var diagnostic in diagnostics)
                    {
                        listDiagnostic.Add(diagnostic);
                    }
                }

                return listDiagnostic.ToArray();
            }
            return Array.Empty<Diagnostic>();   
        }

        /// <summary> Попытка перейти к определению символа </summary>
        /// <param name="symbol"></param>
        /// <returns></returns>
        public static async Task<GoToResult> TryGoToImplementations(this ISymbol symbol)
        {
            bool isSuccess;
            string defaultErrorMessage = "Реализация не найдена";
            if (symbol is ITypeSymbol typeSymbol)
            {
                var implementations = typeSymbol.GetImplementations().ToArray();
                if (implementations.Length == 1)
                {
                    isSuccess = await implementations.First().TryGoToDefinitionAsync();
                    return new GoToResult(isSuccess, !isSuccess ? defaultErrorMessage : null);
                }
                if (implementations.Length == 0)
                {
                    if (typeSymbol.TypeKind == TypeKind.Interface || typeSymbol.TypeKind == TypeKind.Unknown)
                    {
                        return new GoToResult(false, defaultErrorMessage);
                    }
                    isSuccess = await typeSymbol.TryGoToDefinitionAsync();
                    return new GoToResult(isSuccess, !isSuccess ? defaultErrorMessage : null);
                }

                var dialog = new ListSymbolDialog(implementations.ToArray());
                dialog.Owner = Application.Current.MainWindow;
                dialog.ShowModal();
                return new GoToResult(true, null);
            }

            if(symbol is IMethodSymbol methodSymbol)
            {
                var implementations = methodSymbol.GetImplementations().ToArray();
                if (implementations.Length == 1)
                {
                    isSuccess = await implementations.First().TryGoToDefinitionAsync();
                    return new GoToResult(isSuccess, !isSuccess ? defaultErrorMessage : null);
                }
                if (implementations.Length == 0)
                {
                    isSuccess = await methodSymbol.TryGoToDefinitionAsync();
                    return new GoToResult(isSuccess, !isSuccess ? defaultErrorMessage : null);
                }

                var dialog = new ListSymbolDialog(implementations);
                dialog.Owner = Application.Current.MainWindow;
                dialog.ShowModal();
                return new GoToResult(true, null);
            }

            isSuccess = await symbol.TryGoToDefinitionAsync();
            return new GoToResult(isSuccess, !isSuccess ? defaultErrorMessage : null);
        }
    }
}
