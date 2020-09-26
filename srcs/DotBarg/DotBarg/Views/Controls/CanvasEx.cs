using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace DotBarg.Views.Controls
{
    public class CanvasEx : Canvas
    {
        #region 背景をタイルにする


        public static readonly DependencyProperty IsBackgroundTileProperty =
            DependencyProperty.Register(
                nameof(IsBackgroundTile),
                typeof(bool),
                typeof(CanvasEx),
                new PropertyMetadata(false, OnIsBackgroundTilePropertyChanged));

        public bool IsBackgroundTile
        {
            get { return (bool)GetValue(IsBackgroundTileProperty); }
            set { SetValue(IsBackgroundTileProperty, value); }
        }

        private static void OnIsBackgroundTilePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = d as CanvasEx;

            var isTrue = (bool)e.NewValue;

            if (isTrue)
                SetBackgroundTile(self);
            else
                self.Background = Brushes.Transparent;
        }

        private static void SetBackgroundTile(CanvasEx self)
        {
            var length = 25;

            var rec1 = new Rectangle
            {
                Stroke = Brushes.WhiteSmoke,
                StrokeThickness = 0.5,
                Width = length,
                Height = length,
                //StrokeDashArray = new DoubleCollection { 5, 5 },
            };

            var brush1 = new VisualBrush
            {
                TileMode = TileMode.Tile,
                Viewbox = new Rect(0, 0, length, length),
                ViewboxUnits = BrushMappingMode.Absolute,
                Viewport = new Rect(0, 0, length, length),
                ViewportUnits = BrushMappingMode.Absolute,
            };

            brush1.Visual = rec1;
            self.Background = brush1;
        }

        #endregion

        #region コンストラクタ


        public CanvasEx() : base()
        {
            Background = Brushes.Transparent;

            PreviewMouseWheel += CanvasEx_PreviewMouseWheel;
        }


        #endregion

        #region マウスホイールとコントロークキーで、サイズ変更


        private void CanvasEx_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            // 左側、または右側にある Control キーが押されている場合（かつマウスホイールを回した場合）、拡大・縮小を実施
            var isDownLeftControlKey = (Keyboard.GetKeyStates(Key.LeftCtrl) & KeyStates.Down) == KeyStates.Down;
            var isDownRightControlKey = (Keyboard.GetKeyStates(Key.RightCtrl) & KeyStates.Down) == KeyStates.Down;
            var isDownControlKey = isDownLeftControlKey || isDownRightControlKey;

            if (isDownControlKey)
            {
                var self = sender as CanvasEx;
                var newScale = self.RenderTransform as ScaleTransform;
                if (newScale is null)
                {
                    newScale = new ScaleTransform { ScaleX = 1.0, ScaleY = 1.0 };
                    self.RenderTransform = newScale;
                }

                if (0 < e.Delta)
                {
                    newScale.ScaleX *= 1.1;
                    newScale.ScaleY *= 1.1;
                }
                else
                {
                    newScale.ScaleX /= 1.1;
                    newScale.ScaleY /= 1.1;
                }
            }
        }


        #endregion



    }
}
