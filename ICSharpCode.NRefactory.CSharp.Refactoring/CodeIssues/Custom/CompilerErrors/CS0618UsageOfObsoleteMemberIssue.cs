﻿//
// CS0618UsageOfObsoleteMemberIssue.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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

using System.Collections.Generic;
using System.Linq;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.Refactoring;
using ICSharpCode.NRefactory.CSharp.Refactoring;
using ICSharpCode.NRefactory.PatternMatching;
using Mono.CSharp;
using ICSharpCode.NRefactory.Semantics;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[IssueDescription ("CS0618: Member is obsolete",
		Description = "CS0618: Member is obsolete",
		Category = IssueCategories.CompilerWarnings,
		Severity = Severity.Warning,
		PragmaWarning = 618)]
	public class CS0618UsageOfObsoleteMemberIssue : GatherVisitorCodeIssueProvider
	{
		protected override IGatherVisitor CreateVisitor(BaseRefactoringContext context)
		{
			return new GatherVisitor(context);
		}

		class GatherVisitor : GatherVisitorBase<CS0618UsageOfObsoleteMemberIssue>
		{
			public GatherVisitor(BaseRefactoringContext ctx) : base(ctx)
			{
			}

			void Check(ResolveResult rr, AstNode nodeToMark)
			{
				if (rr == null || rr.IsError)
					return;
				IMember member = null;
				var memberRR = rr as MemberResolveResult;
				if (memberRR != null)
					member = memberRR.Member;

				if (member == null)
					return;

				var attr = member.Attributes.FirstOrDefault(a => a.AttributeType.Name == "ObsoleteAttribute" && a.AttributeType.Namespace == "System");
				if (attr == null)
					return;
				AddIssue(new CodeIssue(nodeToMark, string.Format(ctx.TranslateString("'{0}' is obsolete"), member.FullName)));
			}

			public override void VisitMemberReferenceExpression(MemberReferenceExpression memberReferenceExpression)
			{
				base.VisitMemberReferenceExpression(memberReferenceExpression);
				Check(ctx.Resolve(memberReferenceExpression), memberReferenceExpression.MemberNameToken);
			}

			public override void VisitInvocationExpression(InvocationExpression invocationExpression)
			{
				base.VisitInvocationExpression(invocationExpression);
				Check(ctx.Resolve(invocationExpression), invocationExpression.Target);
			}

			public override void VisitIdentifierExpression(IdentifierExpression identifierExpression)
			{
				base.VisitIdentifierExpression(identifierExpression);
				Check(ctx.Resolve(identifierExpression), identifierExpression);
			}
		}
	}
}

