using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.Rewriting
{
#pragma warning disable RS0016 // Add public types and members to the declared API


    public abstract class CodeRewriter : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray<DiagnosticDescriptor>.Empty;

        public abstract void Initialize(RewriterContext context);

        public override void Initialize(AnalysisContext context)
        {
            // Do nothing.
        }
    }

    public class RewriterContext
    {
        public AnalyzerOptions Options { get; }

        public CancellationToken CancellationToken { get; }

        public IEnumerable<Func<CompilationRewriteContext, Compilation>> RegisteredRewrites
        {
            get
            {
                return registeredRewrites;
            }
        }

        private List<Func<CompilationRewriteContext, Compilation>> registeredRewrites = new List<Func<CompilationRewriteContext, Compilation>>();

        public RewriterContext(AnalyzerOptions options, CancellationToken cancellationToken)
        {
            Options = options;
            CancellationToken = cancellationToken;
        }

        public void RegisterRewriteAction(Func<CompilationRewriteContext, Compilation> rewrite)
        {
            registeredRewrites.Add(rewrite);
        }
    }

    public delegate SyntaxTree ParseFunction(string filePath, bool isScript);

    /// <summary>
    /// Context for a compilation rewrite action.
    /// </summary>
    public struct CompilationRewriteContext
    {
        private readonly Compilation _compilation;
        private readonly AnalyzerOptions _options;
        private readonly CancellationToken _cancellationToken;
        private readonly ParseFunction _parseFunction;

        /// <summary>
        /// <see cref="CodeAnalysis.Compilation"/> that is the subject of the analysis.
        /// </summary>
        public Compilation Compilation { get { return _compilation; } }

        /// <summary>
        /// Options specified for the analysis.
        /// </summary>
        public AnalyzerOptions Options { get { return _options; } }

        /// <summary>
        /// Token to check for requested cancellation of the analysis.
        /// </summary>
        public CancellationToken CancellationToken { get { return _cancellationToken; } }

        internal CompilationRewriteContext(
            Compilation compilation,
            AnalyzerOptions options,
            ParseFunction parseFunction,
            CancellationToken cancellationToken)
        {
            _compilation = compilation;
            _options = options;
            _cancellationToken = cancellationToken;
            _parseFunction = parseFunction;
        }

        /// <summary>
        /// Parses the given file and returns the syntax tree if parse succeeds
        /// </summary>
        public SyntaxTree TryParse(string filePath, bool isScript = false)
        {
            return _parseFunction(filePath, isScript);
        }
    }
}
