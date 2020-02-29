namespace Lab3.Ast {
	interface IForStatement : INode {
		void Accept(IForStatementVisitor visitor);
		T Accept<T>(IForStatementVisitor<T> visitor);
	}
}
