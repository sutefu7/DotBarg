using DotBarg.Models;
using System.IO;
using System.Text;

namespace DotBarg.ViewModels
{
    public class DefinitionDiscoveryViewModel : DocumentPaneViewModel
    {
        #region 変更通知プロパティ


        // AvalonDock 関連

        public override string Title
        {
            get
            {
                var fileName = Path.GetFileName(SelectedItem.FileName);
                var sb = new StringBuilder();

                sb.Append($"{fileName} の定義の追跡");

                return sb.ToString();
            }
        }

        public override string ContentId
        {
            get
            {
                var filePath = SelectedItem.FileName;
                var sb = new StringBuilder();

                sb.Append($"{filePath} の定義の追跡");

                return sb.ToString();
            }
        }

        public override TreeNodeKinds TreeNodeKinds
        {
            get
            {
                var result = TreeNodeKinds.None;
                var extension = Path.GetExtension(SelectedItem.FileName).ToLower();

                if (extension == ".cs")
                    result = TreeNodeKinds.CSharpSourceFile;

                if (extension == ".vb")
                    result = TreeNodeKinds.VBNetSourceFile;

                return result;
            }
        }



        // View 関連

        private TreeViewItemModel _SelectedItem;
        public TreeViewItemModel SelectedItem
        {
            get { return _SelectedItem; }
            set { RaisePropertyChangedIfSet(ref _SelectedItem, value); }
        }


        #endregion

        #region コンストラクタ


        public DefinitionDiscoveryViewModel()
        {
            CanClose = true;
        }


        #endregion


    }
}
