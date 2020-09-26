using DotBarg.Libraries;
using DotBarg.Libraries.AvalonEdits;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Highlighting;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DotBarg.Views.Controls
{
    public class TextEditorEx : TextEditor
    {
        #region SourceFile 依存関係プロパティ


        public static readonly DependencyProperty SourceFileProperty =
            DependencyProperty.Register(
                nameof(SourceFile),
                typeof(string),
                typeof(TextEditorEx),
                new PropertyMetadata(string.Empty, OnSourceFilePropertyChanged));

        private static void OnSourceFilePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var self = sender as TextEditorEx;

            // 謎のタイミングで空文字列が入ってくるバグの対応
            if (!string.IsNullOrEmpty(self.SourceFile) && string.IsNullOrEmpty(e.NewValue?.ToString()))
                return;

            self.SourceFile = e.NewValue.ToString();
        }

        public string SourceFile
        {
            get { return base.Document.FileName; }
            set { base.Document.FileName = value; }
        }

        #endregion

        #region SourceCode 依存関係プロパティ


        public static readonly DependencyProperty SourceCodeProperty =
            DependencyProperty.Register(
                nameof(SourceCode),
                typeof(string),
                typeof(TextEditorEx),
                new PropertyMetadata(string.Empty, OnSourceCodePropertyChanged));

        private static void OnSourceCodePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var self = sender as TextEditorEx;

            // 謎のタイミングで空文字列が入ってくるバグの対応
            if (!string.IsNullOrEmpty(self.SourceCode) && string.IsNullOrEmpty(e.NewValue?.ToString()))
                return;

            self.SourceCode = e.NewValue.ToString();
        }

        /*
         * ※ TextEditor.Document.Text の方を対象にしないでください。
         * TextEditor.Text 内で初期化処理をおこなっているため、こちらを対象にしています。
         * 詳しくは、https://github.com/icsharpcode/AvalonEdit/blob/master/ICSharpCode.AvalonEdit/TextEditor.cs
         * 
         * 
         */

        public string SourceCode
        {
            get 
            { 
                return base.Text; 
            }
            set
            {
                base.Text = value;

                var language = GetLanguages(SourceFile);
                SyntaxHighlighting = GetSyntaxHighlight(language);

                var strategy = default(IFoldingStrategy);

                switch (language)
                {
                    case Languages.CSharp:
                        strategy = new CSharpFoldingStrategy();
                        break;

                    case Languages.VBNet:
                        strategy = new VBNetFoldingStrategy();
                        break;
                }

                // ソースコードがセットされたので、VB.NET 言語用の折りたたみルールを適用
                // ※（雑談）本ツールでは（仕様上問題無いので）何もしていないが、アプリの作りによっては、
                // いったん FoldingManager.Uninstall した後で、Install し直す手順が必要になる場合があるみたい
                var manager = FoldingManager.Install(base.TextArea);
                strategy.UpdateFoldings(manager, base.Document);
            }
        }

        private Languages GetLanguages(string sourceFile)
        {
            var result = default(Languages);
            var ext = Path.GetExtension(sourceFile).ToLower();

            switch (ext)
            {
                case ".cs":
                    result = Languages.CSharp;
                    break;

                case ".vb":
                    result = Languages.VBNet;
                    break;
            }

            return result;
        }

        private IHighlightingDefinition GetSyntaxHighlight(Languages language)
        {
            var result = default(IHighlightingDefinition);

            switch (language)
            {
                case Languages.CSharp:
                    result = HighlightingManager.Instance.GetDefinition("C#");
                    break;

                case Languages.VBNet:
                    result = HighlightingManager.Instance.GetDefinition("VB");
                    break;
            }

            return result;
        }


        #endregion

        #region CaretLocation 依存関係プロパティ


        public static readonly DependencyProperty CaretLocationProperty =
            DependencyProperty.Register(
                nameof(CaretLocation),
                typeof(TextLocation),
                typeof(TextEditorEx),
                new PropertyMetadata(OnCaretLocationPropertyChanged));

        private static void OnCaretLocationPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var self = sender as TextEditorEx;
            self.CaretLocation = (TextLocation)e.NewValue;
        }

        public TextLocation CaretLocation
        {
            get
            {
                return base.TextArea.Caret.Location;
            }
            set
            {
                // Caret_PositionChanged イベントが、２回連続で発生してしまう不具合の対応
                if (base.TextArea.Caret.Location == value)
                    return;

                base.TextArea.Caret.Location = value;
            }
        }

        #endregion

        #region CaretOffset 依存関係プロパティ


        public static readonly DependencyProperty CaretOffsetProperty =
            DependencyProperty.Register(
                nameof(CaretOffset),
                typeof(int),
                typeof(TextEditorEx),
                new PropertyMetadata(OnCaretOffsetPropertyChanged));

        private static void OnCaretOffsetPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var self = sender as TextEditorEx;
            self.CaretOffset = (int)e.NewValue;

            // キャレット位置（メンバー定義位置）までスクロールが見えるようにスクロール
            var jumpLine = self.Document.GetLineByOffset(self.CaretOffset).LineNumber;
            self.ScrollToLine(jumpLine);
        }


        /*
         * 今のところ、TextEditor.CaretOffset プロパティは、TextEditor.TextArea.Caret.Offset をラップしただけのプロパティになっているが、
         * Text プロパティみたいに、将来の機能修正を考慮して TextEditor.CaretOffset の方をラップする
         * 
         * また、継承元クラスに同名のプロパティがあるため、継承元クラス側のプロパティは隠した
         * 依存関係プロパティと対になる CLR プロパティを定義しなくてはいけない仕様みたいなので定義している（これを書かないと xaml 上で見えない？）が、
         * しなくてもいいのであれば、消したい（他の依存関係プロパティも同じ）
         * 
         */

        public new int CaretOffset
        {
            get
            {
                return base.CaretOffset;
            }
            set
            {
                // Caret_PositionChanged イベントが、２回連続で発生してしまう不具合の対応
                if (base.CaretOffset == value)
                    return;

                base.CaretOffset = value;
            }
        }


        #endregion

        #region CaretPositionChanged ルーティングイベント


        public static readonly RoutedEvent CaretPositionChangedEvent =
            EventManager.RegisterRoutedEvent(
                nameof(CaretPositionChanged),
                RoutingStrategy.Bubble,
                typeof(RoutedEventHandler),
                typeof(TextEditorEx));

        public event RoutedEventHandler CaretPositionChanged
        {
            add { AddHandler(CaretPositionChangedEvent, value); }
            remove { RemoveHandler(CaretPositionChangedEvent, value); }
        }

        private void Caret_PositionChanged(object sender, EventArgs e)
        {
            // VM 側のデータが更新されない不具合の対応
            // CLR プロパティ -> 依存関係プロパティへの同期がおこなわれていないため、更新後の値を同期する
            SetValue(CaretLocationProperty, CaretLocation);
            SetValue(CaretOffsetProperty, CaretOffset);

            var newEventArgs = new RoutedEventArgs(CaretPositionChangedEvent, this);
            RaiseEvent(newEventArgs);
        }


        #endregion

        #region コンストラクタ


        public TextEditorEx() : base()
        {
            FontSize = AppEnv.FontSize;

            IsReadOnly = true;
            ShowLineNumbers = true;
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;

            Options.ConvertTabsToSpaces = true;
            Options.HighlightCurrentLine = true;

            PreviewMouseWheel += TextEditorEx_PreviewMouseWheel;

            TextArea.Caret.PositionChanged += Caret_PositionChanged;
        }


        #endregion

        #region マウスホイールとコントロークキーで、サイズ変更


        private void TextEditorEx_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            // 左側、または右側にある Control キーが押されている場合（かつマウスホイールを回した場合）、拡大・縮小を実施
            var isDownLeftControlKey = (Keyboard.GetKeyStates(Key.LeftCtrl) & KeyStates.Down) == KeyStates.Down;
            var isDownRightControlKey = (Keyboard.GetKeyStates(Key.RightCtrl) & KeyStates.Down) == KeyStates.Down;
            var isDownControlKey = isDownLeftControlKey || isDownRightControlKey;

            if (isDownControlKey)
            {
                var self = sender as TextEditorEx;
                if (0 < e.Delta)
                {
                    self.FontSize *= 1.1;
                }
                else
                {
                    self.FontSize /= 1.1;
                }
            }
        }


        #endregion

    }
}
