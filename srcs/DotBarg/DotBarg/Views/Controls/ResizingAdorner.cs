using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

/*
 * ResizableThumb.cs 内で使います。
 * 
 * 
 * DENIS VUYKA
 * WPF. SIMPLE ADORNER USAGE WITH DRAG AND RESIZE OPERATIONS
 * https://denisvuyka.wordpress.com/2007/10/15/wpf-simple-adorner-usage-with-drag-and-resize-operations/
 * 
 * さらに、以下の変更を加えました。
 * ・上下左右への Thumb の追加
 * ・各 Thumb の表示位置を、より外側へ変更
 * 
 * 
 */


namespace DotBarg.Views.Controls
{
    public class ResizingAdorner : Adorner
    {
        // 上、下、左、右（それぞれ線の中央）
        private Thumb TopThumb;
        private Thumb BottomThumb;
        private Thumb LeftThumb;
        private Thumb RightThumb;

        // 左上、右上、右下、左下（角）
        private Thumb TopLeftThumb;
        private Thumb TopRightThumb;
        private Thumb BottomLeftThumb;
        private Thumb BottomRightThumb;

        private VisualCollection ThumbItems;

        public ResizingAdorner(UIElement element) : base(element)
        {
            ThumbItems = new VisualCollection(this);

            BuildAdornerCorner(ref TopThumb, Cursors.SizeNS);
            BuildAdornerCorner(ref BottomThumb, Cursors.SizeNS);
            BuildAdornerCorner(ref LeftThumb, Cursors.SizeWE);
            BuildAdornerCorner(ref RightThumb, Cursors.SizeWE);

            BuildAdornerCorner(ref TopLeftThumb, Cursors.SizeNWSE);
            BuildAdornerCorner(ref BottomRightThumb, Cursors.SizeNWSE);
            BuildAdornerCorner(ref TopRightThumb, Cursors.SizeNESW);
            BuildAdornerCorner(ref BottomLeftThumb, Cursors.SizeNESW);

            TopThumb.DragDelta += TopThumb_DragDelta;
            BottomThumb.DragDelta += BottomThumb_DragDelta;
            LeftThumb.DragDelta += LeftThumb_DragDelta;
            RightThumb.DragDelta += RightThumb_DragDelta;

            TopLeftThumb.DragDelta += TopLeftThumb_DragDelta;
            TopRightThumb.DragDelta += TopRightThumb_DragDelta;
            BottomLeftThumb.DragDelta += BottomLeftThumb_DragDelta;
            BottomRightThumb.DragDelta += BottomRightThumb_DragDelta;
        }

        private void BuildAdornerCorner(ref Thumb cornerThumb, Cursor newCursor)
        {
            if (!(cornerThumb is null))
                return;

            cornerThumb = new Thumb
            {
                Cursor = newCursor,
                Width = 10,
                Height = 10,
                Opacity = 0.4,
                Background = new SolidColorBrush(Colors.MediumBlue),
            };

            ThumbItems.Add(cornerThumb);
        }

        private void TopThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            //
            var element = AdornedElement as FrameworkElement;
            var cornerThumb = e.Source as Thumb;

            if ((element is null) || (cornerThumb is null))
                return;

            EnforceSize(element);

            //
            var oldHeight = element.Height;
            var newHeight = Math.Max(element.Height - e.VerticalChange, cornerThumb.DesiredSize.Height);
            var oldTop = Canvas.GetTop(element);

            Canvas.SetTop(element, oldTop - (newHeight - oldHeight));
            element.Height = newHeight;
        }

        private void BottomThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            //
            var element = AdornedElement as FrameworkElement;
            var cornerThumb = e.Source as Thumb;

            if ((element is null) || (cornerThumb is null))
                return;

            EnforceSize(element);

