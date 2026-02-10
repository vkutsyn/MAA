using System.Globalization;

namespace MAA.Domain.Rules;

/// <summary>
/// Evaluates conditional rule expressions for question visibility.
/// </summary>
public static class ConditionalRuleEvaluator
{
    public static bool Evaluate(string ruleExpression, IReadOnlyDictionary<Guid, string?> answers)
    {
        if (string.IsNullOrWhiteSpace(ruleExpression))
            throw new ArgumentException("Rule expression is required.", nameof(ruleExpression));

        var parser = new Parser(ruleExpression);
        var root = parser.ParseExpression();
        parser.EnsureEndOfInput();
        return root.Evaluate(answers ?? new Dictionary<Guid, string?>());
    }

    public static IReadOnlySet<Guid> GetReferencedQuestionIds(string ruleExpression)
    {
        if (string.IsNullOrWhiteSpace(ruleExpression))
            return new HashSet<Guid>();

        var parser = new Parser(ruleExpression);
        var root = parser.ParseExpression();
        parser.EnsureEndOfInput();

        var ids = new HashSet<Guid>();
        root.CollectQuestionIds(ids);
        return ids;
    }

    private enum TokenType
    {
        Identifier,
        String,
        Number,
        Boolean,
        And,
        Or,
        Not,
        In,
        Equals,
        NotEquals,
        Greater,
        GreaterOrEqual,
        Less,
        LessOrEqual,
        LeftParen,
        RightParen,
        LeftBracket,
        RightBracket,
        Comma,
        End
    }

    private readonly record struct Token(TokenType Type, string Lexeme, int Position);

    private sealed class Tokenizer
    {
        private readonly string _text;
        private int _position;

        public Tokenizer(string text)
        {
            _text = text;
            _position = 0;
        }

        public Token NextToken()
        {
            SkipWhitespace();
            if (_position >= _text.Length)
                return new Token(TokenType.End, string.Empty, _position);

            var start = _position;
            var current = _text[_position];

            switch (current)
            {
                case '(':
                    _position++;
                    return new Token(TokenType.LeftParen, "(", start);
                case ')':
                    _position++;
                    return new Token(TokenType.RightParen, ")", start);
                case '[':
                    _position++;
                    return new Token(TokenType.LeftBracket, "[", start);
                case ']':
                    _position++;
                    return new Token(TokenType.RightBracket, "]", start);
                case ',':
                    _position++;
                    return new Token(TokenType.Comma, ",", start);
                case '=':
                    if (Peek('='))
                    {
                        _position += 2;
                        return new Token(TokenType.Equals, "==", start);
                    }
                    break;
                case '!':
                    if (Peek('='))
                    {
                        _position += 2;
                        return new Token(TokenType.NotEquals, "!=", start);
                    }
                    break;
                case '>':
                    if (Peek('='))
                    {
                        _position += 2;
                        return new Token(TokenType.GreaterOrEqual, ">=", start);
                    }
                    _position++;
                    return new Token(TokenType.Greater, ">", start);
                case '<':
                    if (Peek('='))
                    {
                        _position += 2;
                        return new Token(TokenType.LessOrEqual, "<=", start);
                    }
                    _position++;
                    return new Token(TokenType.Less, "<", start);
                case '\'':
                case '"':
                    return ReadStringToken();
            }

            if (char.IsDigit(current) || (current == '-' && PeekDigit()))
            {
                return ReadNumberToken();
            }

            if (IsIdentifierStart(current))
            {
                return ReadIdentifierOrKeyword();
            }

            throw new ArgumentException($"Unexpected character '{current}' at position {start}.");
        }

        private void SkipWhitespace()
        {
            while (_position < _text.Length && char.IsWhiteSpace(_text[_position]))
                _position++;
        }

        private bool Peek(char expected)
        {
            return _position + 1 < _text.Length && _text[_position + 1] == expected;
        }

