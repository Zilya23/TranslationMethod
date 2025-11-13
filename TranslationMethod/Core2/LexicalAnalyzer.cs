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

        // Состояния автомата для чисел
        private enum NumberState
        {
            Start,      // N0
            After0,     // N1
            After00,    // N2
            After000,   // N3
            After001,   // N4
            After0011,  // N5
            After00110, // N6
            Final       // N7
        }

        // Состояния автомата для идентификаторов
        private enum IdentifierState
        {
            Start,      // I0
            AfterA,     // I1
            AfterAB,    // I2
            Final       // I3
        }

        // Состояния автомата для комментариев
        private enum CommentState
        {
            Start,      // C0
            AfterSlash, // C1
            AfterDoubleSlash, // C2
            InComment,  // C3
            Final       // C4
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

                    // Пропускаем пробелы
                    if (char.IsWhiteSpace(currentChar) && currentChar != '\n' && currentChar != '\r')
                    {
                        MoveToNextChar();
                        continue;
                    }

                    // Обработка переносов строк
                    if (currentChar == '\r' || currentChar == '\n')
                    {
                        ReadEndOfLineToken();
                        continue;
                    }

                    // Проверка на комментарий
                    if (currentChar == '/')
                    {
                        if (ProcessComment())
                            continue;
                    }

                    // Проверка на зарезервированные слова
                    Token reservedToken = ProcessReservedWord();
                    if (reservedToken != null)
                    {
                        _tokens.Add(reservedToken);
                        continue;
                    }

                    // Проверка на числа
                    if (currentChar == '0' || currentChar == '1')
                    {
                        Token numberToken = ProcessNumber();
                        if (numberToken != null)
                        {
                            _tokens.Add(numberToken);
                            continue;
                        }
                    }

                    // Проверка на идентификаторы
                    if (currentChar >= 'a' && currentChar <= 'd')
                    {
                        Token identifierToken = ProcessIdentifier();
                        if (identifierToken != null)
                        {
                            _tokens.Add(identifierToken);
                            continue;
                        }
                    }

                    // Если ни один автомат не принял символ - ошибка
                    return AnalysisResult.Error($"Неизвестный символ: '{currentChar}' " +
                                               $"(позиция: строка {_currentLine}, столбец {_currentColumn})");
                }

                string resultMessage = "Текст верен";
                if (_tokens.Count > 0)
                {
                    resultMessage += "\n" + GetTokensString();
                }
                return AnalysisResult.Success(resultMessage);
            }
            catch (LexicalException ex)
            {
                return AnalysisResult.Error($"Лексическая ошибка: {ex.Message} (строка {ex.Line}, столбец {ex.Column})");
            }
            catch (IndexOutOfRangeException)
            {
                return AnalysisResult.Error("Попытка чтения за пределами исходного текста");
            }
        }

        private bool ProcessComment()
        {
            int startLine = _currentLine;
            int startColumn = _currentColumn;
            int startPosition = _currentPosition;

            CommentState state = CommentState.Start;

            while (_currentPosition < _sourceText.Length)
            {
                char currentChar = _sourceText[_currentPosition];

                switch (state)
                {
                    case CommentState.Start:
                        if (currentChar == '/')
                        {
                            state = CommentState.AfterSlash;
                            MoveToNextChar();
                        }
                        else
                        {
                            _currentPosition = startPosition;
                            _currentLine = startLine;
                            _currentColumn = startColumn;
                            return false;
                        }
                        break;

                    case CommentState.AfterSlash:
                        if (currentChar == '/')
                        {
                            state = CommentState.AfterDoubleSlash;
                            MoveToNextChar();
                        }
                        else
                        {
                            _currentPosition = startPosition;
                            _currentLine = startLine;
                            _currentColumn = startColumn;
                            return false;
                        }
                        break;

                    case CommentState.AfterDoubleSlash:
                        state = CommentState.InComment;
                        while (_currentPosition < _sourceText.Length &&
                               _sourceText[_currentPosition] != '\r' &&
                               _sourceText[_currentPosition] != '\n')
                        {
                            MoveToNextChar();
                        }
                        state = CommentState.Final;
                        return true;

                    case CommentState.Final:
                        return true;
                }
            }

            return state == CommentState.Final;
        }

        private Token ProcessNumber()
        {
            int startLine = _currentLine;
            int startColumn = _currentColumn;
            int startPosition = _currentPosition;

            StringBuilder value = new StringBuilder();

            // Считываем все цифры
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

            // Проверяем соответствие формату (000)*001(100)* с помощью детерминированной проверки
            if (IsValidNumber(number))
            {
                return new Token(TokenType.Number, number, startLine, startColumn);
            }
            else
            {
                _currentPosition = startPosition;
                _currentLine = startLine;
                _currentColumn = startColumn;
                throw new LexicalException($"Некорректный формат числа: '{number}' (ожидается: (000)*001(100)*)", startLine, startColumn);
            }
        }

        private bool IsValidNumber(string number)
        {
            if (string.IsNullOrEmpty(number)) return false;

            int i = 0;
            int n = number.Length;

            // Пропускаем (000)*
            while (i + 2 < n && number[i] == '0' && number[i + 1] == '0' && number[i + 2] == '0')
            {
                i += 3;
            }

            // Проверяем 001
            if (i + 2 >= n || number[i] != '0' || number[i + 1] != '0' || number[i + 2] != '1')
                return false;

            i += 3;

            // Проверяем (100)*
            while (i + 2 < n && number[i] == '1' && number[i + 1] == '0' && number[i + 2] == '0')
            {
                i += 3;
            }

            // Должны были обработать все символы
            return i == n;
        }

        private Token ProcessIdentifier()
        {
            int startLine = _currentLine;
            int startColumn = _currentColumn;
            int startPosition = _currentPosition;

            IdentifierState state = IdentifierState.Start;
            StringBuilder value = new StringBuilder();

            while (_currentPosition < _sourceText.Length)
            {
                char currentChar = _sourceText[_currentPosition];

                if (currentChar < 'a' || currentChar > 'd')
                    break;

                switch (state)
                {
                    case IdentifierState.Start: // I0
                        if (currentChar == 'a')
                            state = IdentifierState.AfterA;
                        else
                            state = IdentifierState.Start; // Сброс
                        break;

                    case IdentifierState.AfterA: // I1
                        if (currentChar == 'b')
                            state = IdentifierState.AfterAB;
                        else
                            state = IdentifierState.Start; // Сброс
                        break;

                    case IdentifierState.AfterAB: // I2
                        if (currentChar >= 'a' && currentChar <= 'd')
                            state = IdentifierState.Final; // Остаемся в конечном состоянии
                        break;

                    case IdentifierState.Final: // I3
                        // Остаемся в конечном состоянии для любых a,b,c,d
                        break;
                }

                value.Append(currentChar);
                MoveToNextChar();
            }

            // Проверяем, достигли ли мы конечного состояния
            if (state == IdentifierState.Final && value.Length >= 2)
            {
                return new Token(TokenType.Identifier, value.ToString(), startLine, startColumn);
            }
            else
            {
                _currentPosition = startPosition;
                _currentLine = startLine;
                _currentColumn = startColumn;
                throw new LexicalException($"Некорректный идентификатор: '{value}' " +
                                         $"(должен начинаться с 'ab' и содержать только a,b,c,d)", startLine, startColumn);
            }
        }

        private Token ProcessReservedWord()
        {
            int startPosition = _currentPosition;
            int startLine = _currentLine;
            int startColumn = _currentColumn;

            // Проверка многосимвольных зарезервированных слов
            if (_currentPosition + 1 < _sourceText.Length)
            {
                string twoChars = _sourceText.Substring(_currentPosition, 2);
                if (twoChars == ":=")
                {
                    MoveToNextChar();
                    MoveToNextChar();
                    return new Token(TokenType.Assignment, ":=", startLine, startColumn);
                }
                if (twoChars == "!:")
                {
                    MoveToNextChar();
                    MoveToNextChar();
                    return new Token(TokenType.ConditionSeparator, "!:", startLine, startColumn);
                }
            }

            // Проверка односимвольных зарезервированных слов
            char currentChar = _sourceText[_currentPosition];
            switch (currentChar)
            {
                case '(':
                    MoveToNextChar();
                    return new Token(TokenType.LeftParenthesis, "(", startLine, startColumn);
                case ')':
                    MoveToNextChar();
                    return new Token(TokenType.RightParenthesis, ")", startLine, startColumn);
                case '!':
                    MoveToNextChar();
                    return new Token(TokenType.Exclamation, "!", startLine, startColumn);
                case ':':
                    MoveToNextChar();
                    return new Token(TokenType.Colon, ":", startLine, startColumn);
            }

            _currentPosition = startPosition;
            return null;
        }

        private void ReadEndOfLineToken()
        {
            int startLine = _currentLine;
            int startColumn = _currentColumn;

            MoveToNextChar();

            if (_tokens.Count > 0 && _tokens[_tokens.Count - 1].Type != TokenType.EndRow)
            {
                _tokens.Add(new Token(TokenType.EndRow, "EndRow", startLine, startColumn));
            }
        }

        private string GetTokensString()
        {
            List<string> tokenStrings = new List<string>();
            foreach (var token in _tokens)
            {
                if (token.Type != TokenType.EndRow)
                {
                    tokenStrings.Add($"<{token.Value}, {token.Type}>");
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
                _currentPosition++;
            }
            else if (currentChar == '\r')
            {
                if (_currentPosition + 1 < _sourceText.Length && _sourceText[_currentPosition + 1] == '\n')
                {
                    _currentPosition++;
                    _currentLine++;
                    _currentColumn = 1;
                    _currentPosition++;
                }
                else
                {
                    _currentLine++;
                    _currentColumn = 1;
                    _currentPosition++;
                }
            }
            else
            {
                _currentColumn++;
                _currentPosition++;
            }
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
