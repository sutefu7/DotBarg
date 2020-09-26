using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotBarg.Libraries.DBs
{
    public class BaseTypeInfo : TableBase
    {
        // 可能性のある名前空間をまとめておく
        // 優先度は、以下の順番です。（C# / VBNet 共通）
        // ・ドットが含まれている場合、すでにフルパスの名前空間付きになっている（このDefineFullNameでDBに入れている）
        // ・継承先クラスの名前空間と合体
        // ・上記名前空間から１つ上の名前空間と合体
        // ・...
        // ・名前空間無しで定義しているかチェック
        // ・using している名前空間リストと合体
        //
        // 全ソースコードをDB登録した後で、名前解決をおこなう
        // １つ１つを、UserDefinitions テーブルの DefineFullName にあるかチェックしていく

        public List<string> CandidatesNamespaces { get; set; } = new List<string>();

        public List<string> CandidatesDefineFullNames { get; set; } = new List<string>();

        public string BaseType { get; set; } = string.Empty;

        // 名前空間除去後
        public string DisplayBaseType { get; set; } = string.Empty;

        // BaseType がジェネリック型の場合、オープンジェネリック型とクローズドジェネリック型で一致しないので、分ける
        public List<string> GenericParts { get; set; } = new List<string>();

        // ソースファイル・行位置は、Roslyn / SymbolFinder.FindSymbolAtPositionAsync() に頼るときに使います。
        public string SourceFile { get; set; } = string.Empty;

        public int StartLength { get; set; } = -1;

        public int EndLength { get; set; } = -1;

        // 見つけた場合は、定義先ファイルのフルパス、定義位置を取得しておく
        public string DefinitionSourceFile { get; set; } = string.Empty;

        public int DefinitionStartLength { get; set; } = -1;

        public int DefinitionEndLength { get; set; } = -1;

        public bool HasGeneric
        {
            get
            {
                return !string.IsNullOrEmpty(BaseType) && BaseType.Contains("<");
            }
        }

        public bool FoundDefinition
        {
            get
            {
                return (DefinitionStartLength != -1);
            }
        }
    }
}
