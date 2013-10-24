using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Text.RegularExpressions;
using MySql.Data.MySqlClient;
using HtmlAgilityPack;

namespace DRPS
{
    class Program
    {

        struct Requisite
        {
            public string[] Must;
            public string[] Recommended;
        }

        struct Person
        {
            public string Name;
            public string Tel;
            public string Email;
        }
       struct Day
       {
           public DateTime Start;
           public DateTime End;
       }

        struct Delivery
        {
            public string Location;
            public string Activity;
            public string Description;
            public int startWeek;
            public int endWeek;
            public Dictionary<int, Day> Schedule;
        }

        struct Lecture
        {
            public string Name;
            public string Description;
            public string Type;
            public string Start;
            public string End;
            public string startDate;
            public string endDate;
            public string Building;
            public string Room;
            public string Staff;
        }

        struct Info
        {
            public string CreditLevel;
            public string HomeSubjectArea;
            public string CourseWebsite;
            public string Description;
            public string Availability;
            public string OtherSubjectArea;
            public bool Gaelic;
            public string College;
            public string Type;
            public string[] Prerequisite;
            public string[] Corequisite;
            public string[] Prohabited;
            public string OtherRequiments;
            public string AdditionalCosts;
            public bool Learn;
            public int Quota;
            public Dictionary<int, Lecture[]> Schedule;
            public string FirstClass;
            public string AdditionalInfo;
            public string LearningOutcomes;
            public string AssessmentInfo;
            public string SpecialArrangements;
            public string AcademicDesc;
            public string Syllabus;
            public string TransSkills;
            public string ReadingList;
            public string StudyAbroad;
            public string StudyPatterns;
            public string[] Keywords;
            public Person Organizer;
            public Person Secretary;
        }

        struct _Class
        {
            public string Name;
            public string Url;
            public string Availability;
            public string Period;
            public int Credits;
            public string Code;
            public Info info;
        }

        struct Subject
        {
            public string Name;
            public string Url;
            public _Class[] Courses;
        }
         
        struct School
        {
            public string Name;
            public string Url;
            public Subject[] Subjects;
        }

        private static string endPoint = "http://www.drps.ed.ac.uk/13-14/dpt/";
        static Regex link = new Regex(@"<li><a href=" + "\"" + @"((\w|_|\.)+)" + "\"" + @">((\w|\s|\(|\))+)</a></li>", RegexOptions.Compiled);
        private static int suid = 1;
        private static int cid = 1;
        private static string connectionString = "";

        static void Main(string[] args)
        {
            DateTime now = DateTime.Now;
            School[] schools = getSchools(getPage("http://www.drps.ed.ac.uk/13-14/dpt/cx_schindex.htm"));
            for (int i = 0; i < schools.Length; i++)
            {
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine("School: " + schools[i].Name);
                Console.ForegroundColor = ConsoleColor.Gray;

                schools[i].Subjects = getSubjects(schools[i]);
                for (int j = 0; j < schools[i].Subjects.Length; j++)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Subject: " + schools[i].Subjects[j].Name);
                    Console.ForegroundColor = ConsoleColor.Gray;
                    schools[i].Subjects[j].Courses = getCourses(schools[i].Subjects[j]);
                    for (int h = 0; h < schools[i].Subjects[j].Courses.Length; h++)
                    {
                        Console.WriteLine("Course: " + schools[i].Subjects[j].Courses[h].Name);
                        schools[i].Subjects[j].Courses[h].info = getInfo(schools[i].Subjects[j].Courses[h]);
                    }
                }
            }
            TimeSpan t = DateTime.Now.Subtract(now);
            Console.WriteLine("It took " + t.Minutes + ":" + t.Seconds);
            //addNames(schools);
            saveToMySQL(schools);
            Console.ReadLine();
        }

        public static  Dictionary<string, string>[] selectQuery(string query)
        {
            Dictionary<string, string>[] d = new Dictionary<string, string>[0];
            MySqlConnection con = new MySqlConnection(connectionString);
            con.Open();
            MySqlCommand com = con.CreateCommand();
            com.CommandText = query;
            MySqlDataReader reader = com.ExecuteReader();


            while (reader.Read())
            {
                Array.Resize<Dictionary<string, string>>(ref d, d.Length + 1);
                d[d.Length - 1] = new Dictionary<string, string>();
                for (int i = 0; i < reader.FieldCount; i++)
                    d[d.Length - 1].Add(reader.GetName(i), reader[i].ToString());
            }
            con.Close();
            return d;
        }

