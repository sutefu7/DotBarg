using Livet;
using System.Collections.ObjectModel;

namespace DotBarg.Models
{
    public class TreeViewItemModel : NotificationObject
    {
        private bool _IsSelected;
        public bool IsSelected
        {
            get { return _IsSelected; }
            set { RaisePropertyChangedIfSet(ref _IsSelected, value); }
        }

        private bool _IsExpanded;
        public bool IsExpanded
        {
            get { return _IsExpanded; }
            set { RaisePropertyChangedIfSet(ref _IsExpanded, value); }
        }

        private string _Text;
        public string Text
        {
            get { return _Text; }
            set { RaisePropertyChangedIfSet(ref _Text, value); }
        }

        private TreeNodeKinds _TreeNodeKinds;
        public TreeNodeKinds TreeNodeKinds
        {
            get { return _TreeNodeKinds; }
            set { RaisePropertyChangedIfSet(ref _TreeNodeKinds, value); }
        }

        private string _FileName;
        public string FileName
        {
            get { return _FileName; }
            set { RaisePropertyChangedIfSet(ref _FileName, value); }
        }

        private string _ContainerName;
        public string ContainerName
        {
            get { return _ContainerName; }
            set { RaisePropertyChangedIfSet(ref _ContainerName, value); }
        }

        private int _StartLength;
        public int StartLength
        {
            get { return _StartLength; }
            set { RaisePropertyChangedIfSet(ref _StartLength, value); }
        }

        private int _EndLength;
        public int EndLength
        {
            get { return _EndLength; }
            set { RaisePropertyChangedIfSet(ref _EndLength, value); }
        }

        private ObservableCollection<TreeViewItemModel> _Children;
        public ObservableCollection<TreeViewItemModel> Children
        {
            get { return _Children; }
            set { RaisePropertyChangedIfSet(ref _Children, value); }
        }

        public TreeViewItemModel()
        {
            IsSelected = false;
            IsExpanded = false;
            Text = string.Empty;
            TreeNodeKinds = TreeNodeKinds.None;
            FileName = string.Empty;
            ContainerName = string.Empty;
            StartLength = -1;
            EndLength = -1;
            Children = new ObservableCollection<TreeViewItemModel>();
        }

        public TreeViewItemModel Copy()
        {
            var model = new TreeViewItemModel
            {
                IsSelected = IsSelected,
                IsExpanded = IsExpanded,
                Text = Text,
                TreeNodeKinds = TreeNodeKinds,
                FileName = FileName,
                ContainerName = ContainerName,
                StartLength = StartLength,
                EndLength = EndLength,
            };

            foreach (var child in Children)
                model.Children.Add(child.Copy());

            return model;
        }
    }
}
