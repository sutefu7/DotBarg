using DotBarg.Libraries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;

namespace DotBarg.Views.Controls
{
    public class TextBlockEx : TextBlock
    {
        // TreeView の ItemTemplate 内で TextBlock を使っているところまでは問題ないのだが、Inlines に Run クラスのオブジェクトをセットしている場合、
        // TreeView のノードをクリックした際、Run 型から Visual 型にキャストできない旨の、例外エラーが発生してしまうバグの対応
        // 
        // https://stackoverflow.com/questions/38325162/why-click-tree-throws-system-windows-documents-run-is-not-a-visual-or-visual3d
        // 
        // 上記より、
        // IsHitTestVisible に false をセットして、クリック反応の対象外にしてしまう

        public TextBlockEx() : base()
        {
            var fontSizeBinding = new Binding("FontSize")
            {
                Source = Util.MainVM,
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
            };
            SetBinding(FontSizeProperty, fontSizeBinding);

            IsHitTestVisible = false;
        }

        public static readonly DependencyProperty ColorTextProperty =
            DependencyProperty.Register(
                nameof(ColorText),
                typeof(string),
                typeof(TextBlockEx),
                new PropertyMetadata(null, OnColorTextPropertyChanged));

        public string ColorText
        {
            get { return GetValue(ColorTextProperty) as string; }
            set { SetValue(ColorTextProperty, value); }
        }

        private static void OnColorTextPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = d as TextBlockEx;
            var s = e.NewValue.ToString();

            if (!(self is null) && !string.IsNullOrEmpty(s))
            {
                if (s.Contains("(") || s.Contains(":") || s.Contains(" As "))
                    SetData(self, s);
                else
                    self.Text = s;
            }
        }

        private static void SetData(TextBlockEx self, string s)
        {
            if (self.Inlines.Any())
                self.Inlines.Clear();

            var items = CreateControls(s);

            foreach (var item in items)
                self.Inlines.Add(item);
        }

        private static IEnumerable<Run> CreateControls(string value)
        {
            var tokens = ConvertToken(value);
            var firstIdentifier = true;
            var firstParenOffset = -1;
            var lastParenOffset = -1;

            // カッコがある場合、引数を見やすくするため、スペースを追加する
            // ただし、引数がある場合だけに制限する
            if (value.Contains("("))
            {
                // 戻り値がある場合かつ、戻り値が配列の場合、誤判定防止のため削る
                var tmp = value;

                // C#
                if (tmp.Contains(" : "))
                    tmp = tmp.Substring(0, tmp.LastIndexOf(" : "));

                // VBNet
                // シグネチャとして表示させるため、引数名は消去している。つまり、
                // Func(Integer, String) As Integer と、As が付く場合は戻り値だけ
                if (tmp.Contains(" As "))
                    tmp = tmp.Substring(0, tmp.LastIndexOf(" As "));

                // 引数無しを置換してもカッコがある場合、引数があるということ
                if (tmp.Replace("()", string.Empty).Contains("("))
                {
                    // VBNet
                    // Func1(Integer)
                    // Func1(Dictionary(Of Integer, String()))
                    // Names(,,,)
                    firstParenOffset = tmp.IndexOf("(");
                    lastParenOffset = tmp.LastIndexOf(")");
                }
            }

            var previousToken = default(Token);

            foreach (var token in tokens)
            {
                switch (token.TokenKinds)
                {
                    case TokenKinds.Comma:

                        // 後置スペース
                        // 引数カンマは後置スペースを入れたいが、
                        // 2次元配列などの場合は入れたくない
                        // Func(int i, string s)
                        // string[,] items
                        // → TokenKinds.Identifier 側で対応する
                        yield return new Run { Foreground = Brushes.Black, Text = $"{token.Value}" };
                        break;

                    case TokenKinds.Coron:

                        // 前後置スペース
                        yield return new Run { Foreground = Brushes.Black, Text = $" {token.Value} " };
                        break;

                    case TokenKinds.Parentheses:

                        if (token.StartOffset == firstParenOffset)
                        {
                            yield return new Run { Foreground = Brushes.Black, Text = $"{token.Value} " };
                        }
                        else if (token.StartOffset == lastParenOffset)
                        {
                            yield return new Run { Foreground = Brushes.Black, Text = $" {token.Value}" };
                        }
                        else
                        {
                            yield return new Run { Foreground = Brushes.Black, Text = $"{token.Value}" };
                        }

                        break;

                    case TokenKinds.Identifier:

                        // 1つ前のトークンがカンマの場合、前置スペースを追加する
                        // 引数に属性がついている場合は、後置スペースを追加する
                        var frontSpace = string.Empty;
                        var rearSpace = string.Empty;

                        if (previousToken?.TokenKinds == TokenKinds.Comma)
                            frontSpace = " ";

                        if (firstIdentifier)
                        {
                            // 定義名
                            yield return new Run { Foreground = Brushes.Black, Text = $"{token.Value}" };
                            firstIdentifier = false;
                        }
                        else
                        {
                            // キーワードと定義で色分けする
                            var b1 = Brushes.LightSeaGreen;

                            if (IsKeyword(token.Value))
                                b1 = Brushes.Blue;

                            if (IsSubKeyword(token.Value))
                            {
                                b1 = Brushes.Blue;
                                rearSpace = " ";

                                // As キーワードの場合、前後スペース
                                if (token.Value == "As")
                                    frontSpace = " ";
                            }

                            yield return new Run { Foreground = b1, Text = $"{frontSpace}{token.Value}{rearSpace}" };
                        }

                        break;
                }

                previousToken = token;
            }
        }