        private bool PeekDigit()
        {
            return _position + 1 < _text.Length && char.IsDigit(_text[_position + 1]);
        }

        private static bool IsIdentifierStart(char value)
        {
            return char.IsLetterOrDigit(value);
        }

        private Token ReadStringToken()
        {
            var quote = _text[_position];
            var start = _position;
            _position++;

            var buffer = new List<char>();
            while (_position < _text.Length)
            {
                var current = _text[_position++];
                if (current == '\\')
                {
                    if (_position >= _text.Length)
                        throw new ArgumentException("Unterminated escape sequence in string literal.");

                    var escaped = _text[_position++];
                    buffer.Add(escaped);
                    continue;
                }

                if (current == quote)
                    return new Token(TokenType.String, new string(buffer.ToArray()), start);

                buffer.Add(current);
            }

            throw new ArgumentException("Unterminated string literal.");
        }

        private Token ReadNumberToken()
        {
            var start = _position;
            var buffer = new List<char>();

            if (_text[_position] == '-')
            {
                buffer.Add('-');
                _position++;
            }

            while (_position < _text.Length && (char.IsDigit(_text[_position]) || _text[_position] == '.'))
            {
                buffer.Add(_text[_position]);
                _position++;
            }

            return new Token(TokenType.Number, new string(buffer.ToArray()), start);
        }

        private Token ReadIdentifierOrKeyword()
        {
            var start = _position;
            var buffer = new List<char>();

            while (_position < _text.Length)
            {
                var current = _text[_position];
                if (!char.IsLetterOrDigit(current) && current != '-' && current != '_')
                    break;

                buffer.Add(current);
                _position++;
            }

            var lexeme = new string(buffer.ToArray());
            var normalized = lexeme.ToUpperInvariant();

            return normalized switch
            {
                "AND" => new Token(TokenType.And, lexeme, start),
                "OR" => new Token(TokenType.Or, lexeme, start),
                "NOT" => new Token(TokenType.Not, lexeme, start),
                "IN" => new Token(TokenType.In, lexeme, start),
                "TRUE" => new Token(TokenType.Boolean, "true", start),
                "FALSE" => new Token(TokenType.Boolean, "false", start),
                _ => new Token(TokenType.Identifier, lexeme, start)
            };
        }
    }

    private sealed class Parser
    {
        private readonly Tokenizer _tokenizer;
        private Token _current;

        public Parser(string text)
        {
            _tokenizer = new Tokenizer(text);
            _current = _tokenizer.NextToken();
        }

        public RuleNode ParseExpression() => ParseOr();

        public void EnsureEndOfInput()
        {
            if (_current.Type != TokenType.End)
                throw new ArgumentException($"Unexpected token '{_current.Lexeme}' at position {_current.Position}.");
        }

        private RuleNode ParseOr()
        {
            var node = ParseAnd();
            while (Match(TokenType.Or))
            {
                var right = ParseAnd();
                node = new BinaryNode(node, right, BinaryOperator.Or);
            }
            return node;
        }

        private RuleNode ParseAnd()
        {
            var node = ParseUnary();
            while (Match(TokenType.And))
            {
                var right = ParseUnary();
                node = new BinaryNode(node, right, BinaryOperator.And);
            }
            return node;
        }

        private RuleNode ParseUnary()
        {
            if (Match(TokenType.Not))
            {
                var operand = ParseUnary();
                return new NotNode(operand);
            }

            return ParsePrimary();
        }

        private RuleNode ParsePrimary()
        {
            if (Match(TokenType.LeftParen))
            {
                var node = ParseExpression();
                Consume(TokenType.RightParen, "Expected ')' after expression.");
                return node;
            }

            return ParseComparison();
        }

