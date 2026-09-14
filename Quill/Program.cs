using System;
using System.IO;
using System.Text;

namespace Quill
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            if (args.Length > 0) return RunFile(args[0]);
            return RunRepl();
        }

        private static int RunFile(string path)
        {
            if (!File.Exists(path))
            {
                Console.Error.WriteLine($"File not found: {path}");
                return 1;
            }

            try
            {
                string source = File.ReadAllText(path);
                var tokens = new Lexer(source).Tokenize();
                var stmts = new Parser(tokens).Parse();
                var interp = new Interpreter();
                interp.Interpret(stmts);
            }
            catch (QuillException e)
            {
                if (e.Line > 0) Console.Error.WriteLine($"Error (line {e.Line}): {e.Message}");
                else Console.Error.WriteLine($"Error: {e.Message}");
                return 1;
            }
            return 0;
        }

        private static int RunRepl()
        {
            Console.WriteLine("Quill Programming Language v1.0");
            Console.WriteLine("Type 'exit' to quit.\n");

            var interp = new Interpreter();
            var buffer = new StringBuilder();
            int braces = 0;

            while (true)
            {
                Console.Write(buffer.Length == 0 ? "quill> " : "...   ");
                string line = Console.ReadLine();
                if (line == null) break;
                if (line == "exit" || line == "quit")
                {
                    if (buffer.Length == 0) break;
                    buffer.Clear(); braces = 0; continue;
                }

                buffer.AppendLine(line);
                braces += Count(line, '{') - Count(line, '}');

                if (braces > 0) continue;

                string src = buffer.ToString();
                buffer.Clear();
                braces = 0;

                try
                {
                    var tokens = new Lexer(src).Tokenize();
                    var stmts = new Parser(tokens).Parse();
                    interp.Interpret(stmts);
                }
                catch (QuillException e)
                {
                    Console.Error.WriteLine($"Error: {e.Message}");
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine($"Unexpected error: {e.Message}");
                }
            }
            return 0;
        }

        private static int Count(string s, char c)
        {
            int n = 0; foreach (var ch in s) if (ch == c) n++; return n;
        }
    }
}