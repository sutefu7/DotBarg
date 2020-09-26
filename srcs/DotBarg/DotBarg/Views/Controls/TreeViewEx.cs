using DotBarg.Libraries;
using System.Windows.Controls;
using System.Windows.Input;

namespace DotBarg.Views.Controls
{
    public class TreeViewEx : TreeView
    {
        public TreeViewEx() : base()
        {
            FontSize = AppEnv.FontSize;

            PreviewMouseWheel += TreeViewEx_PreviewMouseWheel;
        }

        private void TreeViewEx_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            // 左側、または右側にある Control キーが押されている場合（かつマウスホイールを回した場合）、拡大・縮小を実施
            var isDownLeftControlKey = (Keyboard.GetKeyStates(Key.LeftCtrl) & KeyStates.Down) == KeyStates.Down;
            var isDownRightControlKey = (Keyboard.GetKeyStates(Key.RightCtrl) & KeyStates.Down) == KeyStates.Down;
            var isDownControlKey = isDownLeftControlKey || isDownRightControlKey;

            if (isDownControlKey)
            {
                var self = sender as TreeViewEx;
                if (0 < e.Delta)
                {
                    self.FontSize *= 1.1;
                }
                else
                {
                    self.FontSize /= 1.1;
                }
            }
        }
    }
}
