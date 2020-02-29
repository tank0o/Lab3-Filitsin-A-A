using Lab3.Ast.ForStatements;
namespace Lab3.Ast {
	interface IForStatementVisitor
	{
		void VisitForExpressionStatement(ForExpressionStatement expressionStatement);//ForExpressionStatement
		void VisitForAssignment(ForAssignment assignment);//ForBasicAssignment
	}
	interface IForStatementVisitor<T> {

		T VisitForExpressionStatement(ForExpressionStatement expressionStatement);//ForExpressionStatement
		T VisitForAssignment(ForAssignment assignment);//ForBasicAssignment
	}
}
