using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Folding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotBarg.Libraries.AvalonEdits
{
    /// <summary>
    /// AvalonEdit 用に使う、展開・折りたたみ箇所を管理するためのインターフェースです。<br></br>
    /// C# / VB それぞれの言語用に継承してください。
    /// </summary>
    public interface IFoldingStrategy
    {
        void UpdateFoldings(FoldingManager manager, TextDocument document);
    }
}
