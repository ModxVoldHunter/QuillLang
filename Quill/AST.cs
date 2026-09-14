using System.Collections.Generic;

namespace Quill
{
    public abstract record Expr;
    public abstract record Stmt;

    // Expressions
    public record NumberExpr(double Value) : Expr;
    public record StringExpr(string Value) : Expr;
    public record BoolExpr(bool Value) : Expr;
    public record NullExpr : Expr;
    public record IdentifierExpr(string Name) : Expr;
    public record BinaryExpr(string Op, Expr Left, Expr Right) : Expr;
    public record UnaryExpr(string Op, Expr Operand) : Expr;
    public record CallExpr(Expr Callee, List<Expr> Args) : Expr;
    public record LogicalExpr(string Op, Expr Left, Expr Right) : Expr;
    public record AssignExpr(string Name, Expr Value) : Expr;

    // Statements
    public record ExprStmt(Expr Expression) : Stmt;
    public record LetStmt(string Name, Expr Value) : Stmt;
    public record PrintStmt(Expr Expression) : Stmt;
    public record IfStmt(Expr Condition, Stmt ThenBranch, Stmt ElseBranch) : Stmt;
    public record WhileStmt(Expr Condition, Stmt Body) : Stmt;
    public record ForStmt(Stmt Init, Expr Condition, Stmt Update, Stmt Body) : Stmt;
    public record FuncDecl(string Name, List<string> Params, Stmt Body) : Stmt;
    public record ReturnStmt(Expr Value) : Stmt;
    public record BlockStmt(List<Stmt> Statements) : Stmt;
    public record BreakStmt : Stmt;
    public record ContinueStmt : Stmt;
}