using DotBarg.Libraries;
using DotBarg.Models;
using Livet;
using Livet.Commands;
using Livet.EventListeners;
using Livet.Messaging.IO;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using Xceed.Wpf.AvalonDock;

namespace DotBarg.ViewModels
{
    public class MainViewModel : ViewModel
    {
        #region 変更通知プロパティ


        private SolutionExplorerViewModel _SolutionExplorerVM;
        private SolutionExplorerViewModel SolutionExplorerVM
        {
            get { return _SolutionExplorerVM; }
            set { RaisePropertyChangedIfSet(ref _SolutionExplorerVM, value); }
        }

        private ObservableCollection<AnchorablePaneViewModel> _Anchorables;
        public ObservableCollection<AnchorablePaneViewModel> Anchorables
        {
            get { return _Anchorables; }
            set { RaisePropertyChangedIfSet(ref _Anchorables, value); }
        }

        private ObservableCollection<DocumentPaneViewModel> _Documents;
        public ObservableCollection<DocumentPaneViewModel> Documents
        {
            get { return _Documents; }
            set { RaisePropertyChangedIfSet(ref _Documents, value); }
        }


        #endregion

        #region コマンド


        // ソリューションエクスプローラーペイン側
        // TreeView.TreeNode の右クリック → コンテキストメニュー の項目のクリックコマンド用
        // ※以下のコマンドは、SolutionExplorerView.xaml とバインドしています。
        // ちなみに e は、SolutionExplorerVM.SelectedItem と同じはずです。


        // プロジェクト間の呼び出し関係（全体）
        private ListenerCommand<TreeViewItemModel> _SolutionFileContextMenuClickCommand;
        public ListenerCommand<TreeViewItemModel> SolutionFileContextMenuClickCommand
        {
            get { return this.SetCommand(ref _SolutionFileContextMenuClickCommand, SolutionFileContextMenuClick); }
        }

        private void SolutionFileContextMenuClick(TreeViewItemModel e)
        {
            var foundVM = Documents.OfType<ProjectReferenceViewModel>().FirstOrDefault(x => x.FileName == e.FileName);
            if (foundVM is null)
            {
                // 新規登録・表示
                AddProjectReferencePane(e);
            }
            else
            {
                // すでに登録済み（表示済み）なので、再アクティブに切り替える
                foundVM.IsSelected = true;
            }
        }

        private void AddProjectReferencePane(TreeViewItemModel e)
        {
            var vm = new ProjectReferenceViewModel
            {
                FileName = e.FileName,
            };

            Documents.Add(vm);

            // 初回表示時、AvalonDock/DocumentPane? に、SourceView 自体がロードすらされないバグの対応
            // 結局原因は謎のままだが、対応としては、現在登録済みのドキュメントペイン全て未選択にしてから、
            // 今回追加分を選択させることで、初回表示時も表示されるようになった。
            foreach (var document in Documents)
                document.IsSelected = false;

            vm.IsSelected = true;
        }



        // プロジェクト間の呼び出し関係（単独）
        private ListenerCommand<TreeViewItemModel> _ProjectFileContextMenuClickCommand;
        public ListenerCommand<TreeViewItemModel> ProjectFileContextMenuClickCommand
        {
            get { return this.SetCommand(ref _ProjectFileContextMenuClickCommand, ProjectFileContextMenuClick); }
        }

        private void ProjectFileContextMenuClick(TreeViewItemModel e)
        {
            SolutionFileContextMenuClick(e);
        }



        // 定義の追跡
        private ListenerCommand<TreeViewItemModel> _DefinitionDiscoveryContextMenuClickCommand;
        public ListenerCommand<TreeViewItemModel> DefinitionDiscoveryContextMenuClickCommand
        {
            get { return this.SetCommand(ref _DefinitionDiscoveryContextMenuClickCommand, DefinitionDiscoveryContextMenuClick); }
        }

        private void DefinitionDiscoveryContextMenuClick(TreeViewItemModel e)
        {
            var foundVM = Documents.OfType<DefinitionDiscoveryViewModel>().FirstOrDefault(x => x.SelectedItem.FileName == e.FileName);
            if (foundVM is null)
            {
                // 新規登録・表示
                AddDefinitionDiscoveryPane(e);
            }
            else
            {
                // すでに登録済み（表示済み）なので、再アクティブに切り替える
                foundVM.IsSelected = true;
            }
        }

        private void AddDefinitionDiscoveryPane(TreeViewItemModel e)
        {
            var vm = new DefinitionDiscoveryViewModel
            {
                SelectedItem = e,
            };

            Documents.Add(vm);

            // 初回表示時、AvalonDock/DocumentPane? に、SourceView 自体がロードすらされないバグの対応
            // 結局原因は謎のままだが、対応としては、現在登録済みのドキュメントペイン全て未選択にしてから、
            // 今回追加分を選択させることで、初回表示時も表示されるようになった。
            foreach (var document in Documents)
                document.IsSelected = false;

            vm.IsSelected = true;
        }



        // ソースコード間の呼び出し関係
        private ListenerCommand<TreeViewItemModel> _SourceFileContextMenuClickCommand;
        public ListenerCommand<TreeViewItemModel> SourceFileContextMenuClickCommand
        {
            get { return this.SetCommand(ref _SourceFileContextMenuClickCommand, SourceFileContextMenuClick); }
        }

        private void SourceFileContextMenuClick(TreeViewItemModel e)
        {

        }



        // 本コマンドは、AvalonDock の DocumentClosingBehavior のイベントと連携しています。

