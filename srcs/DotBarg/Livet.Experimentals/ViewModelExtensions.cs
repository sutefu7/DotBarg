using Livet.Commands;
using Livet.Messaging;
using Livet.Messaging.Windows;
using System;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;

/*
 * ICommand 系の処理は、以下の移植です。（2018/10/08 時点）
 * Prism
 * BindableBase.cs
 * https://github.com/PrismLibrary/Prism/blob/master/Source/Prism/Mvvm/BindableBase.cs
 * 
 * 
 * 
 */



namespace Livet
{
    /// <summary>
    /// ViewModel に対する拡張クラスです。
    /// </summary>
    public static class ViewModelExtensions
    {
        #region ICommand 系


        /// <summary>
        /// コマンドのインスタンス生成のためのヘルパーメソッドです。
        /// </summary>
        /// <param name="self">ViewModel</param>
        /// <param name="command">ViewModelCommand</param>
        /// <param name="execute">実行処理</param>
        /// <param name="canExecute">実行処理の実行有無</param>
        /// <returns>ViewModelCommand</returns>
        public static ViewModelCommand SetCommand(this ViewModel self, ref ViewModelCommand command, Action execute, Func<bool> canExecute = null)
        {
            if (command == null)
            {
                if (canExecute == null)
                    canExecute = () => true;

                command = new ViewModelCommand(execute, canExecute);
            }

            return command;
        }

        /// <summary>
        /// コマンドのインスタンス生成のためのヘルパーメソッドです。
        /// </summary>
        /// <typeparam name="T">任意の型</typeparam>
        /// <param name="self">ViewModel</param>
        /// <param name="command">ListenerCommand<T></param>
        /// <param name="execute">実行処理</param>
        /// <param name="canExecute">実行処理の実行有無</param>
        /// <returns>ListenerCommand<T></returns>
        public static ListenerCommand<T> SetCommand<T>(this ViewModel self, ref ListenerCommand<T> command, Action<T> execute, Func<bool> canExecute = null)
        {
            if (command == null)
            {
                if (canExecute == null)
                    canExecute = () => true;

                command = new ListenerCommand<T>(execute, canExecute);
            }

            return command;
        }


        #endregion

        #region 通知メッセージ系


        /// <summary>
        /// 情報メッセージを表示します。
        /// </summary>
        /// <param name="self">ViewModel</param>
        /// <param name="messageBoxText">メッセージ本文</param>
        /// <param name="title">タイトル</param>
        /// <param name="messageKey">メッセージキー</param>
        public static void ShowInformationMessage(this ViewModel self, string messageBoxText, string title = "情報", string messageKey = "ShowInformationMessage")
        {
            ShowTargetMessage(self, messageBoxText, title, MessageBoxImage.Information, messageKey);
        }

        /// <summary>
        /// 警告メッセージを表示します。
        /// </summary>
        /// <param name="self">ViewModel</param>
        /// <param name="messageBoxText">メッセージ本文</param>
        /// <param name="title">タイトル</param>
        /// <param name="messageKey">メッセージキー</param>
        public static void ShowWarningMessage(this ViewModel self, string messageBoxText, string title = "注意", string messageKey = "ShowWarningMessage")
        {
            ShowTargetMessage(self, messageBoxText, title, MessageBoxImage.Warning, messageKey);
        }

        /// <summary>
        /// エラーメッセージを表示します。
        /// </summary>
        /// <param name="self">ViewModel</param>
        /// <param name="messageBoxText">メッセージ本文</param>
        /// <param name="title">タイトル</param>
        /// <param name="messageKey">メッセージキー</param>
        public static void ShowErrorMessage(this ViewModel self, string messageBoxText, string title = "エラー", string messageKey = "ShowErrorMessage")
        {
            ShowTargetMessage(self, messageBoxText, title, MessageBoxImage.Error, messageKey);
        }

        /// <summary>
        /// 任意の種類のメッセージを表示します。
        /// </summary>
        /// <param name="self">ViewModel</param>
        /// <param name="messageBoxText">メッセージ本文</param>
        /// <param name="title">タイトル</param>
        /// <param name="image">メッセージの種類別アイコン</param>
        /// <param name="messageKey">メッセージキー</param>
        private static void ShowTargetMessage(ViewModel self, string messageBoxText, string title, MessageBoxImage image, string messageKey)
        {
            var mes = new InformationMessage(messageBoxText, title, image, messageKey);
            self.Messenger.Raise(mes);
        }




