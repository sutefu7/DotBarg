using DotBarg.Models;
using DotBarg.Views.Controls;
using Microsoft.Xaml.Behaviors;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

/*
 * 以下の専用 Behavior です。
 * 
 * SourceView
 * ・継承元ツリー　タブページ
 * 
 * （私の技術力だと）MVVM 形式では難しいため、コードビハインド的に、渡されてきたデータを図形表示させます。
 * 
 * Behavior は、各クラスに添付して使うが、それぞれインスタンス生成されるのではなく、static 使用されるみたいなので、
 * 継承元ツリー、継承先ツリーで共通使用すると、どちらかのデータが消えてしまう動作だったため、機能ごとに分割しました。
 * 
 * 
 */


namespace DotBarg.Behaviors
{
    public class InheritanceSourceTreeBehavior : Behavior<CanvasEx>
    {
        private static CanvasEx Self = null;

        protected override void OnAttached()
        {
            base.OnAttached();
            Self = AssociatedObject;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
        }

        public static readonly DependencyProperty InheritanceSourceTreeItemsProperty =
            DependencyProperty.Register(
                nameof(InheritanceSourceTreeItems),
                typeof(ObservableCollection<DefinitionItemModel>),
                typeof(InheritanceSourceTreeBehavior),
                new PropertyMetadata(null, OnInheritanceSourceTreeItemsPropertyChanged));

        public ObservableCollection<DefinitionItemModel> InheritanceSourceTreeItems
        {
            get { return GetValue(InheritanceSourceTreeItemsProperty) as ObservableCollection<DefinitionItemModel>; }
            set { SetValue(InheritanceSourceTreeItemsProperty, value); }
        }

        private static void OnInheritanceSourceTreeItemsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var parameters = e.NewValue as ObservableCollection<DefinitionItemModel>;

            if (!(parameters is null)) 
            {
                parameters.CollectionChanged -= Parameters_CollectionChanged;
                parameters.CollectionChanged += Parameters_CollectionChanged;
            }
        }

        private static void Parameters_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            var models = sender as ObservableCollection<DefinitionItemModel>;
            if (models is null)
                return;

            Self.Children.Clear();

