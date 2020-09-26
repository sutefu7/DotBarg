using Livet;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace DotBarg
{
    /// <summary>
    /// App.xaml の相互作用ロジック
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            // UIスレッドで実行されているコードで処理されなかったら発生する
            DispatcherUnhandledException += App_DispatcherUnhandledException;

            // バックグラウンドタスク内で処理されなかったら発生する
            // https://www.atmarkit.co.jp/ait/articles/1512/16/news026.html
            // 
            // 以下とのこと。無いよりはマシ程度か
            // > ただし、このイベントが呼び出されるのは、バックグラウンドタスクのインスタンスが破棄されるときである。
            // > 通常はシステムのガベージコレクタに破棄を任せているため、呼び出されるタイミングは不定である。
            // > アプリケーション自体が先に終了してしまい、イベントハンドラーが呼び出されないままになる可能性もあるので注意してほしい。
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show($"{e.Exception.Message}", "例外エラー", MessageBoxButton.OK, MessageBoxImage.Error);

            // このままアプリケーションを終了させたいので、ハンドルを変えない
            //e.Handled = true;
        }

        private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            MessageBox.Show($"{e.Exception.InnerException.Message}", "例外エラー", MessageBoxButton.OK, MessageBoxImage.Error);

            // このままアプリケーションを終了させたいので、ハンドルを変えない
            //e.SetObserved();
        }

        private void OnStartup(object sender, StartupEventArgs e)
        {
            DispatcherHelper.UIDispatcher = Dispatcher;
        }
    }
}
