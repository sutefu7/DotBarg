using DotBarg.Libraries;
using DotBarg.Libraries.DBs;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.VisualBasic;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows.Documents;

namespace DotBarg.Libraries
{
    public class AppEnv
    {
        #region アプリケーションに関する特殊フォルダのパス

        public class IO
        {
            private static string _ExeFile;

            /// <summary>
            /// exe ファイルのパスを取得します。
            /// </summary>
            public static string ExeFile
            {
                get
                {
                    if (string.IsNullOrEmpty(_ExeFile))
                    {
                        _ExeFile = Assembly.GetEntryAssembly().Location;
                    }

                    return _ExeFile;
                }
            }

            private static string _ExeFolder;

            /// <summary>
            /// exe ファイルがあるフォルダのパスを取得します。
            /// </summary>
            public static string ExeFolder
            {
                get
                {
                    if (string.IsNullOrEmpty(_ExeFolder))
                    {
                        _ExeFolder = Path.GetDirectoryName(ExeFile);
                    }

                    return _ExeFolder;
                }
            }

            private static string _LogFolder;

            /// <summary>
            /// Log フォルダのパスを取得します。
            /// </summary>
            public static string LogFolder
            {
                get
                {
                    if (string.IsNullOrEmpty(_LogFolder))
                    {
                        _LogFolder = Path.Combine(ExeFolder, "Log");

                        if (!Directory.Exists(_LogFolder))
                            Directory.CreateDirectory(_LogFolder);
                    }

                    return _LogFolder;
                }
            }

            private static string _TempFolder;

            /// <summary>
            /// Temp フォルダのパスを取得します。
            /// </summary>
            public static string TempFolder
            {
                get
                {
                    if (string.IsNullOrEmpty(_TempFolder))
                    {
                        _TempFolder = Path.Combine(ExeFolder, "Temp");

                        if (!Directory.Exists(_TempFolder))
                            Directory.CreateDirectory(_TempFolder);
                    }

                    return _TempFolder;
                }
            }

            private static string _SystemFolder;

            /// <summary>
            /// System フォルダのパスを取得します。
            /// </summary>
            public static string SystemFolder
            {
                get
                {
                    if (string.IsNullOrEmpty(_SystemFolder))
                    {
                        _SystemFolder = Path.Combine(ExeFolder, "System");

                        if (!Directory.Exists(_SystemFolder))
                            Directory.CreateDirectory(_SystemFolder);
                    }

                    return _SystemFolder;
                }
            }
        }


        #endregion

        #region DB


        // 表示したソースコードが何言語かを取得、または設定します。
        public static Languages Languages { get; set; } = Languages.Unknown;

        // 

        public static List<Parsers.SolutionInfo> SolutionInfos { get; set; } = new List<Parsers.SolutionInfo>();

        public static List<Parsers.ProjectInfo> ProjectInfos { get; set; } = new List<Parsers.ProjectInfo>();

        // DB 内では、C# 言語ベースで管理します。VBNet の場合は、Languages を見て表示用に変換します。

        public static List<UserDefinition> UserDefinitions { get; set; } = new List<UserDefinition>();

        public static List<LanguageConversion> LanguageConversions { get; set; } = LanguageConversion.InitializeItems();

        #endregion

        #region Roslyn


        public static List<SyntaxTree> CSharpSyntaxTrees { get; set; } = new List<SyntaxTree>();

        public static List<SyntaxTree> VisualBasicSyntaxTrees { get; set; } = new List<SyntaxTree>();

        public static List<CSharpCompilation> CSharpCompilations { get; set; } = new List<CSharpCompilation>();

        public static List<VisualBasicCompilation> VisualBasicCompilations { get; set; } = new List<VisualBasicCompilation>();


        #endregion

        #region アプリの設定


        public static double FontSize = 18;


        #endregion

    }
}
