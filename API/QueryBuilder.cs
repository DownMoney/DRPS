using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Xml;

namespace API
{
    public class QueryBuilder
    {

        private struct Item
        {
            public Regex r;
            public string sql;
            public string[] Fields;
            public bool[] Quotations;
            public bool[] Strict;
            public string[] Substitution;
        }

        private List<Item> Items = new List<Item>();

        public QueryBuilder()
        {
            Items = LoadItems("http://54.214.7.238/items.xml");
        }

        private List<Item> LoadItems(string path)
        {
            List<Item> r = new List<Item>();
            XmlDocument doc = new XmlDocument();

            doc.Load(path);

            XmlNodeList nodes = doc.SelectNodes("/Items/Item");

            for (int i = 0; i < nodes.Count; i++)
            {
                Item item = new Item();
                item.r = new Regex(nodes[i].Attributes["Regex"].InnerText, RegexOptions.Compiled);
                XmlNodeList list = nodes[i].SelectNodes("/Items/Item[@Regex='" + nodes[i].Attributes["Regex"].InnerText + "']/Fields/Field");
                item.Fields = new string[list.Count];
                item.Quotations = new bool[list.Count];
                item.Strict = new bool[list.Count];
                item.Substitution = new string[list.Count];
                item.sql = nodes[i].SelectSingleNode("/Items/Item[@Regex='" + nodes[i].Attributes["Regex"].InnerText + "']/SQL").InnerXml;
                
                for (int j = 0; j < list.Count; j++)
                {
                    item.Fields[j] = list[j].InnerText;
                    item.Quotations[j] = bool.Parse(list[j].Attributes["Quotation"].InnerText);
                    item.Strict[j] = bool.Parse(list[j].Attributes["Strict"].InnerText);
                    item.Substitution[j] = list[j].Attributes["Substitution"].InnerText;
                }

                r.Add(item);
            }

            return r;
        }

        public string BuilQuery(string node, string[] partials, string[] taken)
        {
            string sql = "SELECT * FROM " + node.ToUpper()[0] + node.Substring(1) + " WHERE ";
            for (int i = 0; i < partials.Length; i++)
            {
                sql += ToSQL(partials[i], Items);
                if (i < partials.Length - 1)
                    sql += " AND ";
            }
           // sql += " LIMIT 0,10;";
            //if(taken[0]!="")
          //  sql += addRequisites(taken);

            return sql;
        }

        private string addRequisites(string[] taken)
        {
            string s = "";
            SQL sql = new SQL();
            for (int i = 0; i < taken.Length; i++)
            {
                string cid = sql.selectQuery("SELECT CID FROM Courses WHERE Code='"+taken[i]+"';")[0]["CID"];
                string cmd = "SELECT Course FROM Requisites WHERE CID=" + cid + " AND Type=2;";
                Dictionary<string, string>[] temp = sql.selectQuery(cmd);
                for (int j = 0; j < temp.Length; j++)
                {
                    s += " AND Code<>'" + temp[j]["Course"] + "'";
                }
            }

            return s;
        }

        private string ToSQL(string part, List<Item> items)
        {
            string s = "";
            foreach (Item i in items)
            {
                if (i.r.IsMatch(part))
                {
                    Match m = i.r.Match(part);
                    string temp = m.Groups[1].Value;
                    if (i.Fields.Length > 1)
                        s += "(";
                    for (int j = 0; j < i.Fields.Length; j++)
                    {




                        if (i.sql != "")
                        {
                            SQL sql = new SQL();
                            Dictionary<string, string>[] d = sql.selectQuery(i.sql.Replace("#", temp));
                            foreach (var v in d[0])
                                temp = v.Value;

                        }

                        if (i.Substitution[j] != "")
                            temp = i.Substitution[j];


                        s += i.Fields[j];
                        if (i.Quotations[j])
                        {
                            if (!i.Strict[j])
                            {
                                s += " LIKE ";
                                s += "'%" + temp + "%'";
                            }
                            else
                                s += " = '" + temp + "'";
                        }
                        else
                        {
                            s += "=";
                            s += temp;
                        }




                        if (j < i.Fields.Length - 1)
                            s += " OR ";
                    }
                    if (i.Fields.Length > 1)
                        s += ")";
                }
            }
            return s;
        }
    }
}
