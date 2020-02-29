namespace Lab3.Ast.Statements
{
	sealed class ExpressionStatement : BasicExpressionStatement, IStatement
	{
		public ExpressionStatement(int position, IExpression expr) : base(position, expr)
		{

		}
		public override string FormattedString => $"{base.FormattedString};\n";
		public void Accept(IStatementVisitor visitor) => visitor.VisitExpressionStatement(this);
		public T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitExpressionStatement(this);
	}
}
