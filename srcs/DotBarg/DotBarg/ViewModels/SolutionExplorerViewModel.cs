using DotBarg.Libraries;
using DotBarg.Libraries.DBs;
using DotBarg.Libraries.Parsers;
using DotBarg.Libraries.Roslyns;
using DotBarg.Models;
using Livet;
using Livet.Commands;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.VisualBasic;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Threading;

namespace DotBarg.ViewModels
{
    public class SolutionExplorerViewModel : AnchorablePaneViewModel
    {
        #region フィールド、プロパティ


        private string SolutionFile { get; set; } = string.Empty;


        #endregion

        #region 変更通知プロパティ


        // ソリューションエクスプローラーペインは、１つだけなので固定文字列にする
        // ※ドキュメントペインのような、複数表示の場合は識別できないので固定文字列はダメ

        public override string Title
        {
            get { return "ソリューション エクスプローラー"; }
        }

        public override string ContentId
        {
            get { return "SolutionExplorer"; }
        }

        private ObservableCollection<TreeViewItemModel> _Items;
        public ObservableCollection<TreeViewItemModel> Items
        {
            get { return _Items; }
            set { RaisePropertyChangedIfSet(ref _Items, value); }
        }

        private TreeViewItemModel _SelectedItem;
        public TreeViewItemModel SelectedItem
        {
            get { return _SelectedItem; }
            set { RaisePropertyChangedIfSet(ref _SelectedItem, value); }
        }


        #endregion

        #region コンストラクタ


        public SolutionExplorerViewModel()
        {
            Items = new ObservableCollection<TreeViewItemModel>();
            BindingOperations.EnableCollectionSynchronization(Items, new object());
        }


        #endregion

        #region ソリューションエクスプローラーペイン / ツリーのデータ表示


        public async Task ShowDataAsync(string solutionFile)
        {
            SolutionFile = solutionFile;

            using (var vm = new ProgressViewModel())
            {
                vm.DoWorkAsync = DoWorkAsync;
                await this.ShowDialogAsync(vm, "ShowProgressView");
            }
        }

