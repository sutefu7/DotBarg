using DotBarg.ViewModels;
using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Input;
using Xceed.Wpf.AvalonDock;

/*
 * for AvalonDock
 * 
 * 
 * DockingManager_DocumentClosing イベントについて（2018/10/08 時点）
 * 
 * 内部的には View のインスタンスを再利用しているのか？（それとも、自分がそういう設定をしているのか？謎）
 * View が破棄されると例外エラー発生してしまう現象の対応
 * 対応として、キャンセルフラグを立てた状態で戻す
 * 
 * ただし、ViewModel 側は削除する
 * SolutionExplorerVM_PropertyChanged イベント処理内で、表示中の判定として扱われてしまうための対応
 * 
 * --
 * 
 * System.NullReferenceException はハンドルされませんでした。
 * Message: 型 'System.NullReferenceException' のハンドルされていない例外が Xceed.Wpf.AvalonDock.dll で発生しました
 * 追加情報:オブジェクト参照がオブジェクト インスタンスに設定されていません。
 * 
 * Correctly handling document-close and tool-hide in a WPF app with AvalonDock+Caliburn Micro
 * https://stackoverflow.com/questions/28194046/correctly-handling-document-close-and-tool-hide-in-a-wpf-app-with-avalondockcal
 * 
 * ※こちらは参考程度
 * WPF - AvalonDock - Closing Document
 * https://stackoverflow.com/questions/18359818/wpf-avalondock-closing-document
 * 
 * MVVM Passing EventArgs As Command Parameter
 * https://stackoverflow.com/questions/6205472/mvvm-passing-eventargs-as-command-parameter
 * 
 * 
 * 
 * 
 */



namespace DotBarg.Behaviors
{
    public class DocumentClosingBehavior : Behavior<DockingManager>
    {
        public static readonly DependencyProperty DocumentClosingCommandProperty =
            DependencyProperty.Register(
                nameof(DocumentClosingCommand),
                typeof(ICommand),
                typeof(DocumentClosingBehavior),
                new PropertyMetadata());

        public ICommand DocumentClosingCommand
        {
            get { return (ICommand)GetValue(DocumentClosingCommandProperty); }
            set { SetValue(DocumentClosingCommandProperty, value); }
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.DocumentClosing += DockingManager_DocumentClosing;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.DocumentClosing -= DockingManager_DocumentClosing;
        }

        private void DockingManager_DocumentClosing(object sender, DocumentClosingEventArgs e)
        {
            if (DocumentClosingCommand is null)
                return;

            var vm = e.Document.Content as DocumentPaneViewModel;
            if (vm is null)
                return;

            if (DocumentClosingCommand.CanExecute(vm))
                DocumentClosingCommand.Execute(vm);

            e.Cancel = true;
        }
    }
}
