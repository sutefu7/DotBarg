using DotBarg.Libraries.Roslyns;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Folding;
using System.Collections.Generic;
using System.Linq;

namespace DotBarg.Libraries.AvalonEdits
{
    public class VBNetFoldingStrategy : IFoldingStrategy
    {
        public void UpdateFoldings(FoldingManager manager, TextDocument document)
        {
            var firstErrorOffset = -1;
            var foldings = CreateNewFoldings(document, firstErrorOffset);
            var sortedItems = foldings.OrderBy(x => x.StartOffset);

            manager.UpdateFoldings(sortedItems, firstErrorOffset);
        }

        // Class, Method などコンテナ単位で折りたたむ開始位置、終了位置、折りたたんだ際の表示名を返却
        private IEnumerable<NewFolding> CreateNewFoldings(TextDocument document, int firstErrorOffset)
        {
            var source = document.Text;
            var walker = new VBNetFoldingSyntaxWalker();
            walker.Parse(source);

            foreach (var item in walker.Items)
                yield return new NewFolding
                {
                    StartOffset = item.StartOffset,
                    EndOffset = item.EndOffset,
                    Name = item.Name,
                };
        }
    }
}
