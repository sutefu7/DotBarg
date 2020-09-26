using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;

namespace DotBarg.Libraries.Roslyns
{
    public class CSharpFoldingSyntaxWalker : CSharpSyntaxWalker, IFoldingSyntaxWalker
    {
        public List<FoldingData> Items { get; set; }

        public CSharpFoldingSyntaxWalker() : base(SyntaxWalkerDepth.Trivia)
        {
            Items = new List<FoldingData>();
        }

        public void Parse(string sourceCode)
        {
            var tree = CSharpSyntaxTree.ParseText(sourceCode);
            var node = tree.GetRoot();

            Visit(node);
        }

        public override void Visit(SyntaxNode node)
        {
            base.Visit(node);
        }


        private Stack<RegionDirectiveTriviaSyntax> _RegionStack;

        public override void VisitRegionDirectiveTrivia(RegionDirectiveTriviaSyntax node)
        {
            if (_RegionStack is null)
                _RegionStack = new Stack<RegionDirectiveTriviaSyntax>();

            _RegionStack.Push(node);
            base.VisitRegionDirectiveTrivia(node);
        }

        public override void VisitEndRegionDirectiveTrivia(EndRegionDirectiveTriviaSyntax node)
        {
            AddRegionData(node);
            base.VisitEndRegionDirectiveTrivia(node);
        }

        public override void VisitCompilationUnit(CompilationUnitSyntax node)
        {
            base.VisitCompilationUnit(node);
        }

        public override void VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
        {
            AddDeclarationData(node);
            base.VisitNamespaceDeclaration(node);
        }

        public override void VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            AddDeclarationData(node);
            base.VisitClassDeclaration(node);
        }

        public override void VisitStructDeclaration(StructDeclarationSyntax node)
        {
            AddDeclarationData(node);
            base.VisitStructDeclaration(node);
        }

        public override void VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
        {
            AddDeclarationData(node);
            base.VisitInterfaceDeclaration(node);
        }

        public override void VisitEnumDeclaration(EnumDeclarationSyntax node)
        {
            AddDeclarationData(node);
            base.VisitEnumDeclaration(node);
        }

        public override void VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
        {
            AddDeclarationData(node);
            base.VisitConstructorDeclaration(node);
        }

        public override void VisitOperatorDeclaration(OperatorDeclarationSyntax node)
        {
            AddDeclarationData(node);
            base.VisitOperatorDeclaration(node);
        }

        public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            AddDeclarationData(node);
            base.VisitMethodDeclaration(node);
        }

        public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            AddDeclarationData(node);
            base.VisitPropertyDeclaration(node);
        }

        public override void VisitAccessorDeclaration(AccessorDeclarationSyntax node)
        {
            AddDeclarationData(node);
            base.VisitAccessorDeclaration(node);
        }

        public override void VisitEventDeclaration(EventDeclarationSyntax node)
        {
            AddDeclarationData(node);
            base.VisitEventDeclaration(node);
        }

        private void AddRegionData(CSharpSyntaxNode node)
        {
            var startSyntax = _RegionStack.Pop();
            var startLength = startSyntax.Span.Start;
            var endLength = node.Span.End;

            // #region aaa のうち、冒頭の「#region」を除去
            var header = startSyntax.ToString();
            header = header.Substring("#region ".Length);

            Items.Add(new FoldingData
            {
                Name = header,
                StartOffset = startLength,
                EndOffset = endLength,
            });
        }

        private void AddDeclarationData(CSharpSyntaxNode node)
        {
            // ブロック系は、開始ノードが、開始と終了の両方の文字列位置を持っているので、取得する
            var startLength = node.Span.Start;
            var endLength = node.Span.End;

            var header = node.ToString();
            if (header.Contains(Environment.NewLine))
            {
                header = header.Substring(0, header.IndexOf(Environment.NewLine));
                header = $"{header} ...";
            }

            Items.Add(new FoldingData
            {
                Name = header,
                StartOffset = startLength,
                EndOffset = endLength,
            });
        }
    }
}
