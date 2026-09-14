using System;
using System.Collections.Generic;
using System.Globalization;

namespace Quill
{
    public class Parser
    {
        private readonly List<Token> _tokens;
        private int _current;

        public Parser(List<Token> tokens) { _tokens = tokens; _current = 0; }

        public List<Stmt> Parse()
        {
            var stmts = new List<Stmt>();
            while (!IsAtEnd()) stmts.Add(Declaration());
            return stmts;
        }

        private Stmt Declaration()
        {
            if (Match(TokenType.Func)) return FunctionDeclaration();
            if (Match(TokenType.Let)) return LetDeclaration();
            return Statement();
        }

        private Stmt FunctionDeclaration()
        {
            Token name = Consume(TokenType.Identifier, "Expected function name");
            Consume(TokenType.LeftParen, "Expected '(' after function name");
            var parameters = new List<string>();
            if (!Check(TokenType.RightParen))
            {
                do { parameters.Add(Consume(TokenType.Identifier, "Expected parameter name").Value); }
                while (Match(TokenType.Comma));
            }
            Consume(TokenType.RightParen, "Expected ')' after parameters");
            Stmt body = Block();
            return new FuncDecl(name.Value, parameters, body);
        }

        private Stmt LetDeclaration()
        {
            Token name = Consume(TokenType.Identifier, "Expected variable name after 'let'");
            Expr value = null;
            if (Match(TokenType.Assign)) value = Expression();
            Consume(TokenType.Semicolon, "Expected ';' after let declaration");
            return new LetStmt(name.Value, value);
        }

        private Stmt Statement()
        {
            if (Match(TokenType.Print)) return PrintStatement();
            if (Match(TokenType.If)) return IfStatement();
            if (Match(TokenType.While)) return WhileStatement();
            if (Match(TokenType.For)) return ForStatement();
            if (Match(TokenType.Return)) return ReturnStatement();
            if (Check(TokenType.LeftBrace)) return Block();
            if (Match(TokenType.Break)) { Consume(TokenType.Semicolon, "Expected ';' after break"); return new BreakStmt(); }
            if (Match(TokenType.Continue)) { Consume(TokenType.Semicolon, "Expected ';' after continue"); return new ContinueStmt(); }
            return ExpressionStatement();
        }

        private Stmt PrintStatement()
        {
            Expr value = Expression();
            Consume(TokenType.Semicolon, "Expected ';' after print");
            return new PrintStmt(value);
        }

        private Stmt IfStatement()
        {
            Consume(TokenType.LeftParen, "Expected '(' after if");
            Expr condition = Expression();
            Consume(TokenType.RightParen, "Expected ')' after condition");
            Stmt thenBranch = Block();
            Stmt elseBranch = null;
            if (Match(TokenType.Else))
            {
                if (Check(TokenType.If)) elseBranch = IfStatement();
                else elseBranch = Block();
            }
            return new IfStmt(condition, thenBranch, elseBranch);
        }

        private Stmt WhileStatement()
        {
            Consume(TokenType.LeftParen, "Expected '(' after while");
            Expr condition = Expression();
            Consume(TokenType.RightParen, "Expected ')' after condition");
            Stmt body = Block();
            return new WhileStmt(condition, body);
        }

        private Stmt ForStatement()
        {
            Consume(TokenType.LeftParen, "Expected '(' after for");
            Stmt init = null;
            if (!Check(TokenType.Semicolon))
            {
                if (Match(TokenType.Let))
                {
                    Token name = Consume(TokenType.Identifier, "Expected variable name");
                    Consume(TokenType.Assign, "Expected '=' after variable name");
                    init = new LetStmt(name.Value, Expression());
                }
                else init = new ExprStmt(Expression());
            }
            Consume(TokenType.Semicolon, "Expected ';' after for-init");
            Expr condition = null;
            if (!Check(TokenType.Semicolon)) condition = Expression();
            Consume(TokenType.Semicolon, "Expected ';' after for-condition");
            Stmt update = null;
            if (!Check(TokenType.RightParen)) update = new ExprStmt(Expression());
            Consume(TokenType.RightParen, "Expected ')' after for-clauses");
            Stmt body = Block();
            return new ForStmt(init, condition, update, body);
        }

        private Stmt ReturnStatement()
        {
            Expr value = null;
            if (!Check(TokenType.Semicolon)) value = Expression();
            Consume(TokenType.Semicolon, "Expected ';' after return");
            return new ReturnStmt(value);
        }

