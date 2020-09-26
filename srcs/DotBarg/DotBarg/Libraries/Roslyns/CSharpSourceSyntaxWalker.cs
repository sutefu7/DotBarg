using DotBarg.Libraries.DBs;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DotBarg.Libraries.Roslyns
{
    public class CSharpSourceSyntaxWalker : CSharpSyntaxWalker, ISourceSyntaxWalker
    {
        private List<NamespaceInfo> UsingNamespaces { get; set; } = new List<NamespaceInfo>();

        private string SourceFile { get; set; } = string.Empty;

        // 今のところ未使用
        private string RootNamespace { get; set; } = string.Empty;

        public List<UserDefinition> UserDefinitions { get; set; } = new List<UserDefinition>();

        public CSharpSourceSyntaxWalker() : base(SyntaxWalkerDepth.Trivia)
        {

        }

        public void Parse(string sourceFile, string sourceCode, string rootNamespace)
        {
            SourceFile = sourceFile;
            RootNamespace = rootNamespace;

            var tree = CSharpSyntaxTree.ParseText(sourceCode);
            var node = tree.GetRoot();

            Visit(node);
        }

        public override void Visit(SyntaxNode node)
        {
            base.Visit(node);
        }

        public override void VisitCompilationUnit(CompilationUnitSyntax node)
        {
            base.VisitCompilationUnit(node);
        }

        public override void VisitUsingDirective(UsingDirectiveSyntax node)
        {
            if (node.ChildNodes().Any(x => x is NameEqualsSyntax))
            {
                var nameNode = node.ChildNodes().FirstOrDefault(x => x is NameEqualsSyntax);
                var alternate = nameNode.ChildNodes().FirstOrDefault().ToString();
                var ns = node.ChildNodes().LastOrDefault().ToString();

                UsingNamespaces.Add(new NamespaceInfo
                {
                    Namespace = ns,
                    Alternate = alternate,
                });
            }
            else
            {
                var ns = node.ChildNodes().FirstOrDefault().ToString();
                UsingNamespaces.Add(new NamespaceInfo
                { 
                    Namespace = ns,
                    Alternate = string.Empty,
                });
            }

            base.VisitUsingDirective(node);
        }

        public override void VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
        {
            var defineName = string.Empty;
            var qNode = node.ChildNodes().OfType<QualifiedNameSyntax>().FirstOrDefault();

            if (Util.IsNotNull(qNode))
            {
                // Namespace NS1.NS2
                defineName = qNode.ToString();
            }
            else
            {
                // Namespace NS1
                var iNode = node.ChildNodes().OfType<IdentifierNameSyntax>().FirstOrDefault();
                defineName = iNode.ToString();
            }

            var startLength = node.Span.Start;
            var endLength = node.Span.End;
            var parentNamespace = GetNamespace(DefineKinds.Namespace, startLength, endLength);

            UserDefinitions.Add(new UserDefinition
            {
                DefineKinds = DefineKinds.Namespace,
                Namespace = parentNamespace,
                DefineName = defineName,
                DefineFullName = string.IsNullOrEmpty(parentNamespace) ? defineName : $"{parentNamespace}.{defineName}",
                SourceFile = SourceFile,
                StartLength = startLength,
                EndLength = endLength,
            });

            base.VisitNamespaceDeclaration(node);
        }

        public override void VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            var isPartial = node.ChildTokens().Any(x => x.Kind() == SyntaxKind.PartialKeyword);
            var defineName = node.ChildTokens().FirstOrDefault(x => x.Kind() == SyntaxKind.IdentifierToken).ToString();

            // ジェネリック型を定義している場合
            if (node.ChildNodes().OfType<TypeParameterListSyntax>().Any())
            {
                var listNode = node.ChildNodes().OfType<TypeParameterListSyntax>().FirstOrDefault();
                var genericTypes = listNode
                    .ChildNodes()
                    .OfType<TypeParameterSyntax>()
                    .Select(x => x.ChildTokens().FirstOrDefault(y => y.Kind() == SyntaxKind.IdentifierToken).ToString());

                defineName = $"{defineName}<{string.Join(", ", genericTypes)}>";
            }

            var startLength = node.Span.Start;
            var endLength = node.Span.End;
            var parentNamespace = GetNamespace(DefineKinds.Class, startLength, endLength);

            var baseTypeInfos = new List<BaseTypeInfo>();

            // 継承元クラス、またはインターフェースがある場合
            if (node.ChildNodes().OfType<BaseListSyntax>().Any())
            {
                var listNode = node.ChildNodes().OfType<BaseListSyntax>().FirstOrDefault();
                var baseTypes = listNode.ChildNodes().OfType<SimpleBaseTypeSyntax>();

                baseTypeInfos = GetBaseTypeInfos(baseTypes, parentNamespace);
            }

            UserDefinitions.Add(new UserDefinition
            {
                DefineKinds = DefineKinds.Class,
                IsPartial = isPartial,
                Namespace = parentNamespace,
                DefineName = defineName,
                DefineFullName = $"{parentNamespace}.{defineName}",
                BaseTypeInfos = baseTypeInfos,
                SourceFile = SourceFile,
                StartLength = startLength,
                EndLength = endLength,
            });

            base.VisitClassDeclaration(node);
        }

        public override void VisitStructDeclaration(StructDeclarationSyntax node)
        {
            var isPartial = node.ChildTokens().Any(x => x.Kind() == SyntaxKind.PartialKeyword);
            var defineName = node.ChildTokens().FirstOrDefault(x => x.Kind() == SyntaxKind.IdentifierToken).ToString();

            // ジェネリック型を定義している場合
            if (node.ChildNodes().OfType<TypeParameterListSyntax>().Any())
            {
                var listNode = node.ChildNodes().OfType<TypeParameterListSyntax>().FirstOrDefault();
                var genericTypes = listNode
                    .ChildNodes()
                    .OfType<TypeParameterSyntax>()
                    .Select(x => x.ChildTokens().FirstOrDefault(y => y.Kind() == SyntaxKind.IdentifierToken).ToString());

                defineName = $"{defineName}<{string.Join(", ", genericTypes)}>";
            }

            var startLength = node.Span.Start;
            var endLength = node.Span.End;
            var parentNamespace = GetNamespace(DefineKinds.Struct, startLength, endLength);

            var baseTypeInfos = new List<BaseTypeInfo>();

            // 継承元クラス、またはインターフェースがある場合
            if (node.ChildNodes().OfType<BaseListSyntax>().Any())
            {
                var listNode = node.ChildNodes().OfType<BaseListSyntax>().FirstOrDefault();
                var baseTypes = listNode.ChildNodes().OfType<SimpleBaseTypeSyntax>();

                baseTypeInfos = GetBaseTypeInfos(baseTypes, parentNamespace);
            }

            UserDefinitions.Add(new UserDefinition
            {
                DefineKinds = DefineKinds.Struct,
                IsPartial = isPartial,
                Namespace = parentNamespace,
                DefineName = defineName,
                DefineFullName = $"{parentNamespace}.{defineName}",
                BaseTypeInfos = baseTypeInfos,
                SourceFile = SourceFile,
                StartLength = startLength,
                EndLength = endLength,
            });

            base.VisitStructDeclaration(node);
        }

        public override void VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
        {
            var isPartial = node.ChildTokens().Any(x => x.Kind() == SyntaxKind.PartialKeyword);
            var defineName = node.ChildTokens().FirstOrDefault(x => x.Kind() == SyntaxKind.IdentifierToken).ToString();

            // ジェネリック型を定義している場合
            if (node.ChildNodes().OfType<TypeParameterListSyntax>().Any())
            {
                var listNode = node.ChildNodes().OfType<TypeParameterListSyntax>().FirstOrDefault();
                var genericTypes = listNode
                    .ChildNodes()
                    .OfType<TypeParameterSyntax>()
                    .Select(x => x.ChildTokens().FirstOrDefault(y => y.Kind() == SyntaxKind.IdentifierToken).ToString());

                defineName = $"{defineName}<{string.Join(", ", genericTypes)}>";
            }

            var startLength = node.Span.Start;
            var endLength = node.Span.End;
            var parentNamespace = GetNamespace(DefineKinds.Interface, startLength, endLength);

            var baseTypeInfos = new List<BaseTypeInfo>();

            // 継承元クラス、またはインターフェースがある場合
            if (node.ChildNodes().OfType<BaseListSyntax>().Any())
            {
                var listNode = node.ChildNodes().OfType<BaseListSyntax>().FirstOrDefault();
                var baseTypes = listNode.ChildNodes().OfType<SimpleBaseTypeSyntax>();

                baseTypeInfos = GetBaseTypeInfos(baseTypes, parentNamespace);
            }

            UserDefinitions.Add(new UserDefinition
            {
                DefineKinds = DefineKinds.Interface,
                IsPartial = isPartial,
                Namespace = parentNamespace,
                DefineName = defineName,
                DefineFullName = $"{parentNamespace}.{defineName}",
                BaseTypeInfos = baseTypeInfos,
                SourceFile = SourceFile,
                StartLength = startLength,
                EndLength = endLength,
            });

            base.VisitInterfaceDeclaration(node);
        }

        public override void VisitEnumDeclaration(EnumDeclarationSyntax node)
        {
            var defineName = node.ChildTokens().FirstOrDefault(x => x.Kind() == SyntaxKind.IdentifierToken).ToString();

            var enumMembers = node.ChildNodes().OfType<EnumMemberDeclarationSyntax>().Select(x => x.Identifier.Text);

            var startLength = node.Span.Start;
            var endLength = node.Span.End;
            var parentNamespace = GetNamespace(DefineKinds.Enum, startLength, endLength);

            UserDefinitions.Add(new UserDefinition
            {
                DefineKinds = DefineKinds.Enum,
                Namespace = parentNamespace,
                DefineName = defineName,
                DefineFullName = $"{parentNamespace}.{defineName}",
                EnumMembers = enumMembers.ToList(),
                SourceFile = SourceFile,
                StartLength = startLength,
                EndLength = endLength,
            }); ;

            base.VisitEnumDeclaration(node);
        }

        public override void VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
        {
            var isPartial = node.ChildTokens().Any(x => x.Kind() == SyntaxKind.PartialKeyword);
            var defineName = node.ChildTokens().FirstOrDefault(x => x.Kind() == SyntaxKind.IdentifierToken).ToString();

            var methodArguments = new List<MethodArgument>();
            if (node.ChildNodes().OfType<ParameterListSyntax>().FirstOrDefault().ChildNodes().Any())
            {
                var listNode = node.ChildNodes().OfType<ParameterListSyntax>().FirstOrDefault();
                methodArguments = GetMethodArguments(listNode);
            }

            var startLength = node.Span.Start;
            var endLength = node.Span.End;
            var parentNamespace = GetNamespace(DefineKinds.Constructor, startLength, endLength);

            UserDefinitions.Add(new UserDefinition
            {
                DefineKinds = DefineKinds.Constructor,
                IsPartial = isPartial,
                Namespace = parentNamespace,
                DefineName = defineName,
                DefineFullName = $"{parentNamespace}.{defineName}",
                MethodArguments = methodArguments,
                SourceFile = SourceFile,
                StartLength = startLength,
                EndLength = endLength,
            });

            base.VisitConstructorDeclaration(node);
        }

        public override void VisitOperatorDeclaration(OperatorDeclarationSyntax node)
        {
            var returnType = node.ChildNodes().FirstOrDefault().ToString();
            var defineName = node.ChildTokens().LastOrDefault().ToString();

            var methodArguments = new List<MethodArgument>();
            if (node.ChildNodes().OfType<ParameterListSyntax>().FirstOrDefault().ChildNodes().Any())
            {
                var listNode = node.ChildNodes().OfType<ParameterListSyntax>().FirstOrDefault();
                methodArguments = GetMethodArguments(listNode);
            }

            var startLength = node.Span.Start;
            var endLength = node.Span.End;
            var parentNamespace = GetNamespace(DefineKinds.Operator, startLength, endLength);

            UserDefinitions.Add(new UserDefinition
            {
                DefineKinds = DefineKinds.Operator,
                Namespace = parentNamespace,
                DefineName = defineName,
                DefineFullName = $"{parentNamespace}.{defineName}",
                MethodArguments = methodArguments,
                ReturnType = RemoveNamespace(returnType),
                SourceFile = SourceFile,
                StartLength = startLength,
                EndLength = endLength,
            });

            base.VisitOperatorDeclaration(node);
        }

        public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            var returnType = node.ChildNodes().FirstOrDefault().ToString();
            var isSubMethod = node.ChildNodes().FirstOrDefault().ChildTokens().Any(x => x.Kind() == SyntaxKind.VoidKeyword);
            var isPartial = node.ChildTokens().Any(x => x.Kind() == SyntaxKind.PartialKeyword);
            var defineName = node.ChildTokens().FirstOrDefault(x => x.Kind() == SyntaxKind.IdentifierToken).ToString();

            // ジェネリック型を定義している場合
            if (node.ChildNodes().OfType<TypeParameterListSyntax>().Any())
            {
                var listNode = node.ChildNodes().OfType<TypeParameterListSyntax>().FirstOrDefault();
                var genericTypes = listNode
                    .ChildNodes()
                    .OfType<TypeParameterSyntax>()
                    .Select(x => x.ChildTokens().FirstOrDefault(y => y.Kind() == SyntaxKind.IdentifierToken).ToString());

                defineName = $"{defineName}<{string.Join(", ", genericTypes)}>";
            }

            var methodArguments = new List<MethodArgument>();
            if (node.ChildNodes().OfType<ParameterListSyntax>().FirstOrDefault().ChildNodes().Any())
            {
                var listNode = node.ChildNodes().OfType<ParameterListSyntax>().FirstOrDefault();
                methodArguments = GetMethodArguments(listNode);
            }

            var startLength = node.Span.Start;
            var endLength = node.Span.End;
            var parentNamespace = GetNamespace(DefineKinds.Method, startLength, endLength);

            // EventHandler、Method(Sub, Function)
            var isEventHandler = false;
            if (isSubMethod)
            {
                if (methodArguments.Count == 2)
                {
                    if (methodArguments[0].DefineType == "object" && methodArguments[1].DefineType.EndsWith("EventArgs"))
                    {
                        isEventHandler = true;
                    }
                }
            }

            var isWinAPI = node.AttributeLists.Any(x => x.ToString().Contains("DllImport"));
            var kinds = DefineKinds.Method;

            if (isEventHandler) 
                kinds = DefineKinds.EventHandler;

            // EventHandler っぽいけど、Windows API の場合は、こちらを優先する
            if (isWinAPI)
                kinds = DefineKinds.WindowsAPI;

            UserDefinitions.Add(new UserDefinition
            {
                DefineKinds = kinds,
                IsPartial = isPartial,
                Namespace = parentNamespace,
                DefineName = defineName,
                DefineFullName = $"{parentNamespace}.{defineName}",
                MethodArguments = methodArguments,
                ReturnType = RemoveNamespace(returnType),
                SourceFile = SourceFile,
                StartLength = startLength,
                EndLength = endLength,
            });

            base.VisitMethodDeclaration(node);
        }

        public override void VisitFieldDeclaration(FieldDeclarationSyntax node)
        {
            var variable = node.ChildNodes().FirstOrDefault(x => x is VariableDeclarationSyntax);
            var variableType = variable.ChildNodes().FirstOrDefault().ToString();
            var fields = variable.ChildNodes().Where(x => x is VariableDeclaratorSyntax);

            foreach (VariableDeclaratorSyntax field in fields)
            {
                var startLength = field.Span.Start;
                var endLength = field.Span.End;

                var fieldName = field.ChildTokens().FirstOrDefault(x => x.Kind() == SyntaxKind.IdentifierToken).ToString();
                var fieldType = variableType;

                var parentNamespace = GetNamespace(DefineKinds.Field, startLength, endLength);

                UserDefinitions.Add(new UserDefinition
                {
                    DefineKinds = DefineKinds.Field,
                    Namespace = parentNamespace,
                    DefineName = fieldName,
                    DefineFullName = $"{parentNamespace}.{fieldName}",
                    ReturnType = RemoveNamespace(fieldType),
                    SourceFile = SourceFile,
                    StartLength = startLength,
                    EndLength = endLength,
                });
            }

            base.VisitFieldDeclaration(node);
        }

        public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            var startLength = node.Span.Start;
            var endLength = node.Span.End;
            var parentNamespace = GetNamespace(DefineKinds.Property, startLength, endLength);

            var defineType = node.ChildNodes().FirstOrDefault().ToString();
            var defineName = node.ChildTokens().FirstOrDefault(x => x.Kind() == SyntaxKind.IdentifierToken).ToString();

            UserDefinitions.Add(new UserDefinition
            {
                DefineKinds = DefineKinds.Property,
                Namespace = parentNamespace,
                DefineName = defineName,
                DefineFullName = $"{parentNamespace}.{defineName}",
                ReturnType = RemoveNamespace(defineType),
                SourceFile = SourceFile,
                StartLength = startLength,
                EndLength = endLength,
            });

            base.VisitPropertyDeclaration(node);
        }

        // 以下はプロパティではなく別扱いみたいです。本ツール内では、今のところプロパティ扱いします。
        // public object this[int index] { get; set; }
        public override void VisitIndexerDeclaration(IndexerDeclarationSyntax node)
        {
            var startLength = node.Span.Start;
            var endLength = node.Span.End;
            var parentNamespace = GetNamespace(DefineKinds.Indexer, startLength, endLength);

            var defineType = node.ChildNodes().FirstOrDefault().ToString();
            var defineName = node.ChildTokens().FirstOrDefault(x => x.Kind() == SyntaxKind.ThisKeyword).ToString();
            
            var methodArguments = new List<MethodArgument>();
            if (node.ChildNodes().OfType<BracketedParameterListSyntax>().Any())
            {
                if (node.ChildNodes().OfType<BracketedParameterListSyntax>().FirstOrDefault().ChildNodes().Any())
                {
                    var listNode = node.ChildNodes().OfType<BracketedParameterListSyntax>().FirstOrDefault();
                    methodArguments = GetIndexerArguments(listNode);
                }
            }

            UserDefinitions.Add(new UserDefinition
            {
                DefineKinds = DefineKinds.Indexer,
                Namespace = parentNamespace,
                DefineName = defineName,
                DefineFullName = $"{parentNamespace}.{defineName}",
                MethodArguments = methodArguments,
                ReturnType = RemoveNamespace(defineType),
                SourceFile = SourceFile,
                StartLength = startLength,
                EndLength = endLength,
            });
            base.VisitIndexerDeclaration(node);
        }

        // 通常のイベント定義
        // public event EventHandler Clicked;
        public override void VisitEventFieldDeclaration(EventFieldDeclarationSyntax node)
        {
            var variable = node.ChildNodes().FirstOrDefault(x => x is VariableDeclarationSyntax);
            var defineType = variable.ChildNodes().FirstOrDefault(x => x is IdentifierNameSyntax).ToString();
            var defineName = variable.ChildNodes().FirstOrDefault(x => x is VariableDeclaratorSyntax).ToString();

            var startLength = node.Span.Start;
            var endLength = node.Span.End;
            var parentNamespace = GetNamespace(DefineKinds.Event, startLength, endLength);

            UserDefinitions.Add(new UserDefinition
            {
                DefineKinds = DefineKinds.Event,
                Namespace = parentNamespace,
                DefineName = defineName,
                DefineFullName = $"{parentNamespace}.{defineName}",
                ReturnType = RemoveNamespace(defineType),
                SourceFile = SourceFile,
                StartLength = startLength,
                EndLength = endLength,
            });

            base.VisitEventFieldDeclaration(node);
        }

        // add/remove アクセサーを明示的に書く版
        // public event EventHandler Moved
        // {
        //     add {}
        //     remove {}
        // }
        public override void VisitEventDeclaration(EventDeclarationSyntax node)
        {
            var defineType = node.ChildNodes().FirstOrDefault(x => x is IdentifierNameSyntax).ToString();
            var defineName = node.ChildTokens().FirstOrDefault(x => x.Kind() == SyntaxKind.IdentifierToken).ToString();

            var startLength = node.Span.Start;
            var endLength = node.Span.End;
            var parentNamespace = GetNamespace(DefineKinds.Event, startLength, endLength);

            UserDefinitions.Add(new UserDefinition
            {
                DefineKinds = DefineKinds.Event,
                Namespace = parentNamespace,
                DefineName = defineName,
                DefineFullName = $"{parentNamespace}.{defineName}",
                ReturnType = RemoveNamespace(defineType),
                SourceFile = SourceFile,
                StartLength = startLength,
                EndLength = endLength,
            });

            base.VisitEventDeclaration(node);
        }

        public override void VisitDelegateDeclaration(DelegateDeclarationSyntax node)
        {
            var returnType = node.ChildNodes().FirstOrDefault().ToString();
            var isSubMethod = node.ChildNodes().FirstOrDefault().ChildTokens().Any(x => x.Kind() == SyntaxKind.VoidKeyword);
            var isPartial = node.ChildTokens().Any(x => x.Kind() == SyntaxKind.PartialKeyword);
            var defineName = node.ChildTokens().FirstOrDefault(x => x.Kind() == SyntaxKind.IdentifierToken).ToString();

            // ジェネリック型を定義している場合
            if (node.ChildNodes().OfType<TypeParameterListSyntax>().Any())
            {
                var listNode = node.ChildNodes().OfType<TypeParameterListSyntax>().FirstOrDefault();
                var genericTypes = listNode
                    .ChildNodes()
                    .OfType<TypeParameterSyntax>()
                    .Select(x => x.ChildTokens().FirstOrDefault(y => y.Kind() == SyntaxKind.IdentifierToken).ToString());

                defineName = $"{defineName}<{string.Join(", ", genericTypes)}>";
            }

            var methodArguments = new List<MethodArgument>();
            if (node.ChildNodes().OfType<ParameterListSyntax>().FirstOrDefault().ChildNodes().Any())
            {
                var listNode = node.ChildNodes().OfType<ParameterListSyntax>().FirstOrDefault();
                methodArguments = GetMethodArguments(listNode);
            }

            var startLength = node.Span.Start;
            var endLength = node.Span.End;
            var parentNamespace = GetNamespace(DefineKinds.Delegate, startLength, endLength);

            UserDefinitions.Add(new UserDefinition
            {
                DefineKinds = DefineKinds.Delegate,
                IsPartial = isPartial,
                Namespace = parentNamespace,
                DefineName = defineName,
                DefineFullName = $"{parentNamespace}.{defineName}",
                MethodArguments = methodArguments,
                ReturnType = RemoveNamespace(returnType),
                SourceFile = SourceFile,
                StartLength = startLength,
                EndLength = endLength,
            });

            base.VisitDelegateDeclaration(node);
        }

        private string GetNamespace(DefineKinds kinds, int startLength, int endLength)
        {
            var result = string.Empty;

            switch (kinds)
            {
                case DefineKinds.Namespace:

                    // 直近の Namespace
                    for (var i = UserDefinitions.Count - 1; i >= 0; i--)
                    {
                        var item = UserDefinitions[i];

                        switch (item.DefineKinds)
                        {
                            case DefineKinds.Namespace:

                                if (item.StartLength <= startLength && endLength <= item.EndLength)
                                    result = item.DefineFullName;

                                break;
                        }

                        if (!string.IsNullOrEmpty(result))
                            break;
                    }

                    break;

                default:

                    // Class, Struct, Interface, Module, Enum / Field, Property, Method, Delegate, Event
                    // 直近の Namespace, Class, Struct, Interface, Module
                    for (var i = UserDefinitions.Count - 1; i >= 0; i--)
                    {
                        var item = UserDefinitions[i];

                        switch (item.DefineKinds)
                        {
                            case DefineKinds.Namespace:
                            case DefineKinds.Class:
                            case DefineKinds.Struct:
                            case DefineKinds.Interface:
                            case DefineKinds.Module:

                                if (item.StartLength <= startLength && endLength <= item.EndLength)
                                    result = item.DefineFullName;

                                break;
                        }

                        if (!string.IsNullOrEmpty(result))
                            break;
                    }

                    break;
            }

            return result;
        }

        private List<MethodArgument> GetMethodArguments(ParameterListSyntax node)
        {
            var items = new List<MethodArgument>();
            var parameters = node.ChildNodes().OfType<ParameterSyntax>();

            foreach (var parameter in parameters)
            {
                var isIn = parameter.ChildTokens().Any(x => x.Kind() == SyntaxKind.InKeyword);
                var isOut = parameter.ChildTokens().Any(x => x.Kind() == SyntaxKind.OutKeyword);
                var isByRef = parameter.ChildTokens().Any(x => x.Kind() == SyntaxKind.RefKeyword);
                var isByVal = !isByRef;
                var isOptional = parameter.ChildNodes().Any(x => x is EqualsValueClauseSyntax);
                var isParamArray = parameter.ChildTokens().Any(x => x.Kind() == SyntaxKind.ParamsKeyword);

                var parameterName = parameter.ChildTokens().FirstOrDefault(x => x.Kind() == SyntaxKind.IdentifierToken).ToString();
                var parameterType = parameter.ChildNodes().FirstOrDefault().ToString();

                items.Add(new MethodArgument
                {
                    IsByVal = isByVal,
                    IsByRef = isByRef,
                    IsIn = isIn,
                    IsOut = isOut,
                    IsOptional = isOptional,
                    IsParamArray = isParamArray,
                    DefineName = parameterName,
                    DefineType = RemoveNamespace(parameterType),
                });
            }

            return items;
        }

        private List<MethodArgument> GetIndexerArguments(BracketedParameterListSyntax node)
        {
            var items = new List<MethodArgument>();
            var parameters = node.ChildNodes().OfType<ParameterSyntax>();

            foreach (var parameter in parameters)
            {
                var isIn = parameter.ChildTokens().Any(x => x.Kind() == SyntaxKind.InKeyword);
                var isOut = parameter.ChildTokens().Any(x => x.Kind() == SyntaxKind.OutKeyword);
                var isByRef = parameter.ChildTokens().Any(x => x.Kind() == SyntaxKind.RefKeyword);
                var isByVal = !isByRef;
                var isOptional = parameter.ChildNodes().Any(x => x is EqualsValueClauseSyntax);
                var isParamArray = parameter.ChildTokens().Any(x => x.Kind() == SyntaxKind.ParamsKeyword);

                var parameterName = parameter.ChildTokens().FirstOrDefault(x => x.Kind() == SyntaxKind.IdentifierToken).ToString();
                var parameterType = parameter.ChildNodes().FirstOrDefault().ToString();

                items.Add(new MethodArgument
                {
                    IsByVal = isByVal,
                    IsByRef = isByRef,
                    IsIn = isIn,
                    IsOut = isOut,
                    IsOptional = isOptional,
                    IsParamArray = isParamArray,
                    DefineName = parameterName,
                    DefineType = RemoveNamespace(parameterType),
                });
            }

            return items;
        }

        private static Regex RemoveNamespaceRegex;

        private string RemoveNamespace(string parameterType)
        {
            // IEnumerable<Int32>
            // System.Collections.Generic.IEnumerable<System.Int32>
            // ↓
            // IEnumerable<Int32>
            if (parameterType.Contains("."))
            {
                if (RemoveNamespaceRegex is null)
                    RemoveNamespaceRegex = new Regex(@"(\w+\.)*");

                parameterType = RemoveNamespaceRegex.Replace(parameterType, string.Empty);
            }

            return parameterType;
        }

        private List<BaseTypeInfo> GetBaseTypeInfos(IEnumerable<SimpleBaseTypeSyntax> baseTypes, string defaultNamespace)
        {
            var candidatesNamespaces = new List<string>();
            candidatesNamespaces.Add(defaultNamespace);

            while (defaultNamespace.Contains("."))
            {
                defaultNamespace = defaultNamespace.Substring(0, defaultNamespace.LastIndexOf("."));
                candidatesNamespaces.Add(defaultNamespace);
            }

            var firstUsingIndex = -1;

            if (UsingNamespaces.Any())
            {
                var results = UsingNamespaces.Select(x => x.Namespace).ToList();
                candidatesNamespaces.AddRange(results);
                firstUsingIndex = candidatesNamespaces.IndexOf(results.FirstOrDefault());
            }

            var items = new List<BaseTypeInfo>();

            foreach (var baseType in baseTypes)
            {
                var hasGlobal = false;
                var typeName = baseType.ToString();

                if (UsingNamespaces.Any())
                {
                    foreach (var check in UsingNamespaces)
                    {
                        if (!string.IsNullOrEmpty(check.Alternate) && typeName.Contains($"{check.Alternate}"))
                            typeName = typeName.Replace(check.Alternate, check.Namespace);

                        // 名前空間エイリアス修飾子（::演算子）の場合、:: が残る場合は . に置換
                        if (typeName.Contains("global::"))
                        {
                            hasGlobal = true;
                            typeName = typeName.Replace("global::", string.Empty);
                        }

                        if (typeName.Contains("::"))
                            typeName = typeName.Replace("::", ".");
                    }
                }

                var candidatesDefineFullNames = new List<string>();

                if (firstUsingIndex == -1)
                {
                    if (typeName.Contains("."))
                        candidatesDefineFullNames.Add(typeName);

                    for (var i = 0; i < candidatesNamespaces.Count; i++)
                        candidatesDefineFullNames.Add($"{candidatesNamespaces[i]}.{typeName}");

                    if (!typeName.Contains("."))
                        candidatesDefineFullNames.Add(typeName);
                }
                else
                {
                    if (typeName.Contains("."))
                        candidatesDefineFullNames.Add(typeName);

                    if (!hasGlobal)
                    {
                        for (var i = 0; i < firstUsingIndex; i++)
                            candidatesDefineFullNames.Add($"{candidatesNamespaces[i]}.{typeName}");
                    }

                    if (!typeName.Contains("."))
                        candidatesDefineFullNames.Insert(firstUsingIndex, typeName);

                    for (var i = firstUsingIndex; i < candidatesNamespaces.Count; i++)
                        candidatesDefineFullNames.Add($"{candidatesNamespaces[i]}.{typeName}");
                }



                var genericParts = new List<string>();

                // IEnumerable<int>, IEnumerable<IEnumerable<int>>, Dictionary<int, int>
                // Dictionary<int, Dictionary<int, int>>
                var node = baseType.ChildNodes().FirstOrDefault();
                if (node is GenericNameSyntax)
                {
                    var listNode = node.ChildNodes().FirstOrDefault(); // TypeArgumentListSyntax
                    var genericTypes = listNode.ChildNodes(); // PredefinedTypeSyntax, GenericNameSyntax, ...

                    foreach (var genericType in genericTypes)
                        genericParts.Add(genericType.ToString());
                }

                var startLength = baseType.Span.Start;
                var endLength = baseType.Span.End;

                items.Add(new BaseTypeInfo
                {
                    CandidatesNamespaces = candidatesNamespaces,
                    CandidatesDefineFullNames = candidatesDefineFullNames,
                    BaseType = typeName,
                    DisplayBaseType = RemoveNamespace(typeName),
                    GenericParts = genericParts,
                    SourceFile = SourceFile,
                    StartLength = startLength,
                    EndLength = endLength,
                });
            }

            return items;
        }
    }
}