        /// <summary>
        /// OK, Cancel 形式の確認メッセージを表示します。
        /// </summary>
        /// <param name="self">ViewModel</param>
        /// <param name="messageBoxText">メッセージ本文</param>
        /// <param name="title">タイトル</param>
        /// <param name="messageKey">メッセージキー</param>
        /// <param name="defaultResult">初期フォーカスをあてるボタン</param>
        /// <returns>結果</returns>
        public static ConfirmationMessage ShowConfirmationOKCancelMessage(this ViewModel self, string messageBoxText, string title = "確認", string messageKey = "ShowConfirmationOKCancelMessage", MessageBoxResult defaultResult = MessageBoxResult.Cancel)
        {
            if (defaultResult != MessageBoxResult.OK && defaultResult != MessageBoxResult.Cancel)
                defaultResult = MessageBoxResult.Cancel;

            return ShowConfirmationTargetMessage(self, messageBoxText, title, MessageBoxButton.OKCancel, defaultResult, messageKey);
        }

        /// <summary>
        /// Yes, No 形式の確認メッセージを表示します。
        /// </summary>
        /// <param name="self">ViewModel</param>
        /// <param name="messageBoxText">メッセージ本文</param>
        /// <param name="title">タイトル</param>
        /// <param name="messageKey">メッセージキー</param>
        /// <param name="defaultResult">初期フォーカスをあてるボタン</param>
        /// <returns>結果</returns>
        public static ConfirmationMessage ShowConfirmationYesNoMessage(this ViewModel self, string messageBoxText, string title = "確認", string messageKey = "ShowConfirmationYesNoMessage", MessageBoxResult defaultResult = MessageBoxResult.No)
        {
            if (defaultResult != MessageBoxResult.Yes && defaultResult != MessageBoxResult.No)
                defaultResult = MessageBoxResult.No;

            return ShowConfirmationTargetMessage(self, messageBoxText, title, MessageBoxButton.YesNo, defaultResult, messageKey);
        }

        /// <summary>
        /// Yes, No, Cancel 形式の確認メッセージを表示します。
        /// </summary>
        /// <param name="self">ViewModel</param>
        /// <param name="messageBoxText">メッセージ本文</param>
        /// <param name="title">タイトル</param>
        /// <param name="messageKey">メッセージキー</param>
        /// <param name="defaultResult">初期フォーカスをあてるボタン</param>
        /// <returns>結果</returns>
        public static ConfirmationMessage ShowConfirmationYesNoCancelMessage(this ViewModel self, string messageBoxText, string title = "確認", string messageKey = "ShowConfirmationYesNoCancelMessage", MessageBoxResult defaultResult = MessageBoxResult.No)
        {
            if (defaultResult != MessageBoxResult.Yes && defaultResult != MessageBoxResult.No && defaultResult != MessageBoxResult.Cancel)
                defaultResult = MessageBoxResult.No;

            return ShowConfirmationTargetMessage(self, messageBoxText, title, MessageBoxButton.YesNoCancel, defaultResult, messageKey);
        }

        /// <summary>
        /// 任意の形式の確認メッセージを表示します。
        /// </summary>
        /// <param name="self">ViewModel</param>
        /// <param name="messageBoxText">メッセージ本文</param>
        /// <param name="title">タイトル</param>
        /// <param name="buttonKinds">表示するボタンの種類</param>
        /// <param name="defaultResult">初期フォーカスをあてるボタン</param>
        /// <param name="messageKey">メッセージキー</param>
        /// <returns>結果</returns>
        private static ConfirmationMessage ShowConfirmationTargetMessage(ViewModel self, string messageBoxText, string title, MessageBoxButton buttonKinds, MessageBoxResult defaultResult, string messageKey)
        {
            var mes = new ConfirmationMessage(messageBoxText, title, MessageBoxImage.Question, buttonKinds, defaultResult, messageKey);
            mes = self.Messenger.GetResponse(mes); // Messenger.Raise() しつつ、戻り値を取得している？

            return mes;
        }




