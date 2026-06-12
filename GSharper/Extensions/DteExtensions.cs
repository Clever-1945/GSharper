using EnvDTE;
using EnvDTE80;
using GSharper.Models;
using System.Collections.Generic;
using System.Linq;

namespace GSharper.Extensions
{
    public static class PackageExtensions
    {
        public static IEnumerable<ProjectItem> FindProjectItems(this DTE _dte)
        {
            DTE2 dte = _dte as DTE2;
            if (dte == null)
                yield break;

            foreach (Project project in dte.Solution.Projects)
            {
                if (project.ProjectItems != null)
                {
                    foreach (ProjectItem projectItem in FindProjectItems(project.ProjectItems))
                    {
                        if (projectItem.Kind == Constants.vsProjectItemKindPhysicalFile)
                        {
                            yield return projectItem;
                        }
                    }
                }
            }
        }

        public static SymbolModel[] SearchFiles(this DTE dte)
        {
            var listProjectItem = new List<SymbolModel>();

            foreach (var projectItem in dte.FindProjectItems())
            {
                if (projectItem.FileCount < 1)
                    continue;

                // if (projectItem.GetIsDirty())
                //     continue;

                var listFile = projectItem.GetFiles().ToArray();
                if (!listFile.Any())
                    continue;

                listProjectItem.Add(new SymbolModel(projectItem));
            }

            return listProjectItem.ToArray();
        }

        private static IEnumerable<ProjectItem> FindProjectItems(ProjectItems projectItems)
        {
            if (projectItems != null)
            {
                foreach (ProjectItem projectItem in projectItems)
                {
                    if (projectItem.Kind == Constants.vsProjectItemKindPhysicalFile)
                    {
                        yield return projectItem;
                    }
                        
                    foreach (ProjectItem subProjectItem in FindProjectItems(projectItem.ProjectItems))
                    {
                        if (subProjectItem.Kind == Constants.vsProjectItemKindPhysicalFile)
                        {
                            yield return subProjectItem;
                        }
                    }                    
                }
            }
        }
    }
}