        // 進捗画面側で実行してもらう処理
        private async Task DoWorkAsync()
        {
            // ソースコードをRoslynに渡す、ソースコードをDBに入れる
            await Task.Run(async () =>
            {
                var slnParser = new SolutionParser();
                var prjParser = new ProjectParser();

                // ソリューションファイル
                var slnInfo = slnParser.Parse(SolutionFile);
                if (AppEnv.SolutionInfos.Any(x => x.SolutionFile == SolutionFile))
                    return;

                AppEnv.SolutionInfos.Add(slnInfo);

                foreach (var prjRefInfo in slnInfo.ProjectFiles)
                {
                    // プロジェクトファイル
                    var prjInfo = prjParser.Parse(SolutionFile, prjRefInfo.ProjectFile);
                    if (AppEnv.ProjectInfos.Any(x => x.SolutionFile == prjInfo.SolutionFile && x.ProjectFile == prjInfo.ProjectFile))
                        continue;

                    AppEnv.ProjectInfos.Add(prjInfo);

                    // Roslyn 解析に必要なデータ系
                    //Microsoft.CodeAnalysis.ProjectInfo と DotBarg.Libraries.Parsers.ProjectInfo が同じクラス名のため、こちらはフルパスで書く
                    var dllItems = new List<Microsoft.CodeAnalysis.MetadataReference>();
                    var mscorlib = Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location);
                    dllItems.Add(mscorlib);

                    var csSrcItems = new List<Microsoft.CodeAnalysis.SyntaxTree>();
                    var vbSrcItems = new List<Microsoft.CodeAnalysis.SyntaxTree>();

                    // ソースファイル
                    var srcInfos = prjInfo.SourceFiles;
                    foreach (var srcInfo in srcInfos)
                    {
                        var contents = await Util.ReadToEndAsync(srcInfo.SourceFile);
                        
                        switch (prjInfo.Languages)
                        {
                            case Languages.CSharp:

                                var csTree = CSharpSyntaxTree.ParseText(text: contents, path: srcInfo.SourceFile);
                                var csWalker = new CSharpSourceSyntaxWalker();
                                csWalker.Parse(srcInfo.SourceFile, contents, prjInfo.RootNamespace);

                                if (csWalker.UserDefinitions.Any())
                                    AppEnv.UserDefinitions.AddRange(csWalker.UserDefinitions.ToList());

                                csSrcItems.Add(csTree);

                                if (srcInfo.HasDesignerFile)
                                {
                                    contents = await Util.ReadToEndAsync(srcInfo.DesignerFile);
                                    csTree = CSharpSyntaxTree.ParseText(text: contents, path: srcInfo.DesignerFile);
                                    csWalker = new CSharpSourceSyntaxWalker();
                                    csWalker.Parse(srcInfo.DesignerFile, contents, prjInfo.RootNamespace);

                                    if (csWalker.UserDefinitions.Any())
                                        AppEnv.UserDefinitions.AddRange(csWalker.UserDefinitions.ToList());

                                    csSrcItems.Add(csTree);
                                }

                                break;

                            case Languages.VBNet:

                                var vbTree = VisualBasicSyntaxTree.ParseText(text: contents, path: srcInfo.SourceFile);
                                var vbWalker = new VBNetSourceSyntaxWalker();
                                vbWalker.Parse(srcInfo.SourceFile, contents, prjInfo.RootNamespace);

                                if (vbWalker.UserDefinitions.Any())
                                    AppEnv.UserDefinitions.AddRange(vbWalker.UserDefinitions.ToList());

                                vbSrcItems.Add(vbTree);

                                if (srcInfo.HasDesignerFile)
                                {
                                    contents = await Util.ReadToEndAsync(srcInfo.DesignerFile);
                                    vbTree = VisualBasicSyntaxTree.ParseText(text: contents, path: srcInfo.DesignerFile);
                                    vbWalker = new VBNetSourceSyntaxWalker();
                                    vbWalker.Parse(srcInfo.DesignerFile, contents, prjInfo.RootNamespace);

                                    if (vbWalker.UserDefinitions.Any())
                                        AppEnv.UserDefinitions.AddRange(vbWalker.UserDefinitions.ToList());

                                    vbSrcItems.Add(vbTree);
                                }

                                break;
                        }
                    }

                    if (csSrcItems.Any())
                    {
                        var comp = CSharpCompilation.Create(prjInfo.AssemblyName, csSrcItems, dllItems);

                        AppEnv.CSharpCompilations.Add(comp);
                        AppEnv.CSharpSyntaxTrees.AddRange(csSrcItems);
                    }

                    if (vbSrcItems.Any())
                    {
                        var kinds = Path.GetExtension(prjInfo.AssemblyFile).ToLower() == ".exe" ? Microsoft.CodeAnalysis.OutputKind.ConsoleApplication : Microsoft.CodeAnalysis.OutputKind.DynamicallyLinkedLibrary;
                        var options = new VisualBasicCompilationOptions(outputKind: kinds, rootNamespace: prjInfo.RootNamespace);
                        var comp = VisualBasicCompilation.Create(prjInfo.AssemblyName, vbSrcItems, dllItems, options);

                        AppEnv.VisualBasicCompilations.Add(comp);
                        AppEnv.VisualBasicSyntaxTrees.AddRange(vbSrcItems);
                    }
                }
            });

            // 上記で登録したDB内容をもとに、継承元クラス、インターフェースの定義元を名前解決する
            // ツリーノードのモデルを作成する
            await Task.Run(() =>
            {
                var containers = AppEnv.UserDefinitions.Where(x => x.BaseTypeInfos.Any());
                if (containers.Any())
                    FindDefinitionInfo(containers);

                var model = CreateTreeData(SolutionFile);
                Items.Add(model);
            });
        }