        /// <summary>
        /// 情報メッセージを非同期で表示します。
        /// </summary>
        /// <param name="self">ViewModel</param>
        /// <param name="messageBoxText">メッセージ本文</param>
        /// <param name="title">タイトル</param>
        /// <param name="messageKey">メッセージキー</param>
        /// <returns></returns>
        public static async Task ShowInformationMessageAsync(this ViewModel self, string messageBoxText, string title = "情報", string messageKey = "ShowInformationMessageAsync")
        {
            await ShowTargetMessageAsync(self, messageBoxText, title, MessageBoxImage.Information, messageKey);
        }

        /// <summary>
        /// 警告メッセージを非同期で表示します。
        /// </summary>
        /// <param name="self">ViewModel</param>
        /// <param name="messageBoxText">メッセージ本文</param>
        /// <param name="title">タイトル</param>
        /// <param name="messageKey">メッセージキー</param>
        /// <returns></returns>
        public static async Task ShowWarningMessageAsync(this ViewModel self, string messageBoxText, string title = "注意", string messageKey = "ShowWarningMessageAsync")
        {
            await ShowTargetMessageAsync(self, messageBoxText, title, MessageBoxImage.Warning, messageKey);
        }

        /// <summary>
        /// エラーメッセージを非同期で表示します。
        /// </summary>
        /// <param name="self">ViewModel</param>
        /// <param name="messageBoxText">メッセージ本文</param>
        /// <param name="title">タイトル</param>
        /// <param name="messageKey">メッセージキー</param>
        /// <returns></returns>
        public static async Task ShowErrorMessageAsync(this ViewModel self, string messageBoxText, string title = "エラー", string messageKey = "ShowErrorMessageAsync")
        {
            await ShowTargetMessageAsync(self, messageBoxText, title, MessageBoxImage.Error, messageKey);
        }

        /// <summary>
        /// 任意の種類のメッセージを非同期で表示します。
        /// </summary>
        /// <param name="self">ViewModel</param>
        /// <param name="messageBoxText">メッセージ本文</param>
        /// <param name="title">タイトル</param>
        /// <param name="image">メッセージの種類別アイコン</param>
        /// <param name="messageKey">メッセージキー</param>
        /// <returns></returns>
        private static async Task ShowTargetMessageAsync(ViewModel self, string messageBoxText, string title, MessageBoxImage image, string messageKey)
        {
            var mes = new InformationMessage(messageBoxText, title, image, messageKey);
            await self.Messenger.RaiseAsync(mes);
        }




        /// <summary>
        /// OK, Cancel 形式の確認メッセージを非同期で表示します。
        /// </summary>
        /// <param name="self">ViewModel</param>
        /// <param name="messageBoxText">メッセージ本文</param>
        /// <param name="title">タイトル</param>
        /// <param name="messageKey">メッセージキー</param>
        /// <param name="defaultResult">初期フォーカスをあてるボタン</param>
        /// <returns>結果</returns>
        public async static Task<ConfirmationMessage> ShowConfirmationOKCancelMessageAsync(this ViewModel self, string messageBoxText, string title = "確認", string messageKey = "ShowConfirmationOKCancelMessageAsync", MessageBoxResult defaultResult = MessageBoxResult.Cancel)
        {
            if (defaultResult != MessageBoxResult.OK && defaultResult != MessageBoxResult.Cancel)
                defaultResult = MessageBoxResult.Cancel;

            return await ShowConfirmationTargetMessageAsync(self, messageBoxText, title, MessageBoxButton.OKCancel, defaultResult, messageKey);
        }

        /// <summary>
        /// Yes, No 形式の確認メッセージを非同期で表示します。
        /// </summary>
        /// <param name="self">ViewModel</param>
        /// <param name="messageBoxText">メッセージ本文</param>
        /// <param name="title">タイトル</param>
        /// <param name="messageKey">メッセージキー</param>
        /// <param name="defaultResult">初期フォーカスをあてるボタン</param>
        /// <returns>結果</returns>
        public async static Task<ConfirmationMessage> ShowConfirmationYesNoMessageAsync(this ViewModel self, string messageBoxText, string title = "確認", string messageKey = "ShowConfirmationYesNoMessageAsync", MessageBoxResult defaultResult = MessageBoxResult.No)
        {
            if (defaultResult != MessageBoxResult.Yes && defaultResult != MessageBoxResult.No)
                defaultResult = MessageBoxResult.No;

            return await ShowConfirmationTargetMessageAsync(self, messageBoxText, title, MessageBoxButton.YesNo, defaultResult, messageKey);
        }

