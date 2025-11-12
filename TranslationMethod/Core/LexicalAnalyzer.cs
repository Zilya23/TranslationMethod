using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranslationMethod.Core
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
            '0', '1', 'a', 'b', 'c', 'd', '(', ')', '!', ':', '=', '/', ' '
        };

        // Специальные символы
        private readonly HashSet<char> _specialSymbols = new HashSet<char>
        {
            '(', ')', '!', ':', '=', '/', ' '
        };

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

                    // Если символ не в алфавите и это не управляющий символ - ошибка
                    if (!_alphabet.Contains(currentChar) &&
                        currentChar != '\r' && currentChar != '\n')
                    {
                        // Но сначала проверяем, не комментарий ли это
                        if (currentChar == '/' && (_currentPosition + 1 < _sourceText.Length) &&
                            _sourceText[_currentPosition + 1] == '/')
                        {
                            SkipComment();
                            continue;
                        }

                        return AnalysisResult.Error($"Символ не принадлежит алфавиту: '{currentChar}' " +
                                                   $"(позиция: строка {_currentLine}, столбец {_currentColumn})");
                    }

                    // Обработка комментариев
                    if (currentChar == '/' && (_currentPosition + 1 < _sourceText.Length) &&
                        _sourceText[_currentPosition + 1] == '/')
                    {
                        SkipComment();
                        continue;
                    }

                    // Обработка переносов строк
                    if (currentChar == '\r' || currentChar == '\n')
                    {
                        ReadEndOfLineToken();
                        continue;
                    }

                    // Токенизация обычных символов
                    Token token = ReadNextToken();
                    if (token != null)
                    {
                        _tokens.Add(token);
                    }
                    else
                    {
                        // Если символ не распознан (этого не должно происходить), просто пропускаем
                        MoveToNextChar();
                    }
                }

                // Добавляем токен окончания текста
                if (_tokens.Count > 0)
                {
                    _tokens.Add(new Token(TokenType.EndText, "EndText", _currentLine, _currentColumn));
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

        private void SkipComment()
        {
            // Пропускаем два символа '//'
            MoveToNextChar(); // первый '/'
            MoveToNextChar(); // второй '/'

            // Пропускаем ВСЕ символы до конца строки
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

            // Обработка Windows-style \r\n
            if (currentChar == '\r' && (_currentPosition + 1 < _sourceText.Length) &&
                _sourceText[_currentPosition + 1] == '\n')
            {
                MoveToNextChar(); // пропускаем \r
                MoveToNextChar(); // пропускаем \n
            }
            // Обработка Unix-style \n
            else if (currentChar == '\n')
            {
                MoveToNextChar();
            }
            // Обработка одиночного \r
            else if (currentChar == '\r')
            {
                MoveToNextChar();
            }

            // Добавляем EndRow только если в строке были другие токены
            if (_tokens.Count > 0 && (_tokens[_tokens.Count - 1].Type != TokenType.EndRow || _tokens.Count == 1))
            {
                _tokens.Add(new Token(TokenType.EndRow, "EndRow", startLine, 1));
            }
        }

        private Token ReadNextToken()
        {
            if (_currentPosition >= _sourceText.Length)
                return null;

            char currentChar = _sourceText[_currentPosition];
            int startLine = _currentLine;
            int startColumn = _currentColumn;

            // Пропускаем управляющие символы (они уже обработаны)
            if (currentChar == '\r' || currentChar == '\n')
                return null;

            // Цифры (0 или 1)
            if (currentChar == '0' || currentChar == '1')
            {
                return ReadDigitToken(startLine, startColumn);
            }

            // Буквы (a, b, c, d)
            if (currentChar >= 'a' && currentChar <= 'd')
            {
                return ReadLetterToken(startLine, startColumn);
            }

            // Специальные символы
            if (_specialSymbols.Contains(currentChar))
            {
                return ReadSpecialSymbolToken(startLine, startColumn);
            }

            return null;
        }

        private Token ReadDigitToken(int line, int column)
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

            return new Token(TokenType.Digit, value.ToString(), line, column);
        }

        private Token ReadLetterToken(int line, int column)
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

            return new Token(TokenType.Letter, value.ToString(), line, column);
        }

        private Token ReadSpecialSymbolToken(int line, int column)
        {
            char symbol = _sourceText[_currentPosition];
            MoveToNextChar();

            // Для пробела выводим специальное представление
            string displayValue = symbol == ' ' ? "Space" : symbol.ToString();
            return new Token(TokenType.SpecialSymbol, displayValue, line, column);
        }

        private string GetTokensString()
        {
            List<string> tokenStrings = new List<string>();
            foreach (var token in _tokens)
            {
                tokenStrings.Add(token.ToString());
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
                // Для \r\n обрабатываем в ReadEndOfLineToken
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
        Digit,
        Letter,
        SpecialSymbol,
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
            string typeName;
            switch (Type)
            {
                case TokenType.Digit:
                    typeName = "Digit";
                    break;
                case TokenType.Letter:
                    typeName = "Letter";
                    break;
                case TokenType.SpecialSymbol:
                    typeName = "Special Symbol";
                    break;
                case TokenType.EndRow:
                    typeName = "EndRow";
                    break;
                case TokenType.EndText:
                    typeName = "EndText";
                    break;
                default:
                    typeName = "Unknown";
                    break;
            }

            // Для цифр и букв выводим каждый символ отдельно
            if (Type == TokenType.Digit || Type == TokenType.Letter)
            {
                List<string> parts = new List<string>();
                foreach (char c in Value)
                {
                    parts.Add($"{typeName} {c}");
                }
                return string.Join(", ", parts);
            }

            // Для специальных символов выводим тип и значение
            if (Type == TokenType.SpecialSymbol)
            {
                return $"{typeName} {Value}";
            }

            // Для EndRow и EndText выводим только тип
            return typeName;
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
}
