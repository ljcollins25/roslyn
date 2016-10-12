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

        public abstract Compilation Rewrite(Compilation compilation, AnalyzerOptions options, CancellationToken cancellationToken);

        public override void Initialize(AnalysisContext context)
        {
            // Do nothing.
        }
    }
}