        public static  void executeQuery(string query)
        {
            try
            {
                MySqlConnection con = new MySqlConnection(connectionString);
                con.Open();
                MySqlCommand com = con.CreateCommand();
                com.CommandText = query;
                com.ExecuteNonQuery();
                con.Close();
            }
            catch
            { }
        }

        private static void saveToMySQL(School[] schools)
        {
            int sid = 1;
            for (int i = 0; i < schools.Length; i++)
            {
                string cmd = "INSERT INTO Schools(Name, URL) VALUES ('" + schools[i].Name + "','" + schools[i].Url + "');";
                executeQuery(cmd);
                addSchool(schools[i].Subjects,sid);
                sid++;
            }
        }

        private static void addSchool(Subject[] s, int sid)
        {            
            for (int i = 0; i < s.Length; i++)
            {
                string cmd = "INSERT INTO Subjects(SID, Name, URL) VALUES (" + sid.ToString() + ", '" + s[i].Name + "', '" + s[i].Url + "');";
                executeQuery(cmd);
                addSubject(s[i].Courses,sid,suid);
                suid++;
            }
        }

        private static void addNames(School[] schools)
        {
            for (int i = 0; i < schools.Length; i++)
            {
               for(int j=0; j< schools[i].Subjects.Length; j++)
               {
                   for (int h = 0; h < schools[i].Subjects[j].Courses.Length; h++)
                   {
                       string cmd = "UPDATE Courses SET Name='" + schools[i].Subjects[j].Courses[h].Name.Replace("'", "\\'") + "' WHERE Code='" + schools[i].Subjects[j].Courses[h].Code + "' LIMIT 1;";
                       executeQuery(cmd);
                   }
               }
            }
        }

        private static void addSubject(_Class[] c, int sid, int suid)
        {

            addSchedule(c);
            /*

            for (int i = 0; i < c.Length; i++)
            {

                string org = addPerson(c[i].info.Organizer);
                string sec = addPerson(c[i].info.Secretary);

                string cmd = "INSERT INTO Courses(SuID, SID, CreditLevel, HomeSubjectArea, CourseWebsite, Description, Availability, OtherSubjectArea, Gaelic, College, Type, OtherRequiments, AdditionalCosts, Learn, Quota, FirstClass, AdditionalInfo, LearningOutcomes, AssessmentInfo, SpecialArrangements, AcademicDesc, Syllabus, TransSkills, ReadingList, StudyAbroad, StudyPatterns, URL, Period, Credits, Code, Organizer, Secretary, Name)  VALUES("+suid.ToString().Replace("'","\\'") + ", "+sid.ToString().Replace("'","\\'") + ", '"+c[i].info.CreditLevel.Replace("'","\\'") + "','"+c[i].info.HomeSubjectArea.Replace("'","\\'") + "','"+c[i].info.CourseWebsite.Replace("'","\\'") + "','"+c[i].info.Description.Replace("'","\\'") + "','"+c[i].info.Availability.Replace("'","\\'") + "','"+c[i].info.OtherSubjectArea.Replace("'","\\'") + "',"+c[i].info.Gaelic.ToString().Replace("'","\\'") + ",'"+c[i].info.College.Replace("'","\\'") + "','"+c[i].info.Type.Replace("'","\\'") + "','"+c[i].info.OtherRequiments.Replace("'","\\'") + "','"+c[i].info.AdditionalCosts.Replace("'","\\'") + "',"+c[i].info.Learn.ToString().Replace("'","\\'") + ","+c[i].info.Quota.ToString() + ",'"+c[i].info.FirstClass.Replace("'","\\'") + "','"+c[i].info.AdditionalInfo.Replace("'","\\'") + "','"+c[i].info.LearningOutcomes.Replace("'","\\'") + "','"+c[i].info.AssessmentInfo.Replace("'","\\'") + "','"+c[i].info.SpecialArrangements.Replace("'","\\'") + "','"+c[i].info.AcademicDesc.Replace("'","\\'") + "','"+c[i].info.Syllabus.Replace("'","\\'") + "','"+c[i].info.TransSkills.Replace("'","\\'") + "','"+c[i].info.ReadingList.Replace("'","\\'") + "','"+c[i].info.StudyAbroad.Replace("'","\\'") + "','"+c[i].info.StudyPatterns.Replace("'","\\'") +"','"+c[i].Url+"','" +c[i].Period+"', "+c[i].Credits.ToString()+", '"+c[i].Code+"', "+org+", "+sec+", '"+c[i].Name+"');";

                executeQuery(cmd);

                addKeyword(c[i].info.Keywords);

                addRequisite(c[i].info.Prerequisite, 0);
                addRequisite(c[i].info.Corequisite, 1);
                addRequisite(c[i].info.Prohabited, 2);

                cid++;

                 
            }*/
        }