        /// <summary>
        /// Yes, No, Cancel 形式の確認メッセージを非同期で表示します。
        /// </summary>
        /// <param name="self">ViewModel</param>
        /// <param name="messageBoxText">メッセージ本文</param>
        /// <param name="title">タイトル</param>
        /// <param name="messageKey">メッセージキー</param>
        /// <param name="defaultResult">初期フォーカスをあてるボタン</param>
        /// <returns>結果</returns>
        public async static Task<ConfirmationMessage> ShowConfirmationYesNoCancelMessageAsync(this ViewModel self, string messageBoxText, string title = "確認", string messageKey = "ShowConfirmationYesNoCancelMessageAsync", MessageBoxResult defaultResult = MessageBoxResult.No)
        {
            if (defaultResult != MessageBoxResult.Yes && defaultResult != MessageBoxResult.No && defaultResult != MessageBoxResult.Cancel)
                defaultResult = MessageBoxResult.No;

            return await ShowConfirmationTargetMessageAsync(self, messageBoxText, title, MessageBoxButton.YesNoCancel, defaultResult, messageKey);
        }

        /// <summary>
        /// 任意の形式の確認メッセージを非同期で表示します。
        /// </summary>
        /// <param name="self">ViewModel</param>
        /// <param name="messageBoxText">メッセージ本文</param>
        /// <param name="title">タイトル</param>
        /// <param name="buttonKinds">表示するボタンの種類</param>
        /// <param name="defaultResult">初期フォーカスをあてるボタン</param>
        /// <param name="messageKey">メッセージキー</param>
        /// <returns>結果</returns>
        private async static Task<ConfirmationMessage> ShowConfirmationTargetMessageAsync(ViewModel self, string messageBoxText, string title, MessageBoxButton buttonKinds, MessageBoxResult defaultResult, string messageKey)
        {
            var mes = new ConfirmationMessage(messageBoxText, title, MessageBoxImage.Question, buttonKinds, defaultResult, messageKey);
            mes = await self.Messenger.GetResponseAsync(mes); // Messenger.RaiseAsync() しつつ、戻り値を取得している？

            return mes;
        }


        #endregion

        #region 画面遷移系


        /// <summary>
        /// 任意の画面を表示します。
        /// </summary>
        /// <param name="self">ViewModel</param>
        /// <param name="vm">表示したい画面にバインドする ViewModel</param>
        /// <param name="messageKey">メッセージキー</param>
        /// <returns></returns>
        public static TransitionMessage Show(this ViewModel self, ViewModel vm, string messageKey = "Show")
        {
            return ShowTargetWindow(self, vm, TransitionMode.Normal, messageKey);
        }

        /// <summary>
        /// 任意の画面をモーダル表示します。
        /// </summary>
        /// <param name="self">ViewModel</param>
        /// <param name="vm">表示したい画面にバインドする ViewModel</param>
        /// <param name="messageKey">メッセージキー</param>
        /// <returns></returns>
        public static TransitionMessage ShowDialog(this ViewModel self, ViewModel vm, string messageKey = "ShowDialog")
        {
            return ShowTargetWindow(self, vm, TransitionMode.Modal, messageKey);
        }

        /// <summary>
        /// 任意の画面を、任意の形式で表示します。
        /// </summary>
        /// <param name="self">ViewModel</param>
        /// <param name="vm">表示したい画面にバインドする ViewModel</param>
        /// <param name="mode">表示形式</param>
        /// <param name="messageKey">メッセージキー</param>
        /// <returns></returns>
        private static TransitionMessage ShowTargetWindow(ViewModel self, ViewModel vm, TransitionMode mode, string messageKey)
        {
            var msg = new TransitionMessage(vm, mode, messageKey);
            self.Messenger.Raise(msg);

            return msg;
        }



        /// <summary>
        /// 非同期で、任意の画面を表示します。
        /// </summary>
        /// <param name="self">ViewModel</param>
        /// <param name="vm">表示したい画面にバインドする ViewModel</param>
        /// <param name="messageKey">メッセージキー</param>
        /// <returns></returns>
        public async static Task<TransitionMessage> ShowAsync(this ViewModel self, ViewModel vm, string messageKey = "Show")
        {
            return await ShowTargetWindowAsync(self, vm, TransitionMode.Normal, messageKey);
        }

