using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace LexicalAnalyser
{

    // Состояния конечного автомата
    
    public enum State
    {
        Start,
        LetterDigit,
        Digit,
        Space,
        Equal,
        Negative,
        End,
        Error
    }

    public enum TokenType
    {
        Startline = 0,
        Keyword_const = 1,
        Space = 2,
        Keyword_int = 3,
        Identifier = 4,
        Operator_eq = 5,
        Operator_neg = 6,
        Number = 7,
        Endline = 8,
        Unknown = 99 // Неизвестные/ошибочные символы
    }

    // Лексема
    public class Token
    {
        public TokenType Code { get; set; }
        public string Type { get; set; }
        public string Value { get; set; }
        public int Line { get; set; }
        public int StartPos { get; set; }
        public int EndPos { get; set; }

        public Token(TokenType code, string type, string value, int line, int startpos, int endpos)
        {
            Code = code;
            Type = type;
            Value = value;
            Line = line;
            StartPos = startpos;
            EndPos = endpos;
        }

        public override string ToString()
        {
            return $"{(int)Code}\n{Type}\n{Value}\nLine {Line}, {StartPos}-{EndPos}";
        }
    }

    // Ошибки
    public class LexicalError
    {
        public int Position { get; set; }
        public int Line { get; set; }
        public string ErrorCode { get; set; }
        public string Description { get; set; }
        public LexicalError(int position, int line, string errorCode, string description)
        {
            Position = position;
            Line = line;
            ErrorCode = errorCode;
            Description = description;
        }
        public override string ToString()
        {
            return $"Line {Line}, pos {Position}\n{ErrorCode}\n{Description}";
        }
    }

    public class AnalysisResult
    {
        public List<Token> Tokens { get; set; } = new List<Token>();
        public List<LexicalError> Errors { get; set; } = new List<LexicalError>();
    }

    // Лексический анализатор
    public class ScannerFSM
    {
        private HashSet<char> letters;
        private HashSet<char> digits;
        private char whitespace = ' ';
        private char operator_equals = '=';
        private char operator_negative = '-';
        private char operator_separator = ';';

        private string keyword_const = "const";
        private string keyword_int = "int";

        private State currentState;

        public ScannerFSM()
        {
            InitializeCharSets();
        }

        private void InitializeCharSets()
        {
            letters = new HashSet<char>();
            for (char c = 'a'; c <= 'z'; c++) letters.Add(c);
            for (char c = 'A'; c <= 'Z'; c++) letters.Add(c);
            letters.Add('_');

            digits = new HashSet<char>();
            for (char c = '0'; c <= '9'; c++) digits.Add(c);
        }



        // Основной метод анализа

        public AnalysisResult Analyze(string text)
        {
            var result = new AnalysisResult();

            if (string.IsNullOrEmpty(text))
                return result;

            string[] lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            {
                string line = lines[lineIndex];

                AnalyzeLine(line, lineIndex, result);
            }

            return result;
        }



        private void AnalyzeLine(string line, int lineIndex, AnalysisResult result)
        {
            currentState = State.Start;
            int posBeg = 0;
            string subline = "";
            int i = 0;

            while (i < line.Length)
            {
                char c = line[i];
                if (currentState == State.Error)
                {
                    break;
                }
                if (currentState == State.Start)
                {
                    if (letters.Contains(c))
                    {
                        currentState = State.LetterDigit;
                    }
                    else if (digits.Contains(c))
                    {
                        currentState = State.Digit;
                    }
                    else if(c == ' ')
                    {
                        currentState = State.Space;
                    }
                    else if (c == '=')
                    {
                        currentState = State.Equal;
                    }
                    else if (c == '-')
                    {
                        currentState = State.Negative;
                    }
                    else if (c == ';')
                    {
                        currentState = State.End;
                    }
                    else
                    {
                        currentState = State.Error;
                        result.Errors.Add(new LexicalError(i, lineIndex, "Forbidden character", "Lexeme: start. Met: '" + c + "', expected: letter, digit, ' ', '=', '-', ';'"));
                    }
                }
                if (currentState == State.LetterDigit)
                {
                    while ((letters.Contains(c) || digits.Contains(c)))
                    {
                        subline += c;
                        i++;
                        if (i >= line.Length) break;
                        c = line[i];
                    }
                    if (subline == "const" || subline == "constexpr")
                    {
                        result.Tokens.Add(new Token(TokenType.Keyword_const, "Const", subline, lineIndex, posBeg, i - 1));
                        subline = "";
                        posBeg = i;
                        currentState = State.Start;
                    }
                    else if (subline == "int")
                    {
                        result.Tokens.Add(new Token(TokenType.Keyword_int, "Int", subline, lineIndex, posBeg, i - 1));
                        subline = "";
                        posBeg = i;
                        currentState = State.Start;
                    }
                    else
                    {
                        result.Tokens.Add(new Token(TokenType.Identifier, "Identifier", subline, lineIndex, posBeg, i - 1));
                        subline = "";
                        posBeg = i;
                        currentState = State.Start;
                    }
                    continue;
                }

                if (currentState == State.Digit)
                {
                    while (digits.Contains(c))
                    {
                        subline += c;
                        i++;
                        if (i >= line.Length) break;
                        c = line[i];
                    }
                    result.Tokens.Add(new Token(TokenType.Number, "Number", subline, lineIndex, posBeg, i - 1));
                    subline = "";
                    posBeg = i;
                    currentState = State.Start;
                    continue;
                }

                if (currentState == State.Space)
                {
                    result.Tokens.Add(new Token(TokenType.Space, "Space", " ", lineIndex, posBeg, i));
                    subline = "";
                    i++;
                    posBeg = i;
                    currentState = State.Start;
                    continue;
                }

                if (currentState == State.Equal)
                {
                    result.Tokens.Add(new Token(TokenType.Operator_eq, "Equals", "=", lineIndex, posBeg, i));
                    subline = "";
                    i++;
                    posBeg = i;
                    currentState = State.Start;
                    continue;
                }

                if (currentState == State.Negative)
                {
                    result.Tokens.Add(new Token(TokenType.Operator_neg, "Negative", "-", lineIndex, posBeg, i));
                    subline = "";
                    i++;
                    posBeg = i;
                    currentState = State.Start;
                    continue;
                }

                if (currentState == State.End)
                {
                    result.Tokens.Add(new Token(TokenType.Endline, "Separator", ";", lineIndex, posBeg, i));
                    subline = "";
                    i++;
                    posBeg = i;
                    currentState = State.Start;
                    continue;
                }
            }
                

        }

    }
}