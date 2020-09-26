using DotBarg.Libraries.DBs;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace DotBarg.Libraries.Roslyns
{
    public class VBNetSourceSyntaxWalker : VisualBasicSyntaxWalker, ISourceSyntaxWalker
    {
        private List<NamespaceInfo> ImportsNamespaces { get; set; } = new List<NamespaceInfo>();

        private string SourceFile { get; set; } = string.Empty;

        private string RootNamespace { get; set; } = string.Empty;

        public List<UserDefinition> UserDefinitions { get; set; } = new List<UserDefinition>();

        public VBNetSourceSyntaxWalker() : base(SyntaxWalkerDepth.Trivia)
        {

        }

        public void Parse(string sourceFile, string sourceCode, string rootNamespace)
        {
            SourceFile = sourceFile;
            RootNamespace = rootNamespace;

            // プロジェクトの名前空間を最初に登録する
            UserDefinitions.Add(new UserDefinition
            {
                DefineKinds = DefineKinds.Namespace,
                DefineName = RootNamespace,
                DefineFullName = RootNamespace,
                SourceFile = SourceFile,
            });

            var tree = VisualBasicSyntaxTree.ParseText(sourceCode);
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

        public override void VisitImportsStatement(ImportsStatementSyntax node)
        {
            if (node.DescendantNodes().Any(x => x is ImportAliasClauseSyntax))
            {
                var simpleNode = node.ChildNodes().FirstOrDefault(x => x is SimpleImportsClauseSyntax);
                var ns = simpleNode.ChildNodes().LastOrDefault().ToString();

                var aliasNode = simpleNode.ChildNodes().FirstOrDefault(x => x is ImportAliasClauseSyntax);
                var alternate = aliasNode.ChildTokens().FirstOrDefault(x => x.Kind() == SyntaxKind.IdentifierToken).ToString();

                ImportsNamespaces.Add(new NamespaceInfo
                {
                    Namespace = ns,
                    Alternate = alternate,
                });
            }
            else
            {
                var ns = node.ChildNodes().FirstOrDefault().ToString();
                ImportsNamespaces.Add(new NamespaceInfo
                {
                    Namespace = ns,
                    Alternate = string.Empty,
                });
            }

            base.VisitImportsStatement(node);
        }

        public override void VisitNamespaceBlock(NamespaceBlockSyntax node)
        {
            var defineName = string.Empty;
            var nsNode = node.ChildNodes().OfType<NamespaceStatementSyntax>().FirstOrDefault();
            var qNode = nsNode.ChildNodes().OfType<QualifiedNameSyntax>().FirstOrDefault();

            if (Util.IsNotNull(qNode))
            {
                // Namespace NS1.NS2
                defineName = qNode.ToString();
            }
            else
            {
                // Namespace NS1
                var iNode = nsNode.ChildNodes().OfType<IdentifierNameSyntax>().FirstOrDefault();
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
                DefineFullName = $"{parentNamespace}.{defineName}",
                SourceFile = SourceFile,
                StartLength = startLength,
                EndLength = endLength,
            });

            base.VisitNamespaceBlock(node);
        }

        public override void VisitClassBlock(ClassBlockSyntax node)
        {
            var statementNode = node.ChildNodes().OfType<ClassStatementSyntax>().FirstOrDefault();
            var isPartial = statementNode.ChildTokens().Any(x => x.Kind() == SyntaxKind.PartialKeyword);
            var defineName = statementNode.ChildTokens().FirstOrDefault(x => x.Kind() == SyntaxKind.IdentifierToken).ToString();

            // ジェネリック型を定義している場合
            if (statementNode.ChildNodes().OfType<TypeParameterListSyntax>().Any())
            {
                var listNode = statementNode.ChildNodes().OfType<TypeParameterListSyntax>().FirstOrDefault();
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
            var hasInherits = node.ChildNodes().OfType<InheritsStatementSyntax>().Any();
            var hasImplements = node.ChildNodes().OfType<ImplementsStatementSyntax>().Any();
            if (hasInherits || hasImplements)
            {
                var baseTypes = new List<SyntaxNode>();

                if (hasInherits)
                {
                    var inheritsNode = node.ChildNodes().OfType<InheritsStatementSyntax>().FirstOrDefault();
                    var childNodes = inheritsNode.ChildNodes();

                    // Class の場合、多重継承はできない仕様だが、将来仕様変更されるか？されないと思う
                    foreach (var childNode in childNodes)
                        baseTypes.Add(childNode);
                }

                if (hasImplements)
                {
                    var implementsNode = node.ChildNodes().OfType<ImplementsStatementSyntax>().FirstOrDefault();
                    var childNodes = implementsNode.ChildNodes();

                    foreach (var childNode in childNodes)
                        baseTypes.Add(childNode);
                }

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

            base.VisitClassBlock(node);
        }

        public override void VisitStructureBlock(StructureBlockSyntax node)
        {
            var statementNode = node.ChildNodes().OfType<StructureStatementSyntax>().FirstOrDefault();
            var isPartial = statementNode.ChildTokens().Any(x => x.Kind() == SyntaxKind.PartialKeyword);
            var defineName = statementNode.ChildTokens().FirstOrDefault(x => x.Kind() == SyntaxKind.IdentifierToken).ToString();

            // ジェネリック型を定義している場合
            if (statementNode.ChildNodes().OfType<TypeParameterListSyntax>().Any())
            {
                var listNode = statementNode.ChildNodes().OfType<TypeParameterListSyntax>().FirstOrDefault();
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
            var hasInherits = node.ChildNodes().OfType<InheritsStatementSyntax>().Any();
            var hasImplements = node.ChildNodes().OfType<ImplementsStatementSyntax>().Any();
            if (hasInherits || hasImplements)
            {
                var baseTypes = new List<SyntaxNode>();

                if (hasInherits)
                {
                    var inheritsNode = node.ChildNodes().OfType<InheritsStatementSyntax>().FirstOrDefault();
                    var childNodes = inheritsNode.ChildNodes();

                    // Struct の場合、多重継承はできない仕様だが、将来仕様変更されるか？されないと思う
                    foreach (var childNode in childNodes)
                        baseTypes.Add(childNode);
                }

                if (hasImplements)
                {
                    var implementsNode = node.ChildNodes().OfType<ImplementsStatementSyntax>().FirstOrDefault();
                    var childNodes = implementsNode.ChildNodes();

                    foreach (var childNode in childNodes)
                        baseTypes.Add(childNode);
                }

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

            base.VisitStructureBlock(node);
        }

        public override void VisitInterfaceBlock(InterfaceBlockSyntax node)
        {
            var statementNode = node.ChildNodes().OfType<InterfaceStatementSyntax>().FirstOrDefault();
            var isPartial = statementNode.ChildTokens().Any(x => x.Kind() == SyntaxKind.PartialKeyword);
            var defineName = statementNode.ChildTokens().FirstOrDefault(x => x.Kind() == SyntaxKind.IdentifierToken).ToString();

            // ジェネリック型を定義している場合
            if (statementNode.ChildNodes().OfType<TypeParameterListSyntax>().Any())
            {
                var listNode = statementNode.ChildNodes().OfType<TypeParameterListSyntax>().FirstOrDefault();
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
            var hasInherits = node.ChildNodes().OfType<InheritsStatementSyntax>().Any();
            var hasImplements = node.ChildNodes().OfType<ImplementsStatementSyntax>().Any();
            if (hasInherits || hasImplements)
            {
                var baseTypes = new List<SyntaxNode>();

                if (hasInherits)
                {
                    var inheritsNode = node.ChildNodes().OfType<InheritsStatementSyntax>().FirstOrDefault();
                    var childNodes = inheritsNode.ChildNodes();

                    // Interface の場合、Inherits, IInterface1, IInterface2 などと記述する
                    // Implements ではなく Inherits でインターフェースを継承する
                    foreach (var childNode in childNodes)
                        baseTypes.Add(childNode);
                }

                if (hasImplements)
                {
                    // 上記仕様から以下はありえないのだが、将来仕様変更されるか？されないと思う
                    var implementsNode = node.ChildNodes().OfType<ImplementsStatementSyntax>().FirstOrDefault();
                    var childNodes = implementsNode.ChildNodes();

                    foreach (var childNode in childNodes)
                        baseTypes.Add(childNode);
                }

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

            base.VisitInterfaceBlock(node);
        }

        public override void VisitModuleBlock(ModuleBlockSyntax node)
        {
            var statementNode = node.ChildNodes().OfType<ModuleStatementSyntax>().FirstOrDefault();
            var defineName = statementNode.ChildTokens().FirstOrDefault(x => x.Kind() == SyntaxKind.IdentifierToken).ToString();

            var startLength = node.Span.Start;
            var endLength = node.Span.End;
            var parentNamespace = GetNamespace(DefineKinds.Module, startLength, endLength);

            UserDefinitions.Add(new UserDefinition
            {
                DefineKinds = DefineKinds.Module,
                Namespace = parentNamespace,
                DefineName = defineName,
                DefineFullName = $"{parentNamespace}.{defineName}",
                SourceFile = SourceFile,
                StartLength = startLength,
                EndLength = endLength,
            });

            base.VisitModuleBlock(node);
        }

        public override void VisitEnumBlock(EnumBlockSyntax node)
        {
            var statementNode = node.ChildNodes().OfType<EnumStatementSyntax>().FirstOrDefault();
            var defineName = statementNode.ChildTokens().FirstOrDefault(x => x.Kind() == SyntaxKind.IdentifierToken).ToString();

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

            base.VisitEnumBlock(node);
        }

        public override void VisitConstructorBlock(ConstructorBlockSyntax node)
        {
            var statementNode = node.ChildNodes().OfType<SubNewStatementSyntax>().FirstOrDefault();
            var isPartial = statementNode.ChildTokens().Any(x => x.Kind() == SyntaxKind.PartialKeyword);
            var defineName = GetConstructorName(node.Parent); // New() ではなく、C# 変換できるように DB 内ではクラス名・構造体名を扱う

            var methodArguments = new List<MethodArgument>();
            if (statementNode.ChildNodes().OfType<ParameterListSyntax>().FirstOrDefault().ChildNodes().Any())
            {
                var listNode = statementNode.ChildNodes().OfType<ParameterListSyntax>().FirstOrDefault();
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

            base.VisitConstructorBlock(node);
        }

        public override void VisitOperatorBlock(OperatorBlockSyntax node)
        {
            var statementNode = node.ChildNodes().OfType<OperatorStatementSyntax>().FirstOrDefault();
            var defineName = statementNode.ChildTokens().LastOrDefault().ToString();

            var methodArguments = new List<MethodArgument>();
            if (statementNode.ChildNodes().OfType<ParameterListSyntax>().FirstOrDefault().ChildNodes().Any())
            {
                var listNode = statementNode.ChildNodes().OfType<ParameterListSyntax>().FirstOrDefault();
                methodArguments = GetMethodArguments(listNode);
            }

            var asNode = statementNode.ChildNodes().FirstOrDefault(x => x is SimpleAsClauseSyntax);
            var returnType = asNode.ChildNodes().FirstOrDefault().ToString();

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
                ReturnType = ConvertCSharpType(returnType),
                SourceFile = SourceFile,
                StartLength = startLength,
                EndLength = endLength,
            });

            base.VisitOperatorBlock(node);
        }

        // SubBlock, FunctionBlock が含まれているか？
        public override void VisitMethodBlock(MethodBlockSyntax node)
        {
            var statementNode = node.ChildNodes().OfType<MethodStatementSyntax>().FirstOrDefault();
            var isSubMethod = statementNode.ChildTokens().Any(x => x.Kind() == SyntaxKind.SubKeyword);
            var isPartial = statementNode.ChildTokens().Any(x => x.Kind() == SyntaxKind.PartialKeyword);
            var defineName = statementNode.ChildTokens().FirstOrDefault(x => x.Kind() == SyntaxKind.IdentifierToken).ToString();

            // ジェネリック型を定義している場合
            if (statementNode.ChildNodes().OfType<TypeParameterListSyntax>().Any())
            {
                var listNode = statementNode.ChildNodes().OfType<TypeParameterListSyntax>().FirstOrDefault();
                var genericTypes = listNode
                    .ChildNodes()
                    .OfType<TypeParameterSyntax>()
                    .Select(x => x.ChildTokens().FirstOrDefault(y => y.Kind() == SyntaxKind.IdentifierToken).ToString());

                defineName = $"{defineName}<{string.Join(", ", genericTypes)}>";
            }

            var methodArguments = new List<MethodArgument>();
            if (statementNode.ChildNodes().OfType<ParameterListSyntax>().FirstOrDefault().ChildNodes().Any())
            {
                var listNode = statementNode.ChildNodes().OfType<ParameterListSyntax>().FirstOrDefault();
                methodArguments = GetMethodArguments(listNode);
            }

            var returnType = string.Empty;
            if (!isSubMethod)
            {
                var asNode = statementNode.ChildNodes().FirstOrDefault(x => x is SimpleAsClauseSyntax);
                returnType = asNode.ChildNodes().FirstOrDefault().ToString();
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
                    if (methodArguments[0].DefineType == "Object" && methodArguments[1].DefineType.EndsWith("EventArgs"))
                    {
                        isEventHandler = true;
                    }
                }
            }

            UserDefinitions.Add(new UserDefinition
            {
                DefineKinds = isEventHandler ? DefineKinds.EventHandler : DefineKinds.Method,
                IsPartial = isPartial,
                Namespace = parentNamespace,
                DefineName = defineName,
                DefineFullName = $"{parentNamespace}.{defineName}",
                MethodArguments = methodArguments,
                ReturnType = ConvertCSharpType(returnType),
                SourceFile = SourceFile,
                StartLength = startLength,
                EndLength = endLength,
            });

            base.VisitMethodBlock(node);
        }

        // Interface のメソッドの時とか
        // Windows API 系(DllImport)
        public override void VisitMethodStatement(MethodStatementSyntax statementNode)
        {
            // VisitMethodBlock() から来た場合は、二重登録になってしまうので飛ばす
            if (statementNode.Parent is MethodBlockSyntax)
            {
                base.VisitMethodStatement(statementNode);
                return;
            }

            var isWinAPI = statementNode.AttributeLists.Any(x => x.ToString().Contains("DllImport"));
            var isSubMethod = statementNode.ChildTokens().Any(x => x.Kind() == SyntaxKind.SubKeyword);
            var isPartial = statementNode.ChildTokens().Any(x => x.Kind() == SyntaxKind.PartialKeyword);
            var defineName = statementNode.ChildTokens().FirstOrDefault(x => x.Kind() == SyntaxKind.IdentifierToken).ToString();

            // ジェネリック型を定義している場合
            if (statementNode.ChildNodes().OfType<TypeParameterListSyntax>().Any())
            {
                var listNode = statementNode.ChildNodes().OfType<TypeParameterListSyntax>().FirstOrDefault();
                var genericTypes = listNode
                    .ChildNodes()
                    .OfType<TypeParameterSyntax>()
                    .Select(x => x.ChildTokens().FirstOrDefault(y => y.Kind() == SyntaxKind.IdentifierToken).ToString());

                defineName = $"{defineName}<{string.Join(", ", genericTypes)}>";
            }

            var methodArguments = new List<MethodArgument>();
            if (statementNode.ChildNodes().OfType<ParameterListSyntax>().FirstOrDefault().ChildNodes().Any())
            {
                var listNode = statementNode.ChildNodes().OfType<ParameterListSyntax>().FirstOrDefault();
                methodArguments = GetMethodArguments(listNode);
            }

            var returnType = string.Empty;
            if (!isSubMethod)
            {
                var asNode = statementNode.ChildNodes().FirstOrDefault(x => x is SimpleAsClauseSyntax);
                returnType = asNode.ChildNodes().FirstOrDefault().ToString();
            }

            var startLength = statementNode.Span.Start;
            var endLength = statementNode.Span.End;
            var parentNamespace = GetNamespace(DefineKinds.Method, startLength, endLength);

            UserDefinitions.Add(new UserDefinition
            {
                DefineKinds = isWinAPI ? DefineKinds.WindowsAPI : DefineKinds.Method,
                IsPartial = isPartial,
                Namespace = parentNamespace,
                DefineName = defineName,
                DefineFullName = $"{parentNamespace}.{defineName}",
                MethodArguments = methodArguments,
                ReturnType = ConvertCSharpType(returnType),
                SourceFile = SourceFile,
                StartLength = startLength,
                EndLength = endLength,
            });

            base.VisitMethodStatement(statementNode);
        }

        // Windows API 系(Declare)
        public override void VisitDeclareStatement(DeclareStatementSyntax statementNode)
        {
            var isSubMethod = statementNode.ChildTokens().Any(x => x.Kind() == SyntaxKind.SubKeyword);
            var defineName = statementNode.ChildTokens().FirstOrDefault(x => x.Kind() == SyntaxKind.IdentifierToken).ToString();

            var methodArguments = new List<MethodArgument>();
            if (statementNode.ChildNodes().OfType<ParameterListSyntax>().FirstOrDefault().ChildNodes().Any())
            {
                var listNode = statementNode.ChildNodes().OfType<ParameterListSyntax>().FirstOrDefault();
                methodArguments = GetMethodArguments(listNode);
            }

            var returnType = string.Empty;
            if (!isSubMethod)
            {
                var asNode = statementNode.ChildNodes().FirstOrDefault(x => x is SimpleAsClauseSyntax);
                returnType = asNode.ChildNodes().FirstOrDefault().ToString();
            }

            var startLength = statementNode.Span.Start;
            var endLength = statementNode.Span.End;
            var parentNamespace = GetNamespace(DefineKinds.WindowsAPI, startLength, endLength);

            UserDefinitions.Add(new UserDefinition
            {
                DefineKinds = DefineKinds.WindowsAPI,
                Namespace = parentNamespace,
                DefineName = defineName,
                DefineFullName = $"{parentNamespace}.{defineName}",
                MethodArguments = methodArguments,
                ReturnType = ConvertCSharpType(returnType),
                SourceFile = SourceFile,
                StartLength = startLength,
                EndLength = endLength,
            });

            base.VisitDeclareStatement(statementNode);
        }

        public override void VisitFieldDeclaration(FieldDeclarationSyntax node)
        {
            var variables = node.ChildNodes().OfType<VariableDeclaratorSyntax>();

            foreach (var variable in variables)
            {
                // Public i1, i2 As Integer
                // Public i3() As Integer, i4 As Integer

                // 先に型を取得
                var asNode = variable.ChildNodes().FirstOrDefault(x => x is SimpleAsClauseSyntax);
                var variableType = asNode.ChildNodes().FirstOrDefault().ToString();

                // フィールド名を取得
                var fields = variable.ChildNodes().OfType<ModifiedIdentifierSyntax>();

                foreach (var field in fields)
                {
                    var startLength = field.Span.Start;
                    var endLength = field.Span.End;

                    var fieldName = field.ChildTokens().FirstOrDefault(x => x.Kind() == SyntaxKind.IdentifierToken).ToString();
                    var fieldType = variableType;

                    var hasArrayRank = field.ChildNodes().Any();
                    if (hasArrayRank)
                    {
                        var arrayRank = field.ChildNodes().FirstOrDefault().ToString();
                        fieldType = $"{fieldType}{arrayRank}";
                    }

                    var parentNamespace = GetNamespace(DefineKinds.Field, startLength, endLength);

                    UserDefinitions.Add(new UserDefinition
                    {
                        DefineKinds = DefineKinds.Field,
                        Namespace = parentNamespace,
                        DefineName = fieldName,
                        DefineFullName = $"{parentNamespace}.{fieldName}",
                        ReturnType = ConvertCSharpType(fieldType),
                        SourceFile = SourceFile,
                        StartLength = startLength,
                        EndLength = endLength,
                    });
                }
            }

            base.VisitFieldDeclaration(node);
        }

        public override void VisitPropertyBlock(PropertyBlockSyntax node)
        {
            var startLength = node.Span.Start;
            var endLength = node.Span.End;
            var statementNode = node.ChildNodes().OfType<PropertyStatementSyntax>().FirstOrDefault();
            WalkPropertyBlockOrPropertyStatement(statementNode, startLength, endLength);

            base.VisitPropertyBlock(node);
        }

        // 自動実装プロパティとか
        public override void VisitPropertyStatement(PropertyStatementSyntax node)
        {
            // VisitPropertyBlock() から来た場合は、二重登録になってしまうので飛ばす
            if (node.Parent is PropertyBlockSyntax)
            {
                base.VisitPropertyStatement(node);
                return;
            }

            var startLength = node.Span.Start;
            var endLength = node.Span.End;
            WalkPropertyBlockOrPropertyStatement(node, startLength, endLength);

            base.VisitPropertyStatement(node);
        }

        private void WalkPropertyBlockOrPropertyStatement(PropertyStatementSyntax node, int startLength, int endLength)
        {
            var defineName = node.ChildTokens().FirstOrDefault(x => x.Kind() == SyntaxKind.IdentifierToken).ToString();
            var asNode = node.ChildNodes().OfType<SimpleAsClauseSyntax>().FirstOrDefault();
            var defineType = asNode.ChildNodes().FirstOrDefault().ToString();
            var parentNamespace = GetNamespace(DefineKinds.Property, startLength, endLength);

            // Property Name As String の場合、カッコが無い → ParameterListSyntax タグがない
            // Property Name() As String の場合、カッコがある → ParameterListSyntax タグがある（個数0）
            // Default Property Item(i As Index) As String の場合、カッコがある → ParameterListSyntax タグがある（個数1以上）
            var methodArguments = new List<MethodArgument>();
            if (node.ChildNodes().OfType<ParameterListSyntax>().Any())
            {
                if (node.ChildNodes().OfType<ParameterListSyntax>().FirstOrDefault().ChildNodes().Any())
                {
                    var listNode = node.ChildNodes().OfType<ParameterListSyntax>().FirstOrDefault();
                    methodArguments = GetMethodArguments(listNode);
                }
            }

            var isIndexer = methodArguments.Any();

            UserDefinitions.Add(new UserDefinition
            {
                DefineKinds = isIndexer ? DefineKinds.Indexer : DefineKinds.Property,
                Namespace = parentNamespace,
                DefineName = defineName,
                DefineFullName = $"{parentNamespace}.{defineName}",
                MethodArguments = methodArguments,
                ReturnType = ConvertCSharpType(defineType),
                SourceFile = SourceFile,
                StartLength = startLength,
                EndLength = endLength,
            });
        }

        // カスタムイベント定義
        public override void VisitEventBlock(EventBlockSyntax node)
        {
            var startLength = node.Span.Start;
            var endLength = node.Span.End;
            var statementNode = node.ChildNodes().OfType<EventStatementSyntax>().FirstOrDefault();
            WalkEventBlockOrEventStatement(statementNode, startLength, endLength);

            base.VisitEventBlock(node);
        }

        public override void VisitEventStatement(EventStatementSyntax node)
        {
            // VisitEventBlock() から来た場合は、二重登録になってしまうので飛ばす
            if (node.Parent is EventBlockSyntax)
            {
                base.VisitEventStatement(node);
                return;
            }

            var startLength = node.Span.Start;
            var endLength = node.Span.End;
            WalkEventBlockOrEventStatement(node, startLength, endLength);

            base.VisitEventStatement(node);
        }

        private void WalkEventBlockOrEventStatement(EventStatementSyntax node, int startLength, int endLength)
        {
            var parentNamespace = GetNamespace(DefineKinds.Event, startLength, endLength);
            var defineName = node.ChildTokens().FirstOrDefault(x => x.Kind() == SyntaxKind.IdentifierToken).ToString();
            var defineType = string.Empty;

            // Public Event Clicked As EventHandler 系の宣言の場合
            if (node.ChildNodes().OfType<SimpleAsClauseSyntax>().Any())
            {
                var asNode = node.ChildNodes().OfType<SimpleAsClauseSyntax>().FirstOrDefault();
                defineType = asNode.ChildNodes().FirstOrDefault().ToString();
            }

            // Public Event Moved(sender As Object, e As EventArgs) 系の宣言の場合
            var methodArguments = new List<MethodArgument>();
            if (node.ChildNodes().OfType<ParameterListSyntax>().Any())
            {
                if (node.ChildNodes().OfType<ParameterListSyntax>().FirstOrDefault().ChildNodes().Any())
                {
                    var listNode = node.ChildNodes().OfType<ParameterListSyntax>().FirstOrDefault();
                    methodArguments = GetMethodArguments(listNode);
                }
            }

            UserDefinitions.Add(new UserDefinition
            {
                DefineKinds = DefineKinds.Event,
                Namespace = parentNamespace,
                DefineName = defineName,
                DefineFullName = $"{parentNamespace}.{defineName}",
                MethodArguments = methodArguments,
                ReturnType = ConvertCSharpType(defineType),
                SourceFile = SourceFile,
                StartLength = startLength,
                EndLength = endLength,
            });
        }

        public override void VisitDelegateStatement(DelegateStatementSyntax statementNode)
        {
            var isSubMethod = statementNode.ChildTokens().Any(x => x.Kind() == SyntaxKind.SubKeyword);
            var defineName = statementNode.ChildTokens().FirstOrDefault(x => x.Kind() == SyntaxKind.IdentifierToken).ToString();

            // ジェネリック型を定義している場合
            if (statementNode.ChildNodes().OfType<TypeParameterListSyntax>().Any())
            {
                var listNode = statementNode.ChildNodes().OfType<TypeParameterListSyntax>().FirstOrDefault();
                var genericTypes = listNode
                    .ChildNodes()
                    .OfType<TypeParameterSyntax>()
                    .Select(x => x.ChildTokens().FirstOrDefault(y => y.Kind() == SyntaxKind.IdentifierToken).ToString());

                defineName = $"{defineName}<{string.Join(", ", genericTypes)}>";
            }

            var methodArguments = new List<MethodArgument>();
            if (statementNode.ChildNodes().OfType<ParameterListSyntax>().FirstOrDefault().ChildNodes().Any())
            {
                var listNode = statementNode.ChildNodes().OfType<ParameterListSyntax>().FirstOrDefault();
                methodArguments = GetMethodArguments(listNode);
            }

            var returnType = string.Empty;
            if (!isSubMethod)
            {
                var asNode = statementNode.ChildNodes().FirstOrDefault(x => x is SimpleAsClauseSyntax);
                returnType = asNode.ChildNodes().FirstOrDefault().ToString();
            }

            var startLength = statementNode.Span.Start;
            var endLength = statementNode.Span.End;
            var parentNamespace = GetNamespace(DefineKinds.Delegate, startLength, endLength);

            UserDefinitions.Add(new UserDefinition
            {
                DefineKinds = DefineKinds.Delegate,
                Namespace = parentNamespace,
                DefineName = defineName,
                DefineFullName = $"{parentNamespace}.{defineName}",
                MethodArguments = methodArguments,
                ReturnType = ConvertCSharpType(returnType),
                SourceFile = SourceFile,
                StartLength = startLength,
                EndLength = endLength,
            });

            base.VisitDelegateStatement(statementNode);
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

            // （VBNet の場合）見つからなかった場合は、プロジェクト名前空間を返す
            if (string.IsNullOrEmpty(result))
                result = UserDefinitions[0].DefineFullName;

            return result;
        }

        private string GetConstructorName(SyntaxNode node)
        {
            var containerName = node
                .ChildNodes()
                .FirstOrDefault(x => x is ClassStatementSyntax || x is StructureStatementSyntax)
                .ChildTokens()
                .FirstOrDefault(x => x.Kind() == SyntaxKind.IdentifierToken)
                .ToString();

            return containerName;
        }

        private List<MethodArgument> GetMethodArguments(ParameterListSyntax node)
        {
            var items = new List<MethodArgument>();
            var parameters = node.ChildNodes().OfType<ParameterSyntax>();

            foreach (var parameter in parameters)
            {
                var isByRef = parameter.ChildTokens().Any(x => x.Kind() == SyntaxKind.ByRefKeyword);
                var isByVal = !isByRef;
                var isOptional = parameter.ChildTokens().Any(x => x.Kind() == SyntaxKind.OptionalKeyword);
                var isParamArray = parameter.ChildTokens().Any(x => x.Kind() == SyntaxKind.ParamArrayKeyword);

                var modified = parameter.ChildNodes().FirstOrDefault(x => x is ModifiedIdentifierSyntax);
                var parameterName = modified.ChildTokens().FirstOrDefault(x => x.Kind() == SyntaxKind.IdentifierToken).ToString();
                
                var asNode = parameter.ChildNodes().FirstOrDefault(x => x is SimpleAsClauseSyntax);
                var parameterType = asNode.ChildNodes().FirstOrDefault().ToString();

                var hasArrayRank = modified.ChildNodes().Any();
                if (hasArrayRank)
                {
                    var arrayRank = modified.ChildNodes().FirstOrDefault().ToString();
                    parameterType = $"{parameterType}{arrayRank}";
                }

                items.Add(new MethodArgument
                {
                    IsByVal = isByVal,
                    IsByRef = isByRef,
                    IsOptional = isOptional,
                    IsParamArray = isParamArray,
                    DefineName = parameterName,
                    DefineType = ConvertCSharpType(parameterType),
                });
            }

            return items;
        }

        private string ConvertCSharpType(string value)
        {
            value = RemoveNamespace(value);
            value = ConvertGenericTypes(value);
            value = ConvertArrayTypes(value);
            value = ConvertPrimitiveTypes(value);

            return value;
        }

        private string ConvertCSharpTypeWithoutRemoveNamespace(string value)
        {
            value = ConvertGenericTypes(value);
            value = ConvertArrayTypes(value);
            value = ConvertPrimitiveTypes(value);

            return value;
        }

        private static Regex RemoveNamespaceRegex;

        private string RemoveNamespace(string parameterType)
        {
            // IEnumerable(Of Int32)
            // System.Collections.Generic.IEnumerable(Of System.Int32)
            // ↓
            // IEnumerable(Of Int32)
            if (parameterType.Contains("."))
            {
                if (RemoveNamespaceRegex is null)
                    RemoveNamespaceRegex = new Regex(@"(\w+\.)*");

                parameterType = RemoveNamespaceRegex.Replace(parameterType, string.Empty);
            }

            return parameterType;
        }

        private string ConvertGenericTypes(string value)
        {
            // Class1(Of T), Class1(Of T, U)
            // Func1(Of T), Func1(Of T, U)
            // ↓
            // Class1<T>, Class1<T, U>
            // Func1<T>, Func1<T, U>
            value = value.Replace("(Of ", "<");
            value = value.Replace(")", ">");

            return value;
        }

        private string ConvertArrayTypes(string value)
        {
            // items(), items(,)
            // ↓
            // items[], items[,]
            value = value.Replace("(", "[");
            value = value.Replace(")", "]");

            return value;
        }

        private string ConvertPrimitiveTypes(string value)
        {
            // Integer
            // ↓
            // int

            var items = AppEnv.LanguageConversions;

            while (items.Any(x => value.Contains(x.VBNetType)))
            {
                var item = items.First(x => value.Contains(x.VBNetType));
                value = value.Replace(item.VBNetType, item.CSharpType);
            }

            return value;
        }

        private List<BaseTypeInfo> GetBaseTypeInfos(IEnumerable<SyntaxNode> baseTypes, string defaultNamespace)
        {
            var candidatesNamespaces = new List<string>();
            candidatesNamespaces.Add(defaultNamespace);

            while (defaultNamespace.Contains("."))
            {
                defaultNamespace = defaultNamespace.Substring(0, defaultNamespace.LastIndexOf("."));
                candidatesNamespaces.Add(defaultNamespace);
            }

            var firstImportsIndex = -1;

            if (ImportsNamespaces.Any())
            {
                var results = ImportsNamespaces.Select(x => x.Namespace).ToList();
                candidatesNamespaces.AddRange(results);
                firstImportsIndex = candidatesNamespaces.IndexOf(results.FirstOrDefault());
            }

            var items = new List<BaseTypeInfo>();

            foreach (var baseType in baseTypes)
            {
                var hasGlobal = false;
                var typeName = baseType.ToString();
                typeName = ConvertCSharpTypeWithoutRemoveNamespace(typeName);

                if (ImportsNamespaces.Any())
                {
                    foreach (var check in ImportsNamespaces)
                    {
                        if (!string.IsNullOrEmpty(check.Alternate) && typeName.Contains($"{check.Alternate}"))
                            typeName = typeName.Replace(check.Alternate, check.Namespace);

                        // 名前空間エイリアス修飾子（::演算子）の場合、:: が残る場合は . に置換
                        // ※ VBNet では :: は仕様的に無く . になるみたい
                        if (typeName.Contains("Global."))
                        {
                            hasGlobal = true;
                            typeName = typeName.Replace("Global.", string.Empty);
                        }
                    }
                }

                var candidatesDefineFullNames = new List<string>();

                if (firstImportsIndex == -1)
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
                        for (var i = 0; i < firstImportsIndex; i++)
                            candidatesDefineFullNames.Add($"{candidatesNamespaces[i]}.{typeName}");
                    }

                    if (!typeName.Contains("."))
                        candidatesDefineFullNames.Insert(firstImportsIndex, typeName);

                    for (var i = firstImportsIndex; i < candidatesNamespaces.Count; i++)
                        candidatesDefineFullNames.Add($"{candidatesNamespaces[i]}.{typeName}");
                }



                var genericParts = new List<string>();

                // IEnumerable(Of Integer), IEnumerable(Of IEnumerable(Of Integer), Dictionary(Of Integer, Integer)
                // Dictionary(Of Integer, Dictionary(Of Integer, Integer))
                var node = baseType.ChildNodes().FirstOrDefault();
                if (node is GenericNameSyntax)
                {
                    var listNode = node.ChildNodes().FirstOrDefault(); // TypeArgumentListSyntax
                    var genericTypes = listNode.ChildNodes(); // PredefinedTypeSyntax, GenericNameSyntax, ...

                    foreach (var genericType in genericTypes)
                    {
                        var genericTypeName = genericType.ToString();
                        genericTypeName = ConvertCSharpTypeWithoutRemoveNamespace(genericTypeName);

                        genericParts.Add(genericTypeName);
                    }
                }

                var startLength = baseType.Span.Start;
                var endLength = baseType.Span.End;

                items.Add(new BaseTypeInfo
                {
                    CandidatesNamespaces = candidatesNamespaces,
                    CandidatesDefineFullNames = candidatesDefineFullNames,
                    BaseType = typeName,
                    DisplayBaseType = ConvertCSharpType(typeName),
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
