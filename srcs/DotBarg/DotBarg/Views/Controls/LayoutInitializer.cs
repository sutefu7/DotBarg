using System.Collections.Generic;
using System.Linq;
using System.Windows.Markup;
using Xceed.Wpf.AvalonDock.Layout;

/*
 * AvalonDock 設定用クラス
 * 
 * WPF で IDE ライクなビューを作る (ドッキングウィンドウ・AvalonDock)
 * https://qiita.com/lriki/items/475a7fc0e01ef62ef62a
 * https://github.com/lriki/WPFSkeletonIDE/tree/GenericTheme1
 * 
 */


namespace DotBarg.Views.Controls
{
    public class LayoutInsertTarget
    {
        public string ContentId { get; set; }

        public string TargetLayoutName { get; set; }
    }

    [ContentProperty("Items")]
    public class LayoutInitializer : ILayoutUpdateStrategy
    {
        public List<LayoutInsertTarget> Items { get; set; }

        public LayoutInitializer()
        {
            Items = new List<LayoutInsertTarget>();
        }

        public bool BeforeInsertAnchorable(LayoutRoot layout, LayoutAnchorable anchorableToShow, ILayoutContainer destinationContainer)
        {
            var container = destinationContainer as LayoutAnchorablePane;
            if (!(container is null))
            {
                var parent = destinationContainer.FindParent<LayoutFloatingWindow>();
                if (!(parent is null))
                    return false;
            }

            var vm = anchorableToShow.Content as LayoutInsertTarget;
            if (vm is null)
                return false;

            var target = Items.Find(x => x.ContentId == vm.ContentId);
            if (target is null)
                return false;

            var pane = layout.Descendents().OfType<LayoutAnchorablePane>().FirstOrDefault(x => x.Name == target.TargetLayoutName);
            if (pane is null)
                return false;

            pane.Children.Add(anchorableToShow);
            return true;
        }
        public void AfterInsertAnchorable(LayoutRoot layout, LayoutAnchorable anchorableShown)
        {
            //throw new NotImplementedException();
        }

        public bool BeforeInsertDocument(LayoutRoot layout, LayoutDocument documentToShow, ILayoutContainer destinationContainer)
        {
            //throw new NotImplementedException();
            return false;
        }

        public void AfterInsertDocument(LayoutRoot layout, LayoutDocument documentShown)
        {
            //throw new NotImplementedException();
        }
    }
}
