using DotBarg.ViewModels;
using Microsoft.Xaml.Behaviors;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

/*
 * エクスプローラーのファイルを、Window 内にドラッグアンドドロップした場合、MainViewModel のメソッドに転送します。
 * 
 * 
 */


namespace DotBarg.Behaviors
{
    public class WindowDragAndDropBehavior : Behavior<Window>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.PreviewDragOver += AssociatedObject_PreviewDragOver;
            AssociatedObject.Drop += AssociatedObject_Drop;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.PreviewDragOver -= AssociatedObject_PreviewDragOver;
            AssociatedObject.Drop -= AssociatedObject_Drop;
        }

        private void AssociatedObject_PreviewDragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop, true))
                e.Effects = DragDropEffects.Copy;
            else
                e.Effects = DragDropEffects.None;

            e.Handled = true;
        }

        private void AssociatedObject_Drop(object sender, DragEventArgs e)
        {
            var view = sender as Window;
            if (view is null)
                return;

            var vm = view.DataContext as MainViewModel;
            if (vm is null)
                return;

            var files = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (!(files is null))
                vm.OnDragAndDropped(files.ToList());
        }
    }
}
