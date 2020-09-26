using Livet.Experimentals.EventListeners.WeakEvents;
using Livet.Messaging;
using Livet.Messaging.Windows;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;

/*
 * 
 * 以下を元に、処理内容を作成しています。（2018/10/08 時点）
 * 
 * InformationDialogInteractionMessageAction.cs
 * https://github.com/ugaya40/Livet/blob/master/.NET4.0/Livet(.NET4.0)/Behaviors/Messaging/InformationDialogInteractionMessageAction.cs
 * 
 * ConfirmationDialogInteractionMessageAction.cs
 * https://github.com/ugaya40/Livet/blob/master/.NET4.0/Livet(.NET4.0)/Behaviors/Messaging/ConfirmationDialogInteractionMessageAction.cs
 * 
 * WindowInteractionMessageAction.cs
 * https://github.com/ugaya40/Livet/blob/master/.NET4.0/Livet(.NET4.0)/Behaviors/Messaging/Windows/WindowInteractionMessageAction.cs
 * 
 */



namespace Livet.Experimentals
{
    /// <summary>
    /// View 側の Messenger 受信機能を隠ぺいして、自動応答するサポートクラスです。
    /// ViewModelExtensions にある Messenger 呼び出しの各ラッパーメソッドと連携する機能を持ちます。
    /// </summary>
    public static class MessengerOperator
    {
        private static Dictionary<string, LivetWeakEventListener<DependencyPropertyChangedEventHandler, DependencyPropertyChangedEventArgs>> _WindowCache;
        private static Dictionary<string, LivetWeakEventListener<EventHandler<InteractionMessageRaisedEventArgs>, InteractionMessageRaisedEventArgs>> _MessengerCache;

        /// <summary>
        /// 自動応答処理をおこなうかどうかを決める 添付プロパティです。
        /// </summary>
        /// <remarks>ViewModelLocator と同じ形式です。</remarks>
        public static DependencyProperty AutoReceiveOperationProperty =
            DependencyProperty.RegisterAttached("AutoReceiveOperation", typeof(bool), typeof(MessengerOperator), new PropertyMetadata(false, AutoReceiveOperationChanged));

        public static bool GetAutoReceiveOperation(DependencyObject obj)
        {
            return (bool)obj.GetValue(AutoReceiveOperationProperty);
        }

        public static void SetAutoReceiveOperation(DependencyObject obj, bool value)
        {
            obj.SetValue(AutoReceiveOperationProperty, value);
        }

