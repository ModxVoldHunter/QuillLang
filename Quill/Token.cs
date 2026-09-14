using System.Collections.Generic;

namespace Quill
{
    public enum TokenType
    {
        // Literals
        Number, String, Identifier, True, False, Null,

        // Keywords
        Let, If, Else, While, For, Func, Return, Print,
        And, Or, Not, Break, Continue,

        // Operators
        Plus, Minus, Star, Slash, Percent,
        Assign, Equals, NotEquals, Less, Greater, LessEq, GreaterEq,

        // Punctuation
        LeftParen, RightParen, LeftBrace, RightBrace, Comma, Semicolon,

        // End
        Eof
    }

    public readonly struct Token
    {
        public TokenType Type { get; }
        public string Value { get; }
        public int Line { get; }

        public Token(TokenType type, string value, int line)
        {
            Type = type;
            Value = value;
            Line = line;
        }

        public override string ToString() => $"[{Type}: '{Value}' (line {Line})]";
    }
}