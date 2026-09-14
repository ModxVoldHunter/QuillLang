using System;
using System.Collections;
using System.Collections.Generic;

namespace Quill
{
    public class QuillException : Exception
    {
        public int Line { get; }
        public QuillException(string message, int line = 0) : base(message) { Line = line; }
    }

    public class ReturnException : Exception
    {
        public object Value { get; }
        public ReturnException(object value) { Value = value; }
    }

    public class BreakLoopException : Exception { }
    public class ContinueLoopException : Exception { }

    public class Environment
    {
        private readonly Dictionary<string, object> _values = new();
        private readonly Environment _parent;
        public Environment(Environment parent = null) { _parent = parent; }

        public void Define(string name, object value) => _values[name] = value;

        public object Get(string name)
        {
            if (_values.TryGetValue(name, out var v)) return v;
            if (_parent != null) return _parent.Get(name);
            throw new QuillException($"Undefined variable '{name}'");
        }

        public void Set(string name, object value)
        {
            if (_values.ContainsKey(name)) { _values[name] = value; return; }
            if (_parent != null) { _parent.Set(name, value); return; }
            throw new QuillException($"Cannot assign to undefined variable '{name}'");
        }
    }

    public class BuiltinFunction
    {
        public string Name { get; }
        private readonly Func<List<object>, object> _func;
        public BuiltinFunction(string name, Func<List<object>, object> func) { Name = name; _func = func; }
        public object Call(List<object> args) => _func(args);
    }

    public class QuillFunction
    {
        public string Name { get; }
        public List<string> Params { get; }
        public List<Stmt> Body { get; }
        public Environment Closure { get; }

        public QuillFunction(string name, List<string> parameters, List<Stmt> body, Environment closure)
        {
            Name = name; Params = parameters; Body = body; Closure = closure;
        }

        public object Call(Interpreter interpreter, List<object> args)
        {
            var env = new Environment(Closure);
            for (int i = 0; i < Params.Count; i++)
                env.Define(Params[i], i < args.Count ? args[i] : null);

            try { interpreter.ExecuteBlock(Body, env); }
            catch (ReturnException ret) { return ret.Value; }
            return null;
        }
    }

    public class Interpreter
    {
        public Environment Globals { get; } = new Environment();
        private Environment _environment;

        public Interpreter()
        {
            _environment = Globals;

            Globals.Define("clock", new BuiltinFunction("clock", _ => DateTime.UtcNow.Ticks / (double)TimeSpan.TicksPerSecond));
            Globals.Define("len",   new BuiltinFunction("len", a => a[0] switch
            {
                string s => (double)s.Length,
                IList l => (double)l.Count,
                _ => throw new QuillException("len() expects a string or list")
            }));
            Globals.Define("input", new BuiltinFunction("input", _ => Console.ReadLine()));
            Globals.Define("number", new BuiltinFunction("number", a =>
            {
                if (a.Count == 0) return 0.0;
                if (a[0] is string s && double.TryParse(s, out var d)) return d;
                if (a[0] is double dd) return dd;
                if (a[0] is bool b) return b ? 1.0 : 0.0;
                return 0.0;
            }));
            Globals.Define("string", new BuiltinFunction("string", a => a.Count == 0 ? "null" : QuillString(a[0])));
            Globals.Define("sqrt",   new BuiltinFunction("sqrt",   a => Math.Sqrt(AsNumber(a[0]))));
            Globals.Define("abs",    new BuiltinFunction("abs",    a => Math.Abs(AsNumber(a[0]))));
            Globals.Define("floor",  new BuiltinFunction("floor",  a => Math.Floor(AsNumber(a[0]))));
            Globals.Define("ceil",   new BuiltinFunction("ceil",   a => Math.Ceiling(AsNumber(a[0]))));
            Globals.Define("random", new BuiltinFunction("random", a =>
            {
                if (a.Count == 0) return new Random().NextDouble();
                double max = AsNumber(a[0]);
                return Math.Floor(new Random().NextDouble() * max);
            }));
        }

        public void Interpret(List<Stmt> statements)
        {
            try { foreach (var s in statements) Execute(s); }
            catch (ReturnException) { /* top-level return ignored */ }
            catch (BreakLoopException) { }
            catch (ContinueLoopException) { }
            catch (QuillException e)
            {
                if (e.Line > 0) Console.Error.WriteLine($"Runtime error (line {e.Line}): {e.Message}");
                else Console.Error.WriteLine($"Runtime error: {e.Message}");
                System.Environment.Exit(1);
            }
        }

        public void ExecuteBlock(List<Stmt> statements, Environment env)
        {
            Environment previous = _environment;
            try { _environment = env; foreach (var s in statements) Execute(s); }
            finally { _environment = previous; }
        }