            if (!(models is null) && models.Any())
                ShowData(models);
        }

        // Model に対応する View を作成してバインドさせて、Canvas.Children に追加していきます。

        // 謎挙動
        // デバッグ時、DefinitionItemModel.MemberTreeItems : ObservableCollection<TreeViewItemModel> について、
        // 1件以上データがある場合でも 0件と出てしまう、でも表示された画面を見るとちゃんとその件数分表示されている。

        private static void ShowData(ObservableCollection<DefinitionItemModel> models)
        {
            foreach (var model in models)
                ShowData(model);
        }

        private static void ShowData(DefinitionItemModel model)
        {
            // コントロールを作成
            var newThumb = CreateControl(model);

            // 1つ前に登録したコントロールがあれば取得しておく
            var previousThumb = default(ResizableThumb);
            if (Self.Children.OfType<ResizableThumb>().Any())
                previousThumb = Self.Children.OfType<ResizableThumb>().LastOrDefault();

            // コントロールを追加
            Self.Children.Add(newThumb);

            // Canvas 上の表示位置をセット
            SetNewLocation(model, newThumb, previousThumb);

            if (previousThumb is null)
                return;

            // 既に表示済みの任意のコントロールと今回追加するコントロールを線でつなげる
            // つなげたい既存コントロールが１つ前とは限らないため、妥当なコントロールを取得
            previousThumb = GetRelationControl(newThumb);

            if (previousThumb is null)
                return;

            AddArrow(previousThumb, newThumb);
        }

        private static ResizableThumb CreateControl(DefinitionItemModel model)
        {
            var thumbTemplate = Self.Resources["DefinitionMemberTemplate"] as ControlTemplate;
            var newThumb = new ResizableThumb();
            newThumb.Template = thumbTemplate;
            newThumb.DataContext = model;

            // いる？
            var newSize = new Size(50000, 50000);
            Self.Measure(newSize);

            newThumb.ApplyTemplate();
            newThumb.UpdateLayout();

            return newThumb;
        }

        private static void SetNewLocation(DefinitionItemModel model, ResizableThumb newThumb, ResizableThumb previousThumb)
        {
            // 1つ目
            if (previousThumb is null)
            {
                var pos = new Point(10, 10);
                Canvas.SetLeft(newThumb, pos.X);
                Canvas.SetTop(newThumb, pos.Y);
                return;
            }

            // 2つ目以降
            var previousModel = previousThumb.DataContext as DefinitionItemModel;

            if (previousModel.DefinitionName == model.RelationName)
            {
                // 右下
                var pos = GetRightBottomSideLocation(previousThumb);
                Canvas.SetLeft(newThumb, pos.X);
                Canvas.SetTop(newThumb, pos.Y);
                return;
            }
            else
            {
                // 位置関係によっては、１つ右ずれしてしまうバグの対応
                // 関係元のコントロールを基準として、その最初の関係するコントロールの真下とする
                previousThumb = GetRelationFirstChildControl(newThumb);

                // 真下
                var pos = GetMostBottomSideLocation(previousThumb);
                Canvas.SetLeft(newThumb, pos.X);
                Canvas.SetTop(newThumb, pos.Y);
                return;
            }
        }

        // 基準コントロールの真下位置

        private static Point GetBottomSideLocation(ResizableThumb previousThumb)
        {
            var newWidth = previousThumb.DesiredSize.Width;
            var newHeight = previousThumb.DesiredSize.Height;

            var alpha = 40;
            var newX = Canvas.GetLeft(previousThumb);
            var newY = Canvas.GetTop(previousThumb) + newHeight + alpha;

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

        // 基準コントロールの右下位置

        private static Point GetRightBottomSideLocation(ResizableThumb previousThumb)
        {
            var newWidth = previousThumb.DesiredSize.Width;
            var newHeight = previousThumb.DesiredSize.Height;

            var alpha = 40;
            var newX = Canvas.GetLeft(previousThumb) + newWidth + alpha;
            var newY = Canvas.GetTop(previousThumb) + newHeight + alpha;

            var result = new Point(newX, newY);
            return result;
        }

        private static Point GetNewLocation()
        {
            // ActualWidth, ActualHeight / DesiredSize.Width, DesiredSize.Height
            // いまいち 0 から値が更新されるタイミングが分からない

            // Canvas 的に、最も右下に位置する ResizableThumb を探す
            var items = Self.Children.OfType<ResizableThumb>()?.ToList();
            if (items is null || !items.Any())
                return new Point(10, 10);

            // ドラッグアンドドロップでコントロールを移動できるので、
            // Canvas.Children の登録順に、右に位置しているかどうかは判断できない
            // 既に表示されている図形のうち、１つ目の図形位置を基準として、今回の図形位置を計算する
            var item = items[0];
            var newWidth = item.DesiredSize.Width;
            var newHeight = item.DesiredSize.Height;

            // 基準コントロールの右隣り、かつちょっとだけ下辺り
            var alpha = 40;
            var newX = Canvas.GetLeft(item) + newWidth + alpha;
            var newY = Canvas.GetTop(item) + alpha;

            // 予測計算した位置・コントロールの大きさの内に、すでに他のコントロールが配置されていないかチェック
            // 一部が重なり合う場合、重ならないように予測位置を修正して、再度全チェック
            var found = false;

            while (true)
            {
                // 初期化してチャレンジ、または再チャレンジ
                found = false;
                for (var i = 1; i < items.Count; i++)
                {
                    item = items[i];
                    var currentX = Canvas.GetLeft(item);
                    var currentY = Canvas.GetTop(item);

                    var currentWidth = item.DesiredSize.Width;
                    var currentHeight = item.DesiredSize.Height;

                    var currentRect = new Rect(currentX, currentY, currentWidth, currentHeight);
                    var newRect = new Rect(newX, newY, newWidth, newHeight);

                    var b1 = currentRect.Contains(newRect.TopLeft);
                    var b2 = currentRect.Contains(newRect.TopRight);
                    var b3 = currentRect.Contains(newRect.BottomLeft);
                    var b4 = currentRect.Contains(newRect.BottomRight);
                    if (b1 || b2 || b3 || b4)
                    {
                        // 重なっている図形の【右下ちょい上くらい】まで移動
                        found = true;
                        newX = currentX + currentWidth + alpha;
                        newY = currentY + alpha;
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

        private static ResizableThumb GetRelationControl(ResizableThumb target)
        {
            // 1つ目の追加コントロールだった
            var targetModel = target.DataContext as DefinitionItemModel;
            if (string.IsNullOrEmpty(targetModel.RelationName))
                return null;

            var items = Self.Children.OfType<ResizableThumb>().ToList();

            for (var i = items.Count - 1; i >= 0; i--)
            {
                var item = items[i];
                var currentModel = item.DataContext as DefinitionItemModel;

                // 自分自身は飛ばす
                if (item == target)
                    continue;

                if (currentModel.DefinitionName == targetModel.RelationName)
                    return item;
            }

            // 見つからなかった
            return null;
        }

        private static ResizableThumb GetRelationFirstChildControl(ResizableThumb target)
        {
            var relationThumb = GetRelationControl(target);
            if (relationThumb is null)
                return null;

            var relationModel = relationThumb.DataContext as DefinitionItemModel;
            if (relationModel is null)
                return null;

            var items = Self.Children.OfType<ResizableThumb>().ToList();

            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                var currentModel = item.DataContext as DefinitionItemModel;

                if (currentModel.RelationName == relationModel.DefinitionName)
                    return item;
            }

            return null;
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
    }
}
