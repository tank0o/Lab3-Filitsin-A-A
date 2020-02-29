using Lab3.Ast;
using Lab3.Ast.Expressions;
using Lab3.Ast.Statements;
using Lab3.Parsing;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
namespace Lab3.Compiling
{
	sealed class MethodBodyCompiler : IStatementVisitor, IExpressionVisitor<TypeRef>
	{
		readonly SourceFile sourceFile;
		readonly AllTypes types;
		readonly ModuleDefinition module;
		readonly MethodDefinition method;
		readonly ILProcessor cil;
		readonly Dictionary<string, VariableDefinition> variables
			= new Dictionary<string, VariableDefinition>();
		Instruction afterifBW = Instruction.Create(OpCodes.Nop);
		Instruction afterifCW = Instruction.Create(OpCodes.Nop);
		MethodBodyCompiler(SourceFile sourceFile, AllTypes types, MethodDefinition method)
		{
			this.sourceFile = sourceFile;
			this.types = types;
			module = types.Module;
			this.method = method;
			cil = method.Body.GetILProcessor();
		}
		public static void Compile(
			SourceFile sourceFile,
			AllTypes types,
			MethodDefinition md,
			IEnumerable<IStatement> statements
			)
		{
			new MethodBodyCompiler(sourceFile, types, md).CompileMethodStatements(statements);
		}
		void CompileMethodStatements(IEnumerable<IStatement> statements)
		{
			foreach (var s in statements)
				CompileStatement(s);
			cil.Emit(OpCodes.Ret);
		}
		Exception MakeError(INode node, string message)
		{
			return new Exception(sourceFile.MakeErrorMessage(node.Position, message));
		}
		Exception WrongType(INode expression, TypeRef actual, TypeRef expected)
		{
			var message = $@"Выражение {expression.FormattedString} имеет тип {
				types.GetTypeName(actual)} вместо {types.GetTypeName(expected)}";
			return MakeError(expression, message);
		}
		#region statements
		void CompileStatement(IStatement statement)
		{
			statement.Accept(this);
		}
		void CompileBlock(Block block)
		{
			foreach (var statement in block.Statements)
			{
				CompileStatement(statement);
			}
		}
		public void VisitAssignment(Assignment statement)
		{
			var id = statement.Target as Identifier;
			var member = statement.Target as MemberAccess;

			if (id != null)
			{
				VariableDefinition variable;

				if (variables.ContainsKey(id.Name))
				{
					var variableType = variables[id.Name].VariableType;
					var expressionType = CompileExpression(statement.Expr);
					if (types.CanAssign(expressionType, variableType))
					{
						variable = variables[id.Name];
					}
					else
					{
						throw WrongType(statement, expressionType, variableType);
					}
				}
				else
				{
					variable = new VariableDefinition(CompileExpression(statement.Expr).TypeReference);
					method.Body.Variables.Add(variable);
					variables[id.Name] = variable;
				}
				cil.Emit(OpCodes.Stloc, variable);
				return;
			}
			if (member != null)
			{
				var obj = CompileExpression(member.Obj);
				CompileExpression(statement.Expr);
				var obj_typedef = obj.GetTypeDefinition();
				var obj_fields = obj_typedef.Fields;
				foreach (var field in obj_fields)
				{
					if (field.Name == member.Member)
					{
						cil.Emit(OpCodes.Stfld, field);
						return;
					}
				}
			}
			throw MakeError(statement, $"Unexpected statement {statement.FormattedString}");
		}
		public void VisitExpressionStatement(ExpressionStatement statement)
		{
			if (CompileExpression(statement.Expr) != types.Void)
				cil.Emit(OpCodes.Pop);
		}
		public void VisitIf(If statement)
		{
			var checkIf = Instruction.Create(OpCodes.Nop);
			var conditionType = CompileExpression(statement.Condition);
			if (conditionType != types.Bool)
			{
				throw WrongType(statement, conditionType, types.Bool);
			}
			cil.Emit(OpCodes.Brfalse, checkIf);
			CompileBlock(statement.Body);
			cil.Append(checkIf);
		}
		public void VisitReturn(Return statement)
		{
			this.CompileExpression(statement.Expr);
			cil.Emit(OpCodes.Ret);
		}
		public void VisitWhile(While statement)
		{
			var finish = Instruction.Create(OpCodes.Nop);
			var checkWhile = Instruction.Create(OpCodes.Nop);
			cil.Append(checkWhile);
			var conditionType = CompileExpression(statement.Condition);
			if (conditionType == types.Bool)
			{
				cil.Emit(OpCodes.Brfalse, finish);

				CompileBlock(statement.Body);
				cil.Emit(OpCodes.Br, checkWhile);

				cil.Append(finish);
			}
			else
			{
				throw WrongType(statement.Condition, conditionType, types.Bool);
			}
		}
		#endregion
		#region expressions
		TypeRef CompileExpression(IExpression expression)
		{
			return expression.Accept(this);
		}
		public TypeRef VisitBinary(Binary node)
		{
			var leftType = CompileExpression(node.Left);
			var rightType = CompileExpression(node.Right);
			var op = node.Operator;
			if ((op == BinaryOperator.Equal) &&
				((leftType == types.Null && rightType.CanBeNull) ||
				(rightType == types.Null && leftType.CanBeNull) ||
				((leftType == rightType) && ((leftType == types.Int) || (leftType == types.Bool)))
				))
			{
				cil.Emit(OpCodes.Ceq);
				return types.Bool;
			}
			if (leftType == rightType)
			{
				var leftTypeName = types.GetTypeName(leftType);
				switch (op)
				{
					case BinaryOperator.Addition:
						if (leftType != types.Int)
							throw MakeError(node, $"Can't add two {leftTypeName}");
						cil.Emit(OpCodes.Add);
						break;
					case BinaryOperator.Division:
						if (leftType != types.Int)
							throw MakeError(node, $"Can't divide two {leftTypeName}");
						cil.Emit(OpCodes.Div);
						break;
					case BinaryOperator.Less:
						if (leftType != types.Int && leftType != types.Bool)
							throw MakeError(node, $"Can't compare two {leftTypeName}");
						cil.Emit(OpCodes.Clt);
						return types.Bool;
					case BinaryOperator.Multiplication:
						if (leftType != types.Int)
							throw MakeError(node, $"Can't multiply two {leftTypeName}");
						cil.Emit(OpCodes.Mul);
						break;
					case BinaryOperator.Remainder:
						if (leftType != types.Int)
							throw MakeError(node, $"Can't take remain between two {leftTypeName}");
						cil.Emit(OpCodes.Rem);
						break;
					case BinaryOperator.Subtraction:
						if (leftType != types.Int)
							throw MakeError(node, $"Can't substract two {leftTypeName}");
						cil.Emit(OpCodes.Sub);
						break;
					default: throw MakeError(node, $"strange operator {node.Operator}");
				}
				return types.Int;
			}
			throw MakeError(node, $"types are different { node.FormattedString}");
		}
		public TypeRef VisitCall(Call expression)
		{
			var member = expression.Function as MemberAccess;
			if (member != null)
			{
				var obj = CompileExpression(member.Obj);
				List<TypeRef> argumentTypes = new List<TypeRef>();
				foreach (IExpression arg in expression.Arguments)
				{
					TypeRef type = CompileExpression(arg);
					argumentTypes.Add(type);
				}
				var methods = types.GetCallableMethods(obj, member.Member, argumentTypes);
				if (methods.Count == 1)
				{
					cil.Emit(OpCodes.Call, module.ImportReference(methods[0]));
					return methods[0].ReturnType;
				}
				else if (methods.Count > 1)
				{
					throw MakeError(expression, "Some methods have same signature");
				}
			}
			var ident = expression.Function as Identifier;
			if (ident != null)
			{
				List<TypeRef> argumentTypes = new List<TypeRef>();
				foreach (IExpression arg in expression.Arguments)
				{
					TypeRef type = CompileExpression(arg);
					argumentTypes.Add(type);
				}
				var methods = types.GetCallableFunctions(ident.Name, argumentTypes);
				if (methods.Count == 1)
				{
					cil.Emit(OpCodes.Call, module.ImportReference(methods[0]));
					return methods[0].ReturnType;
				}
				else if (methods.Count > 1)
				{
					throw MakeError(expression, "Some methods have same signature");
				}
				var metType = types.TryGetTypeRef(ident.Name);
				if (metType != null)
				{
					var constructors = types.GetCallableMethods(metType.Value, ".ctor", argumentTypes);
					if (constructors.Count == 1)
					{
						cil.Emit(OpCodes.Newobj, constructors[0]);
						return (TypeRef)metType;
					}
					else if (constructors.Count > 1)
					{
						throw MakeError(expression, "Some methods have same signature");
					}
				}
			}
			throw MakeError(expression, $"{expression.Function.FormattedString}");
		}
		public TypeRef VisitParentheses(Parentheses expression)
		{
			return CompileExpression(expression.Expr);
		}
		public TypeRef VisitNumber(Number expression)
		{
			cil.Emit(OpCodes.Ldc_I4, int.Parse(expression.Lexeme));
			return types.Int;
		}
		public TypeRef VisitIdentifier(Identifier expression)
		{
			if (expression.Name == "this")
			{
				if (!method.IsStatic)
				{
					cil.Emit(OpCodes.Ldarg, method.Body.ThisParameter);
					return method.DeclaringType;
				}
				else throw MakeError(expression, $"this in non static method");
			}
			switch (expression.Name)
			{
				case "false": cil.Emit(OpCodes.Ldc_I4, 0); return types.Bool;
				case "true": cil.Emit(OpCodes.Ldc_I4, 1); return types.Bool;
				case "null":
					cil.Emit(OpCodes.Ldnull); return types.Null;
			}
			VariableDefinition value;
			if (variables.TryGetValue(expression.Name, out value))
			{
				cil.Emit(OpCodes.Ldloc, value);
				return value.VariableType;
			}
			foreach (var par in method.Parameters)
			{
				if (par.Name == expression.Name)
				{
					cil.Emit(OpCodes.Ldarg, par);
					return par.ParameterType;
				}
			}
			throw MakeError(expression, $"Unknown identifier {expression.Name}");
		}
		public TypeRef VisitMemberAccess(MemberAccess expression)
		{//Надо было исправить
			var obj = CompileExpression(expression.Obj);
			var Objtypedef = obj.GetTypeDefinition();
			foreach (var field in Objtypedef.Fields)
			{
				if (field.Name == expression.Member)
				{
					cil.Emit(OpCodes.Ldfld, module.ImportReference(field));
					return field.FieldType;
				}
			}
			throw MakeError(expression, "Wrong member access ");
		}
		public TypeRef VisitTypedExpression(TypedExpression expression)
		{//::
			var expr = CompileExpression(expression.Expr);
			var checkTyped = types.TryGetTypeRef(expression.Type);
			if (checkTyped != null)
			{
				if (expr == checkTyped)
				{
					return expr;
				}
				else
				{
					throw MakeError(expression, "Wrong typed expression");
				}
			}
			throw MakeError(expression.Type, $"Such typed doesn't exist - {expression.Type.Name}");
		}
		#endregion
	}
}
