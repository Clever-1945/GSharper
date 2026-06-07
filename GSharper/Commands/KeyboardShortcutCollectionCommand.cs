using EnvDTE;
using System;
using System.Linq;
using GSharper.Extensions;
using System.Collections.Generic;
using GSharper.Assistants;

namespace GSharper.Commands
{
    public class KeyboardShortcutCollectionCommand : GSharperCommandBase<KeyboardShortcutCollectionCommand>
    {
        private Dictionary<string, string> GetShortcuts()
        {
            var shortcut = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            shortcut["View.NavigateForward"] = "Текстовый редактор::Ctrl+Alt+Right Arrow";
            shortcut["View.NavigateBackward"] = "Текстовый редактор::Ctrl+Alt+Left Arrow";
            shortcut["Debug.QuickWatch"] = "Текстовый редактор::Shift+F9";
            shortcut["Edit.QuickInfo"] = "Текстовый редактор::Ctrl+Q";

            // Форматировать выбранный участок кода
            shortcut["Edit.FormatSelection"] = "Текстовый редактор::Ctrl+Alt+L";

            // перейти к базовому
            shortcut["Edit.GoToBase"] = "Текстовый редактор::Ctrl+U";

            // Показать окно поиском типов
            shortcut["GSharper.triggerSearchDialog"] = "Текстовый редактор::Ctrl+N";

            // комментировать / разкомментировать
            shortcut["GSharper.triggerChangeStateCommentCommand"] = "Текстовый редактор::Ctrl+num /";

            // Изменить регистр слова на противоположный
            shortcut["GSharper.triggerChangeCaseCommand"] = "Текстовый редактор::Ctrl+Shift+U";

            // Перейти к реализации
            shortcut["GSharper.triggerGoToImplementationsCommand"] = "Текстовый редактор::Ctrl+Alt+Shift+B";

            // Следущая ошибка в коде
            shortcut["View.NextError"] = "Текстовый редактор::F12";

            // Шаг с обходом
            shortcut["Debug.StepOver"] = "Везде::F10";

            // Шаг с заходом
            shortcut["Debug.StepInto"] = "Везде::F11";

            // Шаг с выходом
            shortcut["Debug.StepOut"] = "Везде::Shift+F11";

            // глобальный поиск текста
            shortcut["Edit.GoToText"] = "Везде::Ctrl+Shift+F";

            return shortcut;
        }

        private void DeleteShortcuts(string[] shortcuts, EnvDTE.Commands commands)
        {
            foreach (Command cmd in commands)
            {
                if (String.IsNullOrWhiteSpace(cmd.Name) || String.IsNullOrWhiteSpace(cmd.LocalizedName))
                {
                    continue;
                }
                object[] objectShortcuts = (cmd.Bindings as object[]) ?? Array.Empty<object>();
                if (objectShortcuts.Length > 0)
                {
                    string[] textShortcuts = objectShortcuts.Select(x => x as string).Where(x => !String.IsNullOrWhiteSpace(x)).ToArray();
                    if (textShortcuts.Length > 0)
                    {
                        foreach(var shortcut in shortcuts)
                        {
                            if (textShortcuts.First().Contains($"::{shortcut}", StringComparison.OrdinalIgnoreCase))
                            {
                                cmd.Bindings = new object[0];
                                break;
                            }
                            else if (textShortcuts.Skip(1).Any(x => String.Equals(x, shortcut, StringComparison.OrdinalIgnoreCase)))
                            {
                                cmd.Bindings = new object[0];
                                break;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Shows the tool window when the menu item is clicked.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        public override void Execute(object sender, EventArgs e)
        {
            var commands = Assistant.GetDte().Commands;
            var report = new List<string>();

            var shortcuts = GetShortcuts();
            System.Windows.MessageBox.Show("Перед примененией комбинаций клавиш обязательно переключите раскладку на английскую!");

            var shortcutNames = shortcuts.Select(x => 
            {
                var index = x.Value.IndexOf("::");
                return index < 0 ? x.Value : x.Value.Substring(index + 2);
            }).ToArray();
            DeleteShortcuts(shortcutNames, commands);

            foreach (Command cmd in commands)
            {
                if (String.IsNullOrWhiteSpace(cmd.Name) || String.IsNullOrWhiteSpace(cmd.LocalizedName))
                {
                    continue;
                }
                var shortcut = shortcuts.GetValueOrDefault(cmd.LocalizedName) ?? shortcuts.GetValueOrDefault(cmd.Name);
                if (shortcut != null)
                {
                    cmd.Bindings = new object[] { $"{shortcut}" };
                }
            }
        }
    }
}
