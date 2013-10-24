//#define LINQ
//-----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace API
{
    /// <summary>
    /// Provides methods for fuzzy string searching.
    /// </summary>
    public class FuzzySearch
    {


        private static Dictionary<string, Regex> cache = new Dictionary<string, Regex>();
        private static QueryBuilder qb = new QueryBuilder();

        public struct Result
        {
            public string Word;
            public double Score;
            public int part;
        }

        /// <summary>
        /// Calculates the Levenshtein-distance of two strings.
        /// </summary>
        /// <param name="src">
        /// 1. string
        /// </param>
        /// <param name="dest">
        /// 2. string
        /// </param>
        /// <returns>
        /// Levenshstein-distance
        /// </returns>
        /// <remarks>
        /// See 
        /// <a href='http://en.wikipedia.org/wiki/Levenshtein_distance'>
        /// http://en.wikipedia.org/wiki/Levenshtein_distance
        /// </a>
        /// </remarks>
        public static int LevenshteinDistance(string src, string dest)
        {
            int[,] d = new int[src.Length + 1, dest.Length + 1];
            int i, j, cost;
            char[] str1 = src.ToCharArray();
            char[] str2 = dest.ToCharArray();

            for (i = 0; i <= str1.Length; i++)
            {
                d[i, 0] = i;
            }
            for (j = 0; j <= str2.Length; j++)
            {
                d[0, j] = j;
            }
            for (i = 1; i <= str1.Length; i++)
            {
                for (j = 1; j <= str2.Length; j++)
                {

                    if (str1[i - 1] == str2[j - 1])
                        cost = 0;
                    else
                        cost = 1;

                    d[i, j] =
                        Math.Min(
                            d[i - 1, j] + 1,					// Deletion
                            Math.Min(
                                (d[i, j - 1] + 1),				// Insertion
                                d[i - 1, j - 1] + cost));		// Substitution

                    if ((i > 1) && (j > 1) && (str1[i - 1] == str2[j - 2]) && (str1[i - 2] == str2[j - 1]))
                    {
                        d[i, j] = Math.Min(d[i, j], d[i - 2, j - 2] + cost);
                    }
                }
            }

            return d[str1.Length, str2.Length];
        }

        private Regex getTokens(string token)
        {
            if (cache.ContainsKey(token))
                return cache[token];

            string cmd = "";
            SQL sql = new SQL();
            switch (token)
            {
                case "people":
                    cmd = "SELECT Name FROM People;";
                    cache.Add(token, ToRegex(sql.ToArray(sql.selectQuery(cmd), "Name")));
                    return cache[token];
                case "schools":
                    cmd = "SELECT Name FROM Schools;";
                    cache.Add(token, ToRegex(sql.ToArray(sql.selectQuery(cmd), "Name")));
                    return cache[token];

                case "number":
                    return new Regex(@"(\d+)", RegexOptions.Compiled);

                case "free":
                    return new Regex(@"about ((\w|\s)+)", RegexOptions.Compiled);

                case "year":
                    return new Regex(@"(year\s?\d+\s?\w*)", RegexOptions.Compiled);

                case "students":
                    return new Regex(@"available to ((\w|\s)+)", RegexOptions.Compiled);

                case "semester":
                    return new Regex(@"run in (semester \d\w*)", RegexOptions.Compiled);

            }

            return null;
        }

        private Regex ToRegex(string[] s)
        {
            string h = "(";

            for (int i = 0; i < s.Length; i++)
                h += s[i] + "|";

            h = h.Substring(0, h.Length - 1);
            h += ")";
            return new Regex(h, RegexOptions.Compiled);
        }

        private string[] getNum(int start, int end)
        {
            string[] s = new string[end - start];

            for (int i = end - 1; i >= start; i--)
            {
                s[i] = i.ToString();
            }

            return s;
        }

        private int calcCost(string[] tokens, string org)
        {


            for (int z = 0; z < tokens.Length; z++)
            {
                if (org.Contains(tokens[z].ToLower()))
                    return 0;
            }

            return 10;
        }


        private int calcCost(string[] s, Regex r)
        {
            string org = "";
            for (int i = 0; i < s.Length; i++)
                org += s[i] + " ";
            org = org.Substring(0, org.Length - 1);
            string[] tokens = r.ToString().Split('|');

            if (tokens.Length > 20)
                return calcCost(tokens, org);

            if (r.IsMatch(org))
                return 0;
            else
                return 10;
        }

        private int LevenshteinDistanceToken(string[] str1, string[] str2)
        {
            int[,] d = new int[str1.Length + 1, str2.Length + 1];
            int i, j, cost;

            for (i = 0; i <= str1.Length; i++)
            {
                d[i, 0] = i;
            }
            for (j = 0; j <= str2.Length; j++)
            {
                d[0, j] = j;
            }
            for (i = 1; i <= str1.Length; i++)
            {
                for (j = 1; j <= str2.Length; j++)
                {
                    if (str2[j - 1].Contains("<"))
                    {
                        string token = str2[j - 1].Substring(1, str2[j - 1].Length - 2);
                        cost = calcCost(str1, getTokens(token));

                    }
                    else
                    {
                        if (str1[i - 1] == str2[j - 1])
                            cost = 0;
                        else
                            cost = 1;
                    }

                    d[i, j] =
                        Math.Min(
                            d[i - 1, j] + 1,					// Deletion
                            Math.Min(
                                (d[i, j - 1] + 1),				// Insertion
                                d[i - 1, j - 1] + cost));		// Substitution

                    if ((i > 1) && (j > 1) && (str1[i - 1] == str2[j - 2]) && (str1[i - 2] == str2[j - 1]))
                    {
                        d[i, j] = Math.Min(d[i, j], d[i - 2, j - 2] + cost);
                    }
                }
            }

            return d[str1.Length, str2.Length];
        }
        //---------------------------------------------------------------------
        /// <summary>
        /// Fuzzy searches a list of strings.
        /// </summary>
        /// <param name="word">
        /// The word to find.
        /// </param>
        /// <param name="wordList">
        /// A list of word to be searched.
        /// </param>
        /// <param name="fuzzyness">
        /// Ration of the fuzzyness. A value of 0.8 means that the 
        /// difference between the word to find and the found words
        /// is less than 20%.
        /// </param>
        /// <returns>
        /// The list with the found words.
        /// </returns>
        /// <example>
        /// 
        /// </example>
        public Result[] Search(
            string word,
            List<string> wordList,
            double fuzzyness)
        {
#if !LINQ
            Result[] foundWords = new Result[0];

            foreach (string s in wordList)
            {

                string[] ss = word.Split(' ');
                for (int i = 0; i < ss.Length; i++)
                    ss[i] = ss[i].Trim().ToLower();

                string[] hh = s.Split(' ');
                for (int i = 0; i < hh.Length; i++)
                    hh[i] = hh[i].Trim().ToLower();
                // Calculate the Levenshtein-distance:
                int levenshteinDistance =
                    LevenshteinDistanceToken(ss, hh);

                // Length of the longer string:
                int length = Math.Max(word.Length, s.Length);

                // Calculate the score:
                double score = 1.0 - (double)levenshteinDistance / length;

                // Match?
                if (score > fuzzyness)
                {
                    Array.Resize<Result>(ref foundWords, foundWords.Length + 1);
                    foundWords[foundWords.Length - 1] = new Result();
                    foundWords[foundWords.Length - 1].Score = score;
                    foundWords[foundWords.Length - 1].Word = replaceToken(s, word);
                }

            }
#else
			// Tests have prove that the !LINQ-variant is about 3 times
			// faster!
			List<string> foundWords =
				(
					from s in wordList
					let levenshteinDistance = LevenshteinDistance(word, s)
					let length = Math.Max(s.Length, word.Length)
					let score = 1.0 - (double)levenshteinDistance / length
					where score > fuzzyness
					select s
				).ToList();
#endif
            return foundWords;
        }

        private string replaceToken(string word, string org)
        {
            Regex r = new Regex(@"<(\w+)>", RegexOptions.Compiled);
            MatchCollection col = r.Matches(word);
            for (int i = 0; i < col.Count; i++)
            {
                Regex rr = getTokens(col[i].Groups[1].Value.ToLower());

                word = word.Replace("<" + col[i].Groups[1].Value + ">", rep(rr, org.ToLower()));
            }

            return word;
        }

        private string rep(string[] tokens, string word)
        {
            for (int i = 0; i < tokens.Length; i++)
                if (word.Contains(tokens[i].ToLower()))
                    return tokens[i];

            return "";
        }

        private string rep(Regex token, string word)
        {
            string[] tokens = token.ToString().Split('|');

            if (tokens.Length > 20)
                return rep(tokens, word);

            if (token.IsMatch(word))
            {
                Match m = token.Match(word);
                return m.Groups[1].Value;
            }

            return "";
        }

        private string getNode(string sentence)
        {
            return sentence.Split(' ')[0];
        }

        public string ChunkSearch(string word, List<string> words, double f, string[] taken)
        {
            word = word.Replace("\n", "").Replace("\r", "");
            List<Result[]> results = new List<Result[]>();
            string[] chunks = Regex.Split(word, @"and");

            string node = getNode(word);

            for (int i = 0; i < chunks.Length; i++)
            {
                string s = chunks[i].Replace(node, "").Trim();
                results.Add(Search(s, words, f));
            }
            if (results.Count > 0)
            {
                Result[] p = Permute(results[0], Tail(results));
                Result max = findMax(p);
                
                return qb.BuilQuery(node, max.Word.Split('|'), taken);

            }
            return "";
        }

        private Result findMax(Result[] r)
        {
            Result max = new Result();
            max.Score = -1;
            max.Word = "";

            for (int i = 0; i < r.Length; i++)
            {
                if (r[i].Score > max.Score)
                {
                    max.Score = r[i].Score;
                    max.Word = r[i].Word;
                }
            }

            return max;
        }

        private List<Result[]> Tail(List<Result[]> r)
        {
            if (r.Count > 1)
            {
                List<Result[]> rr = new List<Result[]>();
                for (int i = 1; i < r.Count; i++)
                    rr.Add(r[i]);
                return rr;
            }
            else
                return new List<Result[]>();
        }

        private Result[] Permute(Result[] head, List<Result[]> tail)
        {

            if (tail.Count <= 0)
                return head;
            Result[] rr = new Result[0];
            int max = head.Length;
            for (int j = 0; j < max; j++)
            {
                for (int i = 0; i < tail[0].Length; i++)
                {
                    Array.Resize<Result>(ref rr, rr.Length + 1);
                    rr[rr.Length - 1] = new Result();

                    rr[rr.Length - 1].Score = head[j].Score * tail[0][i].Score;
                    rr[rr.Length - 1].Word = head[j].Word + "|" + tail[0][i].Word;
                }
            }

            return Permute(rr, Tail(tail));

        }


        /*  public static List<string> SearchRegex(string word, List<Regex> wordList, double fuzzyness)
          {
  #if !LINQ
              List<string> foundWords = new List<string>();

              foreach (Regex s in wordList)
              {
                  if (s.IsMatch(word))
                  {
                      Match m = s.Match(word);
                      string com = word;
                      for (int i = 0; i < m.Groups.Count; i++)
                      {
                          word
                      }

                      // Calculate the Levenshtein-distance:
                      int levenshteinDistance =
                          LevenshteinDistance(word, s);

                      // Length of the longer string:
                      int length = Math.Max(word.Length, s.Length);

                      // Calculate the score:
                      double score = 1.0 - (double)levenshteinDistance / length;

                      // Match?
                      if (score > fuzzyness)
                          foundWords.Add(s);
                  }
              }
  #else
              // Tests have prove that the !LINQ-variant is about 3 times
              // faster!
              List<string> foundWords =
                  (
                      from s in wordList
                      let levenshteinDistance = LevenshteinDistance(word, s)
                      let length = Math.Max(s.Length, word.Length)
                      let score = 1.0 - (double)levenshteinDistance / length
                      where score > fuzzyness
                      select s
                  ).ToList();
  #endif
              return foundWords;
          }*/
    }
}