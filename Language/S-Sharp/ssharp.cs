using System;
using System.Collections.Generic;

namespace S_Sharp
{
    public static class ssharp
    {
        ///  Errors
        
        #region Errors

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

        public class IllegalCharError : Error
        {

            public IllegalCharError(Position _posStart, Position _posEnd, string _details) : base(_posStart, _posEnd, "Illegal Character", _details)
            {

            }
        }

        public class ExpectedCharError : Error
        {
            public ExpectedCharError(Position _posStart, Position _posEnd, string _details) : base(_posStart, _posEnd, "Expected Character", _details)
            {

            }
        }

        public class InvalidSyntaxError : Error
        {
            public InvalidSyntaxError(Position _posStart, Position _posEnd, string _details = "") : base(_posStart, _posEnd, "Invalid Syntax", _details)
            {

            }
        }

        public class RTError : Error
        {
            string context;

            public RTError(Position posStart, Position posEnd, string _details, string _context) : base(posStart, posEnd, "Runtime Error", _details)
            {
                context = _context;
            }

            public string as_string()
            {
                string result = generate_traceback();
                result += "{error_name}: {details}";
                result += "\n\n" + new Arrows(posStart.ftxt, posStart, posEnd);
                return result;
            }

            public string generate_traceback()
            {
                string result = "";
                Position pos = posStart;
                var ctx = context;
                while (ctx != null)
                {
                    result = $"File {pos.fn}, line {pos.ln + 1}, in {/* ctx.display_name */""}\n" + result;
                    // pos = ctx.parent_entry_pos;
                    // ctx = ctx.parent;
                }
                return "Traceback (most recent call last):\n" + result;
            }
        }

        #endregion

        // Position

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

        #region Tokens

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
        static string TT_EQ = "EQ";
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

            public bool Matches(string _type, object _value)
            {
                return type == _type && value == _value;
            }

            public override string ToString()
            {
                if (value != null) {
                    return $"{type}:{value}";
                }
                return type;
            }
        }

        #endregion


        /// Lexer

        public class Lexer
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

            public TokensWithError makeTokens()
            {
                List<Token> tokens = new List<Token>();

                while (currentChar != '\0')
                {
                    if (" \t".Contains(currentChar))
                    {
                        Advance();
                    }
                    else if (currentChar == '#')
                    {
                        skip_comment();
                    }
                    else if (";\n".Contains(currentChar))
                    {
                        tokens.Add(new Token(TT_NEWLINE, _posStart: pos));
                        Advance();
                    }
                    else if (char.IsDigit(currentChar))
                    {
                        tokens.Add(makeNumber());
                    }
                    else if (char.IsLetter(currentChar))
                    {
                        tokens.Add(makeIdentifier());
                    }
                    else if (currentChar == '\"')
                    {
                        tokens.Add(makeString());
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
                        Token token = _tup_1.Item1;
                        Error error = _tup_1.Item2;
                        if (error != null)
                        {
                            return new TokensWithError(new List<Token>(), error);
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
                        return new TokensWithError(new List<Token>(), new IllegalCharError(posStart, pos, "'" + chr + "'"));
                    }
                }
                tokens.Add(new Token(TT_EOF, _posStart: pos));
                return new TokensWithError(tokens, null);
            }

            public class TokensWithError
            {
                public List<Token> tokens;
                public Error error;

                public TokensWithError(List<Token> _tokens, Error _error)
                {
                    tokens = _tokens;
                    error = _error;
                }
            }

            public Token makeNumber()
            {
                var num_str = "";
                var dot_count = 0;
                var _posStart = pos.Copy();
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
                    return new Token(TT_INT, int.Parse(num_str), _posStart, pos);
                } else
                {
                    return new Token(TT_FLOAT, float.Parse(num_str), _posStart, pos);
                }
            }

            public Token makeString()
            {
                string str = "";
                Position posStart = pos.Copy();
                bool escapeChar = false;

                Advance();

                Dictionary<char, char> escapeChars = new Dictionary<char, char> {
                    {
                        'n',
                        '\n'
                    },
                    {
                        't',
                        '\t'
                    }};
                while (currentChar != '\0' && (currentChar != '\"' || escapeChar))
                {
                    if (escapeChar)
                    {
                        str += escapeChars[currentChar];
                    }
                    else if (currentChar == '\\')
                    {
                        escapeChar = true;
                    }
                    else
                    {
                        str += currentChar;
                    }
                    Advance();
                    escapeChar = false;
                }
                Advance();
                return new Token(TT_STRING, str, posStart, pos);
            }

            public Token makeIdentifier()
            {
                string idStr = "";
                Position posStart = pos.Copy();
                while (currentChar != '\0' && (char.IsLetter(currentChar) || currentChar == '_'))
                {
                    idStr += currentChar;
                    Advance();
                }
                string tokenType = Array.Exists(KEYWORDS, element => element == idStr) ? TT_KEYWORD : TT_IDENTIFIER;
                return new Token(tokenType, idStr, posStart, pos);
            }