        private Stmt Block()
        {
            Consume(TokenType.LeftBrace, "Expected '{'");
            var statements = new List<Stmt>();
            while (!Check(TokenType.RightBrace) && !IsAtEnd()) statements.Add(Declaration());
            Consume(TokenType.RightBrace, "Expected '}'");
            return new BlockStmt(statements);
        }

        private Stmt ExpressionStatement()
        {
            Expr expr = Expression();
            Consume(TokenType.Semicolon, "Expected ';' after expression");
            return new ExprStmt(expr);
        }

        private Expr Expression() => Assignment();

        private Expr Assignment()
        {
            Expr expr = Or();
            if (Match(TokenType.Assign))
            {
                Token eq = Previous();
                Expr value = Assignment();
                if (expr is IdentifierExpr id) return new AssignExpr(id.Name, value);
                throw new QuillException("Invalid assignment target", eq.Line);
            }
            return expr;
        }

        private Expr Or()
        {
            Expr expr = And();
            while (Match(TokenType.Or))
            {
                Expr right = And();
                expr = new LogicalExpr("or", expr, right);
            }
            return expr;
        }

        private Expr And()
        {
            Expr expr = Equality();
            while (Match(TokenType.And))
            {
                Expr right = Equality();
                expr = new LogicalExpr("and", expr, right);
            }
            return expr;
        }

        private Expr Equality()
        {
            Expr expr = Comparison();
            while (Match(TokenType.Equals, TokenType.NotEquals))
            {
                Token op = Previous();
                Expr right = Comparison();
                expr = new BinaryExpr(op.Value, expr, right);
            }
            return expr;
        }

        private Expr Comparison()
        {
            Expr expr = Term();
            while (Match(TokenType.Less, TokenType.Greater, TokenType.LessEq, TokenType.GreaterEq))
            {
                Token op = Previous();
                Expr right = Term();
                expr = new BinaryExpr(op.Value, expr, right);
            }
            return expr;
        }

        private Expr Term()
        {
            Expr expr = Factor();
            while (Match(TokenType.Plus, TokenType.Minus))
            {
                Token op = Previous();
                Expr right = Factor();
                expr = new BinaryExpr(op.Value, expr, right);
            }
            return expr;
        }

        private Expr Factor()
        {
            Expr expr = Unary();
            while (Match(TokenType.Star, TokenType.Slash, TokenType.Percent))
            {
                Token op = Previous();
                Expr right = Unary();
                expr = new BinaryExpr(op.Value, expr, right);
            }
            return expr;
        }

        private Expr Unary()
        {
            if (Match(TokenType.Not, TokenType.Minus))
            {
                Token op = Previous();
                Expr right = Unary();
                return new UnaryExpr(op.Value, right);
            }
            return Call();
        }

        private Expr Call()
        {
            Expr expr = Primary();
            while (Match(TokenType.LeftParen))
            {
                var args = new List<Expr>();
                if (!Check(TokenType.RightParen))
                {
                    do { args.Add(Expression()); } while (Match(TokenType.Comma));
                }
                Consume(TokenType.RightParen, "Expected ')' after arguments");
                expr = new CallExpr(expr, args);
            }
            return expr;
        }

        private Expr Primary()
        {
            if (Match(TokenType.Number)) return new NumberExpr(double.Parse(Previous().Value, CultureInfo.InvariantCulture));
            if (Match(TokenType.String)) return new StringExpr(Previous().Value);
            if (Match(TokenType.True)) return new BoolExpr(true);
            if (Match(TokenType.False)) return new BoolExpr(false);
            if (Match(TokenType.Null)) return new NullExpr();
            if (Match(TokenType.Identifier)) return new IdentifierExpr(Previous().Value);
            if (Match(TokenType.LeftParen))
            {
                Expr expr = Expression();
                Consume(TokenType.RightParen, "Expected ')' after expression");
                return expr;
            }
            throw new QuillException($"Unexpected token: '{Peek().Value}'", Peek().Line);
        }

        private bool Match(params TokenType[] types)
        {
            foreach (var t in types) if (Check(t)) { Advance(); return true; }
            return false;
        }
        private bool Check(TokenType t) => !IsAtEnd() && Peek().Type == t;
        private Token Advance() { if (!IsAtEnd()) _current++; return Previous(); }
        private Token Peek() => _tokens[_current];
        private Token Previous() => _tokens[_current - 1];
        private bool IsAtEnd() => Peek().Type == TokenType.Eof;

        private Token Consume(TokenType type, string message)
        {
            if (Check(type)) return Advance();
            throw new QuillException($"{message}, but got '{Peek().Value}'", Peek().Line);
        }
    }
}