        private static void addSchedule(_Class[] c)
        {
            string cmd = "INSERT INTO Schedule(Name, Description, Type, Start, End, Building, Room, Staff, Code, Day, startDate, endDate) VALUES ";
            for (int i = 0; i < c.Length; i++)
            {
                for(int j=0; j<c[i].info.Schedule.Count; j++)
                {
                    if (c[i].info.Schedule.ContainsKey(j))
                    {
                        for (int z = 0; z < c[i].info.Schedule[j].Length; z++)
                        {
                            cmd += "('" + c[i].Name + "', '" + c[i].info.Schedule[j][z].Description + "', '" + c[i].info.Schedule[j][z].Type + "', '" + c[i].info.Schedule[j][z].Start + "', '" + c[i].info.Schedule[j][z].End + "', '" + c[i].info.Schedule[j][z].Building + "', '" + c[i].info.Schedule[j][z].Room + "', '" + c[i].info.Schedule[j][z].Staff + "', '" + c[i].Code + "', " + j.ToString() + ", STR_TO_DATE('" + c[i].info.Schedule[j][z].startDate + "', '%d/%m/%Y'), STR_TO_DATE('" + c[i].info.Schedule[j][z].endDate + "', '%d/%m/%Y') ),";
                        }
                    }
                }                
            }

            cmd = cmd.Substring(0, cmd.Length - 1);
            cmd += ";";

           executeQuery(cmd);
        }

        private static void addRequisite(string[] s, int type)
        {
            for (int i = 0; i < s.Length; i++)
            {
                string cmd = "INSERT INTO Requisites(CID, Type, Course) VALUES (" + cid.ToString() + "," + type.ToString() + ", '" + s[i] + "');";
                executeQuery(cmd);
            }
        }

        private static void addSchedule(Delivery d)
        {
            foreach(var v in d.Schedule)
            {
                string cmd = "INSERT INTO Schedule(Day, Start, _End, Activity, Location, StartWeek, EndWeek, Description, CID) VALUES (" + v.Key.ToString() + ", '" + v.Value.Start.ToShortTimeString() + "', '" + v.Value.End.ToShortDateString() + "', '" + d.Location + "'," + d.startWeek.ToString() + ", " + d.endWeek.ToString() + ", '" + d.Description + "', " + cid.ToString() + ");";
                executeQuery(cmd);
            }
        }

        private static void addKeyword(string[] s)
        {
            for (int i = 0; i < s.Length; i++)
            {
                string cmd = "INSERT INTO Keywords(Tag, CID) VALUES ('" + s[i].Replace("'","\\'") + "', " + cid.ToString() + ");";
                executeQuery(cmd);
            }
        }

        private static string addPerson(Person c)
        {
            string pid = getPersonID(c);
            if (pid != "-1")
                return pid;
            else
            {
                if (c.Email == null)
                    c.Email = "";
                if (c.Name == null)
                    c.Name = "";
                if (c.Tel == null)
                    c.Tel = "";
                string cmd = "INSERT INTO People(Name, Tel, Email) VALUES ('" + c.Name.Replace("'", "\\'") + "', '" + c.Tel.Replace("'", "\\'") + "', '" + c.Email.Replace("'", "\\'") + "');";
                executeQuery(cmd);
                return getPersonID(c);
            }
            
        }

        private static string getPersonID(Person p)
        {
            if (p.Email == null)
                p.Email = "";
            string cmd = "SELECT PID FROM People WHERE Name='" + p.Name.Replace("'", "\\'") + "' AND Email='" + p.Email.Replace("'", "\\'") + "';";
            Dictionary<string,string>[] d = selectQuery(cmd);
            if (d.Length > 0)
                return d[0]["PID"];
            else
                return "-1";
        }

