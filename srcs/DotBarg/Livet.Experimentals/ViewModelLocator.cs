using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Windows;

/*
 * 
 * 以下の移植です。（2018/10/08 時点）
 * ソースを簡略化して、本ソース内で完結するように変えています。また処理内容も簡略に変えています。
 * 
 * ViewModelLocator.cs
 * https://github.com/PrismLibrary/Prism/blob/master/Source/Wpf/Prism.Wpf/Mvvm/ViewModelLocator.cs
 * 
 * ViewModelLocationProvider.cs
 * https://github.com/PrismLibrary/Prism/blob/master/Source/Prism/Mvvm/ViewModelLocationProvider.cs
 * 
 * 
 * TODO というか迷っている事:
 * ViewModel のインスタンス生成時、引数無しのコンストラクタを呼び出して生成しているので、引数ありコンストラクタに未対応。
 * 引数ありコンストラクタをサポートする場合、App クラスなどで、あらかじめ DI みたいなことをしなくてはいけなくなる。
 * 
 * ※適当なイメージ
 * xxx.Ready(view, (viewmodel, arguments))
 * 
 * ViewModelLocator では引数を持っていないし、どういう状態の引数を渡せばいいか分からない。
 * 引数ありコンストラクタを使いたい場合は、ViewModelLocator を使わないで、手動で DataContext に ViewModel をセットしてもらう運用にしたいところ。
 * 
 */



namespace Livet.Experimentals
{
    /// <summary>
    /// View に対応する ViewModel を見つけ出して、View の DataContext に ViewModel のインスタンスをセットするサポートクラスです。
    /// </summary>
    public static class ViewModelLocator
    {
        // key: View.GetType().FullName, value: ViewModel's instance
        private static Dictionary<string, object> _Cache;

        /// <summary>
        /// 自動的に ViewModel をバインドするかどうかを決める 添付プロパティです。
        /// </summary>
        public static DependencyProperty AutoWireViewModelProperty =
            DependencyProperty.RegisterAttached("AutoWireViewModel", typeof(bool), typeof(ViewModelLocator), new PropertyMetadata(false, AutoWireViewModelChanged));

        public static bool GetAutoWireViewModel(DependencyObject obj)
        {
            return (bool)obj.GetValue(AutoWireViewModelProperty);
        }

        public static void SetAutoWireViewModel(DependencyObject obj, bool value)
        {
            obj.SetValue(AutoWireViewModelProperty, value);
        }

        private static void AutoWireViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!DesignerProperties.GetIsInDesignMode(d))
            {
                if ((bool)e.NewValue)
                {
                    AutoWireViewModelChangedInternal(d);
                }
            }
        }

        private static void AutoWireViewModelChangedInternal(DependencyObject view)
        {
            object viewModel;
            var viewName = view.GetType().FullName;

            if (_Cache == null)
                _Cache = new Dictionary<string, object>();

            // キャッシュに残っていて、破棄されていないなら再利用
            if (_Cache.ContainsKey(viewName))
            {
                viewModel = _Cache[viewName];
                if (viewModel != null)
                {
                    Bind(view, viewModel);
                    return;
                }
                _Cache.Remove(viewName);
            }

            // 新規取得
            viewModel = GetViewModelInstance(view);
            if (viewModel == null)
                return;

            _Cache.Add(viewName, viewModel);
            Bind(view, viewModel);
        }

        /// <summary>
        /// 指定の View の DataContext に ViewModel をセットします。
        /// </summary>
        /// <param name="view">コントロール</param>
        /// <param name="viewModel">ビューモデル</param>
        private static void Bind(object view, object viewModel)
        {
            FrameworkElement element = view as FrameworkElement;
            if (element != null)
                element.DataContext = viewModel;
        }

        /// <summary>
        /// View を元に、対応する ViewModel のインスタンスを返却します。
        /// </summary>
        /// <param name="view">WPF コントロール</param>
        /// <returns>対応する ViewModel のインスタンス</returns>
        private static object GetViewModelInstance(DependencyObject view)
        {
            var viewModelType = FindViewModelType(view);
            if (viewModelType == null)
                return null;

            return Activator.CreateInstance(viewModelType);
        }

        /// <summary>
        /// View を元に、対応する ViewModel の Type を返却します。
        /// </summary>
        /// <param name="view">WPF コントロール</param>
        /// <returns>対応する ViewModel の Type</returns>
        private static Type FindViewModelType(DependencyObject view)
        {
            var viewType = view.GetType();
            var viewName = viewType.FullName;
            viewName = viewName.Replace(".Views.", ".ViewModels.");
            var viewAssemblyName = viewType.GetTypeInfo().Assembly.FullName;
            var suffix = viewName.EndsWith("View") ? "Model" : "ViewModel";
            var viewModelName = String.Format(CultureInfo.InvariantCulture, "{0}{1}, {2}", viewName, suffix, viewAssemblyName);
            return Type.GetType(viewModelName);
        }
    }
}
