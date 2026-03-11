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
        InConst,
        SpaceAfterConst,
        InInt,
        SpaceAfterInt,
        InID,
        SpaceAfterID,
        Equals,
        SpaceAfterEquals,
        InNumber,
        EndLine,
        Error
    }

    public enum TokenType
    {
        Startline = 0,
        Keyword_const = 1,
        Space_after_const = 2,
        Keyword_int = 3,
        Space_after_int = 4,
        Identifier = 5,
        Space_after_id = 6,
        Operator_eq = 7,
        Space_after_op_eq = 8,
        Number = 9,
        Endline = 10,
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

            for (int i = 0; i < line.Length; i++)
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
                        currentState = State.InConst;
                    }
                    else
                    {
                        currentState = State.Error;
                        result.Errors.Add(new LexicalError(i, lineIndex, "Unexpected character", "Lexeme: start. Met: '" + c + "', expected: letter"));
                    }
                }
                if (currentState == State.InConst)
                {
                    if (letters.Contains(c))
                    {
                        subline += c;
                    }
                    else if (c == whitespace)
                    {
                        if (subline == "const" || subline == "constexpr")
                        {
                            result.Tokens.Add(new Token(TokenType.Keyword_const, "const", subline, lineIndex, posBeg, i - 1));
                            subline = "";
                            posBeg = i;
                            currentState = State.SpaceAfterConst;
                        }
                    }
                    else
                    {
                        currentState = State.Error;
                        result.Errors.Add(new LexicalError(i, lineIndex, "Unexpected character", "Lexeme: const. Met: '" + subline + c + "', expected: 'const ' or 'constexpr '"));
                    }

                }
                if (currentState == State.SpaceAfterConst)
                {
                    if (c == ' ')
                    {
                        result.Tokens.Add(new Token(TokenType.Space_after_const, "Space", " ", lineIndex, posBeg, i));
                        subline = "";
                        posBeg = i + 1;
                        currentState = State.InInt;
                        continue;
                    }
                    else
                    {
                        currentState = State.Error;
                        result.Errors.Add(new LexicalError(i, lineIndex, "Unexpected character", "Lexeme: space. Met: '" + c + "', expected: ' '"));
                    }
                }
                if (currentState == State.InInt)
                {
                    if (letters.Contains(c))
                    {
                        subline += c;
                    }
                    else if (c == whitespace)
                    {
                        if (subline == "int")
                        {
                            result.Tokens.Add(new Token(TokenType.Keyword_int, "int", subline, lineIndex, posBeg, i - 1));
                            subline = "";
                            posBeg = i;
                            currentState = State.SpaceAfterInt;
                        }
                    }
                    else
                    {
                        currentState = State.Error;
                        result.Errors.Add(new LexicalError(i, lineIndex, "Unexpected character", "Lexeme: int. Met: '" + subline + c + "', expected: 'int '"));
                    }

                }
                if (currentState == State.SpaceAfterInt)
                {
                    if (c == ' ')
                    {
                        result.Tokens.Add(new Token(TokenType.Space_after_int, "Space", " ", lineIndex, posBeg, i));
                        subline = "";
                        posBeg = i + 1;
                        currentState = State.InID;
                        continue;
                    }
                    else
                    {
                        currentState = State.Error;
                        result.Errors.Add(new LexicalError(i, lineIndex, "Unexpected character", "Lexeme: space. Met: '" + c + "', expected: ' '"));
                    }
                }
                if (currentState == State.InID)
                {
                    if (subline == "" && digits.Contains(c))
                    {
                        currentState = State.Error;
                        result.Errors.Add(new LexicalError(i, lineIndex, "Unexpected character", "Lexeme: identifier. Met: '" + c + "', expected: letter"));
                    }
                    else if (letters.Contains(c) || digits.Contains(c))
                    {
                        subline += c;
                    }
                    else
                    {
                        if (c == whitespace)
                        {
                            result.Tokens.Add(new Token(TokenType.Identifier, "identifier", subline, lineIndex, posBeg, i - 1));
                            subline = "";
                            posBeg = i;
                            currentState = State.SpaceAfterID;
                        }
                        else
                        {
                            currentState = State.Error;
                            result.Errors.Add(new LexicalError(i, lineIndex, "Unexpected character", "Lexeme: identifier. Met: '" + subline + c + "', expected: '[letters or digits] '"));
                        }
                    }
                }
                if (currentState == State.SpaceAfterID)
                {
                    if (c == ' ')
                    {
                        result.Tokens.Add(new Token(TokenType.Space_after_id, "Space", " ", lineIndex, posBeg, i));
                        subline = "";
                        posBeg = i + 1;
                        currentState = State.Equals;
                        continue;
                    }
                    else
                    {
                        currentState = State.Error;
                        result.Errors.Add(new LexicalError(i, lineIndex, "Unexpected character", "Lexeme: space. Met: '" + c + "', expected: ' '"));
                    }
                }
                if (currentState == State.Equals)
                {
                    if (c == operator_equals)
                    {
                        result.Tokens.Add(new Token(TokenType.Operator_eq, "Equals", "=", lineIndex, posBeg, i));
                        subline = "";
                        posBeg = i + 1;
                        currentState = State.SpaceAfterEquals;
                        continue;
                    }
                    else
                    {
                        currentState = State.Error;
                        result.Errors.Add(new LexicalError(i, lineIndex, "Unexpected character", "Lexeme: equals. Met: '" + c + "', expected: '='"));
                    }
                }
                if (currentState == State.SpaceAfterEquals)
                {
                    if (c == ' ')
                    {
                        result.Tokens.Add(new Token(TokenType.Space_after_op_eq, "Space", " ", lineIndex, posBeg, i));
                        subline = "";
                        posBeg = i + 1;
                        currentState = State.InNumber;
                        continue;
                    }
                    else
                    {
                        currentState = State.Error;
                        result.Errors.Add(new LexicalError(i, lineIndex, "Unexpected character", "Lexeme: space. Met: '" + c + "', expected: ' '"));
                    }
                }
                if (currentState == State.InNumber)
                {
                    if (digits.Contains(c))
                    {
                        subline += c;
                    }
                    else
                    {
                        if (c == operator_separator)
                        {
                            result.Tokens.Add(new Token(TokenType.Number, "number", subline, lineIndex, posBeg, i - 1));
                            subline = "";
                            posBeg = i;
                            currentState = State.EndLine;
                        }
                        else
                        {
                            currentState = State.Error;
                            result.Errors.Add(new LexicalError(i, lineIndex, "Unexpected character", "Lexeme: number. Met: '" + subline + c + "', expected: '[digits];'"));
                        }
                    }
                }
                if (currentState == State.EndLine)
                {
                    if (c == operator_separator)
                    {
                        result.Tokens.Add(new Token(TokenType.Endline, "Separator", ";", lineIndex, posBeg, i));
                        subline = "";
                        posBeg = i + 1;
                        currentState = State.Start;
                    }
                    else
                    {
                        currentState = State.Error;
                        result.Errors.Add(new LexicalError(i, lineIndex, "Unexpected character", "Lexeme: separator. Met: '" + c + "', expected: ';'"));
                    }
                }
            }
        }

    }
}