        private static bool getLearn(string page)
        {
            Regex r = new Regex(@"Learn enabled:\s*(Yes|No)");
            Match m = r.Match(page);

            if (m.Groups[1].Value == "Yes")
                return true;
            else
                return false;
        }

        private static int getQuota(string page)
        {
            Regex r = new Regex(@"Quota:\s*(None|\d+)");
            Match m = r.Match(page);
            if (m.Groups[1].Value == "None")
                return -1;
            else
            {
                try
                {
                    return Convert.ToInt32(m.Groups[1].Value);
                }
                catch
                {
                    return -1;
                }
            }

            
        }

        private static Info getInfo(_Class c)
        {
            string page = getPage(endPoint + c.Url).Replace("\n", "").Replace("&nbsp;", "");
            Info info = new Info();
            info.CreditLevel = getItem("Credit level (Normal year taken)", page);
            info.HomeSubjectArea = getItem("Home subject area", page);
            info.CourseWebsite = getItem("Course website", page);
            info.Availability = getItem("Availability", page);
            info.OtherSubjectArea = getItem("Other subject area", page);
            info.Gaelic = ToBool(getItem("Taught in Gaelic\\?", page));
            info.Description = getLong("Course description", page);
            info.Type = getItem("Course type", page);
            info.College = getItem("College", page);
            info.Prohabited = ParseRequisites(getItem("Prohibited Combinations", page));
            info.Prerequisite = ParseRequisites(getItem("Pre-requisites", page));
            info.Corequisite = ParseRequisites( getItem("Co-requisites", page));
            info.OtherRequiments = getItem("Other requirements", page);
            info.AdditionalCosts = getLong("Additional Costs", page);
            info.AcademicDesc = getItem("Academic description", page);
            info.Syllabus = getItem("Syllabus", page);
            info.TransSkills = getItem("Transferable skills", page);
            info.ReadingList = getItem("Reading list", page);
            info.StudyAbroad = getItem("Study Abroad", page);
            info.StudyPatterns = getItem("Study Pattern", page);
            info.Keywords = getItem("Keywords", page).Split(',');
            info.Organizer = ToPerson(getItem("Course organiser", page));
            info.Secretary = ToPerson(getItem("Course secretary", page));
            info.Learn = getLearn(page);
            info.Quota = getQuota(page);
            info.FirstClass = getLongClass("First Class", page);
            info.AdditionalInfo = getLongClass("Additional information", page);
            info.LearningOutcomes = getTable("Learning Outcomes", page);
            info.AssessmentInfo = getTable("Assessment Information", page);
            info.SpecialArrangements = getTable("Special Arrangements", page);
            info.Schedule = getSchedule(page);
            return info;
        }

        private static string[] ParseRequisites(string s)
        {
            Regex code = new Regex(@"\(((\w|\s)+)\)", RegexOptions.Compiled);
            MatchCollection col = code.Matches(s);
            string[] h = new string[col.Count];

            for (int i = 0; i < col.Count; i++)
            {
                h[i] = col[i].Groups[1].Value;
            }

            return h;
        }

        private static Dictionary<int, Lecture[]> getSchedule(string page)
        {
            Regex r = new Regex(@"https://www.ted.is.ed.ac.uk/((\w|\d|\?|\.|\&|/|=|\+|\-)+)", RegexOptions.Compiled);

            Match m = r.Match(page);
            return getWebTable(m.Groups[0].Value);
        }