            public Token make_minus_or_arrow()
            {
                string tokenType = TT_MINUS;
                Position posStart = pos.Copy();
                Advance();
                if (currentChar == '>')
                {
                    Advance();
                    tokenType = TT_ARROW;
                }
                return new Token(tokenType, _posStart: posStart, _posEnd: pos);
            }

            public Tuple<Token, Error> make_not_equals()
            {
                Position posStart = pos.Copy();
                Advance();
                if (currentChar == '=')
                {
                    Advance();
                    return Tuple.Create<Token, Error>(new Token(TT_NE, _posStart: posStart, _posEnd: pos), null);
                }
                Advance();
                return Tuple.Create<Token, Error>(null, new ExpectedCharError(posStart, pos, "'=' (after '!')"));
            }

            public Token make_equals()
            {
                var tokenType = TT_EQ;
                var posStart = pos.Copy();
                Advance();
                if (currentChar == '=')
                {
                    Advance();
                    tokenType = TT_EE;
                }
                return new Token(tokenType, _posStart: posStart, _posEnd: pos);
            }

            public Token make_less_than()
            {
                var tokenType = TT_LT;
                var posStart = pos.Copy();
                Advance();
                if (currentChar == '=')
                {
                    Advance();
                    tokenType = TT_LTE;
                }
                return new Token(tokenType, _posStart: posStart, _posEnd: pos);
            }

            public Token make_greater_than()
            {
                var tokenType = TT_GT;
                var posStart = pos.Copy();
                Advance();
                if (currentChar == '=')
                {
                    Advance();
                    tokenType = TT_GTE;
                }
                return new Token(tokenType, _posStart: posStart, _posEnd: pos);
            }

