namespace ICSharpCode.NRefactory.CSharp
{
	public class LoadTypeArgumentExpression : Expression {
		public CSharpTokenNode LParToken {
			get { return GetChildByRole(Roles.LPar); }
		}

		public AstType Type {
			get { return GetChildByRole(Roles.Type); }
			set { SetChildByRole(Roles.Type, value); }
		}

		public CSharpTokenNode RParToken {
			get { return GetChildByRole(Roles.RPar); }
		}

		public LoadTypeArgumentExpression() {
		}

		public LoadTypeArgumentExpression(AstType type) {
			AddChild(type, Roles.Type);
		}

		public override void AcceptVisitor(IAstVisitor visitor) {
			visitor.VisitLoadTypeArgumentExpression(this);
		}

		public override T AcceptVisitor<T>(IAstVisitor<T> visitor) {
			return visitor.VisitLoadTypeArgumentExpression(this);
		}

		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data) {
			return visitor.VisitLoadTypeArgumentExpression(this, data);
		}

		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match) {
			LoadTypeArgumentExpression o = other as LoadTypeArgumentExpression;
			return o != null && this.Type.DoMatch(o.Type, match);
		}
	}
}