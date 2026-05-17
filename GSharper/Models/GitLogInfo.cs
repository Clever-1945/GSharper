using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GSharper.Models
{
    public class GitLogInfo
    {
        public string Commit { set; get; }
        public string Author { set; get; }
        public DateTime? Date { set; get; }
        public string Comment { set; get; }

        public static GitLogInfo ParseInstance(string text)
        {
            if (String.IsNullOrWhiteSpace(text))
                return null;

            text = text.Trim();

            var info = new GitLogInfo();
            var listLine = text.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            var listComment = new List<string>();
            foreach (var line in listLine)
            {
                if (String.IsNullOrWhiteSpace(info.Commit))
                {
                    if (line.Contains("commit ", StringComparison.OrdinalIgnoreCase))
                    {
                        info.Commit = line.Substring("commit ".Length).Trim();
                        continue;
                    }
                }

                if (String.IsNullOrWhiteSpace(info.Author))
                {
                    if (line.Contains("Author: ", StringComparison.OrdinalIgnoreCase))
                    {
                        info.Author = line.Substring("Author: ".Length).Trim();
                        continue;
                    }
                }
                if (info.Date == null)
                {
                    if (line.Contains("Date: ", StringComparison.OrdinalIgnoreCase))
                    {
                        var dateText = line.Substring("Date: ".Length).Trim();
                        string format = "ddd MMM d HH:mm:ss yyyy zzz";

                        bool success = DateTimeOffset.TryParseExact(
                            dateText,
                            format,
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.None,
                            out DateTimeOffset result
                        );

                        if (success)
                        {
                            info.Date = result.Date;
                        }
                        else
                        {
                            info.Date = DateTime.MinValue;
                        }
                        continue;
                    }
                }

                if (!String.IsNullOrWhiteSpace(line))
                {
                    listComment.Add(line.Trim());
                }
            }


            info.Comment = String.Join(" ", listComment);
            return info;
        }


        public static IEnumerable<GitLogInfo> Parse(string text)
        {
            if (String.IsNullOrWhiteSpace(text))
                yield break;

            int previousIndex = 0;
            do
            {
                var currentIndex = text.IndexOf("\r\n\r\ncommit ", previousIndex + 1);
                if (currentIndex < 0)
                {
                    currentIndex = text.IndexOf("\n\ncommit ", previousIndex + 1);
                }
                if (currentIndex > previousIndex && previousIndex >= 0)
                {
                    var currentContent = text.Substring(previousIndex, currentIndex - previousIndex);
                    var instance = ParseInstance(currentContent);
                    if (instance != null)
                        yield return instance;
                }
                else
                {
                    var currentContent = text.Substring(previousIndex, text.Length - previousIndex);
                    var instance = ParseInstance(currentContent);
                    if (instance != null)
                        yield return instance;
                }

                previousIndex = currentIndex;
            } while (previousIndex >= 0);
        }
    }
}
