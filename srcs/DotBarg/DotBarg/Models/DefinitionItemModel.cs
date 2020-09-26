using Livet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotBarg.Models
{
    public class DefinitionItemModel : NotificationObject
    {

        // 定義クラス・インターフェースと、継承元クラス・インターフェースを色分けするための判断フラグ

        private bool _IsTargetDefinition;
        public bool IsTargetDefinition
        {
            get { return _IsTargetDefinition; }
            set { RaisePropertyChangedIfSet(ref _IsTargetDefinition, value); }
        }

        // 関連付ける定義名。Class1, Interface1 など

        private string _RelationName;
        public string RelationName
        {
            get { return _RelationName; }
            set { RaisePropertyChangedIfSet(ref _RelationName, value); }
        }

        // 定義名。Class1, Interface1 など

        private string _DefinitionName;
        public string DefinitionName
        {
            get { return _DefinitionName; }
            set { RaisePropertyChangedIfSet(ref _DefinitionName, value); }
        }

        // Expander の開閉有無
        private bool _IsExpanded;
        public bool IsExpanded
        {
            get { return _IsExpanded; }
            set { RaisePropertyChangedIfSet(ref _IsExpanded, value); }
        }

        // メンバーツリー
        private ObservableCollection<TreeViewItemModel> _MemberTreeItems;
        public ObservableCollection<TreeViewItemModel> MemberTreeItems
        {
            get { return _MemberTreeItems; }
            set { RaisePropertyChangedIfSet(ref _MemberTreeItems, value); }
        }

        public DefinitionItemModel()
        {
            MemberTreeItems = new ObservableCollection<TreeViewItemModel>();
        }

        public DefinitionItemModel Copy()
        {
            var model = new DefinitionItemModel
            {
                IsTargetDefinition = IsTargetDefinition,
                DefinitionName = DefinitionName,
            };

            foreach (var child in MemberTreeItems)
                model.MemberTreeItems.Add(child.Copy());

            return model;
        }
    }
}
