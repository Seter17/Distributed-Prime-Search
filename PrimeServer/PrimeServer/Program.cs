using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace PrimeServer
{
    class Program
    {
        private static bool isclosing = false;

        enum MessageType
        {
            Notification,
            Warning,
            Exception,
            Common
        }

        static void Main(string[] args)
        {
            SetConsoleCtrlHandler(new HandlerRoutine(ConsoleCtrlCheck), true);

            Server.Started += (sender, s) => WriteToSystemLog(s, MessageType.Notification);
            //Server.Listening += (sender, s) => this.WriteToSystemLog(s, MessageType.Notification);
            Server.DataRecieved += (sender, s) =>
                                   {
                                       if (s.Equals("Gimme")) return; //we do not need this shit
                                       WriteToSystemLog(String.Format("Recieved Message: {0}", s),
                                           MessageType.Common);
                                   };
            Server.Sent += (sender, s) =>
                WriteToSystemLog(String.Format("Sent Message: {0}", s), MessageType.Notification);

            Server.TimeoutCheckBegan += (sender, ar) =>
                WriteToSystemLog("Checking pending values...", MessageType.Warning);

            Server.Exception += (sender, e) => WriteToSystemLog(e.Message, MessageType.Exception);

            Console.WriteLine("Press ESC for correct exit");

            try
            {
                var ip = ConfigurationManager.AppSettings["ServerIp"];
                var port = Int32.Parse(ConfigurationManager.AppSettings["ServerPort"]);
                var pendingPath = ConfigurationManager.AppSettings["PendingFilePath"];
                var primePath = ConfigurationManager.AppSettings["PrimeDataFilePath"];

                Server.Instance.Configurate(pendingPath,primePath, port, ip);
            }
            catch (Exception ex)
            {

                WriteToSystemLog(String.Format("Configuration data loading failed: {0}", ex.Message), MessageType.Exception);
            }

            Server.Instance.Launch();

            var key  = Console.ReadKey();
            if (key.Key == ConsoleKey.Escape)
            {
                Server.Instance.Stop();
            }
        }

        static private void WriteToSystemLog(string message, MessageType type)
        {
            switch (type)
            {
                case MessageType.Notification:
                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                    break;
                case MessageType.Warning:
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    break;
                case MessageType.Exception:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                default:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
            }

            Console.WriteLine(message);
            Console.ResetColor();
        }

        private static bool ConsoleCtrlCheck(CtrlTypes ctrlType)
        {

            switch (ctrlType)
            {
                case CtrlTypes.CTRL_CLOSE_EVENT:
                    WriteToSystemLog("Program being closed!",MessageType.Warning);
                    Server.Instance.Stop();
                    break;
            }

            return true;
        }

        #region unmanaged
        // Declare the SetConsoleCtrlHandler function
        // as external and receiving a delegate.
        [DllImport("Kernel32")]
        public static extern bool SetConsoleCtrlHandler(HandlerRoutine Handler, bool Add);

        // A delegate type to be used as the handler routine
        // for SetConsoleCtrlHandler.
        public delegate bool HandlerRoutine(CtrlTypes CtrlType);

        // An enumerated type for the control messages
        // sent to the handler routine.
        public enum CtrlTypes
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT,
            CTRL_CLOSE_EVENT,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT
        }

        #endregion
    }
}
