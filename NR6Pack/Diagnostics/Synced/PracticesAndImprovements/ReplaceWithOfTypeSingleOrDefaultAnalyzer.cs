//
// ReplaceWithOfTypeSingleOrDefaultAnalyzer.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2013 Xamarin Inc. (http://xamarin.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeFixes;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Text;
using System.Threading;
using ICSharpCode.NRefactory6.CSharp.Refactoring;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.FindSymbols;

namespace ICSharpCode.NRefactory6.CSharp.Diagnostics
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class ReplaceWithOfTypeSingleOrDefaultAnalyzer : DiagnosticAnalyzer
	{
		static readonly DiagnosticDescriptor descriptor = new DiagnosticDescriptor (
			NRefactoryDiagnosticIDs.ReplaceWithOfTypeSingleOrDefaultAnalyzerID, 
			GettextCatalog.GetString("Replace with call to OfType<T>().SingleOrDefault()"),
			GettextCatalog.GetString("Replace with 'OfType<T>().SingleOrDefault()'"), 
			DiagnosticAnalyzerCategories.PracticesAndImprovements, 
			DiagnosticSeverity.Info, 
			isEnabledByDefault: true,
			helpLinkUri: HelpLink.CreateFor(NRefactoryDiagnosticIDs.ReplaceWithOfTypeSingleOrDefaultAnalyzerID)
		);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create (descriptor);

		public override void Initialize(AnalysisContext context)
		{
			context.RegisterSyntaxNodeAction(
				(nodeContext) => {
					Diagnostic diagnostic;
					if (TryGetDiagnostic(nodeContext, out diagnostic))
						nodeContext.ReportDiagnostic(diagnostic);
				},
				SyntaxKind.InvocationExpression
			);
		}

		static bool TryGetDiagnostic (SyntaxNodeAnalysisContext nodeContext, out Diagnostic diagnostic)
		{
			diagnostic = default(Diagnostic);
			if (nodeContext.IsFromGeneratedCode())
				return false;
			var anyInvoke = nodeContext.Node as InvocationExpressionSyntax;

			var info = nodeContext.SemanticModel.GetSymbolInfo(anyInvoke);

			IMethodSymbol anyResolve = info.Symbol as IMethodSymbol;
			if (anyResolve == null) {
				anyResolve = info.CandidateSymbols.OfType<IMethodSymbol>().FirstOrDefault(candidate => HasPredicateVersion(candidate));
			} 

			if (anyResolve == null || !HasPredicateVersion(anyResolve))
				return false;

			ExpressionSyntax target, followUp;
			TypeSyntax type;
			ParameterSyntax param;
			if (ReplaceWithOfTypeAnyAnalyzer.MatchSelect(anyInvoke, out target, out type, out param, out followUp)) {
				// if (member == "Where" && followUp == null) return;
				diagnostic = Diagnostic.Create (
					descriptor,
					anyInvoke.GetLocation ()
				);
				return true;
			}
			return false;
		}

		static bool HasPredicateVersion(IMethodSymbol member)
		{
			if (!ReplaceWithOfTypeAnyAnalyzer.IsQueryExtensionClass(member.ContainingType))
				return false;
			return member.Name == "SingleOrDefault";
		}
	}
}