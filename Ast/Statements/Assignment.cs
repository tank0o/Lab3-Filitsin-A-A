namespace Lab3.Ast.Statements
{
	sealed class Assignment : BasicAssignment, IStatement
	{

		public Assignment(int position, IExpression target, TypeNode type, IExpression expr) : base(position, target, type, expr)
		{

		}
		public override string FormattedString
		{
			get
			{
				return $"{base.FormattedString};\n";
			}
		}
		public void Accept(IStatementVisitor visitor) => visitor.VisitAssignment(this);
		public T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitAssignment(this);
	}
}
