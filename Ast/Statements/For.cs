namespace Lab3.Ast.Statements
{
	sealed class For : IStatement
	{
		public int Position { get; }
		public readonly IForStatement Initializer;//
		public readonly IExpression Condition;
		public readonly IForStatement Iterator;
		public readonly Block Body;

		public For(int position, IForStatement initializer, IExpression condition, IForStatement iterator, Block body)
		{
			Position = position;
			Initializer = initializer;
			Condition = condition;
			Iterator = iterator;
			Body = body;
		}

		public string FormattedString => $"for ({(Initializer != null ? Initializer.FormattedString : "")}; {(Condition != null ? Condition.FormattedString : "")}; {Iterator.FormattedString}) {Body.FormattedString}";
		public void Accept(IStatementVisitor visitor) => visitor.VisitFor(this);
		public T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitFor(this);
	}
}
