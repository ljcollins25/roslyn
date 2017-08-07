using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Rewriting
{
#pragma warning disable RS0016 // Add public types and members to the declared API

    /// <summary>
    /// Provides facility for tracking nodes
    /// </summary>
    public static class SyntaxTracker
    {
        internal interface ITrackingSyntaxTree
        {
            bool IsRewritten { get; }
        }

        private static ConditionalWeakTable<SyntaxAnnotation, SyntaxNode> s_trackedSyntaxNodes
            = new ConditionalWeakTable<SyntaxAnnotation, SyntaxNode>();
        private static ConditionalWeakTable<SyntaxAnnotation, Tuple<SyntaxToken>> s_trackedSyntaxTokens
            = new ConditionalWeakTable<SyntaxAnnotation, Tuple<SyntaxToken>>();

        public const string TrackingAnnotationId = "Microsoft.CodeAnalysis.Rewriting.SyntaxTracker.Tracking";

        //public static T TrackTree<T>(this T node)
        //    where T : SyntaxNode
        //{
        //    var result = node.ReplaceNodes(node.DescendantNodesAndSelf(), (n, r) =>
        //       n.HasAnnotations(TrackingAnnotationId) ? r : n.WithAdditionalAnnotations(GetAnnotation(n)));

        //    foreach (var d in result.DescendantNodesAndSelf())
        //    {
        //        var od = d.TryGetOriginalNode();
        //    }

        //    return result;
        //}

        public static bool IsRewritten(this SyntaxTree tree)
        {
            return (tree as ITrackingSyntaxTree)?.IsRewritten ?? false;
        }

        public static T TrackNode<T>(this T node)
            where T : SyntaxNode
        {
            if (node == null)
            {
                return node;
            }

            return node.HasAnnotations(TrackingAnnotationId) ? node : node.WithAdditionalAnnotations(GetAnnotation(node));
        }

        public static SyntaxToken TrackToken(this SyntaxToken node)
        {
            if (node.Width == 0)
            {
                return node;
            }

            return node.HasAnnotations(TrackingAnnotationId) ? node : node.WithAdditionalAnnotations(GetAnnotation(node));
        }

        private static SyntaxAnnotation GetAnnotation(SyntaxNode node)
        {
            var annotation = new SyntaxAnnotation(TrackingAnnotationId);
            //s_trackedSyntaxNodes.Add(annotation, node);
            return annotation;
        }

        private static SyntaxAnnotation GetAnnotation(SyntaxToken node)
        {
            var annotation = new SyntaxAnnotation(TrackingAnnotationId);
            //s_trackedSyntaxTokens.Add(annotation, Tuple.Create(node));
            return annotation;
        }

        public static SyntaxNode TryGetOriginalNode(this SyntaxNode node)
        {
            foreach(var annotation in node.GetAnnotations(TrackingAnnotationId))
            {
                SyntaxNode original;
                if (s_trackedSyntaxNodes.TryGetValue(annotation, out original))
                {
                    return original;
                }
            }

            return null;
        }

        public static SyntaxToken? TryGetOriginalToken(this SyntaxToken node)
        {
            foreach (var annotation in node.GetAnnotations(TrackingAnnotationId))
            {
                Tuple<SyntaxToken> original;
                if (s_trackedSyntaxTokens.TryGetValue(annotation, out original))
                {
                    return original.Item1;
                }
            }

            return null;
        }

        public static void AddOriginalNode(this SyntaxNode node)
        {
            foreach (var annotation in node.GetAnnotations(TrackingAnnotationId))
            {
                s_trackedSyntaxNodes.Add(annotation, node);
                return;
            }
        }

        public static void AddOriginalToken(this SyntaxToken node)
        {
            foreach (var annotation in node.GetAnnotations(TrackingAnnotationId))
            {
                s_trackedSyntaxTokens.Add(annotation, Tuple.Create(node));
                return;
            }
        }

    }
}