        //

        private enum TokenKinds
        {
            // 文字列（キーワード、定義名は共通）
            Identifier,

            // カッコ（()<>{}[]）
            Parentheses,

            // ,
            Comma,

            // :
            Coron,
        }

        private class Token
        {
            public int StartOffset { get; set; }

            public string Value { get; set; }

            public TokenKinds TokenKinds { get; set; }
        }

        private static IEnumerable<Token> ConvertToken(string value)
        {
            // C#, VBNet 共通

            var position = -1;
            var buffer = new StringBuilder();
            var keywords = new List<char> { '(', ')', '<', '>', '[', ']', ',', ':', ' ' };

            for (var i = 0; i < value.Length; i++)
            {
                //
                var currentChar = value[i];
                var nextChar = '\0';
                var thirdChar = '\0';

                if (i + 1 < value.Length) nextChar = value[i + 1];
                if (i + 2 < value.Length) thirdChar = value[i + 2];

                position++;


                //
                switch (currentChar)
                {
                    case '(':
                    case ')':
                    case '<':
                    case '>':
                    case '[':
                    case ']':

                        yield return new Token { StartOffset = position, TokenKinds = TokenKinds.Parentheses, Value = $"{currentChar}" };
                        break;

                    case ',':

                        yield return new Token { StartOffset = position, TokenKinds = TokenKinds.Comma, Value = $"{currentChar}" };
                        break;

                    case ':':

                        yield return new Token { StartOffset = position, TokenKinds = TokenKinds.Coron, Value = $"{currentChar}" };
                        break;

                    case ' ':
                        break;

                    default:

                        buffer.Append(currentChar);

                        if (keywords.Contains(nextChar))
                        {
                            yield return new Token { StartOffset = position, TokenKinds = TokenKinds.Identifier, Value = $"{buffer}" };
                            buffer.Clear();
                        }

                        break;
                }
            }

            if (buffer.Length != 0)
            {
                yield return new Token { StartOffset = position, TokenKinds = TokenKinds.Identifier, Value = $"{buffer}" };
                buffer.Clear();
            }
        }

        private static bool IsKeyword(string value)
        {
            switch (AppEnv.Languages)
            {
                case Languages.CSharp:

                    if (AppEnv.LanguageConversions.Any(x => x.CSharpType == value))
                        return true;
              
                    break;

                case Languages.VBNet:

                    if (AppEnv.LanguageConversions.Any(x => x.VBNetType == value))
                        return true;

                    break;
            }

            return false;
        }

        private static bool IsSubKeyword(string value)
        {
            var keywords = new List<string>();

            switch (AppEnv.Languages)
            {
                case Languages.CSharp:

                    keywords.AddRange(new string[] { "ref", "in", "out", "params" });

                    if (keywords.Contains(value))
                        return true;

                    break;

                case Languages.VBNet:

                    keywords.AddRange(new string[] { "ByRef", "ParamArray", "Of", "As" });

                    if (keywords.Contains(value))
                        return true;

                    break;
            }

            return false;
        }
    }
}
