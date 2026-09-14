using System;
using System.Collections.Generic;
using System.Text;

namespace Quill
{
    public class Lexer
    {
        private readonly string _source;
        private int _position;
        private int _line = 1;

        private static readonly Dictionary<string, TokenType> Keywords = new()
        {
            {"let", TokenType.Let},     {"if", TokenType.If},
            {"else", TokenType.Else},   {"while", TokenType.While},
            {"for", TokenType.For},     {"func", TokenType.Func},
            {"return", TokenType.Return},{"print", TokenType.Print},
            {"and", TokenType.And},     {"or", TokenType.Or},
            {"not", TokenType.Not},     {"true", TokenType.True},
            {"false", TokenType.False}, {"null", TokenType.Null},
            {"break", TokenType.Break}, {"continue", TokenType.Continue},
        };

        public Lexer(string source) => _source = source;

        public List<Token> Tokenize()
        {
            var tokens = new List<Token>();

            while (!IsAtEnd())
            {
                char c = Peek();

                if (char.IsWhiteSpace(c))
                {
                    if (c == '\n') _line++;
                    Advance();
                    continue;
                }

                // Comments
                if (c == '/' && PeekNext() == '/')
                {
                    while (!IsAtEnd() && Peek() != '\n') Advance();
                    continue;
                }

                switch (c)
                {
                    case '+': tokens.Add(MakeToken(TokenType.Plus)); Advance(); break;
                    case '-': tokens.Add(MakeToken(TokenType.Minus)); Advance(); break;
                    case '*': tokens.Add(MakeToken(TokenType.Star)); Advance(); break;
                    case '%': tokens.Add(MakeToken(TokenType.Percent)); Advance(); break;
                    case '(':
                    case ')':
                    case '{':
                    case '}':
                    case ',':
                    case ';':
                        tokens.Add(Punct(c)); Advance(); break;
                    case '/': tokens.Add(MakeToken(TokenType.Slash)); Advance(); break;
                    case '=':
                        if (PeekNext() == '=') { Advance(); Advance(); tokens.Add(new Token(TokenType.Equals, "==", _line)); }
                        else { tokens.Add(MakeToken(TokenType.Assign)); Advance(); }
                        break;
                    case '!':
                        if (PeekNext() == '=') { Advance(); Advance(); tokens.Add(new Token(TokenType.NotEquals, "!=", _line)); }
                        else throw new QuillException("Expected '!=' but found '!'", _line);
                        break;
                    case '<':
                        if (PeekNext() == '=') { Advance(); Advance(); tokens.Add(new Token(TokenType.LessEq, "<=", _line)); }
                        else { tokens.Add(MakeToken(TokenType.Less)); Advance(); }
                        break;
                    case '>':
                        if (PeekNext() == '=') { Advance(); Advance(); tokens.Add(new Token(TokenType.GreaterEq, ">=", _line)); }
                        else { tokens.Add(MakeToken(TokenType.Greater)); Advance(); }
                        break;
                    case '"':
                        tokens.Add(StringToken());
                        break;
                    default:
                        if (char.IsDigit(c)) tokens.Add(NumberToken());
                        else if (char.IsLetter(c) || c == '_') tokens.Add(IdentifierToken());
                        else throw new QuillException($"Unexpected character: '{c}'", _line);
                        break;
                }
            }

            tokens.Add(new Token(TokenType.Eof, "", _line));
            return tokens;
        }

        private Token MakeToken(TokenType t) => new Token(t, _source[_position].ToString(), _line);
        private Token Punct(char c) => new Token(
            c switch { '(' => TokenType.LeftParen, ')' => TokenType.RightParen,
                       '{' => TokenType.LeftBrace, '}' => TokenType.RightBrace,
                       ',' => TokenType.Comma, ';' => TokenType.Semicolon,
                       _ => throw new InvalidOperationException() }, c.ToString(), _line);

        private Token NumberToken()
        {
            int start = _position;
            bool seenDot = false;
            while (!IsAtEnd() && (char.IsDigit(Peek()) || (Peek() == '.' && !seenDot)))
            {
                if (Peek() == '.') seenDot = true;
                Advance();
            }
            string num = _source.Substring(start, _position - start);
            return new Token(TokenType.Number, num, _line);
        }

        private Token StringToken()
        {
            Advance(); // opening quote
            var sb = new StringBuilder();
            while (!IsAtEnd() && Peek() != '"')
            {
                char c = Peek();
                if (c == '\\')
                {
                    Advance();
                    char esc = Peek();
                    sb.Append(esc switch
                    {
                        'n' => '\n', 't' => '\t', 'r' => '\r',
                        '\\' => '\\', '"' => '"', _ => esc,
                    });
                    Advance();
                }
                else
                {
                    if (c == '\n') _line++;
                    sb.Append(c);
                    Advance();
                }
            }
            if (IsAtEnd()) throw new QuillException("Unterminated string", _line);
            Advance(); // closing quote
            return new Token(TokenType.String, sb.ToString(), _line);
        }

        private Token IdentifierToken()
        {
            int start = _position;
            while (!IsAtEnd() && (char.IsLetterOrDigit(Peek()) || Peek() == '_')) Advance();
            string ident = _source.Substring(start, _position - start);
            return Keywords.TryGetValue(ident, out var kw)
                ? new Token(kw, ident, _line)
                : new Token(TokenType.Identifier, ident, _line);
        }

        private char Peek() => IsAtEnd() ? '\0' : _source[_position];
        private char PeekNext() => _position + 1 >= _source.Length ? '\0' : _source[_position + 1];
        private void Advance() => _position++;
        private bool IsAtEnd() => _position >= _source.Length;
    }
}