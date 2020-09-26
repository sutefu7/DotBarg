using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;

/*
 * このコントロールは、Canvas コントロール上に配置して使われることを前提に作成しています。
 * それ以外のコンテナコントロール（Grid 他）上に配置した場合の考慮は、含まれていません。
 * 
 * 
 */

namespace DotBarg.Views.Controls
{
    public class ResizableThumb : Thumb
    {
        #region フィールド・プロパティ


        private AdornerLayer _AdornerLayer;

        public List<Arrow> StartLines;
        public List<Arrow> EndLines;

        #endregion

        #region UseAdorner 依存関係プロパティ


        public static readonly DependencyProperty UseAdornerProperty =
            DependencyProperty.Register(
                nameof(UseAdorner),
                typeof(bool),
                typeof(ResizableThumb),
                new FrameworkPropertyMetadata(false, UseAdornerPropertyChanged));

        private static void UseAdornerPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var changedThumb = sender as ResizableThumb;
            if (changedThumb is null)
                return;

            // UseAdorner プロパティが False なのに、IsSelected プロパティが True の場合、False に戻す
            var useAdornerValue = (bool)e.NewValue;
            if (!useAdornerValue)
            {
                if (changedThumb.IsSelected)
                    changedThumb.IsSelected = false;

                return;
            }
        }

        public bool UseAdorner
        {
            get { return (bool)GetValue(UseAdornerProperty); }
            set { SetValue(UseAdornerProperty, value); }
        }


        #endregion

        #region IsSelected 依存関係プロパティ


        public static readonly DependencyProperty IsSelectedProperty =
            DependencyProperty.Register(
                nameof(IsSelected),
                typeof(bool),
                typeof(ResizableThumb),
                new FrameworkPropertyMetadata(false, IsSelectedPropertyChanged, IsSelectedPropertyCoerceValue));

        private static void IsSelectedPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var changedThumb = sender as ResizableThumb;
            if (changedThumb is null)
                return;

            // 変更後の値を見て、Adorner 装飾を取り付けたり外したりする
            var isSelectedValue = (bool)e.NewValue;
            if (isSelectedValue)
                changedThumb.AttachAdorner();
            else
                changedThumb.DetachAdorner();
        }

        private static object IsSelectedPropertyCoerceValue(DependencyObject sender, object boolObject)
        {
            var changedThumb = sender as ResizableThumb;
            if (changedThumb is null)
                return boolObject;

            // UseAdorner プロパティが False の場合、IsSelected プロパティに True/False をセットしても False として受け取る
            if (!changedThumb.UseAdorner)
                return false;

            return boolObject;
        }

        public bool IsSelected
        {
            get { return (bool)GetValue(IsSelectedProperty); }
            set { SetValue(IsSelectedProperty, value); }
        }


        #endregion

        #region コンストラクタ


        public ResizableThumb() : base()
        {
            StartLines = new List<Arrow>();
            EndLines = new List<Arrow>();

            Loaded += ResizableThumb_Loaded;
            DragDelta += ResizableThumb_DragDelta;
        }


        #endregion

        #region ロード


        private void ResizableThumb_Loaded(object sender, RoutedEventArgs e)
        {
            // コンストラクタで実行すると、xaml例外エラーが発生するため、ロードイベントで実行するように、実行タイミングを変更

            /*
             * 親インスタンスを取得
             * 背景色が未セットの場合、透明色？を塗る（何かを塗っていないと、クリック判定されない現象の対応）
             * 
             * WPF:CanvasなどのコントロールはBackground/Fillを明示的に指定しないとマウスイベントが発生しない
             * https://qiita.com/nossey/items/3cf152c5fc2a2f24f585
             * 
             * 
             * 
             */

            var parentCanvas = Parent as Canvas;
            if (parentCanvas.Background is null)
                parentCanvas.Background = Brushes.Transparent;

            parentCanvas.PreviewMouseLeftButtonDown += ParentCanvas_PreviewMouseLeftButtonDown;
        }


        #endregion

        #region ドラッグ移動


        private void ResizableThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            var moveThumb = e.Source as ResizableThumb;
            if (moveThumb is null)
                return;

            var movedLeft = Canvas.GetLeft(moveThumb) + e.HorizontalChange;
            var movedTop = Canvas.GetTop(moveThumb) + e.VerticalChange;

            Canvas.SetLeft(moveThumb, movedLeft);
            Canvas.SetTop(moveThumb, movedTop);

            UpdateLineLocation(moveThumb);

        }

        // 他のクラスからも参照している

        public static void UpdateLineLocation(ResizableThumb target)
        {
            var newX = Canvas.GetLeft(target);
            var newY = Canvas.GetTop(target);

            var newWidth = target.ActualWidth;
            var newHeight = target.ActualHeight;

            if (double.IsNaN(newWidth) || newWidth == 0)
                newWidth = target.DesiredSize.Width;

            if (double.IsNaN(newHeight) || newHeight == 0)
                newHeight = target.DesiredSize.Height;

            // 終了位置
            for (var i = 0; i < target.EndLines.Count; i++)
            {
                target.EndLines[i].X2 = newX + newWidth;
                target.EndLines[i].Y2 = newY + (newHeight / 2);
            }

            // 開始位置、矢印側
            for (var i = 0; i < target.StartLines.Count; i++)
            {
                target.StartLines[i].X1 = newX;
                target.StartLines[i].Y1 = newY + (newHeight / 2);
            }
        }


        #endregion

        #region 親キャンバスのプレビューマウス左ボタンダウン


        private void ParentCanvas_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var clickedElement = e.Source as UIElement;
            if (clickedElement is null)
                return;

            if (clickedElement == this)
                IsSelected = true;
            else
                IsSelected = false;
        }


        #endregion

        #region その他のメソッド


        // AttachAdorner メソッドは直接呼び出さず、IsSelected プロパティを利用してください。
        private void AttachAdorner()
        {
            _AdornerLayer = AdornerLayer.GetAdornerLayer(this);
            _AdornerLayer.Add(new ResizingAdorner(this));
        }

        // DetachAdorner メソッドは直接呼び出さず、IsSelected プロパティを利用してください。
        private void DetachAdorner()
        {
            if (_AdornerLayer is null)
                return;

            var items = _AdornerLayer.GetAdorners(this);
            if (!(items is null) && items.Any())
                _AdornerLayer.Remove(items[0]);
        }

        #endregion

    }
}
