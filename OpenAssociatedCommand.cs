using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using Task = System.Threading.Tasks.Task;
using EnvDTE;
using EnvDTE80;

namespace MolenAssist
{
    internal sealed class OpenAssociatedCommand
    {
        public const int CommandId = 0x0100;

        public static readonly Guid CommandSet = new Guid("4a8dce2d-1259-427f-9574-ae3ac94304c6");
        public static readonly string PhysicalFileKind = "{6BB5F8EE-4483-11D3-8BCF-00C04F8EC28C}";

        private readonly MolenAssistPackage package;

        private readonly DTE2 m_dte;

        private ErrorListProvider errorListProvider;

        private OpenAssociatedCommand(MolenAssistPackage package, OleMenuCommandService commandService, DTE2 dte)
        {
            m_dte = dte;
            errorListProvider = new ErrorListProvider(package);

            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        public static OpenAssociatedCommand Instance
        {
            get;
            private set;
        }

        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        public static async Task InitializeAsync(MolenAssistPackage package, DTE2 dte)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new OpenAssociatedCommand(package, commandService, dte);
        }

        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            Document doc = m_dte.ActiveDocument;

            string fileStem = GetNameStem(doc.FullName);

            List<ProjectItem> allFiles = FindAllFilesMatching(fileStem);

            int currentIndex = allFiles.IndexOf(doc.ProjectItem);
            if (currentIndex < 0)
            {
                var errMsg = new ErrorTask()
                {
                    ErrorCategory = TaskErrorCategory.Warning,
                    Category = TaskCategory.User,
                    Text = "couldn't file an associated file for '" + doc.FullName + "', stem '" + fileStem + "'",
                    Document = doc.FullName
                };
                errorListProvider.Tasks.Add(errMsg);
                errorListProvider.Show();
                return;
            }

            int nextIndex = (currentIndex + 1) % allFiles.Count;
            ProjectItem nextItem = allFiles[nextIndex];

            string nextFilename = nextItem.FileNames[1];
            m_dte.ItemOperations.OpenFile(nextFilename);
        }

        private static string GetNameStem(string fullName)
        {
            string filename = System.IO.Path.GetFileNameWithoutExtension(fullName);
            if (filename.StartsWith("test_"))
                filename = filename.Replace("test_", "");
            if (filename.StartsWith("shader_"))
                filename = filename.Replace("shader_", "");
            if (filename.EndsWith("_vert"))
                filename = filename.Replace("_vert", "");
            if (filename.EndsWith("_frag"))
                filename = filename.Replace("_frag", "");
            return filename;
        }

        private List<ProjectItem> FindAllFilesMatching(string stem)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            List<ProjectItem> allFiles = new List<ProjectItem>();

            Array projects = (Array)m_dte.ActiveSolutionProjects;
            for (int i = 0; i < projects.Length; ++i)
            {
                Project project = (Project)projects.GetValue(i);
                foreach (ProjectItem item in project.ProjectItems)
                    allFiles.AddRange(FindAllFilesMatching(stem, item));
            }

            return allFiles;
        }
        private List<ProjectItem> FindAllFilesMatching(string stem, ProjectItem item)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            List<ProjectItem> allFiles = new List<ProjectItem>();
            string kind = item.Kind;
            if (kind == PhysicalFileKind && GetNameStem(item.Name) == stem)
                allFiles.Add(item);

            foreach (ProjectItem subItem in item.ProjectItems)
                allFiles.AddRange(FindAllFilesMatching(stem, subItem));

            return allFiles;
        }

        private List<ProjectItem> FindAllFiles()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            List<ProjectItem> allFiles = new List<ProjectItem>();

            Array projects = (Array)m_dte.ActiveSolutionProjects;
            for (int i = 0; i < projects.Length; ++i)
            {
                Project project = (Project)projects.GetValue(i);
                foreach (ProjectItem item in project.ProjectItems)
                    allFiles.AddRange(FindAllFiles(item));
            }

            return allFiles;
        }
        private List<ProjectItem> FindAllFiles(ProjectItem item)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            List<ProjectItem> allFiles = new List<ProjectItem>();
            allFiles.Add(item);

            foreach (ProjectItem subItem in item.ProjectItems)
                allFiles.AddRange(FindAllFiles(subItem));

            return allFiles;
        }
    }
}
