using System;
using System.Windows.Forms;
using System.Net.Sockets;
using System.IO;

namespace client
{
    static class Program
    {
        public static Socket client;
        public static NetworkStream ns;
        public static StreamReader nr;
        public static StreamWriter nw;

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new client());
        }
    }
}