        private RuleNode ParseComparison()
        {
            var identifier = Consume(TokenType.Identifier, "Expected question identifier.");
            if (!Guid.TryParse(identifier.Lexeme, out var questionId))
                throw new ArgumentException($"Invalid question identifier '{identifier.Lexeme}'.");

            if (Match(TokenType.Not))
            {
                if (Match(TokenType.In))
                {
                    var values = ParseList();
                    return new ComparisonNode(questionId, ComparisonOperator.NotIn, values);
                }

                throw new ArgumentException("Expected IN after NOT for membership comparison.");
            }

            if (Match(TokenType.In))
            {
                var values = ParseList();
                return new ComparisonNode(questionId, ComparisonOperator.In, values);
            }

            if (Match(TokenType.Equals))
                return new ComparisonNode(questionId, ComparisonOperator.Equals, new[] { ParseLiteral() });

            if (Match(TokenType.NotEquals))
                return new ComparisonNode(questionId, ComparisonOperator.NotEquals, new[] { ParseLiteral() });

            if (Match(TokenType.GreaterOrEqual))
                return new ComparisonNode(questionId, ComparisonOperator.GreaterOrEqual, new[] { ParseLiteral() });

            if (Match(TokenType.Greater))
                return new ComparisonNode(questionId, ComparisonOperator.Greater, new[] { ParseLiteral() });

            if (Match(TokenType.LessOrEqual))
                return new ComparisonNode(questionId, ComparisonOperator.LessOrEqual, new[] { ParseLiteral() });

            if (Match(TokenType.Less))
                return new ComparisonNode(questionId, ComparisonOperator.Less, new[] { ParseLiteral() });

            throw new ArgumentException($"Expected comparison operator after '{identifier.Lexeme}'.");
        }

        private IReadOnlyList<RuleValue> ParseList()
        {
            Consume(TokenType.LeftBracket, "Expected '[' to start list.");
            var values = new List<RuleValue>();

            values.Add(ParseLiteral());
            while (Match(TokenType.Comma))
            {
                values.Add(ParseLiteral());
            }

            Consume(TokenType.RightBracket, "Expected ']' after list.");
            return values;
        }

        private RuleValue ParseLiteral()
        {
            if (Match(TokenType.String, out var token))
                return RuleValue.FromString(token.Lexeme);

            if (Match(TokenType.Number, out token))
            {
                if (!decimal.TryParse(token.Lexeme, NumberStyles.Number, CultureInfo.InvariantCulture, out var number))
                    throw new ArgumentException($"Invalid numeric literal '{token.Lexeme}'.");

                return RuleValue.FromNumber(number);
            }

            if (Match(TokenType.Boolean, out token))
                return RuleValue.FromBoolean(string.Equals(token.Lexeme, "true", StringComparison.OrdinalIgnoreCase));

            throw new ArgumentException("Expected literal value.");
        }

        private bool Match(TokenType type)
        {
            if (_current.Type == type)
            {
                _current = _tokenizer.NextToken();
                return true;
            }
            return false;
        }

        private bool Match(TokenType type, out Token token)
        {
            if (_current.Type == type)
            {
                token = _current;
                _current = _tokenizer.NextToken();
                return true;
            }

            token = default;
            return false;
        }

        private Token Consume(TokenType type, string message)
        {
            if (_current.Type == type)
            {
                var token = _current;
                _current = _tokenizer.NextToken();
                return token;
            }

            throw new ArgumentException(message);
        }
    }

    private abstract record RuleNode
    {
        public abstract bool Evaluate(IReadOnlyDictionary<Guid, string?> answers);
        public abstract void CollectQuestionIds(HashSet<Guid> questionIds);
    }

    private enum BinaryOperator
    {
        And,
        Or
    }

    private sealed record BinaryNode(RuleNode Left, RuleNode Right, BinaryOperator Operator) : RuleNode
    {
        public override bool Evaluate(IReadOnlyDictionary<Guid, string?> answers)
        {
            return Operator == BinaryOperator.And
                ? Left.Evaluate(answers) && Right.Evaluate(answers)
                : Left.Evaluate(answers) || Right.Evaluate(answers);
        }