        private static void AutoReceiveOperationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!DesignerProperties.GetIsInDesignMode(d))
            {
                if ((bool)e.NewValue)
                {
                    AutoReceiveOperationChangedInternal(d);
                }
            }
        }

        /// <summary>
        /// 渡された View を元に、DataContext(=ViewModel), Messenger を取得して、イベント購読して、メッセージキーに応じた各処理をおこないます。
        /// </summary>
        /// <param name="view">Window</param>
        private static void AutoReceiveOperationChangedInternal(DependencyObject view)
        {
            if (!(view is Window))
                return;

            var window = view as Window;

            if (_WindowCache == null)
                _WindowCache = new Dictionary<string, LivetWeakEventListener<DependencyPropertyChangedEventHandler, DependencyPropertyChangedEventArgs>>();

            // DataContext が null の場合処理を抜けてしまうので、抜ける前に変更監視と再開処理を残しておく
            // View 単位で確保しておく
            var keyName = window.GetType().FullName;
            if (!_WindowCache.ContainsKey(keyName))
            {
                var listener = new LivetWeakEventListener<DependencyPropertyChangedEventHandler, DependencyPropertyChangedEventArgs>(
                    h => new DependencyPropertyChangedEventHandler(h),
                    h => window.DataContextChanged += h,
                    h => window.DataContextChanged -= h,
                    (s, e) => AutoReceiveOperationChangedInternal(s as DependencyObject));

                _WindowCache.Add(keyName, listener);

            }

            if (window.DataContext == null)
                return;

            var dataContext = window.DataContext;
            if (!(dataContext is ViewModel))
                return;

            var vm = dataContext as ViewModel;
            var messenger = vm.Messenger;

            if (_MessengerCache == null)
                _MessengerCache = new Dictionary<string, LivetWeakEventListener<EventHandler<InteractionMessageRaisedEventArgs>, InteractionMessageRaisedEventArgs>>();

            // ViewModel 単位で確保しておく
            keyName = $"{vm.GetType().FullName}.{messenger.GetType().Name}";
            if (!_MessengerCache.ContainsKey(keyName))
            {
                var listener = new LivetWeakEventListener<EventHandler<InteractionMessageRaisedEventArgs>, InteractionMessageRaisedEventArgs>(
                    h => new EventHandler<InteractionMessageRaisedEventArgs>(h),
                    h => messenger.Raised += h,
                    h => messenger.Raised -= h,
                    (s, e) => Messenger_Raised(window, s, e));

                _MessengerCache.Add(keyName, listener);

            }

        }

        /// <summary>
        /// Messenger の Raised イベント処理です。メッセージキーに応じた各処理をおこないます。
        /// </summary>
        /// <param name="window">Window</param>
        /// <param name="sender">Messenger</param>
        /// <param name="e">InteractionMessageRaisedEventArgs</param>
        private static void Messenger_Raised(Window window, object sender, InteractionMessageRaisedEventArgs e)
        {
            // 既存の XxxTrigger や XxxAction にお任せ案 (View に書く版のクラスの再利用と処理のお任せ) は、難しいため諦めた。
            //var mes = new InformationDialogInteractionMessageAction();

            // 非同期で呼ばれた場合は、同期呼び出し（UI スレッド）に変えて実行する
            if (!window.Dispatcher.CheckAccess())
            {
                var recall = new Action<Window, object, InteractionMessageRaisedEventArgs>((w, s, e2) => Messenger_Raised(w, s, e2));
                window.Dispatcher.Invoke(recall, new object[] { window, sender, e });
                return;
            }

            switch (e.Message.MessageKey)
            {
                case "ShowInformationMessage":
                case "ShowWarningMessage":
                case "ShowErrorMessage":
                case "ShowInformationMessageAsync":
                case "ShowWarningMessageAsync":
                case "ShowErrorMessageAsync":

                    // 表示のみの MessageBox 系
                    var informationMessage = e.Message as InformationMessage;

                    if (informationMessage != null)
                    {
                        MessageBox.Show(
                            window,
                            informationMessage.Text,
                            informationMessage.Caption,
                            MessageBoxButton.OK,
                            informationMessage.Image
                            );
                    }

                    break;

                case "ShowConfirmationOKCancelMessage":
                case "ShowConfirmationYesNoMessage":
                case "ShowConfirmationYesNoCancelMessage":
                case "ShowConfirmationOKCancelMessageAsync":
                case "ShowConfirmationYesNoMessageAsync":
                case "ShowConfirmationYesNoCancelMessageAsync":

                    // 戻り値ありの MessageBox 系

                    var confirmMessage = e.Message as ConfirmationMessage;

                    if (confirmMessage != null)
                    {
                        var result = MessageBox.Show(
                            window,
                            confirmMessage.Text,
                            confirmMessage.Caption,
                            confirmMessage.Button,
                            confirmMessage.Image,
                            confirmMessage.DefaultResult
                            );

                        if (result == MessageBoxResult.Yes || result == MessageBoxResult.OK)
                        {
                            confirmMessage.Response = true;
                        }
                        else if (result == MessageBoxResult.Cancel)
                        {
                            confirmMessage.Response = null;
                        }
                        else
                        {
                            confirmMessage.Response = false;
                        }
                    }

                    break;

                case "Close":
                case "CloseAsync":

                    var closeMessage = e.Message as WindowActionMessage;
                    if (closeMessage != null && closeMessage.Action == WindowAction.Close)
                        window.Close();
                    
                    break;

                case "Maximize":
                case "MaximizeAsync":

                    var maximizeMessage = e.Message as WindowActionMessage;
                    if (maximizeMessage != null && maximizeMessage.Action == WindowAction.Maximize)
                        window.WindowState = WindowState.Maximized;

                    break;

                case "Minimize":
                case "MinimizeAsync":

                    var minimizeMessage = e.Message as WindowActionMessage;
                    if (minimizeMessage != null && minimizeMessage.Action == WindowAction.Minimize)
                        window.WindowState = WindowState.Minimized;

                    break;

                case "Active":
                case "ActiveAsync":

                    var activeMessage = e.Message as WindowActionMessage;
                    if (activeMessage != null && activeMessage.Action == WindowAction.Active)
                        window.Activate();

                    break;

            }
        }
    }
}