        /// <summary>
        /// 非同期で、任意の画面をモーダル表示します。
        /// </summary>
        /// <param name="self">ViewModel</param>
        /// <param name="vm">表示したい画面にバインドする ViewModel</param>
        /// <param name="messageKey">メッセージキー</param>
        /// <returns></returns>
        public async static Task<TransitionMessage> ShowDialogAsync(this ViewModel self, ViewModel vm, string messageKey = "ShowDialog")
        {
            return await ShowTargetWindowAsync(self, vm, TransitionMode.Modal, messageKey);
        }

        /// <summary>
        /// 非同期で、任意の画面を、任意の形式で表示します。
        /// </summary>
        /// <param name="self">ViewModel</param>
        /// <param name="vm">表示したい画面にバインドする ViewModel</param>
        /// <param name="mode">表示形式</param>
        /// <param name="messageKey">メッセージキー</param>
        /// <returns></returns>
        private async static Task<TransitionMessage> ShowTargetWindowAsync(ViewModel self, ViewModel vm, TransitionMode mode, string messageKey)
        {
            var msg = new TransitionMessage(vm, mode, messageKey);
            await self.Messenger.RaiseAsync(msg);

            return msg;
        }


        #endregion

        #region （バインド先の）View の状態変更系


        /// <summary>
        /// バインド先の Window を閉じます。
        /// </summary>
        /// <param name="self">ViewModel</param>
        public static void Close(this ViewModel self)
        {
            var mes = new WindowActionMessage(WindowAction.Close, "Close");
            self.Messenger.Raise(mes);
        }

        /// <summary>
        /// バインド先の Window を非同期で閉じます。
        /// </summary>
        /// <param name="self">ViewModel</param>
        /// <returns></returns>
        public async static Task CloseAsync(this ViewModel self)
        {
            var mes = new WindowActionMessage(WindowAction.Close, "CloseAsync");
            await self.Messenger.RaiseAsync(mes);
        }



        /// <summary>
        /// バインド先の Window を最大化します。
        /// </summary>
        /// <param name="self">ViewModel</param>
        public static void Maximize(this ViewModel self)
        {
            var mes = new WindowActionMessage(WindowAction.Maximize, "Maximize");
            self.Messenger.Raise(mes);
        }

        /// <summary>
        /// バインド先の Window を非同期で最大化します。
        /// </summary>
        /// <param name="self">ViewModel</param>
        /// <returns></returns>
        public async static Task MaximizeAsync(this ViewModel self)
        {
            var mes = new WindowActionMessage(WindowAction.Maximize, "MaximizeAsync");
            await self.Messenger.RaiseAsync(mes);
        }



        /// <summary>
        /// バインド先の Window を最小化します。
        /// </summary>
        /// <param name="self">ViewModel</param>
        public static void Minimize(this ViewModel self)
        {
            var mes = new WindowActionMessage(WindowAction.Minimize, "Minimize");
            self.Messenger.Raise(mes);
        }

        /// <summary>
        /// バインド先の Window を非同期で最小化します。
        /// </summary>
        /// <param name="self">ViewModel</param>
        /// <returns></returns>
        public async static Task MinimizeAsync(this ViewModel self)
        {
            var mes = new WindowActionMessage(WindowAction.Minimize, "MinimizeAsync");
            await self.Messenger.RaiseAsync(mes);
        }



        /// <summary>
        /// バインド先の Window をアクティブにします。
        /// </summary>
        /// <param name="self">ViewModel</param>
        public static void Active(this ViewModel self)
        {
            var mes = new WindowActionMessage(WindowAction.Active, "Active");
            self.Messenger.Raise(mes);
        }

        /// <summary>
        /// バインド先の Window を非同期でアクティブにします。
        /// </summary>
        /// <param name="self">ViewModel</param>
        /// <returns></returns>
        public async static Task ActiveAsync(this ViewModel self)
        {
            var mes = new WindowActionMessage(WindowAction.Active, "ActiveAsync");
            await self.Messenger.RaiseAsync(mes);
        }


        #endregion

    }
}
