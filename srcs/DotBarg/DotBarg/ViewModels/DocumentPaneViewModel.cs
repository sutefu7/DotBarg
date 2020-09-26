using DotBarg.Models;
using Livet;

/*
 * AvalonDock / DocumentPane 向けの画面用の基底ビューモデルです。
 * 
 * 
 * 
 */

namespace DotBarg.ViewModels
{
    public abstract class DocumentPaneViewModel : ViewModel
    {
        public abstract string Title { get; }

        public abstract string ContentId { get; }

        public abstract TreeNodeKinds TreeNodeKinds { get; }


        private bool _CanClose;
        public bool CanClose
        {
            get { return _CanClose; }
            set { RaisePropertyChangedIfSet(ref _CanClose, value); }
        }

        private bool _CanFloat;
        public bool CanFloat
        {
            get { return _CanFloat; }
            set { RaisePropertyChangedIfSet(ref _CanFloat, value); }
        }

        private bool _IsActive;
        public bool IsActive
        {
            get { return _IsActive; }
            set { RaisePropertyChangedIfSet(ref _IsActive, value); }
        }

        private bool _IsSelected;
        public bool IsSelected
        {
            get { return _IsSelected; }
            set { RaisePropertyChangedIfSet(ref _IsSelected, value); }
        }
    }
}