        public override void CollectQuestionIds(HashSet<Guid> questionIds)
        {
            Left.CollectQuestionIds(questionIds);
            Right.CollectQuestionIds(questionIds);
        }
    }

    private sealed record NotNode(RuleNode Operand) : RuleNode
    {
        public override bool Evaluate(IReadOnlyDictionary<Guid, string?> answers)
        {
            return !Operand.Evaluate(answers);
        }

        public override void CollectQuestionIds(HashSet<Guid> questionIds)
        {
            Operand.CollectQuestionIds(questionIds);
        }
    }

    private enum ComparisonOperator
    {
        Equals,
        NotEquals,
        Greater,
        GreaterOrEqual,
        Less,
        LessOrEqual,
        In,
        NotIn
    }

    private sealed record ComparisonNode(Guid QuestionId, ComparisonOperator Operator, IReadOnlyList<RuleValue> Values) : RuleNode
    {
        public override bool Evaluate(IReadOnlyDictionary<Guid, string?> answers)
        {
            if (!answers.TryGetValue(QuestionId, out var raw) || string.IsNullOrWhiteSpace(raw))
                return false;

            var comparisonValue = Values.FirstOrDefault();
            return Operator switch
            {
                ComparisonOperator.Equals => comparisonValue.Matches(raw, StringComparison.OrdinalIgnoreCase),
                ComparisonOperator.NotEquals => !comparisonValue.Matches(raw, StringComparison.OrdinalIgnoreCase),
                ComparisonOperator.Greater => CompareNumeric(raw, comparisonValue, (left, right) => left > right),
                ComparisonOperator.GreaterOrEqual => CompareNumeric(raw, comparisonValue, (left, right) => left >= right),
                ComparisonOperator.Less => CompareNumeric(raw, comparisonValue, (left, right) => left < right),
                ComparisonOperator.LessOrEqual => CompareNumeric(raw, comparisonValue, (left, right) => left <= right),
                ComparisonOperator.In => Values.Any(value => value.Matches(raw, StringComparison.OrdinalIgnoreCase)),
                ComparisonOperator.NotIn => Values.All(value => !value.Matches(raw, StringComparison.OrdinalIgnoreCase)),
                _ => false
            };
        }

        public override void CollectQuestionIds(HashSet<Guid> questionIds)
        {
            questionIds.Add(QuestionId);
        }

        private static bool CompareNumeric(string raw, RuleValue value, Func<decimal, decimal, bool> compare)
        {
            if (!value.TryGetNumber(out var ruleNumber))
                return false;

            if (!decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var answerNumber))
                return false;

            return compare(answerNumber, ruleNumber);
        }
    }

    private readonly record struct RuleValue(RuleValueType Type, string Raw, decimal? Number, bool? Boolean)
    {
        public static RuleValue FromString(string value) => new(RuleValueType.String, value, null, null);
        public static RuleValue FromNumber(decimal value) => new(RuleValueType.Number, value.ToString(CultureInfo.InvariantCulture), value, null);
        public static RuleValue FromBoolean(bool value) => new(RuleValueType.Boolean, value ? "true" : "false", null, value);

        public bool TryGetNumber(out decimal number)
        {
            number = Number ?? 0m;
            return Type == RuleValueType.Number && Number.HasValue;
        }

        public bool Matches(string raw, StringComparison comparison)
        {
            return Type switch
            {
                RuleValueType.Number => decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var answer) &&
                                        Number.HasValue && answer == Number.Value,
                RuleValueType.Boolean => bool.TryParse(raw, out var answerBool) && Boolean.HasValue && answerBool == Boolean.Value,
                _ => string.Equals(raw, Raw, comparison)
            };
        }
    }

    private enum RuleValueType
    {
        String,
        Number,
        Boolean
    }
}