        private static Dictionary<int, Lecture[]> getWebTable(string url)
        {
            Dictionary<int, Lecture[]> d = new Dictionary<int, Lecture[]>();

            WebClient client = new WebClient();
            try
            {
                string data = client.DownloadString(url);
                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(data);
                try
                {
                    HtmlNodeCollection nodes = doc.DocumentNode.SelectNodes("/html/body/table[@class=\"spreadsheet\"]");

                    for (int i = 0; i < nodes.Count; i++)
                    {
                        try
                        {
                            string dateStart = "01/01/2013";
                            string dateEnd = "01/01/2013";
                            try
                            {
                                HtmlNode node = doc.DocumentNode.SelectSingleNode("//span[@class=\"header-1-2-3\"]");
                               dateStart = DateTime.Parse(node.InnerText.Split('-')[0]).ToShortDateString();
                               dateEnd = DateTime.Parse(node.InnerText.Split('-')[1]).ToShortDateString();
                            }
                            catch
                            {

                            }

                            HtmlNodeCollection classes = nodes[i].SelectNodes("tr");
                            Lecture[] temp = new Lecture[classes.Count - 1];
                            for (int j = 1; j < classes.Count; j++)
                            {
                                temp[j - 1] = new Lecture();
                                HtmlNodeCollection n = classes[j].SelectNodes("td");
                                temp[j - 1].Name = n[0].InnerText;
                                temp[j - 1].Description = n[1].InnerText;
                                temp[j - 1].Type = n[2].InnerText;
                                temp[j - 1].Start = n[3].InnerText;
                                temp[j - 1].End = n[4].InnerText;
                                temp[j - 1].startDate = dateStart;
                                temp[j - 1].endDate = dateEnd;
                                temp[j - 1].Building = n[6].SelectSingleNode("a").InnerText;
                                temp[j - 1].Room = n[7].InnerText;
                                temp[j - 1].Staff = n[8].InnerText;
                            }
                            d.Add(i, temp);
                        }
                        catch
                        {

                        }
                    }
                }
                catch
                { }
            }
            catch
            { }

            return d;

        }

      /*  private static Delivery getSchedule(string page)
        {
            Regex r = new Regex("<tr><td class=\"data1nobg\" nowrap=\"nowrap\">" + @"((\w|\s|\d)+)" + "</td><td>" + @"(\w+)" + "</td><td></td><td>" + @"(\d+\-\d+)" + "</td><td>" + @"\s*((\d+|:|\s)*\-?(\d+|:|\s)*)" + "</td><td>" + @"\s*((\d+|:|\s)*\-?(\d+|:|\s)*)" + "</td><td>" + @"\s*((\d+|:|\s)*\-?(\d+|:|\s)*)" + "</td><td>" + @"\s*((\d+|:|\s)*\-?(\d+|:|\s)*)" + "</td><td>" + @"\s*((\d+|:|\s)*\-?(\d+|:|\s)*)" + "</td></tr>", RegexOptions.Compiled);

            MatchCollection col = r.Matches(page);
            Delivery d = new Delivery();
            int days = 0;
           

            for (int i = 0; i < col.Count; i++)
            {
                days = 0;
                d.Location = col[i].Groups[1].Value;
                d.Activity = col[i].Groups[3].Value;
                string[] s = col[i].Groups[4].Value.Split('-');
                d.startWeek = Convert.ToInt32(s[0]);
                d.endWeek = Convert.ToInt32(s[1]);
                d.Schedule = new Dictionary<int, Day>();
                for (int j = 5; j < 20; j += 3)
                {
                    string[] h = col[i].Groups[j].Value.Split('-');

                    if (h.Length > 1)
                    {
                        Day day = new Day();
                        day.Start = DateTime.Parse(h[0]);
                        day.End = DateTime.Parse(h[1]);
                        d.Schedule.Add(days, day);
                    }
                    days++;
                }
                
            }

                return d;

        }*/

        private static bool ToBool(string s)
        {
            if (s.ToLower() == "yes")
                return true;
            else
                return false;
        }

        private static Person ToPerson(string s)
        {
            Person p = new Person();
            Regex r = new Regex(@"\d+");
            string[] ss = Regex.Split(s, @"<((\w|/|\s)+)>");
            p.Name = ss[0];
            for (int i = 0; i < ss.Length; i++)
            {
                if (ss[i].Contains("@"))
                    p.Email = ss[i].Trim();
                else
                {
                    ss[i] = ss[i].Replace("(", "").Replace(")", "").Trim();
                    if (r.IsMatch(ss[i]))
                        p.Tel = ss[i];
                }
            }
                return p;
        }
        
        private static string getItem(string item, string page)
        {
            item = item.Replace("(", "\\(").Replace(")", "\\)");
            Regex i = new Regex("<td class=\"rowhead1\"" + @"\s*" + "width=\"" + @"\d+" + "%\">" + item + "</td><td width=\"" + @"\d+" + "%\">" + @"(.+)</td>", RegexOptions.Compiled);
            string s = i.Match(page).Groups[1].Value;
            if (s != "")
                return s.Substring(0, s.IndexOf("</td>"));
            else
                return s;
        }

