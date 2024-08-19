using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace umpvw
{
    static class Program
    {
        [STAThread]
        static void Main(String[] args)
        {
            if (args.Length > 1)
            {
                var pipe = MpvLaunch();

                // Все файлы добавляются в очередь
                foreach (var file in args)
                {
                    MpvLoadFile(file, true, pipe);
                }
            }
            else if (args.Length == 1)
            {
                doIpc(args[0]);
            }
            else if (args.Length == 0)
            {
                MpvLaunch();
            }
        }

        static private string pipePrefix = @"\\.\pipe\";
        static private string mpvPipe = "umpvw-mpv-pipe";
        static private string umpvwPipe = "umpvw-pipe";

        static private NamedPipeServerStream serverPipe;
        static private bool timeout = false;
        static private int timer = 300;

        static void serverTimeout()
        {
            Thread.Sleep(timer);
            timeout = true;
            var pipe = new NamedPipeClientStream(umpvwPipe);
            try
            {
                pipe.Connect();
            }
            catch (Exception)
            {
                Application.Exit();
            }
            pipe.Dispose();
        }

        static void doIpc(string arg)
        {
            bool createdNew;
            var m_Mutex = new Mutex(true, "umpvwMutex", out createdNew);

            if (createdNew) // серверная роль
            {
                var pipe = MpvLaunch(); // запускаем mpv

                serverPipe = new NamedPipeServerStream(umpvwPipe);
                var pipeReader = new StreamReader(serverPipe);
                var thread = new Thread(new ThreadStart(serverTimeout));
                thread.Start();

                var list = new List<string>();
                list.Add(arg);

                while (timeout == false)
                {
                    serverPipe.WaitForConnection();
                    var s = pipeReader.ReadLine();
                    if (!String.IsNullOrEmpty(s))
                    {
                        list.Add(s);
                    }
                    serverPipe.Disconnect();
                }

                // Все файлы добавляются в очередь
                foreach (var file in list)
                {
                    MpvLoadFile(file, true, pipe);
                }

            }
            else
            {
                var clientPipe = new NamedPipeClientStream(umpvwPipe);
                try
                {
                    clientPipe.Connect(timer);
                }
                catch (Exception)
                {
                    return;
                }
                var pipeWriter = new StreamWriter(clientPipe);
                pipeWriter.Write(arg);
                pipeWriter.Flush();
            }
        }

        static string mpvPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mpv.exe");

        // запуск mpv или получение канала
        static NamedPipeClientStream MpvLaunch()
        {
            if (!File.Exists(pipePrefix + mpvPipe))
            {
                Process.Start(mpvPath, @"--input-ipc-server=" + pipePrefix + mpvPipe);
            }
            var pipe = new NamedPipeClientStream(mpvPipe);
            pipe.Connect();
            return pipe;
        }

        // загрузка файла в mpv
        static void MpvLoadFile(string file, bool append, NamedPipeClientStream pipe)
        {
            WriteString("loadfile \"" + file.Replace("\\", "\\\\") + "\" append-play", pipe);
        }

        // запись строки в поток mpv в формате utf-8
        static public void WriteString(string outString, Stream ioStream)
        {
            byte[] outBuffer = Encoding.UTF8.GetBytes(outString + "\n");
            int len = outBuffer.Length;
            ioStream.Write(outBuffer, 0, len);
            ioStream.Flush();
        }
    }
}
