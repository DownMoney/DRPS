using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;

namespace API
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "Search" in code, svc and config file together.
    // NOTE: In order to launch WCF Test Client for testing this service, please select Search.svc or Search.svc.cs at the Solution Explorer and start debugging.
    public class Search : ISearch
    {
        private static FuzzySearch f = new FuzzySearch();
        private SQL sq = new SQL();
        List<string> wordList = new List<string>();

        public AjaxDictionary<string,string>[] search(string sql)
        {
            if (wordList.Count == 0)
                loadWordList();
            string s = ("SELECT * FROM Courses WHERE Description LIKE '%" + sql + "%' OR Name LIKE '%" + sql + "%' OR AcademicDesc LIKE '%" + sql + "%'");
            if (sql.IndexOf("courses that") != -1)                
                s= f.ChunkSearch(sql, wordList, 0.50, new string[0]);

            Dictionary<string, string>[] old = sq.selectQuery(s,0);
            AjaxDictionary<string, string>[] d = new AjaxDictionary<string, string>[old.Length];

            for (int i = 0; i < d.Length; i++)
            {
                AjaxDictionary<string, string> temp = AjaxDictionary<string, string>.ToAjax(old[i]);
                d[i] = temp;
            }

            return d;
        }

        public AjaxDictionary<string,string>[] get(string code)
        {
            Dictionary<string, string>[] old = sq.selectQuery("SELECT * FROM Courses WHERE Code='" + code + "';");
            AjaxDictionary<string, string>[] d = new AjaxDictionary<string, string>[old.Length];

            for (int i = 0; i < d.Length; i++)
            {
                AjaxDictionary<string, string> temp = AjaxDictionary<string, string>.ToAjax(old[i]);
                d[i] = temp;
            }

            return d;

        }

        public AjaxDictionary<string, string>[] getToken(string token)
        {
            Dictionary<string, string>[] old = new Dictionary<string,string>[0];
            
            switch(token.ToLower())
            {
                case "people":
                    old = sq.selectQuery("SELECT DISTINCT(Name) AS val FROM People ORDER BY Name;");
                    break;

                case "schools":
                    old = sq.selectQuery("SELECT DISTINCT(Name) AS val FROM Schools ORDER BY Name;");
                    break;

                case "year":
                    old = sq.selectQuery("SELECT DISTINCT(CreditLevel) AS val FROM Courses ORDER BY CreditLevel;");
                    break;

                case "students":
                    old = sq.selectQuery("SELECT DISTINCT(Availability) AS val FROM Courses ORDER BY Availability;");
                    break;

                case "semester":
                    old = sq.selectQuery("SELECT DISTINCT(Period) AS val FROM Courses ORDER BY Period;");
                    break;

                case "credits":
                    old = sq.selectQuery("SELECT DISTINCT(Credits) AS val FROM Courses ORDER BY Credits;");
                    break;

            }

           
            AjaxDictionary<string, string>[] d = new AjaxDictionary<string, string>[old.Length];

            for (int i = 0; i < d.Length; i++)
            {
                AjaxDictionary<string, string> temp = AjaxDictionary<string, string>.ToAjax(old[i]);
                d[i] = temp;
            }

            return d;

        }

        private void loadWordList()
        {
            WebClient client = new WebClient();
            string[] s = client.DownloadString("http://54.214.7.238/words.txt").Split('\n');

            for (int i = 0; i < s.Length; i++)
                wordList.Add(s[i].Replace("\n", "").Replace("\r", ""));
        }

        public AjaxDictionary<string, string>[] search2(string sql, string page, string taken)
        {
            if (wordList.Count == 0)
                loadWordList();

            if (taken == "null")
                taken = "";
            string s = ("SELECT * FROM Courses WHERE Description LIKE '%"+sql+"%' OR Name LIKE '%"+sql+"%' OR AcademicDesc LIKE '%"+sql+"%'");
            if(sql.IndexOf("courses that") != -1)
             s = f.ChunkSearch(sql, wordList, 0.50,taken.Split(';'));

            

            Dictionary<string, string>[] old = Requisites( Requisites(Requisites(sq.selectQuery(s, Convert.ToInt32(page)),"1",taken.Split(';'),"green"),"0",taken.Split(';'),"orange"),"2",taken.Split(';'),"red");
            
            AjaxDictionary<string, string>[] d = new AjaxDictionary<string, string>[old.Length];

            for (int i = 0; i < d.Length; i++)
            {
                AjaxDictionary<string, string> temp = AjaxDictionary<string, string>.ToAjax(old[i]);
                d[i] = temp;
            }

            return d;
        }

        private Dictionary<string,string>[] Requisites(Dictionary<string, string>[] d, string type, string[] taken, string color)
        {
            SQL sql = new SQL();
            if (taken[0] != "")
            {
                for (int i = 0; i < taken.Length; i++)
                {
                    string cid = sql.selectQuery("SELECT CID FROM Courses WHERE Code='" + taken[i] + "';")[0]["CID"];
                    string cmd = "SELECT Course FROM Requisites WHERE CID=" + cid + " AND Type=" + type + ";";
                    Dictionary<string, string>[] temp = sql.selectQuery(cmd);

                    

                    for (int j = 0; j < temp.Length; j++)
                    {
                        string s = "SELECT * FROM Courses WHERE Code='" + temp[j]["Course"] + "';";
                        Array.Resize<Dictionary<string, string>>(ref d, d.Length + 1);
                        d[d.Length - 1] = sql.selectQuery(s)[0];
                        int p = find(d, temp[j]["Course"]);
                        if (p != -1 && !d[p]["Name"].Contains("<i style"))
                        {
                            d[p]["Name"] = "<i style=\"color: " + color + "\">" + d[p]["Name"] + "</i>";
                        }
                    }
                }
            }
           return d;
        }

        private int find(Dictionary<string, string>[] d, string code)
        {
            for (int i = 0; i < d.Length; i++)
            {
                if (code == d[i]["Code"])
                    return i;
            }
            return -1;
        }

        public List<string> complete(string query)
        {
            if (wordList.Count == 0)
                loadWordList();

            List<string> available = new List<string>();

            if (query == "")
            {
                available.Add("courses");
            }
            else
            {

                for (int i = 0; i < wordList.Count; i++)
                {
                    string temp = (Regex.Replace(wordList[i], @"<\w+>", ""));
                    if (!query.Contains(temp))
                    {
                        if (query != "courses")
                            available.Add(query + " and are " + temp);
                        else
                            available.Add(query + " " + temp);
                    }
                }
            }
            return available;
        }
    }
}
