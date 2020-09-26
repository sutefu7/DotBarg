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
    [ContentProperty("Items")]
    public class LayoutItemTemplateSelector : DataTemplateSelector
    {
        public List<DataTemplate> Items { get; set; }

        public LayoutItemTemplateSelector()
        {
            Items = new List<DataTemplate>();
        }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            var foundTemplate = Items.Find(x => item.GetType().Equals(x.DataType));
            if (!(foundTemplate is null))
                return foundTemplate;

            return base.SelectTemplate(item, container);
        }
    }
}
