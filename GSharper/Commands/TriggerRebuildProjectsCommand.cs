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
    public class TriggerRebuildProjectsCommand : GSharperCommandBase<TriggerRebuildProjectsCommand>
    {
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
        public override void Execute(object sender, EventArgs e)
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