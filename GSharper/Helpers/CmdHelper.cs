using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GSharper.Helpers
{
    public class CmdCommandResult
    {
        public int ExitCode { get; }
        public string Output { get; }
        public string Error { get; }

        public bool IsError => ExitCode != 0;

        public CmdCommandResult(int exitCode, string output, string error)
        {
            this.ExitCode = exitCode;
            this.Output = output;
            this.Error = error;
        }
    }


    public class CmdHelper
    {
        public CmdCommandResult Run(string arguments, bool isUtf8 = true)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Encoding ibm866 = Encoding.GetEncoding(866);

            var processInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c {arguments}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = ibm866,
                StandardErrorEncoding = ibm866
                // StandardOutputEncoding = Encoding.UTF8,
                // StandardErrorEncoding = Encoding.UTF8
            };
            if (isUtf8)
            {
                processInfo.StandardOutputEncoding = Encoding.UTF8;
                processInfo.StandardErrorEncoding = Encoding.UTF8;
                processInfo.EnvironmentVariables["LESSCHARSET"] = "utf-8";
            }

            using (var process = Process.Start(processInfo))
            {
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                return new CmdCommandResult(process.ExitCode, output.Trim(), error.Trim());
            }
        }
    }
}
