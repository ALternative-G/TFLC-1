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

        public class ParcerResultList
        {
            public List<ParcerResult> ResultsList { get; set; } = new List<ParcerResult>();
        }

        public ParcerResultList ParcerRecursion(List<Token> Tokens, LexemState currentLexem, ParcerResult PrevErrors, int initial_line) 
        {
            var result = new ParcerResultList();
            var resultErrors = new ParcerResult();
            foreach (var errors in PrevErrors.Errors)
            {
                resultErrors.Errors.Add(errors);
            }
            if (resultErrors.Errors.Count > 10)
            {
                result.ResultsList.Add(resultErrors);
                return result;
            }



            if (Tokens.Count == 0)
            {
                if (currentLexem == LexemState.EndLine)
                {
                    result.ResultsList.Add(resultErrors);
                    return result;
                }
                else
                {
                    resultErrors.Errors.Add(new ParcerError(0, initial_line, "Missing lexeme(-s)", "Line did not end with correct lexeme. Current lexeme: '" + currentLexem + "', expected: " + (currentLexem + 1)));

                    result.ResultsList.AddRange(ParcerRecursion(Tokens, currentLexem+1, resultErrors, initial_line).ResultsList); // вставка токена на правильный

                    return result;
                }
            }

            if (initial_line != Tokens[0].Line)
            {
                resultErrors.Errors.Add(new ParcerError(Tokens[0].StartPos, Tokens[0].Line, "Multi-line expression", "Lexeme: end. Expression started on line: " + initial_line + ", current line: " + Tokens[0].Line));

                result.ResultsList.Add(resultErrors);
                return result;
            }

            if (currentLexem == LexemState.Start)
            {
                if (Tokens[0].Code == TokenType.Keyword_const)
                {
                    currentLexem = LexemState.InConst;
                    initial_line = Tokens[0].Line;
                    return ParcerRecursion(Tokens.GetRange(1, Tokens.Count - 1), currentLexem, PrevErrors, initial_line);
                }
                else
                {
                    resultErrors.Errors.Add(new ParcerError(Tokens[0].StartPos, Tokens[0].Line, "Unexpected lexeme", "Lexeme: <start>. Met: '" + Tokens[0].Value + "', expected: 'const'"));
                    result.ResultsList.AddRange(ParcerRecursion(Tokens.GetRange(1, Tokens.Count - 1), currentLexem, resultErrors, initial_line).ResultsList); // пропуск токена
                    initial_line = Tokens[0].Line;
                    result.ResultsList.AddRange(ParcerRecursion(Tokens.GetRange(1, Tokens.Count - 1), LexemState.InConst, resultErrors, initial_line).ResultsList); // изменение токена на правильный
                    result.ResultsList.AddRange(ParcerRecursion(Tokens, LexemState.InConst, resultErrors, initial_line).ResultsList); // вставка токена на правильный

                    return result;
                }
            }
            else if (currentLexem == LexemState.InConst)
            {
                if (Tokens[0].Code == TokenType.Space)
                {
                    currentLexem = LexemState.SpaceAfterConst;
                    return ParcerRecursion(Tokens.GetRange(1, Tokens.Count - 1), currentLexem, PrevErrors, initial_line);
                }
                else
                {
                    resultErrors.Errors.Add(new ParcerError(Tokens[0].StartPos, Tokens[0].Line, "Unexpected lexeme", "Lexeme: const. Met: '" + Tokens[0].Value + "', expected: ' '"));

                    result.ResultsList.AddRange(ParcerRecursion(Tokens.GetRange(1, Tokens.Count - 1), currentLexem, resultErrors, initial_line).ResultsList); // пропуск токена
                    result.ResultsList.AddRange(ParcerRecursion(Tokens.GetRange(1, Tokens.Count - 1), LexemState.SpaceAfterConst, resultErrors, initial_line).ResultsList); // изменение токена на правильный
                    result.ResultsList.AddRange(ParcerRecursion(Tokens, LexemState.SpaceAfterConst, resultErrors, initial_line).ResultsList); // вставка токена на правильный

                    return result;
                }
            }
            else if (currentLexem == LexemState.SpaceAfterConst)
            {
                if (Tokens[0].Code == TokenType.Keyword_int)
                {
                    currentLexem = LexemState.InInt;
                    return ParcerRecursion(Tokens.GetRange(1, Tokens.Count - 1), currentLexem, PrevErrors, initial_line);
                }
                else
                {
                    resultErrors.Errors.Add(new ParcerError(Tokens[0].StartPos, Tokens[0].Line, "Unexpected lexeme", "Lexeme: space after const. Met: '" + Tokens[0].Value + "', expected: 'int'"));


                    result.ResultsList.AddRange(ParcerRecursion(Tokens.GetRange(1, Tokens.Count - 1), currentLexem, resultErrors, initial_line).ResultsList); // пропуск токена
                    result.ResultsList.AddRange(ParcerRecursion(Tokens.GetRange(1, Tokens.Count - 1), LexemState.InInt, resultErrors, initial_line).ResultsList); // изменение токена на правильный
                    result.ResultsList.AddRange(ParcerRecursion(Tokens, LexemState.InInt, resultErrors, initial_line).ResultsList); // вставка токена на правильный

                    return result;
                }
            }
            else if (currentLexem == LexemState.InInt)
            {
                if (Tokens[0].Code == TokenType.Space)
                {
                    currentLexem = LexemState.SpaceAfterInt;
                    return ParcerRecursion(Tokens.GetRange(1, Tokens.Count - 1), currentLexem, PrevErrors, initial_line);
                }
                else
                {
                    resultErrors.Errors.Add(new ParcerError(Tokens[0].StartPos, Tokens[0].Line, "Unexpected lexeme", "Lexeme: int. Met: '" + Tokens[0].Value + "', expected: ' '"));


                    result.ResultsList.AddRange(ParcerRecursion(Tokens.GetRange(1, Tokens.Count - 1), currentLexem, resultErrors, initial_line).ResultsList); // пропуск токена
                    result.ResultsList.AddRange(ParcerRecursion(Tokens.GetRange(1, Tokens.Count - 1), LexemState.SpaceAfterInt, resultErrors, initial_line).ResultsList); // изменение токена на правильный
                    result.ResultsList.AddRange(ParcerRecursion(Tokens, LexemState.SpaceAfterInt, resultErrors, initial_line).ResultsList); // вставка токена на правильный

                    return result;
                }
            }
            else if (currentLexem == LexemState.SpaceAfterInt)
            {
                if (Tokens[0].Code == TokenType.Identifier)
                {
                    currentLexem = LexemState.InID;
                    return ParcerRecursion(Tokens.GetRange(1, Tokens.Count - 1), currentLexem, PrevErrors, initial_line);
                }
                else
                {
                    resultErrors.Errors.Add(new ParcerError(Tokens[0].StartPos, Tokens[0].Line, "Unexpected lexeme", "Lexeme: space after int. Met: '" + Tokens[0].Value + "', expected: letter{letter|digit}"));


                    result.ResultsList.AddRange(ParcerRecursion(Tokens.GetRange(1, Tokens.Count - 1), currentLexem, resultErrors, initial_line).ResultsList); // пропуск токена
                    result.ResultsList.AddRange(ParcerRecursion(Tokens.GetRange(1, Tokens.Count - 1), LexemState.InID, resultErrors, initial_line).ResultsList); // изменение токена на правильный
                    result.ResultsList.AddRange(ParcerRecursion(Tokens, LexemState.InID, resultErrors, initial_line).ResultsList); // вставка токена на правильный

                    return result;
                }
            }
            else if (currentLexem == LexemState.InID)
            {
                if (Tokens[0].Code == TokenType.Operator_eq)
                {
                    currentLexem = LexemState.Equals;
                    return ParcerRecursion(Tokens.GetRange(1, Tokens.Count - 1), currentLexem, PrevErrors, initial_line);
                }
                else
                {
                    resultErrors.Errors.Add(new ParcerError(Tokens[0].StartPos, Tokens[0].Line, "Unexpected lexeme", "Lexeme: index. Met: '" + Tokens[0].Value + "', expected: '='"));


                    result.ResultsList.AddRange(ParcerRecursion(Tokens.GetRange(1, Tokens.Count - 1), currentLexem, resultErrors, initial_line).ResultsList); // пропуск токена
                    result.ResultsList.AddRange(ParcerRecursion(Tokens.GetRange(1, Tokens.Count - 1), LexemState.Equals, resultErrors, initial_line).ResultsList); // изменение токена на правильный
                    result.ResultsList.AddRange(ParcerRecursion(Tokens, LexemState.Equals, resultErrors, initial_line).ResultsList); // вставка токена на правильный

                    return result;
                }
            }
            else if (currentLexem == LexemState.Equals)
            {
                if (Tokens[0].Code == TokenType.Operator_neg)
                {
                    currentLexem = LexemState.Negative;
                    return ParcerRecursion(Tokens.GetRange(1, Tokens.Count - 1), currentLexem, PrevErrors, initial_line);
                }
                else if (Tokens[0].Code == TokenType.Number)
                {
                    currentLexem = LexemState.InNumber;
                    return ParcerRecursion(Tokens.GetRange(1, Tokens.Count - 1), currentLexem, PrevErrors, initial_line);
                }
                else
                {
                    resultErrors.Errors.Add(new ParcerError(Tokens[0].StartPos, Tokens[0].Line, "Unexpected lexeme", "Lexeme: equals. Met: '" + Tokens[0 ].Value + "', expected: '-' or 'digit{digit}'"));


                    result.ResultsList.AddRange(ParcerRecursion(Tokens.GetRange(1, Tokens.Count - 1), currentLexem, resultErrors, initial_line).ResultsList); // пропуск токена
                    result.ResultsList.AddRange(ParcerRecursion(Tokens.GetRange(1, Tokens.Count - 1), LexemState.InNumber, resultErrors, initial_line).ResultsList); // изменение токена на правильный
                    result.ResultsList.AddRange(ParcerRecursion(Tokens, LexemState.InNumber, resultErrors, initial_line).ResultsList); // вставка токена на правильный
                    result.ResultsList.AddRange(ParcerRecursion(Tokens.GetRange(1, Tokens.Count - 1), LexemState.Negative, resultErrors, initial_line).ResultsList); // изменение токена на правильный
                    result.ResultsList.AddRange(ParcerRecursion(Tokens, LexemState.Negative, resultErrors, initial_line).ResultsList); // вставка токена на правильный

                    return result;
                }
            }
            else if (currentLexem == LexemState.Negative)
            {
                if (Tokens[0].Code == TokenType.Number)
                {
                    currentLexem = LexemState.InNumber;
                    return ParcerRecursion(Tokens.GetRange(1, Tokens.Count - 1), currentLexem, PrevErrors, initial_line);
                }
                else
                {
                    resultErrors.Errors.Add(new ParcerError(Tokens[0].StartPos, Tokens[0].Line, "Unexpected lexeme", "Lexeme: negative. Met: '" + Tokens[0].Value + "', expected: 'digit{digit}'"));


                    result.ResultsList.AddRange(ParcerRecursion(Tokens.GetRange(1, Tokens.Count - 1), currentLexem, resultErrors, initial_line).ResultsList); // пропуск токена
                    result.ResultsList.AddRange(ParcerRecursion(Tokens.GetRange(1, Tokens.Count - 1), LexemState.InNumber, resultErrors, initial_line).ResultsList); // изменение токена на правильный
                    result.ResultsList.AddRange(ParcerRecursion(Tokens, LexemState.InNumber, resultErrors, initial_line).ResultsList); // вставка токена на правильный

                    return result;
                }
            }
            else if (currentLexem == LexemState.InNumber)
            {
                if (Tokens[0].Code == TokenType.Endline)
                {
                    currentLexem = LexemState.EndLine;
                    return ParcerRecursion(Tokens.GetRange(1, Tokens.Count - 1), currentLexem, PrevErrors, initial_line);
                }
                else
                {
                    resultErrors.Errors.Add(new ParcerError(Tokens[0].StartPos, Tokens[0].Line, "Unexpected lexeme", "Lexeme: number. Met: '" + Tokens[0].Value + "', expected: ';'"));


                    result.ResultsList.AddRange(ParcerRecursion(Tokens.GetRange(1, Tokens.Count - 1), currentLexem, resultErrors, initial_line).ResultsList); // пропуск токена
                    result.ResultsList.AddRange(ParcerRecursion(Tokens.GetRange(1, Tokens.Count - 1), LexemState.EndLine, resultErrors, initial_line).ResultsList); // изменение токена на правильный
                    result.ResultsList.AddRange(ParcerRecursion(Tokens, LexemState.EndLine, resultErrors, initial_line).ResultsList); // вставка токена на правильный

                    return result;
                }
            }
            else if (currentLexem == LexemState.EndLine)
            {
                currentLexem = LexemState.Start;
                return ParcerRecursion(Tokens.GetRange(0, Tokens.Count - 1), currentLexem, PrevErrors, initial_line);
            }

            return result;
        }


        public ParcerResult Parce(AnalysisResult input)
        {
            var result = new ParcerResult();
            LexemState currentLexem = LexemState.Start;
            int initial_line = 0;

            //if (input.Errors.Count > 0 || input.Tokens.Count == 0)
            if (input.Tokens.Count == 0)
                return result;

            List<Token> Tokens = input.Tokens;
            bool StartExists = false;
            foreach(var el in Tokens)
            {
                if (el.Code == TokenType.Keyword_const){
                    StartExists = true;
                    break;
                }
            }
            if (!StartExists)
            {
                result.Errors.Add(new ParcerError(Tokens[0].StartPos, Tokens[0].Line, "No start", "No starting lexeme detected. Met: '" + Tokens[0].Value + "', expected: 'const'"));
                return result;
            }
            
            var Allresults = new ParcerResultList();
            Allresults = ParcerRecursion(Tokens, currentLexem, result, initial_line);
            var Finalresult = new ParcerResult();
            Finalresult = Allresults.ResultsList[0];
            int minErrors = Allresults.ResultsList[0].Errors.Count;
            for (int i = 0; i < Allresults.ResultsList.Count; i++)
            {
                if (Allresults.ResultsList[i].Errors.Count < minErrors)
                {
                    minErrors = Allresults.ResultsList[i].Errors.Count;
                    Finalresult = Allresults.ResultsList[i];
                }
            }

            return Finalresult;
        }

    }
}
