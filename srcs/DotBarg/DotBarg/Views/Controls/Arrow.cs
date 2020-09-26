using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

/*
 * 矢印コントロール
 * http://www.kanazawa-net.ne.jp/~pmansato/wpf/wpf_graph_drawtool.htm
 * 
 * 開始位置(X1, Y1) から終了位置（X2, Y2）までの直線を引きつつ、
 * 開始位置に矢印を描画します。
 * 
 * 開始位置に矢印が付くところに注意です。
 * 
 * 
 */


namespace DotBarg.Views.Controls
{
    public class Arrow : Shape
    {
        // 開始地点、矢じり側の X1 座標

        public static readonly DependencyProperty X1Property =
            DependencyProperty.Register(
                nameof(X1),
                typeof(double),
                typeof(Arrow),
                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure));

        [TypeConverter(typeof(LengthConverter))]
        public double X1
        {
            get { return (double)GetValue(X1Property); }
            set { SetValue(X1Property, value); }
        }

        // 開始地点、矢じり側の Y1 座標

        public static readonly DependencyProperty Y1Property =
            DependencyProperty.Register(
                nameof(Y1),
                typeof(double),
                typeof(Arrow),
                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure));

        [TypeConverter(typeof(LengthConverter))]
        public double Y1
        {
            get { return (double)GetValue(Y1Property); }
            set { SetValue(Y1Property, value); }
        }

        // 終了地点の X2 座標

        public static readonly DependencyProperty X2Property =
            DependencyProperty.Register(
                nameof(X2),
                typeof(double),
                typeof(Arrow),
                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure));

        [TypeConverter(typeof(LengthConverter))]
        public double X2
        {
            get { return (double)GetValue(X2Property); }
            set { SetValue(X2Property, value); }
        }

        // 終了地点の Y2 座標

        public static readonly DependencyProperty Y2Property =
            DependencyProperty.Register(
                nameof(Y2),
                typeof(double),
                typeof(Arrow),
                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure));

        [TypeConverter(typeof(LengthConverter))]
        public double Y2
        {
            get { return (double)GetValue(Y2Property); }
            set { SetValue(Y2Property, value); }
        }

        // 矢じり部の長さ

        public static readonly DependencyProperty ArrowLengthProperty =
            DependencyProperty.Register(
                nameof(ArrowLength),
                typeof(double),
                typeof(Arrow),
                new FrameworkPropertyMetadata(5.0, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure));

        [TypeConverter(typeof(LengthConverter))]
        public double ArrowLength
        {
            get { return (double)GetValue(ArrowLengthProperty); }
            set { SetValue(ArrowLengthProperty, value); }
        }

        // 矢じり部の幅（幅を長さの2倍にすると直交三角形になる）

        public static readonly DependencyProperty ArrowWidthProperty =
            DependencyProperty.Register(
                nameof(ArrowWidth),
                typeof(double),
                typeof(Arrow),
                new FrameworkPropertyMetadata(10.0, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure));

        [TypeConverter(typeof(LengthConverter))]
        public double ArrowWidth
        {
            get { return (double)GetValue(ArrowWidthProperty); }
            set { SetValue(ArrowWidthProperty, value); }
        }

        // コントロールの形状を定義する
        protected override Geometry DefiningGeometry
        {
            get
            {
                // 直線部の長さ
                var length = Math.Sqrt((X2 - X1) * (X2 - X1) + (Y2 - Y1) * (Y2 - Y1));

                var pf1 = new PathFigure();
                pf1.StartPoint = new Point(X1, Y1 - length); // 矢じりでない側の位置

                var points = new Point[4];
                points[0] = new Point(X1, Y1); // 矢じりの先端
                points[1] = new Point(X1 - ArrowWidth / 2, Y1 - ArrowLength);
                points[2] = new Point(X1 + ArrowWidth / 2, Y1 - ArrowLength);
                points[3] = new Point(X1, Y1);

                pf1.Segments.Add(new PolyLineSegment(points, true));
                pf1.IsFilled = true;

                var pg1 = new PathGeometry();
                pg1.FillRule = FillRule.Nonzero;
                pg1.Figures.Add(pf1);

                // 以下の操作は、垂直に立てた状態の時の形状である。次に角度をつけるために座標変換する
                var angle = 180 - Math.Atan2(X2 - X1, Y2 - Y1) * 180 / Math.PI;
                var transform1 = new RotateTransform(angle, X1, Y1);
                pg1.Transform = transform1;

                return pg1;
            }
        }

        public Arrow()
        {
            Stroke = Brushes.Black;
            Fill = Stroke;
            SnapsToDevicePixels = true;
        }
    }
}
