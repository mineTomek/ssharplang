using System;
using System.Collections.Generic;

namespace S_Sharp
{
    public static class ssharp
    {
        /// Errors

        public class Error
        {
            public Position posStart;
            public Position posEnd;
            public string errorName;
            public string details;

            public Error(Position _posStart, Position _posEnd, string _errorName, string _details)
            {
                posStart = _posStart;
                posEnd = _posEnd;
                errorName = _errorName;
                details = _details;
            }

            public override string ToString()
            {
                string result = "{error_name}: {details}\n";
                result += "File {posStart.fn}, line {posStart.ln + 1}";
                result += "\n\n" + new Arrows(posStart.ftxt, posStart, posEnd);
                return result;
            }
        }


        /// Position

        public class Position
        {
            public int idx;
            public int ln;
            public int col;
            public string fn;
            public string ftxt;

            public Position(int _idx, int _ln, int _col, string _fn, string _ftxt)
            {
                // Index (?)
                idx = _idx;
                // Line num
                ln = _ln;
                // Column
                col = _col;
                // Filename
                fn = _fn;
                // Code to run (file text)
                ftxt = _ftxt;
            }

            public void Advance(char currentChar = '\0')
            {
                idx++;
                col++;

                if (currentChar == '\n')
                {
                    ln++;
                    col = 0;
                }
            }

            public Position Copy()
            {
                return new Position(idx, ln, col, fn, ftxt);
            }
        }


        /// Tokens

        static string TT_INT = "INT";
        static string TT_FLOAT = "FLOAT";
        static string TT_STRING = "STRING";
        static string TT_IDENTIFIER = "IDENTIFIER";
        static string TT_KEYWORD = "KEYWORD";
        static string TT_PLUS = "PLUS";
        static string TT_MINUS = "MINUS";
        static string TT_MUL = "MUL";
        static string TT_DIV = "DIV";
        static string TT_POW = "POW";
        static string TT_LPAREN = "LPAREN";
        static string TT_RPAREN = "RPAREN";
        static string TT_LSQUARE = "LSQUARE";
        static string TT_RSQUARE = "RSQUARE";
        static string TT_EE = "EE";
        static string TT_NE = "NE";
        static string TT_LT = "LT";
        static string TT_GT = "GT";
        static string TT_LTE = "LTE";
        static string TT_GTE = "GTE";
        static string TT_COMMA = "COMMA";
        static string TT_ARROW = "ARROW";
        static string TT_NEWLINE = "NEWLINE";
        static string TT_EOF = "EOF";

        static string[] KEYWORDS = new string[17] {
            "VAR",
            "AND",
            "OR",
            "NOT",
            "IF",
            "ELIF",
            "ELSE",
            "FOR",
            "TO",
            "STEP",
            "WHILE",
            "FUN",
            "THEN",
            "END",
            "RETURN",
            "CONTINUE",
            "BREAK",
        };


        public class Token
        {
            public string type;
            public object value;
            public Position posStart;
            public Position posEnd;

            public Token(string _type, object _value = null, Position _posStart = null, Position _posEnd = null)
            {
                type = _type;
                value = _value;

                if (_posStart != null)
                {
                    posStart = _posStart.Copy();
                    posEnd = _posStart.Copy();
                    posEnd.Advance();
                }

                if (posEnd != null)
                {
                    posEnd = _posEnd.Copy();
                }
            }

            public bool Matches(Token token)
            {
                return type == token.type && value == token.value;
            }

            public override string ToString()
            {
                if (value != null) {
                    return $"{type}:{value}";
                }
                return type;
            }
        }


        /// Lexer

        class Lexer
        {
            public string fn; // Filename
            public string text;
            public Position pos;
            public char currentChar;

            public Lexer(string _fn, string _text)
            {
                fn = _fn;
                text = _text;
                pos = new Position(-1, 0, -1, fn, text);
                currentChar = '\0';
                Advance();
            }

            public void Advance()
            {
                pos.Advance(currentChar);
                currentChar = pos.idx < text.Length ? text[pos.idx] : '\0';
            }

