namespace DotBarg.Libraries.Roslyns
{
    /// <summary>
    /// AvalonEdit 用に使う、展開・折りたたみ箇所を取得するためのインターフェースです。<br></br>
    /// C# / VB それぞれの言語用に継承してください。
    /// </summary>
    public interface IFoldingSyntaxWalker
    {
    }

    public class FoldingData
    {
        public string Name { get; set; }

        public int StartOffset { get; set; }

        public int EndOffset { get; set; }
    }
}
