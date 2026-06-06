using GSharper.Assistants;
using GSharper.Dialogs;
using System;
using System.IO;
using System.Threading;
using System.Windows;

namespace GSharper.Commands
{
    public class TriggerDecompilationPackagesCommand : GSharperCommandBase<TriggerDecompilationPackagesCommand>
    {
        /// <summary>
        /// Shows the tool window when the menu item is clicked.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        public override void Execute(object sender, EventArgs e)
        {
            var dialog = new SelectAssemblyDialog();
            dialog.Owner = Application.Current.MainWindow;
            dialog.ShowModal();
            if (!dialog.IsOk)
                return;

            var listSelectedProjectPackage = dialog.ListSelectedProjectPackage;
            if (listSelectedProjectPackage.Length < 1)
                return;

            ThreadPool.QueueUserWorkItem(s =>
            {
                foreach (var assemblyFile in listSelectedProjectPackage)
                {
                    var assemblyInfo = Assistant.Decompile.Value.GetDecompiledInfo(assemblyFile.Dll.FullName);
                    if (assemblyInfo == null)
                    {
                        Assistant.Decompile.Value.DecompileAssembly(assemblyFile.Dll.FullName);
                    }
                    
                    assemblyInfo = Assistant.Decompile.Value.GetDecompiledInfo(assemblyFile.Dll.FullName);
                    if (assemblyInfo == null)
                        continue;

                    var pdbFile = Path.ChangeExtension(assemblyFile.Dll.FullName, ".pdb");
                    var dllFile = assemblyFile.Dll.FullName;
                    if (File.Exists(pdbFile))
                        File.Delete(pdbFile);

                    if (File.Exists(dllFile))
                        File.Delete(dllFile);

                    File.Copy(assemblyInfo.Pdb, pdbFile);
                    File.Copy(assemblyInfo.Dll, dllFile);
                }
            });
        }
    }
}
