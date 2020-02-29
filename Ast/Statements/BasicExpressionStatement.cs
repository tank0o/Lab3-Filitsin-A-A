using Lab3.Ast;
namespace Lab3.Ast.Statements {
	class BasicExpressionStatement : INode
	{
		public int Position { get; }
		public readonly IExpression Expr;
		public BasicExpressionStatement(int position, IExpression expr) {
			Position = position;
			Expr = expr;
		}
		public virtual string FormattedString => $"{Expr.FormattedString}\n";
	}
}
