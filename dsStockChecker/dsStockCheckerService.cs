using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;
using System.Data.SqlClient;

namespace dsStockChecker
{
    public partial class dsStockCheckerService : ServiceBase
    {
        public DataTable myDataTable;

        public dsStockCheckerService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            myDataTable.Columns.Add("Id");
            myDataTable.Columns.Add("Status");
        }

        protected override void OnStop()
        {
        }

        public int CountStringOccurrences(string text, string pattern)
        {
            // Loop through all instances of the string 'text'.
            int count = 0;
            int i = 0;
            while ((i = text.IndexOf(pattern, i)) != -1)
            {
                i += pattern.Length;
                count++;
            }
            return count;
        }

        private void GetData(int startId, int endId)
        {
            string urlAddress = "";
            SqlConnection myConnection = new SqlConnection("Data Source = (LocalDB)\\MSSQLLocalDB; AttachDbFilename=\"C:\\Users\\ASUS\\documents\\visual studio 2015\\Projects\\DropShipHelper\\DropShipHelper\\App_Data\\DropShipHelperDB.mdf\"; Integrated Security = True");

            try
            {
                myConnection.Open();

                SqlCommand myCommand = new SqlCommand("select * from WordCount where Id >= " + startId.ToString() + " and Id <= " + endId.ToString(), myConnection);
                SqlDataReader myReader = null;

                myReader = myCommand.ExecuteReader();

                while (myReader.Read())

                {
                    urlAddress = myReader["Url"].ToString();

                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(urlAddress);
                    request.UserAgent = "Mozilla/5.0 (Windows; U; Windows NT 5.0; en-US; rv:1.8.0.5) Gecko/20060719 Firefox/1.5.0.5)";
                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        Stream receiveStream = response.GetResponseStream();
                        StreamReader readStream = null;

                        if (response.CharacterSet == null)
                        {
                            readStream = new StreamReader(receiveStream);
                        }
                        else
                        {
                            readStream = new StreamReader(receiveStream, Encoding.GetEncoding(response.CharacterSet));
                        }

                        string data = readStream.ReadToEnd();

                        response.Close();
                        readStream.Close();

                        var wordCount = CountStringOccurrences(data, "ขออภัย สินค้าหมดค่ะ");

                        DataRow myRow = myDataTable.NewRow();
                        myRow["Id"] = myReader["Id"];
                        
                        if ((int)myReader["WordCount"] != wordCount)
                        {
                            myRow["Status"] = 1;                            
                        }
                        else
                        {
                            myRow["Status"] = 0;
                        }                        
                        
                        myDataTable.Rows.Add(myRow);
                    }
                }

                myConnection.Close();
            }
            catch (Exception ex)
            {
                this.EventLog.WriteEntry(ex.ToString(), EventLogEntryType.Error);
            }
        }
    }
}