        private ListenerCommand<DocumentPaneViewModel> _DocumentClosingCommand;
        public ListenerCommand<DocumentPaneViewModel> DocumentClosingCommand
        {
            get { return this.SetCommand(ref _DocumentClosingCommand, DocumentClosing); }
        }

        private void DocumentClosing(DocumentPaneViewModel e)
        {
            Documents.Remove(e);
        }


        #endregion

        #region コンストラクタ


        public MainViewModel()
        {
            // 画面デザイン時は処理しない（これにより画面デザインがプレビュー反映されなくなったが、保留）
            if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
                return;

            SolutionExplorerVM = new SolutionExplorerViewModel();
            Anchorables = new ObservableCollection<AnchorablePaneViewModel>();
            Anchorables.Add(SolutionExplorerVM);

            Documents = new ObservableCollection<DocumentPaneViewModel>();

            // ソリューションエクスプローラーペインからの、プロパティ変更通知を受け取る
            // （ソリューションエクスプローラーツリー内のソースノードを、クリックした際のイベント処理を引き継ぐ）
            var listener = new PropertyChangedEventListener(SolutionExplorerVM);
            listener.RegisterHandler(SolutionExplorerVM_PropertyChanged);
            CompositeDisposable.Add(listener);
        }

        #endregion

        #region ファイルを開くダイアログの戻り値（ソリューションファイルの選択メニューから）


        public async void OpenFileDialogCallback(OpeningFileSelectionMessage message)
        {
            if (Util.IsNull(message.Response))
                return;

            var slnFile = message.Response.FirstOrDefault();
            await SolutionExplorerVM.ShowDataAsync(slnFile);
        }

        #endregion

        #region ファイルのドラッグアンドドロップ（ソリューションファイルの指定）


        public async void OnDragAndDropped(List<string> dropFiles)
        {
            var slnFile = dropFiles.FirstOrDefault(x => Path.GetExtension(x).ToLower() == ".sln");
            if (slnFile is null)
                return;

            // エクスプローラー画面から D&D した場合、自画面が非アクティブ状態のままとなるので、自画面をアクティブに切り替える
            // こうしないと、後続処理の進捗画面が表示されない現象が発生してしまう（SolutionExplorerVM 側で対策をおこなってもいいのかも）
            await this.ActiveAsync();

            await SolutionExplorerVM.ShowDataAsync(slnFile);
        }


        #endregion

        #region ソリューションエクスプローラーペイン内の SelectedItemChanged 処理（実質、左クリックイベント処理）


        private void SolutionExplorerVM_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // AvalonDock.Theme と ComboBox とのやり取り時、ComboBox の選択肢変更の影響を受けてしまうバグの対応
            if (SolutionExplorerVM.SelectedItem is null)
                return;

            // ソリューションエクスプローラーのツリーノード選択
            if (e.PropertyName == nameof(SolutionExplorerVM.SelectedItem))
            {
                var selectedModel = SolutionExplorerVM.SelectedItem;
                switch (selectedModel.TreeNodeKinds)
                {
                    case TreeNodeKinds.SolutionFile:

                        // ソリューションファイルノードをクリックした
                        break;

                    case TreeNodeKinds.CSharpProjectFile:
                    case TreeNodeKinds.VBNetProjectFile:

                        // プロジェクトファイルノードをクリックした
                        break;

                    case TreeNodeKinds.CSharpSourceFile:
                    case TreeNodeKinds.VBNetSourceFile:
                    case TreeNodeKinds.GeneratedFile:

                        // ソースファイルノードをクリックした
                        var foundVM = Documents.OfType<SourceViewModel>().FirstOrDefault(x => x.SourceFile == selectedModel.FileName);
                        if (foundVM is null)
                        {
                            // 新規登録・表示
                            AddSourceFilePane(selectedModel);
                        }
                        else
                        {
                            // すでに登録済み（表示済み）なので、再アクティブに切り替える
                            foundVM.IsSelected = true;
                        }

                        break;

                }
            }
        }

        private void AddSourceFilePane(TreeViewItemModel e)
        {
            var vm = new SourceViewModel
            {
                SourceFile = e.FileName,
                SourceCode = File.ReadAllText(e.FileName, Util.GetEncoding(e.FileName)),
                MainVM = this,
            };

            Documents.Add(vm);

            // 初回表示時、AvalonDock/DocumentPane? に、SourceView 自体がロードすらされないバグの対応
            // 結局原因は謎のままだが、対応としては、現在登録済みのドキュメントペイン全て未選択にしてから、
            // 今回追加分を選択させることで、初回表示時も表示されるようになった。
            foreach (var document in Documents)
                document.IsSelected = false;

            vm.IsSelected = true;
        }



        // SourceViewModel から呼び出されます。

        public void AddSourceFilePane(string sourceFile, int offset)
        {
            var foundVM = Documents.OfType<SourceViewModel>().FirstOrDefault(x => x.SourceFile == sourceFile);
            if (foundVM is null)
            {
                // 新規登録・表示
                AddSourceFilePane(new TreeViewItemModel { FileName = sourceFile });
                foundVM = Documents.OfType<SourceViewModel>().FirstOrDefault(x => x.SourceFile == sourceFile);
            }
            else
            {
                // すでに登録済み（表示済み）なので、再アクティブに切り替える
                foundVM.IsSelected = true;
            }

            foundVM.CaretOffset = offset;
        }


        #endregion

        #region フォントサイズの変更


        public void SetFontSize(object value)
        {
            double doubleValue;

            if (!double.TryParse(value?.ToString(), out doubleValue))
                return;

            AppEnv.FontSize = doubleValue;
        }


        #endregion

    }
}
