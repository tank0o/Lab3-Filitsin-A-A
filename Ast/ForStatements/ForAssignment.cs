using Lab3.Ast.Statements;

namespace Lab3.Ast.ForStatements
{
	sealed class ForAssignment : BasicAssignment, IForStatement
	{
		public ForAssignment(int position, IExpression target, TypeNode type, IExpression expr) : base(position, target, type, expr)
		{

		}
		public void Accept(IForStatementVisitor visitor) => visitor.VisitForAssignment(this);
		public T Accept<T>(IForStatementVisitor<T> visitor) => visitor.VisitForAssignment(this);
	}
}