            public void skip_comment()
            {
                Advance();
                while (currentChar != '\n')
                {
                    Advance();
                }
                Advance();
            }
        }


        /// Nodes

        #region Nodes

        public class Node
        {
            public Position posStart;
            public Position posEnd;
        }

        public class NumberNode : Node
        {
            public Token token;

            public NumberNode(Token _token)
            {
                token = _token;
                posStart = token.posStart;
                posEnd = token.posEnd;
            }

            public override string ToString()
            {
                return token.ToString();
            }
        }

        public class StringNode : Node
        {
            public Token token;

            public StringNode(Token _token)
            {
                token = _token;
                posStart = token.posStart;
                posEnd = token.posEnd;
            }

            public override string ToString()
            {
                return token.ToString();
            }
        }

        public class ListNode : Node
        {
            public List<Node> elementNodes;

            public ListNode(List<Node> _elementNodes, Position _posStart, Position _posEnd)
            {
                elementNodes = _elementNodes;
                posStart = _posStart;
                posEnd = _posEnd;
            }
        }

        public class VarAccessNode : Node
        {
            public Token varibleNameToken;

            public VarAccessNode(Token _varibleNameToken)
            {
                varibleNameToken = _varibleNameToken;
                posStart = _varibleNameToken.posStart;
                posEnd = _varibleNameToken.posEnd;
            }
        }

        public class VarAssignNode : Node
        {
            public Token varibleNameToken;
            public Node valueNode;

            public VarAssignNode(Token _varibleNameToken, Node _valueNode)
            {
                varibleNameToken = _varibleNameToken;
                valueNode = _valueNode;
                posStart = _varibleNameToken.posStart;
                posEnd = _valueNode.posEnd;
            }
        }

        public class BinOpNode : Node
        {
            public Node leftNode;
            public Token opToken;
            public Node rightNode;
            public BinOpNode(Node _leftNode, Token _opToken, Node _rightNode)
            {
                leftNode = _leftNode;
                opToken = _opToken;
                rightNode = _rightNode;
                posStart = leftNode.posStart;
                posEnd = rightNode.posEnd;
            }

            public override string ToString()
            {
                return $"({leftNode}, {opToken}, {rightNode})";
            }
        }

        public class UnaryOpNode : Node
        {
            public Token opToken;
            public Node node;

            public UnaryOpNode(Token _opToken, Node _node)
            {
                opToken = _opToken;
                node = _node;
                posStart = _opToken.posStart;
                posEnd = node.posEnd;
            }

            public override string ToString()
            {
                return $"({opToken}, {node})";
            }
        }

        public class IfNode : Node
        {
            // Instrictions class or smthing (?)
            public object[][] cases;
            public object elseCase;

            public IfNode(object[][] _cases, object _elseCase)
            {
                cases = _cases;

                elseCase = _elseCase;
                posStart = cases[0][0].posStart;
                posEnd = (else_case || cases[cases.Length - 1])[0].posEnd;
            }
        }

        public class ForNode : Node
        {
            public Token varNameToken;
            public Node startValueNode;
            public Node endValueNode;
            public Node stepValueNode;
            public Node bodyNode;
            public bool shouldReturnNull;

            public ForNode(Token _varNameTok,
                Node _startValueNode,
                Node _endValueNode,
                Node _stepValueNode,
                Node _bodyNode,
                bool _shouldReturnNull
            )
            {
                varNameToken = _varNameTok;
                startValueNode = _startValueNode;
                endValueNode = _endValueNode;
                stepValueNode = _stepValueNode;
                bodyNode = _bodyNode;
                shouldReturnNull = _shouldReturnNull;
                posStart = _varNameTok.posStart;
                posEnd = _bodyNode.posEnd;
            }
        }

        public class WhileNode : Node
        {
            public Node conditionNode;
            public Node bodyNode;
            public bool shouldReturnNull;

            public WhileNode(Node _conditionNode, Node _bodyNode, bool _shouldReturnNull)
            {
                conditionNode = _conditionNode;
                bodyNode = _bodyNode;
                shouldReturnNull = _shouldReturnNull;
                posStart = _conditionNode.posStart;
                posEnd = _bodyNode.posEnd;
            }
        }

        public class FuncDefNode : Node
        {
            public Token varNameToken;
            public Token[] argNameTokens;
            public Node bodyNode;
            public bool shouldAutoReturn;

            public FuncDefNode(Token _varNameToken, Token[] _argNameTokens, Node _bodyNode, bool _shouldAutoReturn)
            {
                varNameToken = _varNameToken;
                argNameTokens = _argNameTokens;
                bodyNode = _bodyNode;
                shouldAutoReturn = _shouldAutoReturn;

                if (_varNameToken != null)
                {
                    posStart = varNameToken.posStart;
                }
                else if (argNameTokens.Length > 0)
                {
                    posStart = argNameTokens[0].posStart;
                }
                else
                {
                    posStart = bodyNode.posStart;
                }
                posEnd = bodyNode.posEnd;
            }
        }

        public class CallNode : Node
        {
            public Node nodeToCall;
            public List<Node> argNodes;

            public CallNode(Node _nodeToCall, List<Node> _argNodes)
            {
                nodeToCall = _nodeToCall;
                argNodes = _argNodes;
                posStart = nodeToCall.posStart;
                if (argNodes.Count > 0)
                {
                    posEnd = argNodes[argNodes.Count - 1].posEnd;
                }
                else
                {
                    posEnd = nodeToCall.posEnd;
                }
            }
        }

        public class ReturnNode : Node
        {
            public Node nodeToReturn;

            public ReturnNode(Node _nodeToReturn, Position _posStart, Position _posEnd)
            {
                nodeToReturn = _nodeToReturn;
                posStart = _posStart;
                posEnd = _posEnd;
            }
        }

        public class ContinueNode : Node
        {
            public ContinueNode(Position _posStart, Position _posEnd)
            {
                posStart = _posStart;
                posEnd = _posEnd;
            }
        }

        public class BreakNode : Node
        {
            public BreakNode(Position _posStart, Position _posEnd)
            {
                posStart = _posStart;
                posEnd = _posEnd;
            }
        }

        #endregion

        /// Parser Result

        public class ParseResult
        {
            public Error error;
            public Node node;
            public int lastRegisteredAdvanceCount;
            public int AdvanceCount;
            public int toReverseCount;

            public ParseResult()
            {
                error = null;
                node = null;
                lastRegisteredAdvanceCount = 0;
                AdvanceCount = 0;
                toReverseCount = 0;
            }

            public void RegisterAdvancement()
            {
                lastRegisteredAdvanceCount = 1;
                AdvanceCount += 1;
            }

            public Node Register(ParseResult res)
            {
                lastRegisteredAdvanceCount = res.AdvanceCount;
                AdvanceCount += res.AdvanceCount;
                if (res.error != null)
                {
                    error = res.error;
                }
                return res.node;
            }

            public Node TryRegister(ParseResult res)
            {
                if (res.error != null)
                {
                    toReverseCount = res.AdvanceCount;
                    return null;
                }
                return Register(res);
            }

            public ParseResult Success(Node _node)
            {
                node = _node;
                return this;
            }

            public ParseResult Failure(Error _error)
            {
                if (error == null || lastRegisteredAdvanceCount == 0)
                {
                    error = _error;
                }
                return this;
            }
        }

        /// Parser

        #region Parser

        class Parser
        {
            public Token[] tokens;
            public int tokenIndex;
            public Token currentToken;

            public Parser(Token[] _tokens)
            {
                tokens = _tokens;
                tokenIndex = -1;
                Advance();
            }

            public Token Advance()
            {
                tokenIndex += 1;
                UpdateCurrentToken();
                return currentToken;
            }

            public object Reverse(int amount = 1)
            {
                tokenIndex -= amount;
                UpdateCurrentToken();
                return currentToken;
            }

            public void UpdateCurrentToken()
            {
                if (tokenIndex >= 0 && tokenIndex < tokens.Length)
                {
                    currentToken = tokens[tokenIndex];
                }
            }

            public ParseResult Parse()
            {
                ParseResult res = Statements();
                if (res.error == null && currentToken.type != TT_EOF)
                {
                    return res.Failure(new InvalidSyntaxError(currentToken.posStart, currentToken.posEnd, "Token cannot appear after previous tokens"));
                }
                return res;
            }


            public ParseResult Statements()
            {
                ParseResult res = new ParseResult();
                List<Node> statements = new List<Node>();
                Position posStart = currentToken.posStart.Copy();

                while (currentToken.type == TT_NEWLINE)
                {
                    res.RegisterAdvancement();
                    Advance();
                }

                Node statement = res.Register(Statement());

                if (res.error != null)
                {
                    return res;
                }

                statements.Add(statement);

                bool moreStatements = true;

                while (true)
                {
                    var newline_count = 0;
                    while (currentToken.type == TT_NEWLINE)
                    {
                        res.RegisterAdvancement();
                        Advance();
                        newline_count += 1;
                    }
                    if (newline_count == 0)
                    {
                        moreStatements = false;
                    }
                    if (!moreStatements)
                    {
                        break;
                    }
                    statement = res.TryRegister(Statement());
                    if (statement == null)
                    {
                        Reverse(res.toReverseCount);
                        moreStatements = false;
                        continue;
                    }
                    statements.Add(statement);
                }
                return res.Success(new ListNode(statements, posStart, currentToken.posEnd.Copy()));
            }

            public ParseResult Statement()
            {
                Node expr;
                ParseResult res = new ParseResult();
                Position posStart = currentToken.posStart.Copy();
                if (currentToken.Matches(TT_KEYWORD, "RETURN"))
                {
                    res.RegisterAdvancement();
                    Advance();
                    expr = res.TryRegister(Expr());
                    if (expr == null)
                    {
                        Reverse(res.toReverseCount);
                    }
                    return res.Success(new ReturnNode(expr, posStart, currentToken.posStart.Copy()));
                }
                if (currentToken.Matches(TT_KEYWORD, "CONTINUE"))
                {
                    res.RegisterAdvancement();
                    Advance();
                    return res.Success(new ContinueNode(posStart, currentToken.posStart.Copy()));
                }
                if (currentToken.Matches(TT_KEYWORD, "BREAK"))
                {
                    res.RegisterAdvancement();
                    Advance();
                    return res.Success(new BreakNode(posStart, currentToken.posStart.Copy()));
                }
                expr = res.Register(Expr());
                if (res.error != null)
                {
                    return res.Failure(new InvalidSyntaxError(currentToken.posStart, currentToken.posEnd, "Expected 'RETURN', 'CONTINUE', 'BREAK', 'VAR', 'IF', 'FOR', 'WHILE', 'FUN', int, float, identifier, '+', '-', '(', '[' or 'NOT'"));
                }
                return res.Success(expr);
            }

            public ParseResult Expr()
            {
                ParseResult res = new ParseResult();
                if (currentToken.Matches(TT_KEYWORD, "VAR"))
                {
                    res.RegisterAdvancement();
                    Advance();
                    if (currentToken.type != TT_IDENTIFIER)
                    {
                        return res.Failure(new InvalidSyntaxError(currentToken.posStart, currentToken.posEnd, "Expected identifier"));
                    }
                    Token var_name = currentToken;
                    res.RegisterAdvancement();
                    Advance();
                    if (currentToken.type != TT_EQ)
                    {
                        return res.Failure(new InvalidSyntaxError(currentToken.posStart, currentToken.posEnd, "Expected '='"));
                    }
                    res.RegisterAdvancement();
                    Advance();
                    Node expr = res.Register(Expr());
                    if (res.error != null)
                    {
                        return res;
                    }
                    return res.Success(new VarAssignNode(var_name, expr));
                }
                Node node = res.Register(BinOp(CompExpr(), ((TT_KEYWORD, "AND"), (TT_KEYWORD, "OR"))));
                if (res.error != null)
                {
                    return res.Failure(new InvalidSyntaxError(currentToken.posStart, currentToken.posEnd, "Expected 'VAR', 'IF', 'FOR', 'WHILE', 'FUN', int, float, identifier, '+', '-', '(', '[' or 'NOT'"));
                }
                return res.Success(node);
            }

            public ParseResult CompExpr()
            {
                Node node;
                ParseResult res = new ParseResult();
                if (currentToken.Matches(TT_KEYWORD, "NOT"))
                {
                    var op_tok = currentToken;
                    res.RegisterAdvancement();
                    Advance();
                    node = res.Register(CompExpr());
                    if (res.error != null)
                    {
                        return res;
                    }
                    return res.Success(new UnaryOpNode(op_tok, node));
                }
                node = res.Register(BinOp(ArithExpr(), (TT_EE, TT_NE, TT_LT, TT_GT, TT_LTE, TT_GTE)));
                if (res.error != null)
                {
                    return res.Failure(new InvalidSyntaxError(currentToken.posStart, currentToken.posEnd, "Expected int, float, identifier, '+', '-', '(', '[', 'IF', 'FOR', 'WHILE', 'FUN' or 'NOT'"));
                }
                return res.Success(node);
            }

            public ParseResult ArithExpr()
            {
                return BinOp(Term(), (TT_PLUS, TT_MINUS));
            }

            public ParseResult Term()
            {
                return BinOp(Factor(), (TT_MUL, TT_DIV));
            }

            public ParseResult Factor()
            {
                ParseResult res = new ParseResult();
                Token tok = currentToken;
                if (tok.type == TT_PLUS || tok.type == TT_MINUS)
                {
                    res.RegisterAdvancement();
                    Advance();
                    Node factor = res.Register(Factor());
                    if (res.error != null)
                    {
                        return res;
                    }
                    return res.Success(new UnaryOpNode(tok, factor));
                }
                return Power();
            }

            public ParseResult Power()
            {
                return BinOp(Call(), ValueTuple.Create(TT_POW), Factor());
            }

            public ParseResult Call()
            {
                ParseResult res = new ParseResult();
                Node atom = res.Register(Atom());
                if (res.error != null)
                {
                    return res;
                }
                if (currentToken.type == TT_LPAREN)
                {
                    res.RegisterAdvancement();
                    Advance();
                    List<Node> arg_nodes = new List<Node>();
                    if (currentToken.type == TT_RPAREN)
                    {
                        res.RegisterAdvancement();
                        Advance();
                    }
                    else
                    {
                        arg_nodes.Add(res.Register(Expr()));
                        if (res.error != null)
                        {
                            return res.Failure(new InvalidSyntaxError(currentToken.posStart, currentToken.posEnd, "Expected ')', 'VAR', 'IF', 'FOR', 'WHILE', 'FUN', int, float, identifier, '+', '-', '(', '[' or 'NOT'"));
                        }
                        while (currentToken.type == TT_COMMA)
                        {
                            res.RegisterAdvancement();
                            Advance();
                            arg_nodes.Add(res.Register(Expr()));
                            if (res.error != null)
                            {
                                return res;
                            }
                        }
                        if (currentToken.type != TT_RPAREN)
                        {
                            return res.Failure(new InvalidSyntaxError(currentToken.posStart, currentToken.posEnd, "Expected ',' or ')'"));
                        }
                        res.RegisterAdvancement();
                        Advance();
                    }
                    return res.Success(new CallNode(atom, arg_nodes));
                }
                return res.Success(atom);
            }

            public ParseResult Atom()
            {
                ParseResult res = new ParseResult();
                Token tok = currentToken;
                if (tok.type == TT_INT || tok.type == TT_FLOAT)
                {
                    res.RegisterAdvancement();
                    Advance();
                    return res.Success(new NumberNode(tok));
                }
                else if (tok.type == TT_STRING)
                {
                    res.RegisterAdvancement();
                    Advance();
                    return res.Success(new StringNode(tok));
                }
                else if (tok.type == TT_IDENTIFIER)
                {
                    res.RegisterAdvancement();
                    Advance();
                    return res.Success(new VarAccessNode(tok));
                }
                else if (tok.type == TT_LPAREN)
                {
                    res.RegisterAdvancement();
                    Advance();
                    Node expr = res.Register(Expr());
                    if (res.error != null)
                    {
                        return res;
                    }
                    if (currentToken.type == TT_RPAREN)
                    {
                        res.RegisterAdvancement();
                        Advance();
                        return res.Success(expr);
                    }
                    else
                    {
                        return res.Failure(new InvalidSyntaxError(currentToken.posStart, currentToken.posEnd, "Expected ')'"));
                    }
                }
                else if (tok.type == TT_LSQUARE)
                {
                    Node list_expr = res.Register(ListExpr());
                    if (res.error != null)
                    {
                        return res;
                    }
                    return res.Success(list_expr);
                }
                else if (tok.Matches(TT_KEYWORD, "IF"))
                {
                    Node if_expr = res.Register(IfExpr());
                    if (res.error != null)
                    {
                        return res;
                    }
                    return res.Success(if_expr);
                }
                else if (tok.Matches(TT_KEYWORD, "FOR"))
                {
                    Node for_expr = res.Register(ForExpr());
                    if (res.error != null)
                    {
                        return res;
                    }
                    return res.Success(for_expr);
                }
                else if (tok.Matches(TT_KEYWORD, "WHILE"))
                {
                    Node while_expr = res.Register(WhileExpr());
                    if (res.error != null)
                    {
                        return res;
                    }
                    return res.Success(while_expr);
                }
                else if (tok.Matches(TT_KEYWORD, "FUN"))
                {
                    Node func_def = res.Register(FuncDef());
                    if (res.error != null)
                    {
                        return res;
                    }
                    return res.Success(func_def);
                }
                return res.Failure(new InvalidSyntaxError(tok.posStart, tok.posEnd, "Expected int, float, identifier, '+', '-', '(', '[', IF', 'FOR', 'WHILE', 'FUN'"));
            }

            public ParseResult ListExpr()
            {
                ParseResult res = new ParseResult();
                List<Node> element_nodes = new List<Node>();
                Position posStart = currentToken.posStart.Copy();
                if (currentToken.type != TT_LSQUARE)
                {
                    return res.Failure(new InvalidSyntaxError(currentToken.posStart, currentToken.posEnd, "Expected '['"));
                }
                res.RegisterAdvancement();
                Advance();
                if (currentToken.type == TT_RSQUARE)
                {
                    res.RegisterAdvancement();
                    Advance();
                }
                else
                {
                    element_nodes.Add(res.Register(Expr()));
                    if (res.error != null)
                    {
                        return res.Failure(new InvalidSyntaxError(currentToken.posStart, currentToken.posEnd, "Expected ']', 'VAR', 'IF', 'FOR', 'WHILE', 'FUN', int, float, identifier, '+', '-', '(', '[' or 'NOT'"));
                    }
                    while (currentToken.type == TT_COMMA)
                    {
                        res.RegisterAdvancement();
                        Advance();
                        element_nodes.Add(res.Register(Expr()));
                        if (res.error != null)
                        {
                            return res;
                        }
                    }
                    if (currentToken.type != TT_RSQUARE)
                    {
                        return res.Failure(new InvalidSyntaxError(currentToken.posStart, currentToken.posEnd, "Expected ',' or ']'"));
                    }
                    res.RegisterAdvancement();
                    Advance();
                }
                return res.Success(new ListNode(element_nodes, posStart, currentToken.posEnd.Copy()));
            }

            public ParseResult IfExpr()
            {
                ParseResult res = new ParseResult();
                Node allCases = res.Register(IfExprCases("IF"));
                if (res.error != null)
                {
                    return res;
                }
                Node cases = allCases;
                Node elseCase = allCases;

                return res.Success(new IfNode(cases, elseCase)); // (?)
            }

            public ParseResult IfExprB()
            {
                return IfExprCases("ELIF");
            }

            public ParseResult IfExprC()
            {
                ParseResult res = new ParseResult();
                Node elseCase = null;
                if (currentToken.Matches(TT_KEYWORD, "ELSE"))
                {
                    res.RegisterAdvancement();
                    Advance();
                    if (currentToken.type == TT_NEWLINE)
                    {
                        res.RegisterAdvancement();
                        Advance();
                        var statements = res.Register(Statements());
                        if (res.error != null)
                        {
                            return res;
                        }
                        elseCase = (statements, true);
                        if (currentToken.Matches(TT_KEYWORD, "END"))
                        {
                            res.RegisterAdvancement();
                            Advance();
                        }
                        else
                        {
                            return res.Failure(new InvalidSyntaxError(currentToken.posStart, currentToken.posEnd, "Expected 'END'"));
                        }
                    }
                    else
                    {
                        var expr = res.Register(Statement());
                        if (res.error != null)
                        {
                            return res;
                        }
                        elseCase = (expr, false);
                    }
                }
                return res.Success(elseCase);
            }

            public ParseResult IfExprBOrC()
            {
                ParseResult res = new ParseResult();
                var cases = new List<object>();
                object else_case = null;
                if (currentToken.Matches(TT_KEYWORD, "ELIF"))
                {
                    var all_cases = res.Register(IfExprB());
                    if (res.error != null)
                    {
                        return res;
                    }
                    cases = all_cases;
                    else_case = all_cases;
                }
                else
                {
                    else_case = res.Register(IfExprC());
                    if (res.error != null)
                    {
                        return res;
                    }
                }
                return res.Success((cases, else_case));
            }

            public ParseResult IfExprCases(object case_keyword)
            {
                object new_cases;
                object all_cases;
                ParseResult res = new ParseResult();
                var cases = new List<object>();
                object else_case = null;
                if (!currentToken.Matches(TT_KEYWORD, case_keyword))
                {
                    return res.Failure(new InvalidSyntaxError(currentToken.posStart, currentToken.posEnd, "Expected '{case_keyword}'"));
                }
                res.RegisterAdvancement();
                Advance();
                var condition = res.Register(Expr());
                if (res.error != null)
                {
                    return res;
                }
                if (!currentToken.Matches(TT_KEYWORD, "THEN"))
                {
                    return res.Failure(new InvalidSyntaxError(currentToken.posStart, currentToken.posEnd, "Expected 'THEN'"));
                }
                res.RegisterAdvancement();
                Advance();
                if (currentToken.type == TT_NEWLINE)
                {
                    res.RegisterAdvancement();
                    Advance();
                    Node statements = res.Register(Statements());
                    if (res.error != null)
                    {
                        return res;
                    }
                    cases.Add((condition, statements, true));
                    if (currentToken.Matches(TT_KEYWORD, "END"))
                    {
                        res.RegisterAdvancement();
                        Advance();
                    }
                    else
                    {
                        all_cases = res.Register(IfExprBOrC());
                        if (res.error != null)
                        {
                            return res;
                        }
                        var _tup_1 = all_cases;
                        new_cases = _tup_1.Item1;
                        else_case = _tup_1.Item2;
                        cases.extend(new_cases);
                    }
                }
                else
                {
                    var expr = res.Register(Statement());
                    if (res.error != null)
                    {
                        return res;
                    }
                    cases.Add((condition, expr, false));
                    all_cases = res.Register(IfExprBOrC());
                    if (res.error != null)
                    {
                        return res;
                    }
                    var _tup_2 = all_cases;
                    new_cases = _tup_2.Item1;
                    else_case = _tup_2.Item2;
                    cases.extend(new_cases);
                }
                return res.Success((cases, else_case));
            }

            public ParseResult ForExpr()
            {
                Node body;
                Node step_value;
                ParseResult res = new ParseResult();
                if (!currentToken.Matches(TT_KEYWORD, "FOR"))
                {
                    return res.Failure(new InvalidSyntaxError(currentToken.posStart, currentToken.posEnd, "Expected 'FOR'"));
                }
                res.RegisterAdvancement();
                Advance();
                if (currentToken.type != TT_IDENTIFIER)
                {
                    return res.Failure(new InvalidSyntaxError(currentToken.posStart, currentToken.posEnd, "Expected identifier"));
                }
                var var_name = currentToken;
                res.RegisterAdvancement();
                Advance();
                if (currentToken.type != TT_EQ)
                {
                    return res.Failure(new InvalidSyntaxError(currentToken.posStart, currentToken.posEnd, "Expected '='"));
                }
                res.RegisterAdvancement();
                Advance();
                var start_value = res.Register(Expr());
                if (res.error != null)
                {
                    return res;
                }
                if (!currentToken.Matches(TT_KEYWORD, "TO"))
                {
                    return res.Failure(new InvalidSyntaxError(currentToken.posStart, currentToken.posEnd, "Expected 'TO'"));
                }
                res.RegisterAdvancement();
                Advance();
                var end_value = res.Register(Expr());
                if (res.error != null)
                {
                    return res;
                }
                if (currentToken.Matches(TT_KEYWORD, "STEP"))
                {
                    res.RegisterAdvancement();
                    Advance();
                    step_value = res.Register(Expr());
                    if (res.error != null)
                    {
                        return res;
                    }
                }
                else
                {
                    step_value = null;
                }
                if (!currentToken.Matches(TT_KEYWORD, "THEN"))
                {
                    return res.Failure(new InvalidSyntaxError(currentToken.posStart, currentToken.posEnd, "Expected 'THEN'"));
                }
                res.RegisterAdvancement();
                Advance();
                if (currentToken.type == TT_NEWLINE)
                {
                    res.RegisterAdvancement();
                    Advance();
                    body = res.Register(Statements());
                    if (res.error != null)
                    {
                        return res;
                    }
                    if (!currentToken.Matches(TT_KEYWORD, "END"))
                    {
                        return res.Failure(new InvalidSyntaxError(currentToken.posStart, currentToken.posEnd, "Expected 'END'"));
                    }
                    res.RegisterAdvancement();
                    Advance();
                    return res.Success(new ForNode(var_name, start_value, end_value, step_value, body, true));
                }
                body = res.Register(Statement());
                if (res.error != null)
                {
                    return res;
                }
                return res.Success(new ForNode(var_name, start_value, end_value, step_value, body, false));
            }

            public ParseResult WhileExpr()
            {
                Node body;
                ParseResult res = new ParseResult();
                if (!currentToken.Matches(TT_KEYWORD, "WHILE"))
                {
                    return res.Failure(new InvalidSyntaxError(currentToken.posStart, currentToken.posEnd, "Expected 'WHILE'"));
                }
                res.RegisterAdvancement();
                Advance();
                var condition = res.Register(Expr());
                if (res.error != null)
                {
                    return res;
                }
                if (!currentToken.Matches(TT_KEYWORD, "THEN"))
                {
                    return res.Failure(new InvalidSyntaxError(currentToken.posStart, currentToken.posEnd, "Expected 'THEN'"));
                }
                res.RegisterAdvancement();
                Advance();
                if (currentToken.type == TT_NEWLINE)
                {
                    res.RegisterAdvancement();
                    Advance();
                    body = res.Register(Statements());
                    if (res.error != null)
                    {
                        return res;
                    }
                    if (!currentToken.Matches(TT_KEYWORD, "END"))
                    {
                        return res.Failure(new InvalidSyntaxError(currentToken.posStart, currentToken.posEnd, "Expected 'END'"));
                    }
                    res.RegisterAdvancement();
                    Advance();
                    return res.Success(new WhileNode(condition, body, true));
                }
                body = res.Register(Statement());
                if (res.error != null)
                {
                    return res;
                }
                return res.Success(new WhileNode(condition, body, false));
            }

            public ParseResult FuncDef()
            {
                Node body;
                Token var_name_tok;
                ParseResult res = new ParseResult();
                if (!currentToken.Matches(TT_KEYWORD, "FUN"))
                {
                    return res.Failure(new InvalidSyntaxError(currentToken.posStart, currentToken.posEnd, "Expected 'FUN'"));
                }
                res.RegisterAdvancement();
                Advance();
                if (currentToken.type == TT_IDENTIFIER)
                {
                    var_name_tok = currentToken;
                    res.RegisterAdvancement();
                    Advance();
                    if (currentToken.type != TT_LPAREN)
                    {
                        return res.Failure(new InvalidSyntaxError(currentToken.posStart, currentToken.posEnd, "Expected '('"));
                    }
                }
                else
                {
                    var_name_tok = null;
                    if (currentToken.type != TT_LPAREN)
                    {
                        return res.Failure(new InvalidSyntaxError(currentToken.posStart, currentToken.posEnd, "Expected identifier or '('"));
                    }
                }
                res.RegisterAdvancement();
                Advance();
                var arg_name_toks = new List<object>();
                if (currentToken.type == TT_IDENTIFIER)
                {
                    arg_name_toks.Add(currentToken);
                    res.RegisterAdvancement();
                    Advance();
                    while (currentToken.type == TT_COMMA)
                    {
                        res.RegisterAdvancement();
                        Advance();
                        if (currentToken.type != TT_IDENTIFIER)
                        {
                            return res.Failure(new InvalidSyntaxError(currentToken.posStart, currentToken.posEnd, "Expected identifier"));
                        }
                        arg_name_toks.Add(currentToken);
                        res.RegisterAdvancement();
                        Advance();
                    }
                    if (currentToken.type != TT_RPAREN)
                    {
                        return res.Failure(new InvalidSyntaxError(currentToken.posStart, currentToken.posEnd, "Expected ',' or ')'"));
                    }
                }
                else if (currentToken.type != TT_RPAREN)
                {
                    return res.Failure(InvalidSyntaxError(currentToken.posStart, currentToken.posEnd, "Expected identifier or ')'"));
                }
                res.RegisterAdvancement();
                Advance();
                if (currentToken.type == TT_ARROW)
                {
                    res.RegisterAdvancement();
                    Advance();
                    body = res.Register(Expr());
                    if (res.error != null)
                    {
                        return res;
                    }
                    return res.Success(FuncDefNode(var_name_tok, arg_name_toks, body, true));
                }
                if (currentToken.type != TT_NEWLINE)
                {
                    return res.Failure(InvalidSyntaxError(currentToken.posStart, currentToken.posEnd, "Expected '->' or NEWLINE"));
                }
                res.RegisterAdvancement();
                Advance();
                body = res.Register(Statements());
                if (res.error != null)
                {
                    return res;
                }
                if (!currentToken.Matches(TT_KEYWORD, "END"))
                {
                    return res.Failure(InvalidSyntaxError(currentToken.posStart, currentToken.posEnd, "Expected 'END'"));
                }
                res.RegisterAdvancement();
                Advance();
                return res.Success(FuncDefNode(var_name_tok, arg_name_toks, body, false));
            }


            public ParseResult BinOp(object func_a, object ops, object func_b = null)
            {
                if (func_b == null)
                {
                    func_b = func_a;
                }
                ParseResult res = new ParseResult();
                var left = res.Register(func_a());
                if (res.error != null)
                {
                    return res;
                }
                while (ops.Contains(currentToken.type) || ops.Contains((currentToken.type, currentToken.value)))
                {
                    var op_tok = currentToken;
                    res.RegisterAdvancement();
                    Advance();
                    var right = res.Register(func_b());
                    if (res.error != null)
                    {
                        return res;
                    }
                    left = new BinOpNode(left, op_tok, right);
                }
                return res.Success(left);
            }
        }

        #endregion

        /// Experimental

        public static Result Run(string filename, string code)
        {
            return new Result(null, $"{filename}: {code}");
        }
    }
}
