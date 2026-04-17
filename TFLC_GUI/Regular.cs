using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Reflection;

namespace TFLC_GUI
{

    public class RegexMatch
    {
        public int Position { get; set; }
        public int Line { get; set; }
        public string Match { get; set; }
        public int FoundAmount { get; set; }
        public string Pattern { get; set; }
        public RegexMatch(int position, int line, string match, int count, string pattern)
        {
            Position = position;
            Line = line;
            Match = match;
            FoundAmount = count;
            Pattern = pattern;
        }
        public override string ToString()
        {
            return $"Line {Line}, pos {Position}\n{Match.Replace("\n", "[/N]")} with length of {Match.Count()}, {FoundAmount} number of times\npattern:{Pattern}";
        }
    }

    public class RegexResult
    {
        public List<RegexMatch> Matches { get; set; } = new List<RegexMatch>();
    }

    
    public class RegularAnalyze
    {
        public RegularAnalyze()
        {
            
        }

        public RegexResult Analyze(string text, string pattern)
        {
            var result = new RegexResult();
            int k = 0;

            if (string.IsNullOrEmpty(text))
                return result;


            //string[] lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            //for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            {
                //string line = lines[lineIndex];

                Regex regex = new Regex(pattern);
                MatchCollection matches = regex.Matches(text);
                if (matches.Count > 0)
                {
                    foreach (Match match in matches)
                    {
                        string sub = text.Substring(0, match.Index);
                        int line = sub.Split('\n').Length;
                        int col = sub.Length - sub.LastIndexOf('\n');

                        result.Matches.Add(new RegexMatch(col-1, line-1, match.Value, 0, pattern));
                        k++;
                    }
                    foreach (RegexMatch match in result.Matches)
                    {
                        match.FoundAmount = k;
                    }
                }
                else
                {
                    Console.WriteLine("Совпадений не найдено");
                }
            }

            return result;
        }
    }
}
