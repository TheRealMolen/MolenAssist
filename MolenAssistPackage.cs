using Microsoft.VisualStudio.Shell;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using Task = System.Threading.Tasks.Task;
using EnvDTE;
using EnvDTE80;

namespace MolenAssist
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(MolenAssistPackage.PackageGuidString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    public sealed class MolenAssistPackage : AsyncPackage
    {
        public const string PackageGuidString = "e870814b-0b20-4529-b17c-76b436ad6980";

        #region Package Members

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            DTE2 dte = (DTE2)await GetServiceAsync(typeof(DTE));

            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            await OpenAssociatedCommand.InitializeAsync(this, dte);
        }

        #endregion
    }
}
