using LexicalAnalyser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using System.Windows.Forms;

namespace TFLC_GUI
{
    internal class Parcer
    {
        public enum LexemState
        {
            Start,
            InConst,
            SpaceAfterConst,
            InInt,
            SpaceAfterInt,
            InID,
            Equals,
            Negative,
            InNumber,
            EndLine,
            Error
        }

        public class ParcerError
        {
            public int Position { get; set; }
            public int Line { get; set; }
            public string ErrorCode { get; set; }
            public string Description { get; set; }
            public ParcerError(int position, int line, string errorCode, string description)
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

        public class ParcerResult
        {
            public List<ParcerError> Errors { get; set; } = new List<ParcerError>();
        }

        public ParcerResult Parce(AnalysisResult input)
        {
            var result = new ParcerResult();
            LexemState currentLexem = LexemState.Start;
            int initial_line = 0;

            if (input.Errors.Count > 0 || input.Tokens.Count == 0)
                return result;

            List<Token> Tokens = input.Tokens;
            int i = 0;
            while (i < Tokens.Count)
            {
                if (currentLexem == LexemState.Start)
                {
                    if (Tokens[i].Code == TokenType.Keyword_const)
                    {
                        currentLexem = LexemState.InConst;
                        initial_line = Tokens[i].Line;
                    }
                    else
                    {
                        result.Errors.Add(new ParcerError(Tokens[i].StartPos, Tokens[i].Line, "Unexpected lexeme", "Lexeme: <start>. Met: '" + Tokens[i].Value + "', expected: 'const'"));
                    }
                }
                else if (currentLexem == LexemState.InConst)
                {
                    if (Tokens[i].Code == TokenType.Space)
                        currentLexem = LexemState.SpaceAfterConst;
                    else
                    {
                        result.Errors.Add(new ParcerError(Tokens[i].StartPos, Tokens[i].Line, "Unexpected lexeme", "Lexeme: const. Met: '" + Tokens[i].Value + "', expected: ' '"));
                    }
                }
                else if (currentLexem == LexemState.SpaceAfterConst)
                {
                    if (Tokens[i].Code == TokenType.Keyword_int)
                        currentLexem = LexemState.InInt;
                    else
                    {
                        result.Errors.Add(new ParcerError(Tokens[i].StartPos, Tokens[i].Line, "Unexpected lexeme", "Lexeme: space after const. Met: '" + Tokens[i].Value + "', expected: 'int'"));
                    }
                }
                else if (currentLexem == LexemState.InInt)
                {
                    if (Tokens[i].Code == TokenType.Space)
                        currentLexem = LexemState.SpaceAfterInt;
                    else
                    {
                        result.Errors.Add(new ParcerError(Tokens[i].StartPos, Tokens[i].Line, "Unexpected lexeme", "Lexeme: int. Met: '" + Tokens[i].Value + "', expected: ' '"));
                    }
                }
                else if (currentLexem == LexemState.SpaceAfterInt)
                {
                    if (Tokens[i].Code == TokenType.Identifier)
                        currentLexem = LexemState.InID;
                    else
                    {
                        result.Errors.Add(new ParcerError(Tokens[i].StartPos, Tokens[i].Line, "Unexpected lexeme", "Lexeme: space after int. Met: '" + Tokens[i].Value + "', expected: liter{liter|digit}"));
                    }
                }
                else if (currentLexem == LexemState.InID)
                {
                    if (Tokens[i].Code == TokenType.Operator_eq)
                        currentLexem = LexemState.Equals;
                    else
                    {
                        result.Errors.Add(new ParcerError(Tokens[i].StartPos, Tokens[i].Line, "Unexpected lexeme", "Lexeme: index. Met: '" + Tokens[i].Value + "', expected: '='"));
                    }
                }
                else if (currentLexem == LexemState.Equals)
                {
                    if (Tokens[i].Code == TokenType.Operator_neg)
                        currentLexem = LexemState.Negative;
                    else if (Tokens[i].Code == TokenType.Number)
                        currentLexem = LexemState.InNumber;
                    else
                    {
                        result.Errors.Add(new ParcerError(Tokens[i].StartPos, Tokens[i].Line, "Unexpected lexeme", "Lexeme: equals. Met: '" + Tokens[i].Value + "', expected: '-' or 'digit{digit}'"));
                    }
                }
                else if (currentLexem == LexemState.Negative)
                {
                    if (Tokens[i].Code == TokenType.Number)
                        currentLexem = LexemState.InNumber;
                    else
                    {
                        result.Errors.Add(new ParcerError(Tokens[i].StartPos, Tokens[i].Line, "Unexpected lexeme", "Lexeme: negative. Met: '" + Tokens[i].Value + "', expected: 'digit{digit}'"));
                    }
                }
                else if (currentLexem == LexemState.InNumber)
                {
                    if (Tokens[i].Code == TokenType.Endline)
                    {
                        currentLexem = LexemState.EndLine;
                        continue;
                    }
                    else
                    {
                        result.Errors.Add(new ParcerError(Tokens[i].StartPos, Tokens[i].Line, "Unexpected lexeme", "Lexeme: number. Met: '" + Tokens[i].Value + "', expected: ';'"));
                    }
                }
                else if (currentLexem == LexemState.EndLine)
                {
                    if (initial_line != Tokens[i].Line)
                    {
                        result.Errors.Add(new ParcerError(Tokens[i].StartPos, Tokens[i].Line, "Multi-line expression", "Lexeme: end. Expression started on line: " + initial_line + ", current line: " + Tokens[i].Line));
                    }
                    currentLexem = LexemState.Start;
                }
                i++;
            }

            if (currentLexem != LexemState.Start)
            {
                i--;
                result.Errors.Add(new ParcerError(Tokens[i].EndPos, Tokens[i].Line, "Missing lexeme(-s)", "Line did not end with correct lexeme. Current lexeme: '" + currentLexem + "', expected: ('Start')"));
            }


            return result;
        }

    }
}
