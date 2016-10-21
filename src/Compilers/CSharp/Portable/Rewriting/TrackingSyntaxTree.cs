using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Rewriting;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Rewriting
{
    internal class TrackingSyntaxTree : CSharpSyntaxTree
    {
        private static readonly TrackingRewriter Rewriter = new TrackingRewriter();
        private static readonly TrackingVisitor Visitor = new TrackingVisitor();

        private CSharpSyntaxTree _tree;
        private CSharpSyntaxNode _root;
        private bool _isRewritten = false;

        private TrackingSyntaxTree(
            CSharpSyntaxTree tree,
            bool isRewritten = false)
        {
            _tree = tree;
            _root = CloneNodeAsRoot(tree.GetRoot());
            var originalRoot = _root.TryGetOriginalNode();
            _isRewritten = isRewritten;
        }

        public static CSharpSyntaxTree TrackTree(CSharpSyntaxTree tree)
        {
            var oldRoot = tree.GetRoot();

            var newRoot = Rewriter.Visit(oldRoot);
            tree = (CSharpSyntaxTree)tree.WithRootAndOptions(newRoot, tree.Options);
            Visitor.Visit(tree.GetRoot());

            var result = new TrackingSyntaxTree(tree);
            var trackingOriginalRoot = result._root.TryGetOriginalNode();
            return result;
        }

        public override Encoding Encoding => _tree.Encoding;

        public override string FilePath => _tree.FilePath;

        public override bool HasCompilationUnitRoot => _tree.HasCompilationUnitRoot;

        public override int Length => _tree.Length;

        public override CSharpParseOptions Options => _tree.Options;

        public override SyntaxReference GetReference(SyntaxNode node)
        {
            return new TrackingSyntaxReference(this, _tree.GetReference(node));
        }

        public override CSharpSyntaxNode GetRoot(CancellationToken cancellationToken = default(CancellationToken))
        {
            return _root;
        }

        public override SourceText GetText(CancellationToken cancellationToken = default(CancellationToken))
        {
            return _tree.GetText(cancellationToken);
        }

        public override bool TryGetRoot(out CSharpSyntaxNode root)
        {
            root = _root;
            return true;
        }

        public override bool TryGetText(out SourceText text)
        {
            return _tree.TryGetText(out text);
        }

        public override SyntaxTree WithFilePath(string path)
        {
            return new TrackingSyntaxTree((CSharpSyntaxTree)_tree.WithFilePath(path), isRewritten: _isRewritten);
        }

        public override SyntaxTree WithRootAndOptions(SyntaxNode root, ParseOptions options)
        {
            return new TrackingSyntaxTree(
                (CSharpSyntaxTree)_tree.WithRootAndOptions(root, options),
                isRewritten: _isRewritten || root != _root);
        }

        public override FileLinePositionSpan GetMappedLineSpan(TextSpan span, CancellationToken cancellationToken = default(CancellationToken))
        {
            bool isHiddenPosition;
            return GetMappedLineSpanAndVisibility(span, out isHiddenPosition, cancellationToken);
        }

        public override LineVisibility GetLineVisibility(int position, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (_isRewritten)
            {
                TextSpan originalSpan;
                SyntaxTree originalTree;
                bool foundOriginal = TryGetOriginalSpan(TextSpan.FromBounds(position, position), out originalTree, out originalSpan);

                if (foundOriginal)
                {
                    return originalTree.GetLineVisibility(originalSpan.Start, cancellationToken);
                }

                // For rewritten trees, only lines mapped from the original tree should be visible.
                return LineVisibility.Hidden;
            }

            return _tree.GetLineVisibility(position, cancellationToken);
        }

        internal override FileLinePositionSpan GetMappedLineSpanAndVisibility(TextSpan span, out bool isHiddenPosition)
        {
            if (_isRewritten)
            {
                TextSpan originalSpan;
                SyntaxTree originalTree;
                bool foundOriginal = TryGetOriginalSpan(span, out originalTree, out originalSpan);

                if (foundOriginal)
                {
                    return AsMapped(originalTree.GetMappedLineSpanAndVisibility(originalSpan, out isHiddenPosition));
                }
            }

            var result = _tree.GetMappedLineSpanAndVisibility(span, out isHiddenPosition);
            // For rewritten trees, only lines mapped from the original tree should be visible.
            isHiddenPosition |= _isRewritten;
            return result;
        }

        private FileLinePositionSpan AsMapped(FileLinePositionSpan span)
        {
            return new FileLinePositionSpan(span.Path, span.Span, true);
        }

        private bool TryGetOriginalSpan(
            TextSpan span,
            out SyntaxTree originalTree,
            out TextSpan originalSpan,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            originalTree = null;
            originalSpan = span;

            if (_isRewritten)
            {
                var root = _tree.GetRoot(cancellationToken);
                var token = root.FindToken(span.Start);
                if (token.Width > 0)
                {
                    if (token.FullSpan.Contains(span))
                    {
                        var originalToken = token.TryGetOriginalToken();
                        if (originalToken != null)
                        {
                            originalTree = originalToken.Value.SyntaxTree;
                            originalSpan = token.FullSpan == span ?
                                originalToken.Value.FullSpan :
                                originalToken.Value.Span;
                            return true;
                        }

                        return false;
                    }

                    var node = token.Parent;
                    while (node != null)
                    {
                        if (node.FullSpan.Contains(span))
                        {
                            var originalNode = node.TryGetOriginalNode();
                            if (originalNode != null)
                            {
                                originalTree = originalNode.SyntaxTree;
                                originalSpan = node.FullSpan == span ?
                                    originalNode.FullSpan :
                                    originalNode.Span;
                                return true;
                            }

                            return false;
                        }

                        node = node.Parent;
                    }
                }
            }

            return false;
        }

        private FileLinePositionSpan GetMappedLineSpanAndVisibility(TextSpan span, out bool isHiddenPosition, CancellationToken cancellationToken)
        {
            if (_isRewritten)
            {
                isHiddenPosition = true;
                var root = _tree.GetRoot(cancellationToken);
                var node = root.FindNode(span);
                if (node != null)
                {
                    var originalNode = node.TryGetOriginalNode();
                    if (originalNode != null)
                    {
                        isHiddenPosition = false;
                        var originalSpan = node.FullSpan == span ?
                            originalNode.FullSpan :
                            originalNode.Span;
                        return originalNode.SyntaxTree.GetMappedLineSpan(originalSpan);
                    }
                }
            }

            return _tree.GetMappedLineSpanAndVisibility(span, out isHiddenPosition);
        }

        private class TrackingVisitor : CSharpSyntaxWalker
        {
            public TrackingVisitor() 
                : base(SyntaxWalkerDepth.Token)
            {
            }

            public override void Visit(SyntaxNode node)
            {
                node.AddOriginalNode();
                base.Visit(node);
            }

            public override void VisitToken(SyntaxToken token)
            {
                token.AddOriginalToken();
                base.VisitToken(token);
            }
        }

        private class TrackingRewriter : CSharpSyntaxRewriter
        {
            public TrackingRewriter()
                : base()
            {
            }

            public override SyntaxNode Visit(SyntaxNode node)
            {
                return base.Visit(SyntaxTracker.TrackNode(node));
            }

            public override SyntaxToken VisitToken(SyntaxToken token)
            {
                return base.VisitToken(SyntaxTracker.TrackToken(token));
            }
        }

        private class TrackingSyntaxReference : SyntaxReference
        {
            private readonly TrackingSyntaxTree _tree;
            private readonly SyntaxReference _reference;

            public TrackingSyntaxReference(TrackingSyntaxTree tree, SyntaxReference reference)
            {
                _tree = tree;
                _reference = reference;
            }

            public override TextSpan Span
            {
                get
                {
                    return _reference.Span;
                }
            }

            public override SyntaxTree SyntaxTree
            {
                get
                {
                    return _tree;
                }
            }

            public override SyntaxNode GetSyntax(CancellationToken cancellationToken = default(CancellationToken))
            {
                return _reference.GetSyntax(cancellationToken);
            }
        }
    }
}
