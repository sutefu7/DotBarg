using DotBarg.Libraries;
using DotBarg.Models;
using DotBarg.Views.Controls;
using Microsoft.Xaml.Behaviors;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

/*
 * どうやら、いったんドキュメントペインを閉じて破棄する他に、
 * 別のドキュメントペイン選択から、再度選択された際に、初期化処理が走るみたい
 * → OnAttached(), OnDetaching(), OnSelectedItemPropertyChanged()
 * 
 * ペインを切り替えるたびに初期化される
 * 
 * 
 * 
 */



namespace DotBarg.Behaviors
{
    public class DefinitionDiscoveryTreeBehavior : Behavior<CanvasEx>
    {
        private static CanvasEx Self;

        private static ResizableThumb SelectedThumb;
        private static TextEditorEx SelectedEditor;


        protected override void OnAttached()
        {
            base.OnAttached();
            Self = AssociatedObject;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
        }



        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register(
                nameof(SelectedItem),
                typeof(TreeViewItemModel),
                typeof(DefinitionDiscoveryTreeBehavior),
                new PropertyMetadata(null, OnSelectedItemPropertyChanged));

        public TreeViewItemModel SelectedItem
        {
            get { return GetValue(SelectedItemProperty) as TreeViewItemModel; }
            set { SetValue(SelectedItemProperty, value); }
        }

        private static void OnSelectedItemPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var item = e.NewValue as TreeViewItemModel;
            if (item is null)
                return;

            // いったんペインを消した後、再度表示させた際、ゴミデータとして残っているため初期化
            SelectedThumb = null;
            SelectedEditor = null;

            SetData(item.FileName, 0);
        }

        private static void SetData(string sourceFile, int offset)
        {
            // コントロールを作成
            var newThumb = CreateControl(sourceFile, offset);

            // コントロールを追加
            Self.Children.Add(newThumb);

            // Canvas 上の表示位置をセット
            SetNewLocation(newThumb);

            if (SelectedThumb is null)
                return;

            // 既に表示済みの任意のコントロールと今回追加するコントロールを線でつなげる
            AddArrow(SelectedThumb, newThumb);
        }

        private static ResizableThumb CreateControl(string sourceFile, int offset)
        {
            var thumbTemplate = Self.Resources["EditorTemplate"] as ControlTemplate;
            var newThumb = new ResizableThumb
            {
                UseAdorner = true,
                Template = thumbTemplate,
            };

            newThumb.ApplyTemplate();
            newThumb.UpdateLayout();

            // ソース全体が長すぎると見づらい、探しづらい、理解しづらい
            // 仮対応として固定サイズで表示する。見づらかったらリサイズしてもらう運用
            newThumb.Width = 640;
            newThumb.Height = 480;

            // タイトルをセット
            var textBlock1 = newThumb.Template.FindName("textBlock1", newThumb) as TextBlock;

            var fi = new FileInfo(sourceFile);
            textBlock1.Text = $"{fi.Directory.Name}/{fi.Name}";

            // ソースコードをセット
            var editor1 = newThumb.Template.FindName("textEditor1", newThumb) as TextEditorEx;
            editor1.SourceFile = sourceFile;
            editor1.SourceCode = File.ReadAllText(sourceFile, Util.GetEncoding(sourceFile));

            // キャレット位置を移動
            editor1.TextArea.Caret.Offset = offset;

            // メンバー定義位置が見えるようにスクロール
            // （いまいちうまく扱えないスクロール処理
            // TextEditor.ScrollToVerticalOffset メソッドと、TextEditor.ScrollToLine メソッドの組み合わせで、うまくスクロールしてくれた）
            editor1.ScrollToVerticalOffset(offset);

            // メソッド定義の開始行が見やすいように、２行分上に表示する
            var jumpLine = editor1.Document.GetLineByOffset(offset).LineNumber;
            if (0 <= jumpLine - 2)
                jumpLine -= 2;

            editor1.ScrollToLine(jumpLine);

            // その他の設定
            editor1.Options.ConvertTabsToSpaces = true;
            editor1.Options.HighlightCurrentLine = true;

            // イベントの購読
            editor1.MouseDown += Editor1_MouseDown;

            var menuItem1 = newThumb.Template.FindName("menuItem1", newThumb) as MenuItem;
            menuItem1.Click += MenuItem1_Click;


            return newThumb;
        }

