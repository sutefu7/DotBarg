using DotBarg.Libraries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace DotBarg.Views.Controls
{
    public class TextBlockEx : TextBlock
    {
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
                if (s.Contains("(") || s.Contains(":"))
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

        private static List<Run> CreateControls(string value)
        {
            var items = new List<Run>();
            var buffer = new StringBuilder();

            var keywords = new List<char> { '(', ')', '[', ']', '<', '>', ',', ':' };
            var foundFirstStartParentheses = false;

            for (var i = 0; i < value.Length; i++)
            {
                var currentChar = value[i];
                var nextChar = '\0';

                if (i + 1 < value.Length)
                    nextChar = value[i + 1];

                if (currentChar == ' ')
                {
                    // スペースが来た時にバッファにためている場面は、以下か（Of, ByRef, ParamArray）
                    // VBNet
                    // GetAge(IEnumerable(Of Integer), Dictionary(Of Integer, String), [Integer]) : Double
                    // GetName(ByRef Integer, ParamArray String()) : String()
                    if (buffer.Length != 0 && items.Any() && items.LastOrDefault().Text == "(")
                    {
                        items.Add(new Run { Foreground = Brushes.Blue, Text = $"{buffer} " });
                        buffer.Clear();
                    }

                    continue;
                }

                if (buffer.Length != 0 && keywords.Contains(currentChar))
                {
                    if (!foundFirstStartParentheses)
                    {
                        items.Add(new Run { Foreground = Brushes.Black, Text = $"{buffer}" });
                        foundFirstStartParentheses = true;
                    }
                    else
                    {
                        // キーワードと定義で色分けする
                        var b1 = Brushes.LightSeaGreen;

                        if (IsKeyword($"{buffer}"))
                            b1 = Brushes.Blue;

                        items.Add(new Run { Foreground = b1, Text = $"{buffer}" });
                    }

                    buffer.Clear();
                }

                switch (currentChar)
                {
                    case '(':
                    case ')':
                    case '<':
                    case '>':
                    case '[':
                    case ']':

                        items.Add(new Run { Foreground = Brushes.Black, Text = $"{currentChar}" });
                        break;

                    case ',':

                        // 後置スペース
                        items.Add(new Run { Foreground = Brushes.Black, Text = $"{currentChar} " });
                        break;

                    case ':':

                        // 前後スペース
                        items.Add(new Run { Foreground = Brushes.Black, Text = $" {currentChar} " });
                        break;

                    default:

                        buffer.Append(currentChar);
                        break;
                }
            }

            // カッコがあり、引数がある場合
            // 共通, Func1( int, int ) など、引数を見やすくしたい
            if (items.Any(x => x.Text == "("))
            {
                // 引数があるかどうかチェック
                var firstIndex = items.FindIndex(x => x.Text == "(");
                var lastIndex = items.FindLastIndex(x => x.Text == ")");
                if (firstIndex + 1 == lastIndex)
                {
                    // nop.
                }
                else
                {
                    var first = items.FirstOrDefault(x => x.Text == "(");
                    first.Text = "( ";

                    var last = items.LastOrDefault(x => x.Text == ")");
                    last.Text = " )";
                }
            }

            // 戻り値がある型の場合でかつ、単数の型の場合
            // 戻り値が残っている場合は追加
            if (buffer.Length != 0)
            {
                // キーワードと定義で色分けする
                var b1 = Brushes.LightSeaGreen;

                if (IsKeyword($"{buffer}"))
                    b1 = Brushes.Blue;

                items.Add(new Run { Foreground = b1, Text = $"{buffer}" });
            }

            return items;
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
    }
}
