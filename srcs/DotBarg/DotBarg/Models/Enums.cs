using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/*
 * 各場所で共通使用します。
 * 
 * ・ソリューションエクスプローラーペイン
 * ・定義メンバー
 * 
 * 
 */


namespace DotBarg.Models
{
    public enum TreeNodeKinds
    {
        None,

        Folder,
        SolutionFile,
        CSharpProjectFile,
        VBNetProjectFile,
        CSharpSourceFileForHeader,
        VBNetSourceFileForHeader,
        CSharpSourceFile,
        VBNetSourceFile,
        GeneratedFile,
        Dependency,

        Namespace,
        Class,
        Struct,
        Interface,
        Module,
        Enum,
        EnumItem,
        Delegate,
        Event,
        Field,
        Property,
        Operator,
        Method,
    }
}
