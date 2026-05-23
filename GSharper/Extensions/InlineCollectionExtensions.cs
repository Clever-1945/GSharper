using GSharper.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Elfie.Model;
using Microsoft.VisualStudio.VCProjectEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Xml.Linq;

namespace GSharper.Extensions
{
    public static class InlineCollectionExtensions
    {
        public static InlineCollection Clear(this InlineCollection collection)
        {
            collection.Clear();
            return collection;
        }

        public static InlineCollection Add(this InlineCollection collection, Inline item)
        {
            collection.Add(item);
            return collection;
        }

        public static Inline AddText(this InlineCollection collection, string text, bool isBold = false)
        {
            collection.Add(new Run((text ?? ""))
            {
                FontWeight = isBold ? FontWeights.Bold : FontWeights.Light,
                Foreground = Assistant.TextFormatting.IdentifierProperties.ForegroundBrush
            });
            return collection.Last();
        }

        public static Inline AddLineIfNotEmpty(this InlineCollection collection)
        {
            if (collection.Count > 0)
            {
                var line = new Run("\r\n");
                collection.Add(line);
                return line;
            }
            return null;
        }

        public static Inline[] Add(this InlineCollection collection, ISymbol symbol, bool createLink = false)
        {
            var list = symbol.CreateInline(createLink: createLink).ToArray();
            foreach (var item in list) 
            {
                collection.Add(item);
            }
            return list;
        }

        /// <summary>
        /// Создание многокточий для перехода к списку символов
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="symbols"></param>
        /// <returns></returns>
        public static Inline AddDots(this InlineCollection collection, ISymbol[] symbols)
        {
            var run = new Run("...")
            {
                FontWeight = FontWeights.Bold
            };
            var tag = new HyperlinkTagGoToSymbols(symbols);
            var link = new Hyperlink(run)
            {
                TextDecorations = null,
                Tag = tag
            };
            collection.Add(link);
            return link;
        }

        public static Inline[] Add(this InlineCollection collection, ITypeSymbol typeSymbol, bool setNameSpace = true, bool clearValue = true, bool createLink = false)
        {
            if (clearValue)
            {
                collection.Clear();
            }
            var list = typeSymbol.CreateInline(setNameSpace: setNameSpace, createLink: createLink).ToArray();
            foreach(var item in list)
            {
                collection.Add(item);
            }
            return list;
        }

        public static Inline[] Add(this InlineCollection collection, IPropertySymbol propertySymbol, bool createLink = false, bool clearValue = true)
        {
            if (clearValue)
            {
                collection.Clear();
            }
            var list = propertySymbol.CreateInline(createLink: createLink).ToArray();
            foreach (var item in list)
            {
                collection.Add(item);
            }
            return list;
        }

        public static Inline[] Add(this InlineCollection collection, IFieldSymbol fieldSymbol, bool createLink = false, bool clearValue = true)
        {
            if (clearValue)
            {
                collection.Clear();
            }
            var list = fieldSymbol.CreateInline(createLink: createLink).ToArray();
            foreach (var item in list)
            {
                collection.Add(item);
            }
            return list;
        }

        public static Inline[] Add(this InlineCollection collection, ILocalSymbol localSymbol, bool createLink = false, bool clearValue = true)
        {
            if (clearValue)
            {
                collection.Clear();
            }
            var list = localSymbol.CreateInline(createLink: createLink).ToArray();
            foreach (var item in list)
            {
                collection.Add(item);
            }
            return list;
        }

        public static Inline[] Add(this InlineCollection collection, IMethodSymbol methodSymbol, bool createLink = false, bool clearValue = true)
        {
            if (clearValue)
            {
                collection.Clear();
            }
            var list = methodSymbol.CreateInline(createLink: createLink).ToArray();
            foreach (var item in list)
            {
                collection.Add(item);
            }
            return list;
        }
    }
}