        private void Execute(Stmt stmt)
        {
            switch (stmt)
            {
                case ExprStmt e: Evaluate(e.Expression); break;
                case LetStmt l:
                    _environment.Define(l.Name, l.Value != null ? Evaluate(l.Value) : null);
                    break;
                case PrintStmt p:
                    Console.WriteLine(QuillString(Evaluate(p.Expression)));
                    break;
                case IfStmt i:
                    if (IsTruthy(Evaluate(i.Condition))) Execute(i.ThenBranch);
                    else if (i.ElseBranch != null) Execute(i.ElseBranch);
                    break;
                case WhileStmt w: ExecuteWhile(w); break;
                case ForStmt f: ExecuteFor(f); break;
                case FuncDecl fn:
                    var block = fn.Body as BlockStmt;
                    var func = new QuillFunction(fn.Name, fn.Params, block?.Statements ?? new List<Stmt> { fn.Body }, _environment);
                    _environment.Define(fn.Name, func);
                    break;
                case ReturnStmt r:
                    throw new ReturnException(r.Value != null ? Evaluate(r.Value) : null);
                case BlockStmt b:
                    ExecuteBlock(b.Statements, new Environment(_environment));
                    break;
                case BreakStmt: throw new BreakLoopException();
                case ContinueStmt: throw new ContinueLoopException();
            }
        }

        private void ExecuteWhile(WhileStmt s)
        {
            while (IsTruthy(Evaluate(s.Condition)))
            {
                try { Execute(s.Body); }
                catch (BreakLoopException) { break; }
                catch (ContinueLoopException) { continue; }
            }
        }

        private void ExecuteFor(ForStmt s)
        {
            Environment previous = _environment;
            _environment = new Environment(previous);
            try
            {
                if (s.Init != null) Execute(s.Init);
                while (s.Condition == null || IsTruthy(Evaluate(s.Condition)))
                {
                    bool stop = false;
                    try { Execute(s.Body); }
                    catch (BreakLoopException) { stop = true; }
                    catch (ContinueLoopException) { }
                    if (stop) break;
                    if (s.Update != null) Execute(s.Update);
                }
            }
            finally { _environment = previous; }
        }

        private object Evaluate(Expr expr)
        {
            switch (expr)
            {
                case NumberExpr n: return n.Value;
                case StringExpr s: return s.Value;
                case BoolExpr b: return b.Value;
                case NullExpr: return null;
                case IdentifierExpr i: return _environment.Get(i.Name);
                case AssignExpr a:
                    var v = Evaluate(a.Value);
                    _environment.Set(a.Name, v);
                    return v;
                case BinaryExpr bin: return BinaryOp(bin);
                case UnaryExpr u:
                    var operand = Evaluate(u.Operand);
                    if (u.Op == "-") return -AsNumber(operand);
                    if (u.Op == "not") return !IsTruthy(operand);
                    throw new QuillException($"Unknown unary operator: {u.Op}");
                case LogicalExpr l:
                    var left = Evaluate(l.Left);
                    if (l.Op == "or") return IsTruthy(left) ? left : Evaluate(l.Right);
                    return !IsTruthy(left) ? left : Evaluate(l.Right);
                case CallExpr c:
                    var callee = Evaluate(c.Callee);
                    var args = new List<object>();
                    foreach (var a in c.Args) args.Add(Evaluate(a));
                    if (callee is QuillFunction qf) return qf.Call(this, args);
                    if (callee is BuiltinFunction bf) return bf.Call(args);
                    throw new QuillException($"Cannot call non-function: {QuillString(callee)}");
            }
            return null;
        }

        private object BinaryOp(BinaryExpr e)
        {
            var left = Evaluate(e.Left);
            var right = Evaluate(e.Right);

            if (e.Op == "+" && (left is string || right is string))
                return QuillString(left) + QuillString(right);

            return e.Op switch
            {
                "+"  => AsNumber(left) + AsNumber(right),
                "-"  => AsNumber(left) - AsNumber(right),
                "*"  => AsNumber(left) * AsNumber(right),
                "/"  => AsNumber(left) / AsNumber(right),
                "%"  => AsNumber(left) % AsNumber(right),
                "==" => Equals(left, right),
                "!=" => !Equals(left, right),
                "<"  => AsNumber(left) <  AsNumber(right),
                ">"  => AsNumber(left) >  AsNumber(right),
                "<=" => AsNumber(left) <= AsNumber(right),
                ">=" => AsNumber(left) >= AsNumber(right),
                _ => throw new QuillException($"Unknown operator: {e.Op}")
            };
        }

        private static bool IsTruthy(object v)
        {
            if (v == null) return false;
            if (v is bool b) return b;
            if (v is double d) return d != 0;
            if (v is string s) return s.Length > 0;
            return true;
        }

        private static double AsNumber(object v)
        {
            if (v is double d) return d;
            if (v is int i) return i;
            if (v is bool b) return b ? 1 : 0;
            if (v == null) return 0;
            if (v is string s && double.TryParse(s, out var parsed)) return parsed;
            throw new QuillException($"Cannot convert '{QuillString(v)}' to a number");
        }

        public static string QuillString(object v)
        {
            if (v == null) return "null";
            if (v is bool b) return b ? "true" : "false";
            if (v is double d)
            {
                if (d == Math.Floor(d) && !double.IsInfinity(d) && Math.Abs(d) < 1e15) return ((long)d).ToString();
                return d.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
            if (v is string s) return s;
            if (v is QuillFunction qf) return $"<func {qf.Name}>";
            if (v is BuiltinFunction bf) return $"<builtin {bf.Name}>";
            return v.ToString();
        }
    }
}
