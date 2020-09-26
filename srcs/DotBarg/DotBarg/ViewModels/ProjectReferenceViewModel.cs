using DotBarg.Libraries;
using DotBarg.Libraries.DBs;
using DotBarg.Libraries.Parsers;
using DotBarg.Models;
using Livet;
using Livet.Commands;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Data;

/*
 * 以下は、プロジェクト間の呼び出し関係（全体）と（個別）の共通利用になります。
 * ProjectReferenceView
 * ProjectReferenceViewModel
 * ProjectReferenceTreeBehavior
 * 
 * 
 * 
 */


namespace DotBarg.ViewModels
{
    public class ProjectReferenceViewModel : DocumentPaneViewModel
    {
        #region 変更通知プロパティ


        // AvalonDock 関連

        public override string Title
        {
            get 
            {
                var fileName = Path.GetFileNameWithoutExtension(FileName);
                var sb = new StringBuilder();
                
                sb.Append($"{fileName} のプロジェクト間の呼び出し関係");

                if (IsSolutionFile)
                    sb.Append("（全体）");
                else
                    sb.Append("（個別）");

                return sb.ToString();
            }
        }

        public override string ContentId
        {
            get { return FileName; }
        }

        public override TreeNodeKinds TreeNodeKinds
        {
            get
            {
                var value = TreeNodeKinds.None;
                var extension = Path.GetExtension(FileName).ToLower();

                if (IsSolutionFile)
                    value = TreeNodeKinds.SolutionFile;

                if (extension == ".csproj")
                    value = TreeNodeKinds.CSharpProjectFile;

                if (extension == ".vbproj")
                    value = TreeNodeKinds.VBNetProjectFile;

                return value;
            }
        }



        // View 関連

        private string _FileName;
        public string FileName
        {
            get { return _FileName; }
            set 
            { 
                RaisePropertyChangedIfSet(ref _FileName, value);
                SetData();
            }
        }

        private bool IsSolutionFile
        {
            get { return (Path.GetExtension(FileName).ToLower() == ".sln" ? true : false); }
        }

        // 継承先ツリー

        private ObservableCollection<DefinitionItemModel> _ProjectTreeItems;
        public ObservableCollection<DefinitionItemModel> ProjectTreeItems
        {
            get { return _ProjectTreeItems; }
            set { RaisePropertyChangedIfSet(ref _ProjectTreeItems, value); }
        }


        #endregion

        #region コンストラクタ


        public ProjectReferenceViewModel()
        {
            CanClose = true;

            FileName = string.Empty;
            ProjectTreeItems = new ObservableCollection<DefinitionItemModel>();
            BindingOperations.EnableCollectionSynchronization(ProjectTreeItems, new object());
        }


        #endregion

        #region データ取得


        private void SetData()
        {
            if (string.IsNullOrEmpty(FileName))
                return;

            ProjectTreeItems.Clear();

            if (IsSolutionFile)
            {
                // プロジェクト間の呼び出し関係（全体）
                var slnInfo = AppEnv.SolutionInfos.FirstOrDefault(x => x.SolutionFile == FileName);
                var prjInfos = slnInfo.ProjectFiles.Select(x =>
                {
                    var foundInfo = AppEnv.ProjectInfos.FirstOrDefault(y =>
                    {
                        if (y.SolutionFile == FileName && y.ProjectFile == x.ProjectFile)
                            return true;
                        else
                            return false;
                    });

                    return foundInfo;
                });

                foreach (var prjInfo in prjInfos)
                    SetData(prjInfo, true, string.Empty);
            }
            else
            {
                // プロジェクト間の呼び出し関係（個別）
                // ※別ソリューションで、同一パスのプロジェクトファイルがあればアウト
                var prjInfo = AppEnv.ProjectInfos.FirstOrDefault(x => x.ProjectFile == FileName);

                SetData(prjInfo, true, string.Empty);
            }
        }

        private void SetData(ProjectInfo prjInfo, bool isTargetDefinition, string relationName)
        {
            var model = new DefinitionItemModel
            {
                IsExpanded = true,
                IsTargetDefinition = isTargetDefinition,
                RelationName = relationName,
                DefinitionName = prjInfo.ProjectName,
            };

            // VSエクスプローラーペイン上のプロジェクト名とアセンブリファイル名が違う場合があるので、2つ表示させる
            model.MemberTreeItems.Add(new TreeViewItemModel
            {
                Text = prjInfo.AssemblyName,
            });

            ProjectTreeItems.Add(model);

            foreach (var child in prjInfo.ProjectReferenceAssemblyNames)
            {
                var childInfo = AppEnv.ProjectInfos.FirstOrDefault(x => x.AssemblyFile == child.AssemblyFile);
                SetData(childInfo, false, model.DefinitionName);
            }
        }


        #endregion

    }
}
