using EnvDTE;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GSharper.Extensions
{
    public static class ProjectItemExtensions
    {
        public static bool GetIsDirty(this ProjectItem projectItem)
        {
            try
            {
                return projectItem.IsDirty;
            }
            catch 
            {
                return true;
            }
        }

        public static IEnumerable<FileInfo> GetFiles(this ProjectItem projectItem)
        {
            for(short i = 0; i < projectItem.FileCount; i++)
            {
                string fileName = "";
                try
                {
                    fileName = projectItem.FileNames[i];
                }
                catch { 
                }
                if (File.Exists(fileName))
                {
                    var fileInfo = new FileInfo(fileName);
                    yield return fileInfo;
                }
            }
        }

        public static IEnumerable<ProjectItem> GetParents(this ProjectItem projectItem)
        {
            do
            {
                projectItem = projectItem?.Collection?.Parent as ProjectItem;
                if (projectItem != null)
                    yield return projectItem;

            } while (projectItem != null);
        }
    }
}
