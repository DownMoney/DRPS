using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MySql.Data.MySqlClient;


namespace API
{
    public class SQL
    {
        private string connectionString = "";

        public SQL(string conString = "")
        {
            connectionString = conString;
        }

        public void executeQuery(string query)
        {
            MySqlConnection con = new MySqlConnection(connectionString);
            con.Open();
            MySqlCommand com = con.CreateCommand();
            com.CommandText = query;
            com.ExecuteNonQuery();
            con.Close();
        }

        public Dictionary<string, string>[] selectQuery(string query, int page)
        {
            query += " LIMIT " + (page * 10).ToString() + ",10;";
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

        public Dictionary<string, string>[] selectQuery(string query)
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

        public string[] ToArray(Dictionary<string, string>[] d, string token)
        {
            string[] s = new string[0];

            for (int i = 0; i < d.Length; i++)
            {
                if (d[i][token] != "")
                {
                    Array.Resize<string>(ref s, s.Length + 1);
                    s[s.Length - 1] = d[i][token].ToLower();
                }
            }

            return s;
        }

    }
}
