using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;
using System.Data.SqlClient;
using System.Threading;

namespace dsHelper
{
    public partial class StockForm : Form
    {
        private BindingSource bindingSource1 = new BindingSource();
        public DataTable myDataTable = new DataTable();
        public int currentRow = 0;
        public int stockChange = 0;
        public int rowCount = 0;

        public StockForm()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)

        {
            myDataTable.Columns.Add("Id");
            myDataTable.Columns.Add("Name");
            myDataTable.Columns.Add("Status");
            myDataTable.Columns.Add("SourceUrl");
            myDataTable.Columns.Add("DropshipUrl");
            //myDataTable.Columns.Add("DisplayPrice");
            //myDataTable.Columns.Add("RealPrice");
            //myDataTable.Columns.Add("DropshipPrice");

            btnStart.Enabled = true;
            btnStop.Enabled = false;            
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

        private void btnStart_Click(object sender, EventArgs e)
        {
            if (!backgroundWorker1.IsBusy)
            {
                currentRow = 0;
                stockChange = 0;
                lblProgress.Text = "สินค้า: 0 / 0";
                lblChange.Text = "สต็อกเปลี่ยน: 0";
                myDataTable.Clear();                
                btnStart.Enabled = false;
                btnStop.Enabled = true;
                backgroundWorker1.RunWorkerAsync();
            }
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            if (backgroundWorker1.WorkerSupportsCancellation == true)
            {                                
                btnStart.Enabled = true;
                btnStop.Enabled = false;                                
                backgroundWorker1.CancelAsync();                                
            }            
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            GetData(sender, e);
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {       
            if (btnStart.Enabled == false)
            { 
                if (myDataTable.Rows.Count > 0)
                { 
                    gvWordCount.DataSource = myDataTable;
                    gvWordCount.Columns[0].Width = 50;
                    gvWordCount.Columns[1].Width = 350;
                    gvWordCount.Columns[2].Width = 50;
                    gvWordCount.Columns[3].Width = 200;
                    gvWordCount.Update();
                    gvWordCount.Refresh();                                              
                }

                lblProgress.Text = "สินค้า: " + e.ProgressPercentage.ToString() + " / " + rowCount.ToString();
                lblChange.Text = "สต็อกเปลี่ยน: " + stockChange.ToString();
            }
        }       

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {            
            btnStart.Enabled = true;
            btnStop.Enabled = false;
        }

        private void GetData(System.Object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker; 
            string urlAddress = "";
            SqlConnection myConnection = new SqlConnection("Data Source = (LocalDB)\\MSSQLLocalDB; AttachDbFilename=\"C:\\Users\\ASUS\\Documents\\Visual Studio 2015\\Projects\\dsHelper\\dsHelper\\App_Data\\DropShipHelperDB.mdf\"; Integrated Security = True");
            string rowID = "0";

            try
            {
                myConnection.Open();

                SqlCommand myCommand = new SqlCommand("select * from WordCount", myConnection);
                SqlCommand myCommand2 = new SqlCommand("select COUNT(*) from WordCount", myConnection);

                rowCount = (int)myCommand2.ExecuteScalar();
                              

                SqlDataReader myReader = null;

                myReader = myCommand.ExecuteReader();

                while (myReader.Read())
                {
                    if (worker.CancellationPending == true) { break; }
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

                        bool isStockChanged = (int)myReader["WordCount"] != wordCount;
                        bool isDisplayPriceChanged = false;
                        bool isRealPriceChanged = false;
                        bool isDropshipPriceChanged = false;
                        bool isDisplayWhenChanged = isStockChanged || isDisplayPriceChanged || isRealPriceChanged || isDropshipPriceChanged;
                        rowID = myReader["Id"].ToString();

                        if (isDisplayWhenChanged)
                        {
                            DataRow myRow = myDataTable.NewRow();
                            myRow["Id"] = myReader["Id"];
                            myRow["Name"] = myReader["Name"];
                            myRow["SourceUrl"] = myReader["Url"];
                            myRow["Status"] = "เปลี่ยน";
                            myRow["DropshipUrl"] = myReader["ShopeeUrl"];
                            //myRow["DisplayPrice"] = myReader["DisplayPrice"];
                            //myRow["RealPrice"] = myReader["RealPrice"];
                            //myRow["DropshipPrice"] = myReader["DropshipPrice"];
                            stockChange++;
                            myDataTable.Rows.Add(myRow);                        
                        }
                        currentRow++;
                        
                        backgroundWorker1.ReportProgress(currentRow);                       
                    }                                        
                }

                myConnection.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(rowID.ToString() + " : " + ex.ToString());
            }
        }               
    }
}
