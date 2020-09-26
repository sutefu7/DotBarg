using Livet;

/*
 * AvalonDock / AnchorablePane 向け（サイド画面系）の画面用の基底ビューモデルです。
 * 
 * 
 * 
 */

namespace DotBarg.ViewModels
{
    public abstract class AnchorablePaneViewModel : ViewModel
    {
        public abstract string Title { get; }

        public abstract string ContentId { get; }


        private bool _CanHide;
        public bool CanHide
        {
            get { return _CanHide; }
            set { RaisePropertyChangedIfSet(ref _CanHide, value); }
        }

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
