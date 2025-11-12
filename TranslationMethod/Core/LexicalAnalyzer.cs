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

        private readonly HashSet<char> _alphabet = new HashSet<char>
        {
            '0', '1', 'a', 'b', 'c', 'd', '(', ')', '!', ':', '=', '/', ' ', '\r', '\n'
        };

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
                    char currentChar = GetCurrentChar();

                    if (!_alphabet.Contains(currentChar))
                    {
                        return AnalysisResult.Error($"Символ не принадлежит алфавиту: '{(currentChar == '\r' ? "\\r" : currentChar == '\n' ? "\\n" : currentChar.ToString())}' " +
                                                   $"(позиция: строка {_currentLine}, столбец {_currentColumn})");
                    }

                    if (currentChar == '/' && PeekNextChar() == '/')
                    {
                        SkipComment();
                        continue;
                    }

                    if (currentChar == '\r' || currentChar == '\n')
                    {
                        ReadEndOfLineToken();
                        continue;
                    }

                    Token token = ReadNextToken();
                    if (token != null)
                    {
                        _tokens.Add(token);
                    }
                }

                _tokens.Add(new Token(TokenType.EndText, "EndText", _currentLine, _currentColumn));

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

        private void ReadEndOfLineToken()
        {
            int startLine = _currentLine;
            int startColumn = _currentColumn;
            char currentChar = GetCurrentChar();

            if (currentChar == '\r' && PeekNextChar() == '\n')
            {
                MoveToNextChar();
                MoveToNextChar();
            }
            else
            {
                MoveToNextChar();
            }

            _tokens.Add(new Token(TokenType.EndRow, "EndRow", startLine, startColumn));
        }

        private Token ReadNextToken()
        {
            if (_currentPosition >= _sourceText.Length)
                return null;

            char currentChar = GetCurrentChar();
            int startLine = _currentLine;
            int startColumn = _currentColumn;

            if (currentChar == '0' || currentChar == '1')
            {
                return ReadDigitToken(startLine, startColumn);
            }

            if (currentChar >= 'a' && currentChar <= 'd')
            {
                return ReadLetterToken(startLine, startColumn);
            }

            if (_specialSymbols.Contains(currentChar))
            {
                return ReadSpecialSymbolToken(startLine, startColumn);
            }

            MoveToNextChar();
            return null;
        }

        private Token ReadDigitToken(int line, int column)
        {
            StringBuilder value = new StringBuilder();

            while (_currentPosition < _sourceText.Length &&
                   (GetCurrentChar() == '0' || GetCurrentChar() == '1'))
            {
                value.Append(GetCurrentChar());
                MoveToNextChar();
            }

            return new Token(TokenType.Digit, value.ToString(), line, column);
        }

        private Token ReadLetterToken(int line, int column)
        {
            StringBuilder value = new StringBuilder();

            while (_currentPosition < _sourceText.Length &&
                   GetCurrentChar() >= 'a' && GetCurrentChar() <= 'd')
            {
                value.Append(GetCurrentChar());
                MoveToNextChar();
            }

            return new Token(TokenType.Letter, value.ToString(), line, column);
        }

        private Token ReadSpecialSymbolToken(int line, int column)
        {
            char symbol = GetCurrentChar();
            MoveToNextChar();
            return new Token(TokenType.SpecialSymbol, symbol.ToString(), line, column);
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

        private char GetCurrentChar()
        {
            if (_currentPosition >= _sourceText.Length)
                throw new IndexOutOfRangeException();

            return _sourceText[_currentPosition];
        }

        private char? PeekNextChar()
        {
            int nextPosition = _currentPosition + 1;
            if (nextPosition >= _sourceText.Length)
                return null;

            return _sourceText[nextPosition];
        }

        private void MoveToNextChar()
        {
            if (_currentPosition >= _sourceText.Length)
                throw new IndexOutOfRangeException();

            char currentChar = _sourceText[_currentPosition];

            if (currentChar == '\r')
            {
                if (PeekNextChar() == '\n')
                {
                    _currentPosition++;
                }

                _currentLine++;
                _currentColumn = 1;
            }
            else if (currentChar == '\n')
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

        private void SkipComment()
        {
            // Пропускаем два символа '//'
            MoveToNextChar();
            MoveToNextChar();

            // Пропускаем все символы до конца строки
            while (_currentPosition < _sourceText.Length &&
                   _sourceText[_currentPosition] != '\r' &&
                   _sourceText[_currentPosition] != '\n')
            {
                MoveToNextChar();
            }

            // Если после комментария идет перенос строки, обрабатываем его
            if (_currentPosition < _sourceText.Length &&
                (_sourceText[_currentPosition] == '\r' || _sourceText[_currentPosition] == '\n'))
            {
                ReadEndOfLineToken();
            }
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

            // Для специальных символов, EndRow и EndText выводим только тип
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