        private static string getLong(string item, string page)
        {
            item = item.Replace("(", "\\(").Replace(")", "\\)");
            Regex i = new Regex("<td class=\"rowhead1\" " + @"width\s?" + "=\"" + @"\d+" + "%\">" + item + "</td><td width=\"" + @"\d+" + "%\" colspan=\"" + @"\d+" + "\">" + @"(.+)" + "</td>", RegexOptions.Compiled);
            string s= i.Match(page).Groups[1].Value;
            if (s != "")
                return s.Substring(0, s.IndexOf("</td>"));
            else
                return s;
        }

        private static string getLongClass(string item, string page)
        {
            page = page.Replace(">\t<", "><");
            item = item.Replace("(", "\\(").Replace(")", "\\)");
            Regex i = new Regex("<td class=\"rowhead1\" " + @"width\s*" + "=\"" + @"\d+" + "%\">" + item + "</td>"+@"(\\t)?"+"<td class=\"data1nobg\""+@"\s+"+"colspan"+@"\s*"+"=\"" + @"\d+" + "\">" + @"(.+)" + "</td>", RegexOptions.Compiled);
            string s = i.Match(page).Groups[2].Value;
            if (s != "")
                return s.Substring(0, s.IndexOf("</td>"));
            else
                return s;
        }

        private static string getTable(string item, string page)
        {
            Regex i = new Regex("<caption>"+item+"</caption> <tr><td>"+@"(.+)"+"</td></tr></table>", RegexOptions.Compiled);
            string s = i.Match(page).Groups[1].Value;
            if (s != "")
            {
                s= s.Substring(0, s.IndexOf("</td>"));
                s = Regex.Replace(s, @"<((\w|/|\s)+)>", " ");
                return s;
            }
            else
                return s;
        }


        private static _Class[] getCourses(Subject sub)
        {
            string page = getPage(endPoint+ sub.Url);
            Regex course = new Regex(@"<tr><td>((\w|\d)+)</td><td>((\w|\d)+)</td><td><a href=" + "\"" + @"((\w|\s|:|_|\(|\)|\d||\.|,|\-)+)" + "\"" + @">((\w|\s|:|_|\(|\)|\d||\.|,|\-)+)</a></td><td style=" + "\"" + @"white-space:nowrap" + "\"" + @">((\w|\s|\d|\(|\)|\d|\-)+)</td><td>(\d+)</td></tr>", RegexOptions.Compiled);
            MatchCollection col = course.Matches(page.Replace("\n",""));
            _Class[] s = new _Class[col.Count];

            for (int i = 0; i < col.Count; i++)
            {
                s[i] = new _Class();
                s[i].Availability = col[i].Groups[3].Value;
                s[i].Url = col[i].Groups[5].Value;
                s[i].Name = col[i].Groups[7].Value;
                s[i].Period = col[i].Groups[9].Value;
                s[i].Credits =Convert.ToInt32(col[i].Groups[11].Value);
                s[i].Code = col[i].Groups[1].Value;
               // Console.WriteLine(col[i].Groups[1].Value);
            }

            return s;
        }

        private static Subject[] getSubjects(School school)
        {
            string page = getPage(endPoint + school.Url);

            MatchCollection col = link.Matches(page);
            Subject[] s = new Subject[col.Count];

            for (int i = 0; i < col.Count; i++)
            {
                s[i] = new Subject();
                s[i].Name = col[i].Groups[3].Value;
                s[i].Url = col[i].Groups[1].Value;
            }

            return s;
        }

        private static string getPage(string url)
        {
            try
            {
                WebClient client = new WebClient();
                return client.DownloadString(url);
            }
            catch
            {
                Console.WriteLine(url);
                return "";
            }
        }

        private static School[] getSchools(string page)
        {
           
            MatchCollection col = link.Matches(page);

            School[] s = new School[col.Count];

            for (int i = 0; i < col.Count; i++)
            {
                s[i] = new School();
                s[i].Url = col[i].Groups[1].Value;
                s[i].Name=col[i].Groups[3].Value;
            }
              

            return s;
        }
    }
}
