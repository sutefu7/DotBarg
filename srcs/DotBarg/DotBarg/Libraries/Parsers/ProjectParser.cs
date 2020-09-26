using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace DotBarg.Libraries.Parsers
{
    /// <summary>
    /// C# / VB 共通のプロジェクトファイルのパーサーです。
    /// </summary>
    public class ProjectParser
    {
        public ProjectInfo Parse(string slnFile, string prjFile)
        {
            if (!File.Exists(prjFile))
                throw new ArgumentException($"{prjFile} が見つかりません。");

            var info = new ProjectInfo
            {
                SolutionFile = slnFile,
                ProjectFile = prjFile,
                ProjectName = Path.GetFileNameWithoutExtension(prjFile),
                DefaultImportsNamespaces = new List<string>(),
                DefaultReferenceAssemblyNames = new List<ProjectReferenceInfo>(),
                ProjectReferenceAssemblyNames = new List<ProjectReferenceInfo>(),
                NugetReferenceAssemblyNames = new List<ProjectReferenceInfo>(),
                SourceFiles = new List<ProjectSourceInfo>(),
            };

            var folder = Path.GetDirectoryName(prjFile);
            var root = XElement.Load(prjFile);
            XNamespace ns = root.Attribute("xmlns").Value;
            
            SetLanguage(info, folder, root, ns);
            SetPlatform(info, folder, root, ns);
            SetTargetFrameworkVersion(info, folder, root, ns);
            SetAssemblyFile(info, folder, root, ns);

            // 取得順序の調整のため順番入れ替え
            SetRootNamespace(info, folder, root, ns);
            SetDocumentFile(info, folder, root, ns);
            SetOption4(info, folder, root, ns);
            SetDefaultImportNamespaces(info, folder, root, ns);
            SetDefaultReferenceAssemblyNames(info, folder, root, ns);
            SetProjectReferenceAssemblyNames(info, folder, root, ns);
            SetNugetReferenceAssemblyNames(info, folder, root, ns);
            SetSourceFiles(info, folder, root, ns);

            return info;
        }

        private void SetLanguage(ProjectInfo info, string folder, XElement root, XNamespace ns)
        {
            // ソースファイルが1つもないプロジェクトを想定して修正

            var value = Path.GetExtension(info.ProjectFile).ToLower();

            info.Languages = Languages.Unknown;
            if (value == ".csproj") info.Languages = Languages.CSharp;
            if (value == ".vbproj") info.Languages = Languages.VBNet;

            if (info.Languages == Languages.Unknown)
                throw new InvalidOperationException("無効な操作です。");



            //// 以下で頑張らなくても、プロジェクトファイルの拡張子見れば判断できた
            //// 直すかどうかは様子見する

            //// Compile タグで Include 属性があり、属性値に AssemblyInfo が含まれている
            //var value = root
            //    .Descendants(ns + "Compile")
            //    .Where(x => x.HasAttributes && x.Attributes("Include").Any())
            //    .Select(x => x.Attribute("Include").Value)
            //    .FirstOrDefault(x => x.Contains("AssemblyInfo"));

            //// 無い場合もあるみたいなので、何かのソースで確認する
            //if (string.IsNullOrEmpty(value))
            //{
            //    value = root
            //        .Descendants(ns + "Compile")
            //        .Where(x => x.HasAttributes && x.Attributes("Include").Any())
            //        .Select(x => x.Attribute("Include").Value)
            //        .FirstOrDefault();
            //}

            //value = Path.GetExtension(value).ToLower();

            //info.Languages = Languages.Unknown;
            //if (value == ".cs") info.Languages = Languages.CSharp;
            //if (value == ".vb") info.Languages = Languages.VBNet;
        }

        private void SetPlatform(ProjectInfo info, string folder, XElement root, XNamespace ns)
        {
            var outputType = root.Descendants(ns + "OutputType").FirstOrDefault().Value.ToLower();
            var isTestProject = root.Descendants(ns + "TestProjectType").Any();

            switch (outputType)
            {
                case "library":
                    if (isTestProject)
                        info.ApplicationPlatform = ApplicationPlatform.Test;
                    else
                        info.ApplicationPlatform = ApplicationPlatform.ClassLibrary;

                    return;

                case "exe":
                    info.ApplicationPlatform = ApplicationPlatform.Console;
                    return;

                case "module":
                    info.ApplicationPlatform = ApplicationPlatform.Module;
                    return;

                case "winexe":
                    // WinForms/WPF
                    info.ApplicationPlatform = ApplicationPlatform.Unknown;

                    // WinForms
                    // 参照dllに、System.Drawing, System.Windows.Forms がある
                    // Compile tag/SubType tag の値が Form
                    if (IsWinForms(info, folder, root, ns))
                        info.ApplicationPlatform = ApplicationPlatform.WinForms;

                    // WPF
                    // 参照dllに、WindowsBase, PresentationCore, PresentationFramework がある
                    // xaml ファイルがある
                    if (IsWpf(info, folder, root, ns))
                        info.ApplicationPlatform = ApplicationPlatform.Wpf;

                    return;

                default:
                    info.ApplicationPlatform = ApplicationPlatform.Unknown;
                    return;
            }
        }

        private bool IsWinForms(ProjectInfo info, string folder, XElement root, XNamespace ns)
        {
            // 参照dllに、System.Drawing, System.Windows.Forms がある
            var items = root
                .Descendants(ns + "Reference")
                .Where(x => x.HasAttributes && x.Attributes("Include").Any())
                .Select(x => x.Attribute("Include").Value)
                .ToList();

            var check1 = false;
            if (items.Contains("System.Drawing") && items.Contains("System.Windows.Forms"))
            {
                check1 = true;
            }

            // Compile タグで Include 属性があり、属性値に AssemblyInfo が含まれている
            var check2 = root
                .Descendants(ns + "Compile")
                .Where(x => x.HasElements && x.Elements(ns + "SubType").Any())
                .Select(x => x.Element(ns + "SubType"))
                .Any(x => x.Value == "Form");

            return check1 && check2;
        }

        private bool IsWpf(ProjectInfo info, string folder, XElement root, XNamespace ns)
        {
            // 参照dllに、WindowsBase, PresentationCore, PresentationFramework がある
            var items = root
                .Descendants(ns + "Reference")
                .Where(x => x.HasAttributes && x.Attributes("Include").Any())
                .Select(x => x.Attribute("Include").Value)
                .ToList();

            var check1 = false;
            if (items.Contains("WindowsBase") && items.Contains("PresentationCore") && items.Contains("PresentationFramework"))
            {
                check1 = true;
            }

            // xaml ファイルがある
            var check2 = root
                .Descendants(ns + "Page")
                .Where(x => x.HasAttributes && x.Attributes("Include").Any())
                .Select(x => x.Attribute("Include").Value)
                .Any(x => Path.GetExtension(x).ToLower() == ".xaml");

            return check1 && check2;
        }

        private void SetTargetFrameworkVersion(ProjectInfo info, string folder, XElement root, XNamespace ns)
        {
            var version = root.Descendants(ns + "TargetFrameworkVersion").FirstOrDefault()?.Value;

            // Visual Studio でコンバートした場合とか？は、タグが無いこともあるみたい。代わりに以下のタグがあるはず？
            if (string.IsNullOrEmpty(version))
                version = root.Descendants(ns + "OldToolsVersion").FirstOrDefault()?.Value;

            info.TargetFrameworkVersion = version;
        }

        private void SetAssemblyFile(ProjectInfo info, string folder, XElement root, XNamespace ns)
        {
            var assemblyName = root.Descendants(ns + "AssemblyName").FirstOrDefault().Value;
            var outputType = root.Descendants(ns + "OutputType").FirstOrDefault().Value.ToLower();
            var extension = ".dll";

            if (outputType == "exe" || outputType == "winexe")
            {
                extension = ".exe";
            }

            // bin/Debug, bin/x64/Debug, bin/x86/Debug, bin/Release, bin/x64/Release, bin/x86/Release, のパターンを取得
            var conditions = root
                .Descendants(ns + "PropertyGroup")
                .Where(x => x.HasAttributes && x.Attributes("Condition").Any())
                .Where(x => x.Attribute("Condition").Value.Contains("$(Configuration)|$(Platform)"));
            
            var outputPaths = new List<string>();
            foreach (var condition in conditions)
            {
                var outputPath = condition.Descendants(ns + "OutputPath").FirstOrDefault().Value;
                outputPaths.Add(outputPath);
            }

            // ビルド構成とプラットフォームの組み合わせパターンのうち、以下の優先度でアセンブリファイルのパスを取得
            var checkPaths = new List<string>
            {
                @"bin\Debug",
                @"bin\x64\Debug",
                @"bin\x86\Debug",
                @"bin\Release",
                @"bin\x64\Release",
                @"bin\x86\Release",
            };

            foreach (var checkPath in checkPaths)
            {
                if (outputPaths.Any(x => x.Contains(checkPath)))
                {
                    var outputPath = outputPaths.FirstOrDefault(x => x.Contains(checkPath));
                    outputPaths.Remove(outputPath);

                    var assemblyFile = Path.Combine(folder, outputPath, $"{assemblyName}{extension}");
                    info.AssemblyName = $"{assemblyName}{extension}";
                    info.AssemblyFile = assemblyFile;
                    return;
                }
            }
        }

        private void SetDocumentFile(ProjectInfo info, string folder, XElement root, XNamespace ns)
        {
            // bin/Debug, bin/x64/Debug, bin/x86/Debug, bin/Release, bin/x64/Release, bin/x86/Release, のパターンを取得
            var conditions = root
                .Descendants(ns + "PropertyGroup")
                .Where(x => x.HasAttributes && x.Attributes("Condition").Any())
                .Where(x => x.Attribute("Condition").Value.Contains("$(Configuration)|$(Platform)"));

            var outputPaths = new List<(string, string)>();
            foreach (var condition in conditions)
            {
                var outputPath = condition.Descendants(ns + "OutputPath").FirstOrDefault().Value;
                var documentName = condition.Descendants(ns + "DocumentationFile").FirstOrDefault()?.Value;

                if (string.IsNullOrEmpty(documentName))
                {
                    documentName = $"{info.RootNamespace}.xml";
                }

                outputPaths.Add((outputPath, documentName));
            }

            // ビルド構成とプラットフォームの組み合わせパターンのうち、以下の優先度でアセンブリファイルのパスを取得
            var checkPaths = new List<string>
            {
                @"bin\Debug",
                @"bin\x64\Debug",
                @"bin\x86\Debug",
                @"bin\Release",
                @"bin\x64\Release",
                @"bin\x86\Release",
            };

            foreach (var checkPath in checkPaths)
            {
                if (outputPaths.Any(x => x.Item1.Contains(checkPath)))
                {
                    var (outputPath, documentName) = outputPaths.FirstOrDefault(x => x.Item1.Contains(checkPath));
                    outputPaths.Remove((outputPath, documentName));

                    var documentFile = Path.Combine(folder, outputPath, documentName);
                    info.DocumentName = documentName;
                    info.DocumentFile = documentFile;
                    return;
                }
            }
        }

        private void SetRootNamespace(ProjectInfo info, string folder, XElement root, XNamespace ns)
        {
            var rootNamespace = root.Descendants(ns + "RootNamespace").FirstOrDefault().Value;
            info.RootNamespace = rootNamespace;
        }

        private void SetOption4(ProjectInfo info, string folder, XElement root, XNamespace ns)
        {
            if (info.Languages != Languages.VBNet)
                return;

            var value = root.Descendants(ns + "OptionExplicit").FirstOrDefault().Value;
            info.OptionExplicit = value;

            value = root.Descendants(ns + "OptionCompare").FirstOrDefault().Value;
            info.OptionCompare = value;

            value = root.Descendants(ns + "OptionStrict").FirstOrDefault().Value;
            info.OptionStrict = value;

            value = root.Descendants(ns + "OptionInfer").FirstOrDefault().Value;
            info.OptionInfer = value;
        }

        private void SetDefaultImportNamespaces(ProjectInfo info, string folder, XElement root, XNamespace ns)
        {
            if (info.Languages != Languages.VBNet)
                return;

            var imports = root
                .Descendants(ns + "Import")
                .Where(x => x.HasAttributes && x.Attributes("Include").Any());

            foreach (var import in imports)
            {
                var importName = import.Attribute("Include").Value;
                info.DefaultImportsNamespaces.Add(importName);
            }
        }

        private void SetDefaultReferenceAssemblyNames(ProjectInfo info, string folder, XElement root, XNamespace ns)
        {
            // テストプロジェクト（に限らずかもしれないが）の場合、参照 dll の読み込み有無を条件分岐して判定している場合がある
            // MSBuild で読み込み時に動的に判定して評価していると思われる
            // ここでは動的判定は行わない、つまりテストプロジェクトの場合、参照 dll はチェックしない
            if (info.ApplicationPlatform == ApplicationPlatform.Test)
                return;

            var references = root
                .Descendants(ns + "Reference")
                .Where(x => !x.HasElements && x.HasAttributes && x.Attributes("Include").Any())
                .Select(x => x.Attribute("Include").Value);

            foreach (var reference in references)
            {
                info.DefaultReferenceAssemblyNames.Add(new ProjectReferenceInfo 
                { 
                    AssemblyName = reference,
                    AssemblyFile = string.Empty,
                });
            }
        }

        private void SetProjectReferenceAssemblyNames(ProjectInfo info, string folder, XElement root, XNamespace ns)
        {
            if (info.ApplicationPlatform == ApplicationPlatform.Test)
                return;

            var references = root
                .Descendants(ns + "ProjectReference")
                .Where(x => x.HasAttributes && x.Attributes("Include").Any())
                .Select(x => x.Attribute("Include").Value);

            foreach (var reference in references)
            {
                var prjFile = GetFullPath(folder, reference);

                // 循環参照の可能性を心配したが、Visual Studio 上では仕様的に不可能（エラーメッセージ表示）のため、大丈夫と判断
                var parser = new ProjectParser();
                var pi = parser.Parse(info.SolutionFile, prjFile);

                info.ProjectReferenceAssemblyNames.Add(new ProjectReferenceInfo
                {
                    AssemblyName = Path.GetFileNameWithoutExtension(pi.AssemblyFile),
                    AssemblyFile = pi.AssemblyFile,
                });
            }
        }

        // 絶対パスのディレクトリ、相対パスのファイル
        private string GetFullPath(string folder, string relativeFile)
        {
            if (!folder.EndsWith(@"\"))
            {
                folder += @"\";
            }

            var uri1 = new Uri(folder);
            var uri2 = new Uri(uri1, relativeFile);

            return uri2.LocalPath;
        }

        private void SetNugetReferenceAssemblyNames(ProjectInfo info, string folder, XElement root, XNamespace ns)
        {
            if (info.ApplicationPlatform == ApplicationPlatform.Test)
                return;

            var references = root
                .Descendants(ns + "Reference")
                .Where(x => x.HasElements && x.Elements(ns + "HintPath").Any() && x.HasAttributes && x.Attributes("Include").Any());

            foreach (var reference in references)
            {
                // <Reference Include="System.ValueTuple, Version=4.0.1.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51, processorArchitecture=MSIL">
                //   <HintPath>..\packages\System.ValueTuple.4.3.0\lib\netstandard1.0\System.ValueTuple.dll</HintPath>
                //   <Private>True</Private>
                // </Reference>
                var assemblyName = reference.Attribute("Include").Value;
                if (assemblyName.Contains(","))
                    assemblyName = assemblyName.Substring(0, assemblyName.IndexOf(","));

                var hintPath = reference.Element(ns + "HintPath").Value;
                var assemblyFile = GetFullPath(folder, hintPath);

                info.NugetReferenceAssemblyNames.Add(new ProjectReferenceInfo
                {
                    AssemblyName = assemblyName,
                    AssemblyFile = assemblyFile,
                });
            }
        }

        private void SetSourceFiles(ProjectInfo info, string folder, XElement root, XNamespace ns)
        {
            var compiles = root
                .Descendants(ns + "Compile")
                .Where(x => x.HasAttributes && x.Attributes("Include").Any());

            foreach (var compile in compiles)
            {
                var sourceName = compile.Attribute("Include").Value;
                var sourceFile = GetFullPath(folder, sourceName);
                var sourceFolder = Path.GetDirectoryName(sourceFile);

                var designerFile = string.Empty;
                var resxFile = string.Empty;
                var xamlFile = string.Empty;

                // C#: Properties, VB: My Project フォルダ以下にあるソースファイルは自動生成系なので無視
                if (sourceName.Contains("Properties\\") || sourceName.Contains("My Project\\"))
                    continue;

                // WinForms: xxx.Designer.xx の場合は無視（対応ソース側でチェックするため）
                if (sourceName.Contains(".Designer."))
                    continue;

                // WinForms のソースコードの場合
                if (compile.HasElements && compile.Elements(ns + "SubType").Any() && compile.Element(ns + "SubType").Value == "Form")
                {
                    var dummyName = Path.GetFileName(sourceFile);
                    var dependentUpons = root
                        .Descendants(ns + "DependentUpon")
                        .Where(x => x.Value == dummyName);

                    foreach (var dependentUpon in dependentUpons)
                    {
                        // デザイナーファイル
                        if (dependentUpon.Parent.Name == (ns + "Compile"))
                        {
                            designerFile = dependentUpon.Parent.Attribute("Include").Value;
                            designerFile = GetFullPath(folder, designerFile);
                        }

                        // リソースファイル
                        if (dependentUpon.Parent.Name == (ns + "EmbeddedResource"))
                        {
                            resxFile = dependentUpon.Parent.Attribute("Include").Value;
                            resxFile = GetFullPath(folder, resxFile);
                        }
                    }
                }

                // WPF のコードビハインドの場合
                if (compile.HasElements && compile.Elements(ns + "DependentUpon").Any() && compile.Element(ns + "DependentUpon").Value.ToLower().EndsWith(".xaml"))
                {
                    xamlFile = compile.Element(ns + "DependentUpon").Value;
                    xamlFile = Path.Combine(sourceFolder, xamlFile);
                }

                info.SourceFiles.Add(new ProjectSourceInfo
                {
                    SourceFile = sourceFile,
                    DesignerFile = designerFile,
                    ResourceFile = resxFile,
                    XamlFile = xamlFile,
                    SourceName = Path.GetFileName(sourceFile),
                    DesignerName = Path.GetFileName(designerFile),
                    ResourceName = Path.GetFileName(resxFile),
                    XamlName = Path.GetFileName(xamlFile),
                });
            }
        }
    }

    public enum ApplicationPlatform
    {
        Unknown,
        Console,
        ClassLibrary,
        Module,
        WinForms,
        Wpf,
        Test,
    }

    [DebuggerDisplay("{Languages} : {ProjectName}")]
    public class ProjectInfo
    {
        public string SolutionFile { get; set; }

        public string ProjectFile { get; set; }

        public string ProjectName { get; set; }

        public Languages Languages { get; set; }

        public ApplicationPlatform ApplicationPlatform { get; set; }

        public string TargetFrameworkVersion { get; set; }

        public string AssemblyFile { get; set; }

        public string AssemblyName { get; set; }

        public string DocumentFile { get; set; }

        public string DocumentName { get; set; }

        public string RootNamespace { get; set; }

        // VB のみ
        public string OptionExplicit { get; set; }

        // VB のみ
        public string OptionCompare { get; set; }

        // VB のみ
        public string OptionStrict { get; set; }

        // VB のみ
        public string OptionInfer { get; set; }

        // VB のみ、ソースコードに未記載でも、自動適応される名前空間の省略機能
        public List<string> DefaultImportsNamespaces { get; set; }

        public List<ProjectReferenceInfo> DefaultReferenceAssemblyNames { get; set; }

        public List<ProjectReferenceInfo> ProjectReferenceAssemblyNames { get; set; }

        public List<ProjectReferenceInfo> NugetReferenceAssemblyNames { get; set; }

        public List<ProjectSourceInfo> SourceFiles { get; set; }
    }

    [DebuggerDisplay("{SourceName}")]
    public class ProjectSourceInfo
    {
        public string SourceFile { get; set; }

        public string DesignerFile { get; set; }

        public string ResourceFile { get; set; }

        public string XamlFile { get; set; }

        public string SourceName { get; set; }

        public string DesignerName { get; set; }

        public string ResourceName { get; set; }

        public string XamlName { get; set; }

        public bool HasDesignerFile
        {
            get { return !string.IsNullOrEmpty(DesignerFile); }
        }

        public bool HasResourceFile
        {
            get { return !string.IsNullOrEmpty(ResourceFile); }
        }

        public bool HasXamlFile
        {
            get { return !string.IsNullOrEmpty(XamlFile); }
        }

        public bool IsGUIType
        {
            get
            {
                return (HasDesignerFile || HasResourceFile || HasXamlFile);
            }
        }
    }

    [DebuggerDisplay("{AssemblyName}")]
    public class ProjectReferenceInfo
    {
        public string AssemblyName { get; set; }

        public string AssemblyFile { get; set; }
    }
}