            public TokenMakingResult make_tokens()
            {
                List<Token> tokens = new List<Token>();

                while (currentChar != null)
                {
                    if (" \t".Contains(currentChar))
                    {
                        Advance();
                    }
                    else if (currentChar == '#')
                    {
                        skip_comment();
                    }
                    else if (';\n'.Contains(currentChar))
                    {
                        tokens.Add(new Token(TT_NEWLINE, _posStart: pos));
                        Advance();
                    }
                    else if (char.IsDigit(currentChar))
                    {
                        tokens.Add(make_number());
                    }
                    else if (char.IsLetter(currentChar))
                    {
                        tokens.Add(make_identifier());
                    }
                    else if (currentChar == '\"')
                    {
                        tokens.Add(make_string());
                    }
                    else if (currentChar == '+')
                    {
                        tokens.Add(new Token(TT_PLUS, _posStart: pos));
                        Advance();
                    }
                    else if (currentChar == '-')
                    {
                        tokens.Add(make_minus_or_arrow());
                    }
                    else if (currentChar == '*')
                    {
                        tokens.Add(new Token(TT_MUL, _posStart: pos));
                        Advance();
                    }
                    else if (currentChar == '/')
                    {
                        tokens.Add(new Token(TT_DIV, _posStart: pos));
                        Advance();
                    }
                    else if (currentChar == '^')
                    {
                        tokens.Add(new Token(TT_POW, _posStart: pos));
                        Advance();
                    }
                    else if (currentChar == '(')
                    {
                        tokens.Add(new Token(TT_LPAREN, _posStart: pos));
                        Advance();
                    }
                    else if (currentChar == ')')
                    {
                        tokens.Add(new Token(TT_RPAREN, _posStart: pos));
                        Advance();
                    }
                    else if (currentChar == '[')
                    {
                        tokens.Add(new Token(TT_LSQUARE, _posStart: pos));
                        Advance();
                    }
                    else if (currentChar == ']')
                    {
                        tokens.Add(new Token(TT_RSQUARE, _posStart: pos));
                        Advance();
                    }
                    else if (currentChar == '!')
                    {
                        var _tup_1 = make_not_equals();
                        var token = _tup_1.Item1;
                        var error = _tup_1.Item2;
                        if (error)
                        {
                            return new TokenMakingResult(new List<Token>(), error);
                        }
                        tokens.Add(token);
                    }
                    else if (currentChar == '=')
                    {
                        tokens.Add(make_equals());
                    }
                    else if (currentChar == '<')
                    {
                        tokens.Add(make_less_than());
                    }
                    else if (currentChar == '>')
                    {
                        tokens.Add(make_greater_than());
                    }
                    else if (currentChar == ',')
                    {
                        tokens.Add(new Token(TT_COMMA, _posStart: pos));
                        Advance();
                    }
                    else
                    {
                        Position posStart = pos.Copy();
                        char chr = currentChar;
                        Advance();
                        return new TokenMakingResult(new List<Token>(), new IllegalCharError(posStart, pos, "'" + chr + "'"));
                    }
                }
                tokens.Add(new Token(TT_EOF, _posStart: pos));
                return new TokenMakingResult(tokens, null);
            }

            public class TokenMakingResult
            {
                public List<Token> tokens;
                public Error error;

                public TokenMakingResult(List<Token> _tokens, Error _error)
                {
                    tokens = _tokens;
                    error = _error;
                }
            }

            public Token make_number()
            {
                var num_str = "";
                var dot_count = 0;
                var pos_start = pos.Copy();
                while (currentChar != null && (char.IsDigit(currentChar) || currentChar == '.'))
                {
                    if (currentChar == '.')
                    {
                        if (dot_count == 1)
                        {
                            break;
                        }
                        dot_count += 1;
                    }
                    num_str += currentChar;
                    Advance();
                }
                if (dot_count == 0)
                {
                    return new Token(TT_INT, int.Parse(num_str), pos_start, pos);
                } else
                {
                    return new Token(TT_FLOAT, float.Parse(num_str), pos_start, pos);
                }
            }
        }


        /// Nodes
        

        /// Experimental
        
        public static Result Run(string filename, string code)
        {
            return new Result(null, $"{filename}: {code}");
        }
    }
}
