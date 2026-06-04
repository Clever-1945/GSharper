using GSharper.Extensions;
using GSharper.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Xml.Linq;
using System.Xml.XPath;

namespace GSharper.Assistants
{
    public class AssistantDecompile
    {
        public class ReferenceInfo
        {
            public string AssemblyName;
            public string Version;

            public bool IsStandart() 
            {
                return AssemblyName == "mscorlib" || AssemblyName == "netstandard";
            }
        }

        public class DecompiledAssemblyInfo
        {
            public string Dll { get; }
            public string Pdb { get; }
            public string Project { get; }

            public DecompiledAssemblyInfo(string dll, string pdb, string project)
            {
                Dll = dll;
                Pdb = pdb;
                Project = project;
            }
        }

        public class ProjectPackageFile
        {
            public FileInfo Dll { set; get; }
            public string AssemblyName { get; set; }
            public string Version { get; set; }
            public bool IsSelected { set; get; }
            public DirectoryInfo CachDirectory { set; get; }

            public string DisplayText => GetDisplayText();

            private string GetDisplayText()
            {
                return $"{AssemblyName} / Version: {Version} / {Dll.FullName.Substring(CachDirectory.FullName.Length)} ";
            }
        }

        /// <summary>
        /// Найти все зависимости в сборке
        /// </summary>
        /// <param name="dllPath"></param>
        /// <returns></returns>
        private ReferenceInfo[] GetReferencedAssemblies(string dllPath)
        {
            var references = new List<ReferenceInfo>();

            if (!File.Exists(dllPath))
                return Array.Empty<ReferenceInfo>();

            using (var fs = new FileStream(dllPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (var peReader = new PEReader(fs))
                {
                    if (!peReader.HasMetadata)
                        return Array.Empty<ReferenceInfo>();

                    MetadataReader metadataReader = peReader.GetMetadataReader();
                    foreach (AssemblyReferenceHandle handle in metadataReader.AssemblyReferences)
                    {
                        AssemblyReference reference = metadataReader.GetAssemblyReference(handle);
                        string assemblyName = metadataReader.GetString(reference.Name);

                        references.Add(new ReferenceInfo() 
                        { 
                            AssemblyName = assemblyName,
                            Version = reference.Version.ToString(),
                        });
                    }
                }
            }

            return references.ToArray();
        }

        /// <summary> Папка с декомпилированными сборками </summary>
        private DirectoryInfo GetPackagesDirectory()
        {
            var directory = Path.Combine(Assistant.GetPluginDirectory().FullName, "Packages");
            return Directory.CreateDirectory(directory);
        }

        /// <summary> Папка куда декомпилируется сборка </summary>
        /// <param name="assemblyАileName"></param>
        public DirectoryInfo GetPackageDirectory(string assemblyАileName)
        {
            var projectPath = Path.Combine(GetPackagesDirectory().FullName, Assistant.GetMd5(new FileInfo(assemblyАileName)).ToString());
            return Directory.CreateDirectory(projectPath);
        }

        /// <summary>
        /// Получить информацию о сборке из файла решение
        /// </summary>
        /// <param name="projectFile"></param>
        /// <param name="cachDirectory"></param>
        public IEnumerable<ProjectPackageFile> GetSolutionPackageReferences(FileInfo solutionFile, DirectoryInfo cachDirectory)
        {
            var projectFiles = solutionFile.Directory.GetFiles("*.csproj", SearchOption.AllDirectories).ToArray();
            foreach (var projectFile in projectFiles)
            {
                foreach (var projectPackageFile in GetProjectPackageReferences(projectFile, cachDirectory))
                {
                    yield return projectPackageFile;
                }
            }
        }

        /// <summary>
        /// Получить информацию о сборке из файла проекта
        /// </summary>
        /// <param name="projectFile"></param>
        /// <param name="cachDirectory"></param>
        public IEnumerable<ProjectPackageFile> GetProjectPackageReferences(FileInfo projectFile, DirectoryInfo cachDirectory)
        {
            var doc = XDocument.Load(projectFile.FullName);
            var packageReferences = doc.XPathSelectElements("//ItemGroup/PackageReference").ToArray();

            foreach(var packageReference in packageReferences)
            {
                var name = packageReference.Attribute("Include")?.Value;
                var version = packageReference.Attribute("Version")?.Value;

                if (!String.IsNullOrWhiteSpace(name) & !String.IsNullOrWhiteSpace(version))
                {
                    var list = GetCachPackages(name, version, cachDirectory);
                    foreach(var instance in list) 
                    {
                        yield return new ProjectPackageFile() 
                        {
                            Dll = instance,
                            IsSelected = false,
                            AssemblyName = name,
                            Version = version,
                            CachDirectory = cachDirectory
                        };
                    }
                }
            }
        }

        /// <summary> Получить информацию о декомпилированной сборки </summary>
        /// <returns></returns>
        public DecompiledAssemblyInfo GetDecompiledInfo(string assemblyАileName)
        {
            var projectFile = GetPackageDirectory(assemblyАileName).GetFiles("*.csproj", SearchOption.AllDirectories).FirstOrDefault();
            if (projectFile == null)
                return null;

            var dll = projectFile.Directory.GetFiles("*.dll", SearchOption.AllDirectories).FirstOrDefault();
            var pdb = projectFile.Directory.GetFiles("*.pdb", SearchOption.AllDirectories).FirstOrDefault();

            if (dll == null || pdb == null)
                return null;

            return new DecompiledAssemblyInfo(dll.FullName, pdb.FullName, projectFile.FullName);
        }

        /// <summary>
        /// Получить папку, где хранятся библиотеки с нугета
        /// </summary>
        /// <returns></returns>
        public DirectoryInfo GetCachDirectory()
        {
            var cmd = new CmdHelper();
            var result = cmd.Run("dotnet nuget locals global-packages --list");
            if (result.ExitCode != 0)
                return null;

            var output = result.Output?.Trim();
            if (String.IsNullOrWhiteSpace(output))
                return null;

            var prefix = "global-packages:";
            foreach (var line in output.Split('\n'))
            {
                var textLine = line.Trim();
                if (textLine.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    var path = textLine.Substring(prefix.Length).Trim();
                    if (Directory.Exists(path))
                        return new DirectoryInfo(path);
                }
            }

            return null;
        }

        /// <summary>
        /// Получить файлы библиотек, в папке с закешированными сборками
        /// </summary>
        /// <param name="reference"></param>
        /// <returns></returns>
        public FileInfo[] GetCachPackages(string assemblyName, string version, DirectoryInfo cachDirectory)
        {
            if (cachDirectory == null)
                return Array.Empty<FileInfo>();
            var packageDirectory = Path.Combine(cachDirectory.FullName, assemblyName, version);
            return new DirectoryInfo(packageDirectory).GetFiles("*.dll", SearchOption.AllDirectories);
        }

        /// <summary>
        /// Исправить файл проекта т.к. не всегда декомпиляция проходит правильно
        /// </summary>
        /// <param name="projectFile"></param>
        private void FixProjectFile(FileInfo projectFile, ReferenceInfo[] references)
        {
            var doc = XDocument.Load(projectFile.FullName);
            var elements = doc.XPathSelectElements("//ItemGroup/Reference/HintPath").ToArray();
            elements = elements.Select(x => x.Parent).ToArray();
            foreach (var element in elements)
            {
                element.Remove();
            }

            var propertyGroup = doc.XPathSelectElements("//PropertyGroup").FirstOrDefault();
            if (propertyGroup == null)
            {
                propertyGroup = new XElement("PropertyGroup");
                doc.Root.Add(propertyGroup);
            }

            Func<string, XElement> getProperty = (name) => 
            {
                var propertyElement = doc.XPathSelectElements("//PropertyGroup/" + name).FirstOrDefault();
                if (propertyElement != null)
                    return propertyElement;

                propertyElement = new XElement(name);
                propertyGroup.Add(propertyElement);
                return propertyElement;
            };

            getProperty("LangVersion").Value = "latest";
            getProperty("Configuration").Value = "Debug";
            getProperty("Platform").Value = "AnyCPU";
            getProperty("Optimize").Value = "false";
            getProperty("DebugSymbols").Value = "true";
            // getProperty("DebugType").Value = "full";
            getProperty("DebugType").Value = "portable";
            getProperty("EmbedAllSources").Value = "true";

            var itemGroup = doc.XPathSelectElements("//ItemGroup").FirstOrDefault(x => !x.Elements().Any());
            if (itemGroup == null)
            {
                itemGroup = new XElement("ItemGroup");
                doc.Root.Add(itemGroup);
            }

            foreach (var reference in references.Where(x => !x.IsStandart()))
            {
                var packageReference = new XElement("PackageReference");
                packageReference.Add(new XAttribute("Include", reference.AssemblyName));
                packageReference.Add(new XAttribute("Version", reference.Version));
                itemGroup.Add(packageReference);
            }

            doc.Save(projectFile.FullName);
        }

        /// <summary> Декомпиляция сборки </summary>
        /// <param name="fileName"></param>
        public FileInfo DecompileAssembly(string fileName)
        {
            var references = GetReferencedAssemblies(fileName);

            var outputPane = Assistant.GetOutputPane();
            outputPane?.Activate();
            var cmd = new CmdHelper();
            var r = cmd.Run("dotnet tool install -g ilspycmd");
            outputPane.Output(r);

            var projectPath = GetPackageDirectory(fileName).FullName;
            if (Directory.Exists(projectPath))
                Directory.Delete(projectPath, true);

            Directory.CreateDirectory(projectPath);
            outputPane.OutputLine($"Папка проекта: {projectPath}");
            outputPane.OutputLine($"Начата декомпиляция");
            r = cmd.Run($"ilspycmd \"{fileName}\" -p -o \"{projectPath}\"");
            outputPane.Output(r);

            var projectFile = new DirectoryInfo(projectPath).GetFiles("*.csproj").FirstOrDefault();
            if (projectFile == null)
            {
                outputPane.OutputLine($"Не найден файл проекта");
                return null;
            }

            FixProjectFile(projectFile, references);
            r = cmd.Run($"dotnet build \"{projectFile.FullName}\" -c Debug");
            outputPane.Output(r);
            if (r.ExitCode == 0)
            {
                outputPane.OutputLine($"Сборка {projectFile.Name} успешно выполнена");
            }

            return projectFile;
        }
    }
}
