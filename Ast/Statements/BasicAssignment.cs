namespace Lab3.Ast.Statements {
	class BasicAssignment : INode
	{
		public int Position { get; }
		public readonly IExpression Target;
		public readonly TypeNode Type;
		public readonly IExpression Expr;
		public BasicAssignment(int position, IExpression target, TypeNode type, IExpression expr) {
			Position = position;
			Target = target;
			Type = type;
			Expr = expr;
		}
		public virtual string FormattedString {
			get {
				var type = Type != null ? $" : {Type.FormattedString}" : "";
				return $"{Target.FormattedString}{type} = {Expr.FormattedString}\n";
			}
		}
	}
}