        private static void SetNewLocation(ResizableThumb newThumb)
        {
            var pos = default(Point);

            // 1つ目
            if (SelectedThumb is null)
            {
                pos = new Point(10, 10);
                Canvas.SetLeft(newThumb, pos.X);
                Canvas.SetTop(newThumb, pos.Y);
                return;
            }

            // 2つ目以降
            // 右隣、既にあれば、その真下（一番下）
            pos = GetRightSideLocation(SelectedThumb);
            var result = HasAlreadyPutControl(pos);

            if (result.Item1)
            {
                // 右隣りに既にあれば、その真下（一番下）
                var firstThumb = result.Item2;
                pos = GetMostBottomSideLocation(firstThumb);
                Canvas.SetLeft(newThumb, pos.X);
                Canvas.SetTop(newThumb, pos.Y);
                return;
            }
            else
            {
                // 右隣り
                Canvas.SetLeft(newThumb, pos.X);
                Canvas.SetTop(newThumb, pos.Y);
                return;
            }
        }

        // 基準コントロールの右隣り位置

        private static Point GetRightSideLocation(ResizableThumb previousThumb)
        {
            var newWidth = previousThumb.DesiredSize.Width;
            var newHeight = previousThumb.DesiredSize.Height;

            var alpha = 40;
            var newX = Canvas.GetLeft(previousThumb) + newWidth + alpha;
            var newY = Canvas.GetTop(previousThumb);

            var result = new Point(newX, newY);
            return result;
        }

        // 基準コントロールの真下、かつすべてのコントロールよりも真下の位置
        // あるツリー表示の2段目ツリーのイメージ

        private static Point GetMostBottomSideLocation(ResizableThumb previousThumb)
        {
            var newWidth = previousThumb.DesiredSize.Width;
            var newHeight = previousThumb.DesiredSize.Height;

            var alpha = 40;
            var newX = Canvas.GetLeft(previousThumb);
            var newY = Canvas.GetTop(previousThumb) + newHeight + alpha;

            // 予測計算した位置に、すでに他のコントロールが配置されていないかチェック
            // 一部が重なり合う場合、重ならないように予測位置を修正して、再度全チェック
            var items = Self.Children.OfType<ResizableThumb>().ToList();
            var found = false;

            while (true)
            {
                // 初期化してチャレンジ、または再チャレンジ
                found = false;
                for (var i = 1; i < items.Count; i++)
                {
                    var item = items[i];
                    var currentX = Canvas.GetLeft(item);
                    var currentY = Canvas.GetTop(item);

                    var currentWidth = item.DesiredSize.Width;
                    var currentHeight = item.DesiredSize.Height;

                    if (newY < currentY + currentHeight + alpha)
                    {
                        found = true;
                        newY = currentY + currentHeight + alpha;
                    }
                }

                // 既存配置しているコントロールリスト全てに重ならなかったので、この位置で決定
                if (!found)
                    break;

                // 見つかった場合は、１つ以上重なっていたことにより、予測位置を修正したので、
                // もう一度コントロールリスト全部と再チェック
            }

            var result = new Point(newX, newY);
            return result;
        }

        private static (bool, ResizableThumb) HasAlreadyPutControl(Point pos)
        {
            var items = Self.Children.OfType<ResizableThumb>();

            foreach (var item in items)
            {
                var currentX = Canvas.GetLeft(item);
                var currentY = Canvas.GetTop(item);

                var currentWidth = item.DesiredSize.Width;
                var currentHeight = item.DesiredSize.Height;

                var currentRect = new Rect(currentX, currentY, currentWidth, currentHeight);

                if (currentRect.Contains(pos))
                    return (true, item);
            }

            return (false, null);
        }

        private static void AddArrow(ResizableThumb previousThumb, ResizableThumb newThumb)
        {
            var arrow1 = new Arrow { Stroke = Brushes.LightPink, Fill = Brushes.LightPink };
            Self.Children.Add(arrow1);

            previousThumb.EndLines.Add(arrow1);
            newThumb.StartLines.Add(arrow1);

            ResizableThumb.UpdateLineLocation(previousThumb);
            ResizableThumb.UpdateLineLocation(newThumb);

            // なぜか、最後の図形だけ、矢印線が左上の角を指してしまう不具合
            // → ActualWidth, ActualHeight が 0 だから。いったん画面表示させないとダメか？
            // → Measure メソッドを呼び出して、希望サイズを更新する。こちらで矢印線の位置を調整する
            var newSize = new Size(50000, 50000);
            Self.Measure(newSize);
            ResizableThumb.UpdateLineLocation(newThumb);
        }

        private static void Editor1_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var editor1 = sender as TextEditorEx;
            SelectedEditor = editor1;

            var dock1 = editor1.Parent as DockPanel;
            var border1 = dock1.Parent as Border;
            SelectedThumb = border1.TemplatedParent as ResizableThumb;
        }

        private static async void MenuItem1_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedEditor is null || SelectedThumb is null)
                return;

            var srcFile = SelectedEditor.Document.FileName;
            var offset = SelectedEditor.TextArea.Caret.Offset;
            var result = await Util.FindSymbolAtPositionAsync(srcFile, offset);

            if (string.IsNullOrEmpty(result.SourceFile))
                return;

            SetData(result.SourceFile, result.Offset);
        }
    }
}
