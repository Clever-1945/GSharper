using EnvDTE;
using GSharper.Assistants;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GSharper.Commands
{
    /// <summary>
    /// Пересобрать каждый прект в отдельности
    /// </summary>
    public class TriggerRebuildProjectsCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0107;
        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("578213b0-a1b9-49ca-924d-b5488d8e74e4");
        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static TriggerRebuildProjectsCommand Instance { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyboardShortcutCollectionCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private TriggerRebuildProjectsCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new TriggerRebuildProjectsCommand(package, commandService);
        }

        /// <summary> Получить зависимости проекта </summary>
        /// <param name="project"></param>
        /// <param name="solution"></param>
        /// <returns></returns>
        private IEnumerable<Project> GetProjectDependencies(Project project, Solution solution)
        {
            BuildDependency dependency = solution.SolutionBuild.BuildDependencies.Item(project.UniqueName);
            if (dependency != null)
            {
                if (dependency.RequiredProjects is Array requiredProjects)
                {
                    foreach (Project reqProject in requiredProjects)
                    {
                        yield return reqProject;
                    }
                }
            }
        }

        /// <summary>
        /// Shows the tool window when the menu item is clicked.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        private void Execute(object sender, EventArgs e)
        {
            var dte = Assistant.GetDte();
            EnvDTE80.SolutionBuild2 solutionBuild = (EnvDTE80.SolutionBuild2)(dte.Solution.SolutionBuild);

            var dependencies = new Dictionary<Project, HashSet<Project>>();
            foreach(Project project in dte.Solution.Projects)
            {
                if(File.Exists(project.FileName))
                {
                    dependencies[project] = new HashSet<Project>(GetProjectDependencies(project, dte.Solution));
                }
            }

            var collectedProjects = new HashSet<Project>();
            solutionBuild.Clean(true);

            // Список проектов для сборки
            Project[] listBuildProject;
            do
            {
                listBuildProject = dependencies
                    .Where(x => !x.Value.Any() || x.Value.All(z => collectedProjects.Contains(z)))
                    .Select(x => x.Key)
                    .Where(x => !collectedProjects.Contains(x))
                    .ToArray();

                foreach(var project in listBuildProject)
                {
                    solutionBuild.BuildProject("Debug", project.FileName, true);
                    int errorCount = solutionBuild.LastBuildInfo;

                    if (errorCount > 0)
                        return;

                    collectedProjects.Add(project);
                }
            } while (listBuildProject.Length > 0);
        }
    }
}