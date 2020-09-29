using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.MSBuild;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DotBarg.Libraries.Roslyns
{
    public class RoslynHelper
    {
        public async static Task<SearchResultInfo> FindSymbolAtPositionAsync(string sourceFile, int offset)
        {
            var lang = GetLanguages(sourceFile);
            var result = new SearchResultInfo { SourceFile = string.Empty, Offset = -1 };

            var srcTree = default(SyntaxTree);
            var si = default(ISymbol);

            switch (lang)
            {
                case Languages.CSharp:

                    srcTree = AppEnv.CSharpSyntaxTrees.FirstOrDefault(x => x.FilePath == sourceFile);
                    break;

                case Languages.VBNet:

                    srcTree = AppEnv.VisualBasicSyntaxTrees.FirstOrDefault(x => x.FilePath == sourceFile);
                    break;
            }

            // C#, VBNet のコンパイラに探してもらう
            si = await FindSymbolAtPositionByCSharpAsync(srcTree, offset);
            if (si is null)
                si = await FindSymbolAtPositionByVBNetAsync(srcTree, offset);

            if (si is null)
                return result;

            sourceFile = si.Locations[0].SourceTree?.FilePath;
            offset = si.Locations[0].SourceSpan.Start;

            if (string.IsNullOrEmpty(sourceFile) || !File.Exists(sourceFile))
                return result;

            result.SourceFile = sourceFile;
            result.Offset = offset;

            return result;
        }

        private static Languages GetLanguages(string sourceFile)
        {
            var extension = Path.GetExtension(sourceFile).ToLower();

            switch (extension)
            {
                case ".cs": return Languages.CSharp;
                case ".vb": return Languages.VBNet;
            }

            return Languages.Unknown;
        }

        private async static Task<ISymbol> FindSymbolAtPositionByCSharpAsync(SyntaxTree srcTree, int offset)
        {
            var si = default(ISymbol);

            // C# のコンパイラに探してもらう
            if (AppEnv.CSharpCompilations.Any())
            {
                // 定義元が誤検知してしまうバグの対応
                // 
                // 1プロジェクト毎にコンパイラを作成・登録しているが、登録対象のソースファイルは累積のリストとなっている。
                // 後になるにつれて、他プロジェクトのソースコードも含めてコンパイラを作成しているため、
                // 検索範囲が一番多い最後のコンパイラが一番精度が高くなっている。よって、降順で探してもらうことにする
                for (var i = AppEnv.CSharpCompilations.Count - 1; i >= 0; i--)
                {
                    try
                    {
                        var compItem = AppEnv.CSharpCompilations[i];
                        var model = compItem.GetSemanticModel(srcTree);
                        if (model is null)
                            continue;

                        var ws = MSBuildWorkspace.Create();
                        si = await SymbolFinder.FindSymbolAtPositionAsync(model, offset, ws);
                        if (!(si is null) && si.Locations.Count() > 0 && si.Locations[0].IsInSource)
                            break;
                    }
                    catch (Exception)
                    {
                        /*
                         * 以下のタイミングで、例外エラー発生してしまうバグの対応（いつから？開発途中から？最初は出ていなかったような？）
                         * var model = compItem.GetSemanticModel(srcTree);
                         * 
                         * 
                         * ・内容
                         * 'System.ArgumentException' (Microsoft.CodeAnalysis.CSharp.dll の中)
                         * System.ArgumentException: SyntaxTree はコンパイルの一部ではありません
                         * 
                         * 'System.ArgumentException'
                         * 'SyntaxTree is not part of the compilation
                         * 
                         * ・対策
                         * ネット上を調べても原因が分からず。
                         * 例外エラーを無視して、見つからなければ諦めることにする
                         * 
                         * 
                         * 
                         */
                    }
                }
            }

            return si;
        }

        private async static Task<ISymbol> FindSymbolAtPositionByVBNetAsync(SyntaxTree srcTree, int offset)
        {
            var si = default(ISymbol);

            if (AppEnv.VisualBasicCompilations.Any())
            {
                for (var i = AppEnv.VisualBasicCompilations.Count - 1; i >= 0; i--)
                {
                    try
                    {
                        var compItem = AppEnv.VisualBasicCompilations[i];
                        var model = compItem.GetSemanticModel(srcTree);
                        if (model is null)
                            continue;

                        var ws = MSBuildWorkspace.Create();
                        si = await SymbolFinder.FindSymbolAtPositionAsync(model, offset, ws);
                        if (!(si is null) && si.Locations.Count() > 0 && si.Locations[0].IsInSource)
                            break;
                    }
                    catch (Exception)
                    {
                        // 上記 C# のバグに対する類似対策
                    }
                }
            }

            return si;
        }
    }
}
