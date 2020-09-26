using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace DotBarg.Libraries.Parsers
{
    /// <summary>
    /// C# / VB 共通のソリューションファイルのパーサーです。
    /// </summary>
    public class SolutionParser
    {
        public SolutionInfo Parse(string slnFile)
        {
            if (!File.Exists(slnFile))
                throw new ArgumentException($"{slnFile} が見つかりません。");

            var info = new SolutionInfo 
            { 
                SolutionFile = slnFile,
                SolutionName = Path.GetFileNameWithoutExtension(slnFile),
                ProjectFiles = new List<SolutionProjectInfo>(),
            };

            var lines = File.ReadLines(slnFile, Util.GetEncoding(slnFile));
            foreach (var line in lines)
            {
                // Microsoft Visual Studio Solution File, Format Version 12.00
                if (line.StartsWith("Microsoft Visual Studio Solution File, Format Version "))
                {
                    info.FormatVersion = line.Replace("Microsoft Visual Studio Solution File, Format Version ", string.Empty).Trim();
                }

                // VisualStudioVersion = 16.0.30406.217
                if (line.StartsWith("VisualStudioVersion = "))
                {
                    info.VisualStudioVersion = line.Replace("VisualStudioVersion = ", string.Empty).Trim();
                }

                // MinimumVisualStudioVersion = 10.0.40219.1
                if (line.StartsWith("MinimumVisualStudioVersion = "))
                {
                    info.VisualStudioVersion = line.Replace("MinimumVisualStudioVersion = ", string.Empty).Trim();
                }

                // Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "ConsoleApp8", "ConsoleApp8\ConsoleApp8.csproj", "{D6065933-C85B-4AB8-9CE9-9163154C4347}"
                // の左辺の文字列
                if (line.StartsWith("Project(") && string.IsNullOrEmpty(info.SolutionID))
                {
                    var values = line.Split('=');
                    var value = values[0];
                    value = value.Replace(@"Project(""", string.Empty).Replace(@""")", string.Empty).Trim();
                    info.SolutionID = value;
                }

                // Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "ConsoleApp8", "ConsoleApp8\ConsoleApp8.csproj", "{D6065933-C85B-4AB8-9CE9-9163154C4347}"
                // の右辺の文字列３つ
                if (line.StartsWith("Project("))
                {
                    var folder = Path.GetDirectoryName(slnFile);
                    var value = line.Split('=')[1];
                    var values = value.Split(',');

                    var pi = new SolutionProjectInfo();
                    value = values[0];
                    value = value.Replace(@"""", string.Empty).Trim();
                    pi.ProjectName = value;

                    value = values[1];
                    value = value.Replace(@"""", string.Empty).Trim();
                    value = Path.Combine(folder, value);
                    pi.ProjectFile = value;

                    value = values[2];
                    value = value.Replace(@"""", string.Empty).Trim();
                    pi.ProjectID = value;

                    info.ProjectFiles.Add(pi);
                }
            }

            return info;
        }
    }

    public class SolutionInfo
    {
        public string SolutionID { get; set; }

        public string SolutionName { get; set; }

        public string SolutionFile { get; set; }

        public string FormatVersion { get; set; }

        public string VisualStudioVersion { get; set; }

        public string MinimumVisualStudioVersion { get; set; }

        public List<SolutionProjectInfo> ProjectFiles { get; set; }
    }

    [DebuggerDisplay("{ProjectName}")]
    public class SolutionProjectInfo
    {
        public string ProjectID { get; set; }

        public string ProjectName { get; set; }

        public string ProjectFile { get; set; }
    }
}