        private void FindDefinitionInfo(IEnumerable<UserDefinition> containers)
        {
            // class, struct, interface
            foreach (var container in containers)
            {
                // 継承元クラス、インターフェース
                foreach (var baseType in container.BaseTypeInfos)
                {
                    // すでに定義元が判明している場合は、飛ばす
                    if (baseType.DefinitionStartLength != -1)
                        continue;

                    // １つ分について、可能性のある名前空間
                    foreach (var defineFullName in baseType.CandidatesDefineFullNames)
                    {
                        var defineName = defineFullName.Substring(defineFullName.LastIndexOf(".") + 1);

                        if (defineName.Contains("<"))
                        {
                            // 継承元クラス、またはインターフェースはジェネリック型
                            // クローズドジェネリック形式で書いている定義名を、オープンジェネリック形式で見つけることはできない（文字列一致できない）
                            // class Class1 : IEnumerable<int>  ... クローズドジェネリック型
                            // interface IEnumerable<T>         ... オープンジェネリック型
                            // → ジェネリック型の個数一致で判断する

                            var closedCount = CountGenericType(defineFullName);
                            var partialName = defineFullName.Substring(0, defineFullName.IndexOf("<") + 1);

                            // "IEnumerable<int>".StartsWith("IEnumerable<")
                            if (AppEnv.UserDefinitions.Any(x => x.DefineFullName.StartsWith(partialName)))
                            {
                                var items = AppEnv.UserDefinitions.Where(x => x.DefineFullName.StartsWith(partialName));
                                foreach (var item in items)
                                {
                                    var openCount = CountGenericType(item.DefineName);
                                    if (openCount == closedCount)
                                    {
                                        baseType.DefinitionSourceFile = item.SourceFile;
                                        baseType.DefinitionStartLength = item.StartLength;
                                        baseType.DefinitionEndLength = item.EndLength;

                                        // 名前空間の解決が正しくないバグの対応
                                        // 見つけたら候補探しを止める
                                        break;
                                    }
                                }

                                // 名前空間の解決が正しくないバグの対応
                                // 見つけたら候補探しを止める
                                if (baseType.DefinitionStartLength != -1)
                                    break;
                            }
                        }
                        else
                        {
                            // 非ジェネリック型

                            if (AppEnv.UserDefinitions.Any(x => x.DefineFullName == defineFullName))
                            {
                                var foundItem = AppEnv.UserDefinitions.FirstOrDefault(x => x.DefineFullName == defineFullName);
                                baseType.DefinitionSourceFile = foundItem.SourceFile;
                                baseType.DefinitionStartLength = foundItem.StartLength;
                                baseType.DefinitionEndLength = foundItem.EndLength;

                                // 名前空間の解決が正しくないバグの対応
                                // 見つけたら候補探しを止める
                                break;
                            }
                        }
                    }
                }
            }
        }

        private int CountGenericType(string value)
        {
            // 以下みたいな再帰の <> があっても誤判定しないようにする
            // class Class1 : Base<Dictionary<int, int>, Dictionary<string, string>>

            var firstIndex = value.IndexOf("<");
            var lastIndex = value.LastIndexOf(">");

            // Dictionary<int, int>, Dictionary<string, string>
            var tmp = value.Substring(0, lastIndex);
            tmp = tmp.Substring(firstIndex + 1);

            // 各型内にあるカンマは塗りつぶす（判定の邪魔なので）
            // Dictionary<int, int>, Dictionary<string, string>
            // ↓
            // Dictionary__changed__, Dictionary__changed__
            //
            // Dictionary<int, Dictionary<int, int>>, Dictionary<string, Dictionary<int, string>>
            // ↓
            // Dictionary__changed__, Dictionary__changed__

            var sb = new StringBuilder();
            var inParen = false;
            
            // <> ... ジェネリック型を想定
            var startParenCount = 0;
            var endParenCount = 0;
            
            // [] ... 配列型を想定
            var startBraceCount = 0;
            var endBraceCount = 0;

            foreach (var ch in tmp)
            {
                switch (ch)
                {
                    case '<':
                        inParen = true;
                        startParenCount++;
                        break;

                    case '>':
                        endParenCount++;

                        if (startParenCount != 0 && startParenCount == endParenCount)
                        {
                            sb.Append("__changed__");

                            inParen = false;
                            startParenCount = 0;
                            endParenCount = 0;
                        }

                        break;

                    case '[':

                        // カッコチェックの優先度は <> の後に [] となるので、<> のチェックが始まっているときは何もしない
                        if (startParenCount == 0)
                        {
                            inParen = true;
                            startBraceCount++;
                        }

                        break;

                    case ']':

                        // カッコチェックの優先度は <> の後に [] となるので、<> のチェックが始まっているときは何もしない
                        if (startParenCount == 0)
                        {
                            endBraceCount++;

                            if (startBraceCount != 0 && startBraceCount == endBraceCount)
                            {
                                sb.Append("__changed__");

                                inParen = false;
                                startBraceCount = 0;
                                endBraceCount = 0;
                            }
                        }

                        break;

                    default:

                        if (!inParen)
                            sb.Append(ch);

                        break;
                }
            }

            tmp = sb.ToString();
            var values = tmp.Split(',');
            return values.Length;
        }

