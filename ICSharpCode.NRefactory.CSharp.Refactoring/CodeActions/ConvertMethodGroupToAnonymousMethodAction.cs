//
// ConvertMethodGroupToAnonymousMethodAction.cs
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
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ICSharpCode.NRefactory6.CSharp.Refactoring;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Formatting;

namespace ICSharpCode.NRefactory6.CSharp.Refactoring
{
	[NRefactoryCodeRefactoringProvider(Description = "Convert method group to anoymous method")]
	[ExportCodeRefactoringProvider("Convert method group to anoymous method", LanguageNames.CSharp)]
	public class ConvertMethodGroupToAnonymousMethodAction : CodeRefactoringProvider
	{
		public override async Task<IEnumerable<CodeAction>> GetRefactoringsAsync(CodeRefactoringContext context)
		{
			var document = context.Document;
			var span = context.Span;
			var cancellationToken = context.CancellationToken;
			var model = await document.GetSemanticModelAsync(cancellationToken);
			var root = await model.SyntaxTree.GetRootAsync(cancellationToken);

			var node = root.FindNode(span);
			if (!node.IsKind(SyntaxKind.IdentifierName))
				return Enumerable.Empty<CodeAction>();

			if (node.Parent.IsKind(SyntaxKind.SimpleMemberAccessExpression))
				node = node.Parent;

			var info = model.GetTypeInfo(node, cancellationToken);
			var type = info.ConvertedType ?? info.Type;
			if (type == null)
				return Enumerable.Empty<CodeAction>();

			var invocationMethod = type.GetDelegateInvokeMethod();
			if (invocationMethod == null)
				return Enumerable.Empty<CodeAction>();

			return new []  { 
				CodeActionFactory.Create(
					node.Span,
					DiagnosticSeverity.Info,
					"Convert to anonymous method",
					t2 => {
						var expr = SyntaxFactory.InvocationExpression(
							(ExpressionSyntax)node,
							SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(invocationMethod.Parameters.Select(p => SyntaxFactory.Argument(SyntaxFactory.IdentifierName(p.Name)))))
						);
						var parameters = invocationMethod.Parameters.Select(p => CreateParameterSyntax(model, node, p)).ToList();
						var stmt = invocationMethod.ReturnType.SpecialType == SpecialType.System_Void ? (StatementSyntax)SyntaxFactory.ExpressionStatement(expr) : SyntaxFactory.ReturnStatement(expr);
						var ame = SyntaxFactory.AnonymousMethodExpression(
							parameters.Count == 0 ? null : SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(parameters)),
							SyntaxFactory.Block(stmt)
						);
						var newRoot = root.ReplaceNode(node, ame.WithAdditionalAnnotations(Formatter.Annotation));
						return Task.FromResult(document.WithSyntaxRoot(newRoot));
					}
				)
			};
		}

		ParameterSyntax CreateParameterSyntax(SemanticModel model, SyntaxNode node, IParameterSymbol p)
		{
			return SyntaxFactory.Parameter(
				SyntaxFactory.List<AttributeListSyntax>(),
				SyntaxFactory.TokenList(),
				SyntaxFactory.ParseTypeName(p.Type.ToMinimalDisplayString(model, node.SpanStart)),
				SyntaxFactory.Identifier(p.Name),
				null
			);
		}
	}
}