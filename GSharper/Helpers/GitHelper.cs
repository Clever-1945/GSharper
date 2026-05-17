using GSharper.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GSharper.Helpers
{
    public class GitHelper
    {
        public string RepositoryPath { get; }
        public string FileName { get; }

        private string _topLevelPath;

        public GitHelper(string fileName)
        {
            RepositoryPath = Path.GetDirectoryName(fileName);
            FileName = fileName;
            _topLevelPath = RunGitCommand($"rev-parse --show-toplevel");
        }

        public string GetLogText(string fileName)
        {
            var relativeFileName = GetRelativeFileName(fileName);
            var text = RunGitCommand($"log {relativeFileName}");
            return text;
        }

        public string GetShowCurrentText(string commit, string fileName)
        {
            var relativeFileName = GetRelativeFileName(fileName);
            var text = RunGitCommand($"show {commit}:{relativeFileName}");
            return text;
        }

        public string GetShowBeforeText(string commit, string fileName)
        {
            var relativeFileName = GetRelativeFileName(fileName);
            var text = RunGitCommand($"show {commit}~1:{relativeFileName}");
            return text;
        }

        public GitLogInfo[] GetLogs(string fileName)
        {
            var text = GetLogText(fileName);
            return GitLogInfo.Parse(text).ToArray();
        }

        public string RunGitCommand(string arguments)
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = arguments,
                WorkingDirectory = _topLevelPath ?? RepositoryPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };
            processInfo.EnvironmentVariables["LESSCHARSET"] = "utf-8";

            using (var process = Process.Start(processInfo))
            {
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0)
                    throw new Exception($"Git error: {error}");

                return output.Trim();
            }
        }

        public string GetTopLevelPath()
        {
            return _topLevelPath;
        }

        public string GetRelativeFileName(string fullFileName)
        {
            string topLevelPath = GetTopLevelPath();
            var fileName = fullFileName.Substring(topLevelPath.Length + 1).Replace("\\", "/");
            return fileName;
        }

        public string GetContentFile(string branch)
        {
            var fileName = GetRelativeFileName(FileName);
            var command = $"show {branch}:{fileName}";
            string text = RunGitCommand(command);
            return text;
        }

        public GitBranchInfo[] GetListBranch()
        {
            var listLocal = this.RunGitCommand("branch")?.Split('\n') ?? Array.Empty<string>();
            var listRemote = this.RunGitCommand("branch -r")?.Split('\n') ?? Array.Empty<string>();
            listRemote = listRemote.Length < 1
                ? listRemote
                : listRemote.Skip(1).ToArray();

            List<GitBranchInfo> list = new List<GitBranchInfo>();
            var listAllBranch = new string[][] { listLocal, listRemote };

            foreach (var listBranch in listAllBranch)
            {
                foreach (var branch in listBranch)
                {
                    var branchName = branch.Trim();
                    var isCurrent = branchName.StartsWith("*");
                    branchName = branchName.TrimStart('*').Trim();

                    list.Add(new GitBranchInfo()
                    {
                        IsCurrent = isCurrent,
                        Name = branchName,
                        IsRemote = listBranch == listRemote
                    });
                }
            }

            return list.ToArray();
        }
    }
}