        private TreeViewItemModel CreateTreeData(string solutionFile)
        {
            // ソリューションファイル
            var slnInfo = AppEnv.SolutionInfos.FirstOrDefault(x => x.SolutionFile == solutionFile);
            var slnModel = new TreeViewItemModel 
            { 
                Text = slnInfo.SolutionName, 
                FileName = slnInfo.SolutionFile,
                TreeNodeKinds = TreeNodeKinds.SolutionFile,
                IsExpanded = true,
            };

            var prjInfos = AppEnv.ProjectInfos.Where(x => x.SolutionFile == solutionFile);
            foreach (var prjInfo in prjInfos)
            {
                // プロジェクトファイル
                var prjModel = new TreeViewItemModel
                {
                    Text = prjInfo.ProjectName,
                    FileName = prjInfo.ProjectFile,
                    TreeNodeKinds = GetProjectLanguages(prjInfo),
                    IsExpanded = true,
                };
                slnModel.Children.Add(prjModel);

                // 参照 dll
                var refModel = new TreeViewItemModel
                {
                    Text = "参照",
                    TreeNodeKinds = TreeNodeKinds.Dependency,
                };
                prjModel.Children.Add(refModel);

                foreach (var refInfo in prjInfo.DefaultReferenceAssemblyNames)
                {
                    var refItemModel = new TreeViewItemModel
                    {
                        Text = refInfo.AssemblyName,
                        TreeNodeKinds = TreeNodeKinds.Dependency,
                    };
                    refModel.Children.Add(refItemModel);
                }

                // NuGet 参照 dll
                if (prjInfo.NugetReferenceAssemblyNames.Any())
                {
                    var nugetModel = new TreeViewItemModel
                    {
                        Text = "NuGet 参照",
                        TreeNodeKinds = TreeNodeKinds.Dependency,
                    };
                    prjModel.Children.Add(nugetModel);

                    foreach (var nugetInfo in prjInfo.NugetReferenceAssemblyNames)
                    {
                        var nugetItemModel = new TreeViewItemModel
                        {
                            Text = nugetInfo.AssemblyName,
                            TreeNodeKinds = TreeNodeKinds.Dependency,
                        };
                        nugetModel.Children.Add(nugetItemModel);
                    }
                }

                // プロジェクト参照 dll
                if (prjInfo.ProjectReferenceAssemblyNames.Any())
                {
                    var prjRefModel = new TreeViewItemModel
                    {
                        Text = "プロジェクト参照",
                        TreeNodeKinds = TreeNodeKinds.Dependency,
                    };
                    prjModel.Children.Add(prjRefModel);

                    foreach (var prjRefInfo in prjInfo.ProjectReferenceAssemblyNames)
                    {
                        var prjRefItemModel = new TreeViewItemModel
                        {
                            Text = prjRefInfo.AssemblyName,
                            TreeNodeKinds = TreeNodeKinds.Dependency,
                        };
                        prjRefModel.Children.Add(prjRefItemModel);
                    }
                }

                // (VBNet のみ) 自動Imports
                if (prjInfo.Languages == Languages.VBNet && prjInfo.DefaultImportsNamespaces.Any())
                {
                    var importsModel = new TreeViewItemModel
                    {
                        Text = "自動 Imports",
                        TreeNodeKinds = TreeNodeKinds.Namespace,
                    };
                    prjModel.Children.Add(importsModel);

                    foreach (var ns in prjInfo.DefaultImportsNamespaces)
                    {
                        var importsItemModel = new TreeViewItemModel
                        {
                            Text = ns,
                            TreeNodeKinds = TreeNodeKinds.Namespace,
                        };
                        importsModel.Children.Add(importsItemModel);
                    }
                }

                // ソースファイル
                // WPF, WinForms や Control など、デザイナーファイルとソースファイルのペアの場合がある。
                // この場合、ソースファイルを登録しつつ、関連ファイルをその下に登録する。
                //（xaml や Form をデザイン表示する機能は本ツールには無いため、ただの情報提供のみの位置づけ）
                if (prjInfo.SourceFiles.Any())
                {
                    foreach (var srcInfo in prjInfo.SourceFiles)
                    {
                        var srcHeaderModel = new TreeViewItemModel 
                        { 
                            Text = srcInfo.SourceName, 
                            FileName = srcInfo.SourceFile,
                            TreeNodeKinds = srcInfo.IsGUIType ? GetSourceLanguages(prjInfo, true) : GetSourceLanguages(prjInfo, false),
                        };

                        if (srcInfo.IsGUIType)
                        {
                            // ソースファイル
                            var srcModel = new TreeViewItemModel
                            {
                                Text = srcInfo.SourceName,
                                FileName = srcInfo.SourceFile,
                                TreeNodeKinds = GetSourceLanguages(prjInfo, false),
                            };
                            srcHeaderModel.Children.Add(srcModel);

                            // デザイナーファイル
                            if (srcInfo.HasDesignerFile)
                            {
                                var designerModel = new TreeViewItemModel
                                {
                                    Text = srcInfo.DesignerName,
                                    FileName = srcInfo.DesignerFile,
                                    TreeNodeKinds = TreeNodeKinds.GeneratedFile,
                                };
                                srcHeaderModel.Children.Add(designerModel);
                            }

                            // リソースファイル
                            if (srcInfo.HasResourceFile)
                            {
                                var resModel = new TreeViewItemModel
                                {
                                    Text = srcInfo.ResourceName,
                                    FileName = srcInfo.ResourceFile,
                                    TreeNodeKinds = TreeNodeKinds.None,
                                };
                                srcHeaderModel.Children.Add(resModel);
                            }

                            // xaml ファイル
                            if (srcInfo.HasXamlFile)
                            {
                                var xamlModel = new TreeViewItemModel
                                {
                                    Text = srcInfo.XamlName,
                                    FileName = srcInfo.XamlFile,
                                    TreeNodeKinds = TreeNodeKinds.None,
                                };
                                srcHeaderModel.Children.Add(xamlModel);
                            }
                        }

                        // サブフォルダを作成している場合、サブフォルダ数分ノード階層を挟む
                        var prjDir = Path.GetDirectoryName(prjInfo.ProjectFile);
                        var srcDir = Path.GetDirectoryName(srcInfo.SourceFile);
                        
                        // Views
                        // Views\Controls
                        var difDir = srcDir.Replace(prjDir, string.Empty);

                        // 差分が無ければ、プロジェクトノード直下に登録
                        if (string.IsNullOrEmpty(difDir))
                        {
                            prjModel.Children.Add(srcHeaderModel);
                            continue;
                        }

                        // 差分があれば、フォルダを再帰作成
                        var subDirs = difDir.Split(new string[] { @"\" }, StringSplitOptions.RemoveEmptyEntries);
                        var parentModel = default(TreeViewItemModel);
                        var currentModel = default(TreeViewItemModel);

                        var i = 0;
                        var subDir = subDirs[i++];

                        // これから作成しようとしているフォルダが、無ければ作成して登録、
                        // すでに登録されている場合、そのノードインスタンスを取得
                        if (prjModel.Children.Any(x => x.Text == subDir))
                        {
                            parentModel = prjModel.Children.FirstOrDefault(x => x.Text == subDir);
                        }
                        else
                        {
                            parentModel = new TreeViewItemModel { Text = subDir, TreeNodeKinds = TreeNodeKinds.Folder };
                            prjModel.Children.Add(parentModel);
                        }

                        // サブフォルダがある分だけ、繰り返す
                        while (i < subDirs.Length)
                        {
                            subDir = subDirs[i++];

                            if (parentModel.Children.Any(x => x.Text == subDir))
                            {
                                currentModel = parentModel.Children.FirstOrDefault(x => x.Text == subDir);
                            }
                            else
                            {
                                currentModel = new TreeViewItemModel { Text = subDir, TreeNodeKinds = TreeNodeKinds.Folder };
                                parentModel.Children.Add(currentModel);
                            }

                            // 現在のフォルダを親フォルダに変えて、再帰
                            parentModel = currentModel;
                        }

                        parentModel.Children.Add(srcHeaderModel);
                    }
                }

            }

            return slnModel;
        }

        private TreeNodeKinds GetProjectLanguages(ProjectInfo info)
        {
            var result = TreeNodeKinds.None;

            switch (info.Languages)
            {
                case Languages.CSharp:
                    result = TreeNodeKinds.CSharpProjectFile;
                    break;

                case Languages.VBNet:
                    result = TreeNodeKinds.VBNetProjectFile;
                    break;
            }

            return result;
        }

        private TreeNodeKinds GetSourceLanguages(ProjectInfo info, bool isHeader)
        {
            var result = TreeNodeKinds.None;

            switch (info.Languages)
            {
                case Languages.CSharp:
                    result = isHeader ? TreeNodeKinds.CSharpSourceFileForHeader : TreeNodeKinds.CSharpSourceFile;
                    break;

                case Languages.VBNet:
                    result = isHeader ? TreeNodeKinds.VBNetSourceFileForHeader : TreeNodeKinds.VBNetSourceFile;
                    break;
            }

            return result;
        }


        #endregion

    }
}
