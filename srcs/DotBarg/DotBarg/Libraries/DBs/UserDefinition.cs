using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotBarg.Libraries.DBs
{
    [DebuggerDisplay("{DefineKinds} : {DefineName}")]
    public class UserDefinition : TableBase
    {
        public DefineKinds DefineKinds { get; set; } = DefineKinds.Unknown;

        // Class, Method だけど ほぼ Class 専用
        public bool IsPartial { get; set; } = false;

        public string Namespace { get; set; } = string.Empty;

        public string DefineName { get; set; } = string.Empty;

        public string DefineFullName { get; set; } = string.Empty;

        public List<BaseTypeInfo> BaseTypeInfos { get; set; } = new List<BaseTypeInfo>();

        public List<MethodArgument> MethodArguments { get; set; } = new List<MethodArgument>();

        public List<string> EnumMembers { get; set; } = new List<string>();

        // Field, Property : Type
        // Method : ReturnType
        public string ReturnType { get; set; } = string.Empty;

        //

        public string DisplayDefineName
        {
            get { return base.ConvertCurrentLanguageType(DefineName); }
        }

        public string DisplayReturnType
        {
            get { return base.ConvertCurrentLanguageType(ReturnType); }
        }

        public string DisplayMethodArguments
        {
            get 
            {
                var items = MethodArguments.Select(x => x.DisplaySyntax);
                var value = string.Join(", ", items);

                return value;
            }
        }

        public string DisplaySignature
        {
            get
            {
                var value = string.Empty;

                switch (AppEnv.Languages)
                {
                    case Languages.CSharp:
                        value = ToCSharpDisplaySignature();
                        break;

                    case Languages.VBNet:
                        value = ToVBNetDisplaySignature();
                        break;
                }

                return value;
            }
        }

        //

        public string SourceFile { get; set; } = string.Empty;

        public int StartLength { get; set; } = -1;

        public int EndLength { get; set; } = -1;

        //

        private string ToCSharpDisplaySignature()
        {
            var sb = new StringBuilder();

            switch (DefineKinds)
            {
                case DefineKinds.Namespace:
                    break;

                case DefineKinds.Class:
                case DefineKinds.Struct:
                case DefineKinds.Interface:
                case DefineKinds.Module:
                case DefineKinds.Enum:

                    sb.Append(DisplayDefineName);
                    break;

                case DefineKinds.Field:
                case DefineKinds.Property:
                case DefineKinds.Event:

                    // X : int
                    sb.Append($"{DisplayDefineName} : {DisplayReturnType}");
                    break;

                case DefineKinds.Constructor:

                    // Class1()
                    sb.Append($"{DisplayDefineName}");
                    sb.Append("(");

                    if (MethodArguments.Any())
                        sb.Append($"{DisplayMethodArguments}");

                    sb.Append(")");
                    break;

                case DefineKinds.Operator:

                    // Visual Studio では戻り値は表示されないが、本ツールでは戻り値も表示する
                    // operator +(Point, Point)
                    // ↓
                    // operator +(Point, Point) : Point
                    sb.Append("operator ");
                    sb.Append($"{DisplayDefineName}");
                    sb.Append("(");

                    if (MethodArguments.Any())
                        sb.Append($"{DisplayMethodArguments}");

                    sb.Append(")");
                    sb.Append(" : ");

                    sb.Append($"{DisplayReturnType}");

                    break;

                case DefineKinds.WindowsAPI:
                case DefineKinds.EventHandler:
                case DefineKinds.Method:
                case DefineKinds.Delegate:

                    // Beep(uint, uint) : bool
                    // Button1_Click(object sender, EventArgs e) : void
                    // f() : int
                    // f(int i) : IEnumerable<T>
                    sb.Append($"{DisplayDefineName}");
                    sb.Append("(");

                    if (MethodArguments.Any())
                        sb.Append($"{DisplayMethodArguments}");

                    sb.Append(")");
                    sb.Append(" : ");

                    sb.Append($"{DisplayReturnType}");

                    break;

                case DefineKinds.Indexer:

                    // this[int i] : int
                    sb.Append($"{DisplayDefineName}");
                    sb.Append("[");

                    if (MethodArguments.Any())
                        sb.Append($"{DisplayMethodArguments}");

                    sb.Append("]");
                    sb.Append(" : ");

                    sb.Append($"{DisplayReturnType}");

                    break;
            }

            return sb.ToString();
        }

        private string ToVBNetDisplaySignature()
        {
            var sb = new StringBuilder();

            switch (DefineKinds)
            {
                case DefineKinds.Namespace:
                    break;

                case DefineKinds.Class:
                case DefineKinds.Struct:
                case DefineKinds.Interface:
                case DefineKinds.Module:
                case DefineKinds.Enum:

                    sb.Append(DisplayDefineName);
                    break;

                case DefineKinds.Field:
                case DefineKinds.Property:

                    // X As Integer
                    sb.Append($"{DisplayDefineName} As {DisplayReturnType}");
                    break;

                case DefineKinds.Constructor:

                    // New()
                    sb.Append("New");
                    sb.Append("(");

                    if (MethodArguments.Any())
                        sb.Append($"{DisplayMethodArguments}");

                    sb.Append(")");
                    break;

                case DefineKinds.Operator:
                case DefineKinds.WindowsAPI:
                case DefineKinds.EventHandler:
                case DefineKinds.Method:
                case DefineKinds.Delegate:
                case DefineKinds.Indexer:

                    // +(Point, Point) As Point
                    // MoveFile(String, String) As Boolean
                    // Button1_Click(sender As Object, e As EventArgs)
                    // F() : IEnumerable(Of T)
                    // CalcDelegate
                    // Items(i As Integer) As Integer
                    // 
                    // Delegate の場合、デリゲート名しか表示しないが、分かりづらいので本ツールでは通常メソッド扱いする
                    // CalcDelegate ではなく、CalcDelegate(i1 As Integer, i2 As Integer) As Integer
                    sb.Append($"{DisplayDefineName}");
                    sb.Append("(");

                    if (MethodArguments.Any())
                        sb.Append($"{DisplayMethodArguments}");

                    sb.Append(")");

                    if (!string.IsNullOrEmpty(DisplayReturnType) && DisplayReturnType.ToLower() != "void")
                    {
                        sb.Append(" As ");
                        sb.Append($"{DisplayReturnType}");
                    }

                    break;

                case DefineKinds.Event:

                    // Clicked()
                    // Clicked As EventHandler
                    //
                    // Public Event Clicked(sender As Object, e As EventArgs) と書くと、
                    // Clicked() と表示されるが分かりづらいので、本ツールでは引数も表示させる
                    // Clicked(sender As Object, e As EventArgs)

                    sb.Append($"{DisplayDefineName}");

                    sb.Append("(");

                    if (MethodArguments.Any())
                    {
                        sb.Append($"{DisplayMethodArguments}");
                    }

                    sb.Append(")");

                    if (!string.IsNullOrEmpty(DisplayReturnType) && DisplayReturnType.ToLower() != "void")
                    {
                        sb.Append(" As ");
                        sb.Append($"{DisplayReturnType}");
                    }

                    break;
            }

            return sb.ToString();
        }
    }
}