            //
            element.Height = Math.Max(element.Height + e.VerticalChange, cornerThumb.DesiredSize.Height);
        }

        private void LeftThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            //
            var element = AdornedElement as FrameworkElement;
            var cornerThumb = e.Source as Thumb;

            if ((element is null) || (cornerThumb is null))
                return;

            EnforceSize(element);

            //
            var oldWidth = element.Width;
            var newWidth = Math.Max(element.Width - e.HorizontalChange, cornerThumb.DesiredSize.Width);
            var oldLeft = Canvas.GetLeft(element);

            Canvas.SetLeft(element, oldLeft - (newWidth - oldWidth));
            element.Width = newWidth;
        }

        private void RightThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            //
            var element = AdornedElement as FrameworkElement;
            var cornerThumb = e.Source as Thumb;

            if ((element is null) || (cornerThumb is null))
                return;

            EnforceSize(element);

            //
            element.Width = Math.Max(element.Width + e.HorizontalChange, cornerThumb.DesiredSize.Width);
        }

        private void TopLeftThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            //
            var element = AdornedElement as FrameworkElement;
            var cornerThumb = e.Source as Thumb;

            if ((element is null) || (cornerThumb is null))
                return;

            EnforceSize(element);

            //
            var oldWidth = element.Width;
            var newWidth = Math.Max(element.Width - e.HorizontalChange, cornerThumb.DesiredSize.Width);
            var oldLeft = Canvas.GetLeft(element);

            Canvas.SetLeft(element, oldLeft - (newWidth - oldWidth));
            element.Width = newWidth;

            var oldHeight = element.Height;
            var newHeight = Math.Max(element.Height - e.VerticalChange, cornerThumb.DesiredSize.Height);
            var oldTop = Canvas.GetTop(element);

            Canvas.SetTop(element, oldTop - (newHeight - oldHeight));
            element.Height = newHeight;
        }

        private void TopRightThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            //
            var element = AdornedElement as FrameworkElement;
            var cornerThumb = e.Source as Thumb;

            if ((element is null) || (cornerThumb is null))
                return;

            EnforceSize(element);

            //
            element.Width = Math.Max(element.Width + e.HorizontalChange, cornerThumb.DesiredSize.Width);

            var oldHeight = element.Height;
            var newHeight = Math.Max(element.Height - e.VerticalChange, cornerThumb.DesiredSize.Height);
            var oldTop = Canvas.GetTop(element);

            Canvas.SetTop(element, oldTop - (newHeight - oldHeight));
            element.Height = newHeight;
        }

        private void BottomLeftThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            //
            var element = AdornedElement as FrameworkElement;
            var cornerThumb = e.Source as Thumb;

            if ((element is null) || (cornerThumb is null))
                return;

            EnforceSize(element);

            //
            var oldWidth = element.Width;
            var newWidth = Math.Max(element.Width - e.HorizontalChange, cornerThumb.DesiredSize.Width);
            var oldLeft = Canvas.GetLeft(element);

            Canvas.SetLeft(element, oldLeft - (newWidth - oldWidth));
            element.Width = newWidth;

            element.Height = Math.Max(element.Height + e.VerticalChange, cornerThumb.DesiredSize.Height);
        }

        private void BottomRightThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            //
            var element = AdornedElement as FrameworkElement;
            var cornerThumb = e.Source as Thumb;

            if ((element is null) || (cornerThumb is null))
                return;

            EnforceSize(element);

            //
            element.Width = Math.Max(element.Width + e.HorizontalChange, cornerThumb.DesiredSize.Width);
            element.Height = Math.Max(element.Height + e.VerticalChange, cornerThumb.DesiredSize.Height);
        }

        private void EnforceSize(FrameworkElement element)
        {
            if (element.Width.Equals(double.NaN))
                element.Width = element.DesiredSize.Width;

            if (element.Height.Equals(double.NaN))
                element.Height = element.DesiredSize.Height;

            var parentElement = element.Parent as FrameworkElement;
            if (parentElement is null)
                return;

            element.MaxWidth = parentElement.ActualWidth;
            element.MaxHeight = parentElement.ActualHeight;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            //return base.ArrangeOverride(finalSize);

            var desiredWidth = AdornedElement.DesiredSize.Width;
            var desiredHeight = AdornedElement.DesiredSize.Height;
            var adornerWidth = DesiredSize.Width;
            var adornerHeight = DesiredSize.Height;

            double newX;
            double newY;

            // x, y は、図形の中心を(0, 0)として、そこから x 分増減の移動、y 分増減の移動しているっぽい？

            newX = 0;
            newY = -(adornerHeight / 2) - (TopThumb.Height / 2);
            TopThumb.Arrange(new Rect(newX, newY, adornerWidth, adornerHeight));

            newX = 0;
            newY = desiredHeight - (adornerHeight / 2) + (BottomThumb.Height / 2);
            BottomThumb.Arrange(new Rect(newX, newY, adornerWidth, adornerHeight));

            newX = -(adornerWidth / 2) - (LeftThumb.Width / 2);
            newY = 0;
            LeftThumb.Arrange(new Rect(newX, newY, adornerWidth, adornerHeight));

            newX = desiredWidth - (adornerWidth / 2) + (RightThumb.Width / 2);
            newY = 0;
            RightThumb.Arrange(new Rect(newX, newY, adornerWidth, adornerHeight));



            newX = -(adornerWidth / 2) - (TopLeftThumb.Width / 2);
            newY = -(adornerHeight / 2) - (TopLeftThumb.Height / 2);
            TopLeftThumb.Arrange(new Rect(newX, newY, adornerWidth, adornerHeight));

            newX = desiredWidth - (adornerWidth / 2) + (TopRightThumb.Width / 2);
            newY = -(adornerHeight / 2) - (TopRightThumb.Height / 2);
            TopRightThumb.Arrange(new Rect(newX, newY, adornerWidth, adornerHeight));

            newX = -(adornerWidth / 2) - (BottomLeftThumb.Width / 2);
            newY = desiredHeight - (adornerHeight / 2) + (BottomLeftThumb.Height / 2);
            BottomLeftThumb.Arrange(new Rect(newX, newY, adornerWidth, adornerHeight));

            newX = desiredWidth - (adornerWidth / 2) + (BottomRightThumb.Width / 2);
            newY = desiredHeight - (adornerHeight / 2) + (BottomRightThumb.Height / 2);
            BottomRightThumb.Arrange(new Rect(newX, newY, adornerWidth, adornerHeight));

            return finalSize;
        }

        protected override int VisualChildrenCount
        {
            get { return ThumbItems.Count; }
        }

        protected override Visual GetVisualChild(int index)
        {
            return ThumbItems[index];
        }
    }
}
