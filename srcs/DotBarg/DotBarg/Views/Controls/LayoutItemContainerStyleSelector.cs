using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

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
    [ContentProperty("TargetStyle")]
    public class LayoutItemTypedStyle
    {
        public Type DataType { get; set; }

        public Style TargetStyle { get; set; }
    }

    [ContentProperty("Items")]
    public class LayoutItemContainerStyleSelector : StyleSelector
    {
        public List<LayoutItemTypedStyle> Items { get; set; }

        public LayoutItemContainerStyleSelector()
        {
            Items = new List<LayoutItemTypedStyle>();
        }

        public override Style SelectStyle(object item, DependencyObject container)
        {
            var foundStyle = Items.Find(x => item.GetType().IsSubclassOf(x.DataType));
            if (!(foundStyle is null))
                return foundStyle.TargetStyle;

            return base.SelectStyle(item, container);
        }
    }
}
