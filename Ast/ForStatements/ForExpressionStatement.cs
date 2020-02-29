using Lab3.Ast.Statements;

namespace Lab3.Ast.ForStatements
{
	sealed class ForExpressionStatement : BasicExpressionStatement, IForStatement
	{
		public ForExpressionStatement(int position, IExpression expr) : base(position, expr)
		{

		}
		public void Accept(IForStatementVisitor visitor) => visitor.VisitForExpressionStatement(this);
		public T Accept<T>(IForStatementVisitor<T> visitor) => visitor.VisitForExpressionStatement(this);
	}
}
