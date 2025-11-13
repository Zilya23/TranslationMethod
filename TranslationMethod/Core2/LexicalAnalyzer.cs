using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TranslationMethod.Core2
{
    public class LexicalAnalyzer
    {
        private string _sourceText;
        private int _currentPosition;
        private int _currentLine;
        private int _currentColumn;
        private List<Token> _tokens;

        // Алфавит исходного языка
        private readonly HashSet<char> _alphabet = new HashSet<char>
        {
            '0', '1', 'a', 'b', 'c', 'd', '(', ')', '!', ':', '=', ' '
        };

        // Зарезервированные слова
        private readonly Dictionary<string, TokenType> _reservedWords = new Dictionary<string, TokenType>
        {
            { "(", TokenType.LeftParenthesis },
            { ")", TokenType.RightParenthesis },
            { "!", TokenType.Exclamation },
            { ":", TokenType.Colon },
            { ":=", TokenType.Assignment },
            { "!:", TokenType.ConditionSeparator }
        };

        // Регулярные выражения для слов
        private readonly Regex _numberRegex;
        private readonly Regex _identifierRegex;

        public LexicalAnalyzer()
        {
            // Регулярное выражение для чисел: (000)*001(100)*
            _numberRegex = new Regex(@"^(000)*001(100)*$", RegexOptions.Compiled);

            // Регулярное выражение для идентификаторов: (a|b|c|d)+ с проверкой что первые два символа ab
            _identifierRegex = new Regex(@"^[a-d]+$", RegexOptions.Compiled);
        }

        public AnalysisResult Analyze(string text)
        {
            _sourceText = text ?? string.Empty;
            _currentPosition = 0;
            _currentLine = 1;
            _currentColumn = 1;
            _tokens = new List<Token>();

            try
            {
                while (_currentPosition < _sourceText.Length)
                {
                    char currentChar = _sourceText[_currentPosition];

                    if (char.IsWhiteSpace(currentChar))
                    {
                        if (currentChar == '\r' || currentChar == '\n')
                        {
                            ReadEndOfLineToken();
                        }
                        else
                        {
                            MoveToNextChar();
                        }
                        continue;
                    }

                    if (currentChar == '/' && (_currentPosition + 1 < _sourceText.Length) &&
                        _sourceText[_currentPosition + 1] == '/')
                    {
                        SkipComment();
                        continue;
                    }

                    if (!_alphabet.Contains(currentChar))
                    {
                        return AnalysisResult.Error($"Символ не принадлежит алфавиту: '{currentChar}' " +
                                                   $"(позиция: строка {_currentLine}, столбец {_currentColumn})");
                    }

                    Token token = ReadNextToken();
                    if (token != null)
                    {
                        _tokens.Add(token);
                    }
                    else
                    {
                        return AnalysisResult.Error($"Неизвестный символ: '{currentChar}' " +
                                                   $"(позиция: строка {_currentLine}, столбец {_currentColumn})");
                    }
                }

                string resultMessage = "Текст верен";
                if (_tokens.Count > 0)
                {
                    resultMessage += "\n" + GetTokensString();
                }
                return AnalysisResult.Success(resultMessage);
            }
            catch (IndexOutOfRangeException)
            {
                return AnalysisResult.Error("Попытка чтения за пределами исходного текста");
            }
        }

        private Token ReadNextToken()
        {
            if (_currentPosition >= _sourceText.Length)
                return null;

            char currentChar = _sourceText[_currentPosition];
            int startLine = _currentLine;
            int startColumn = _currentColumn;

            // Проверка на многосимвольные зарезервированные слова
            if (currentChar == ':' && (_currentPosition + 1 < _sourceText.Length) &&
                _sourceText[_currentPosition + 1] == '=')
            {
                MoveToNextChar(); // :
                MoveToNextChar(); // =
                return new Token(TokenType.Assignment, ":=", startLine, startColumn);
            }

            if (currentChar == '!' && (_currentPosition + 1 < _sourceText.Length) &&
                _sourceText[_currentPosition + 1] == ':')
            {
                MoveToNextChar(); // !
                MoveToNextChar(); // :
                return new Token(TokenType.ConditionSeparator, "!:", startLine, startColumn);
            }

            // Проверка на односимвольные зарезервированные слова
            if (_reservedWords.ContainsKey(currentChar.ToString()))
            {
                TokenType type = _reservedWords[currentChar.ToString()];
                MoveToNextChar();
                return new Token(type, currentChar.ToString(), startLine, startColumn);
            }

            // Числа (из 0 и 1)
            if (currentChar == '0' || currentChar == '1')
            {
                return ReadNumberToken(startLine, startColumn);
            }

            // Идентификаторы (из a, b, c, d)
            if (currentChar >= 'a' && currentChar <= 'd')
            {
                return ReadIdentifierToken(startLine, startColumn);
            }

            return null;
        }

        private Token ReadNumberToken(int line, int column)
        {
            StringBuilder value = new StringBuilder();

            while (_currentPosition < _sourceText.Length)
            {
                char currentChar = _sourceText[_currentPosition];
                if (currentChar == '0' || currentChar == '1')
                {
                    value.Append(currentChar);
                    MoveToNextChar();
                }
                else
                {
                    break;
                }
            }

            string number = value.ToString();

            // Проверка соответствия регулярному выражению: (000)*001(100)*
            if (!_numberRegex.IsMatch(number))
            {
                throw new LexicalException($"Некорректный формат числа: '{number}' " +
                                         $"(ожидается: (000)*001(100)*)", line, column);
            }

            return new Token(TokenType.Number, number, line, column);
        }

        private Token ReadIdentifierToken(int line, int column)
        {
            StringBuilder value = new StringBuilder();

            while (_currentPosition < _sourceText.Length)
            {
                char currentChar = _sourceText[_currentPosition];
                if (currentChar >= 'a' && currentChar <= 'd')
                {
                    value.Append(currentChar);
                    MoveToNextChar();
                }
                else
                {
                    break;
                }
            }

            string identifier = value.ToString();

            // Проверка соответствия регулярному выражению и дополнительным условиям
            if (!_identifierRegex.IsMatch(identifier))
            {
                throw new LexicalException($"Некорректный идентификатор: '{identifier}' " +
                                         $"(разрешены только символы a, b, c, d)", line, column);
            }

            // Проверка условия: первые два символа всегда ab
            if (identifier.Length >= 2 && (identifier[0] != 'a' || identifier[1] != 'b'))
            {
                throw new LexicalException($"Некорректный идентификатор: '{identifier}' " +
                                         $"(первые два символа должны быть 'ab')", line, column);
            }

            return new Token(TokenType.Identifier, identifier, line, column);
        }

        private void SkipComment()
        {
            // Пропускаем два символа '//'
            MoveToNextChar();
            MoveToNextChar();

            // Пропускаем все символы до конца строки
            while (_currentPosition < _sourceText.Length)
            {
                char currentChar = _sourceText[_currentPosition];
                if (currentChar == '\r' || currentChar == '\n')
                {
                    break;
                }
                MoveToNextChar();
            }
        }

        private void ReadEndOfLineToken()
        {
            int startLine = _currentLine;
            char currentChar = _sourceText[_currentPosition];

            if (currentChar == '\r' && (_currentPosition + 1 < _sourceText.Length) &&
                _sourceText[_currentPosition + 1] == '\n')
            {
                MoveToNextChar();
                MoveToNextChar();
            }
            else if (currentChar == '\n')
            {
                MoveToNextChar();
            }
            else if (currentChar == '\r')
            {
                MoveToNextChar();
            }

            // Добавляем EndRow только если в строке были другие токены
            if (_tokens.Count > 0 && _tokens[_tokens.Count - 1].Type != TokenType.EndRow)
            {
                _tokens.Add(new Token(TokenType.EndRow, "EndRow", startLine, 1));
            }
        }

        private string GetTokensString()
        {
            List<string> tokenStrings = new List<string>();
            foreach (var token in _tokens)
            {
                if (token.Type != TokenType.EndRow)
                {
                    tokenStrings.Add(token.ToString());
                }
            }
            return string.Join(", ", tokenStrings);
        }

        private void MoveToNextChar()
        {
            if (_currentPosition >= _sourceText.Length)
                return;

            char currentChar = _sourceText[_currentPosition];

            if (currentChar == '\n')
            {
                _currentLine++;
                _currentColumn = 1;
            }
            else if (currentChar == '\r')
            {
                _currentLine++;
                _currentColumn = 1;
            }
            else
            {
                _currentColumn++;
            }

            _currentPosition++;
        }
    }

    public enum TokenType
    {
        // Зарезервированные слова
        LeftParenthesis,
        RightParenthesis,
        Exclamation,
        Colon,
        Assignment,
        ConditionSeparator,

        // Основные типы токенов
        Number,
        Identifier,
        EndRow,
        EndText
    }

    public class Token
    {
        public TokenType Type { get; }
        public string Value { get; }
        public int Line { get; }
        public int Column { get; }

        public Token(TokenType type, string value, int line, int column)
        {
            Type = type;
            Value = value;
            Line = line;
            Column = column;
        }

        public override string ToString()
        {
            return $"{Type} '{Value}'";
        }
    }

    public class AnalysisResult
    {
        public bool IsSuccess { get; }
        public string Message { get; }

        private AnalysisResult(bool isSuccess, string message)
        {
            IsSuccess = isSuccess;
            Message = message;
        }

        public static AnalysisResult Success(string message) => new AnalysisResult(true, message);
        public static AnalysisResult Error(string message) => new AnalysisResult(false, message);
    }

    public class LexicalException : Exception
    {
        public int Line { get; }
        public int Column { get; }

        public LexicalException(string message, int line, int column) : base(message)
        {
            Line = line;
            Column = column;
        }
    }
}
