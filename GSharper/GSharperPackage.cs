using System;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using GSharper.Commands;

namespace GSharper
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(GSharperPackage.PackageGuidString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideAutoLoad(UIContextGuids80.SolutionExists, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideToolWindow(typeof(GSharper.Dialogs.QuickInfoBlockDialog), Style = VsDockStyle.Tabbed)]
    public sealed class GSharperPackage : AsyncPackage
    {
        /// <summary>
        /// GSharperPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "ab2b149a-69ba-4d5e-859d-9a2b400ff7c4";

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            // When initialized asynchronously, the current thread may be a background thread at this point.
            // Do any initialization that requires the UI thread after switching to the UI thread.
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var contextMenuId = new Guid("545A271A-937C-4DAA-951F-CF155A82C3FA");

            await KeyboardShortcutCollectionCommand.InitializeAsync(this, 0x0101);
            await KeyboardShortcutRestart.InitializeAsync(this, 0x0102);
            await TriggerSearchCommand.InitializeAsync(this, 0x0103);
            await TriggerQuickInfoDialogCommand.InitializeAsync(this, 0x0104);
            await TriggerChangeStateCommentCommand.InitializeAsync(this, 0x0105);
            await TriggerChangeCaseCommand.InitializeAsync(this, 0x0106);
            await TriggerRebuildProjectsCommand.InitializeAsync(this, 0x0107);
            await TriggerDecompilationPackagesCommand.InitializeAsync(this, 0x0108);
            
            await ShowChnagesBranchCommand.InitializeAsync(this, 0x0109, contextMenuId);
            await ShowHistoryFileCommand.InitializeAsync(this, 0x0110, contextMenuId);

            await TriggerGoToImplementationsCommand.InitializeAsync(this, 0x0111);
            await TriggerGoToBaseTypesCommand.InitializeAsync(this, 0x0112);
            await TriggerSearchTextCommand.InitializeAsync(this, 0x0113);
        }
    }
}
