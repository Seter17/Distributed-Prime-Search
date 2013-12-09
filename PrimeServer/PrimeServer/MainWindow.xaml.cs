using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PrimeServer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        enum MessageType
        {
            Notification,
            Warning,
            Exception
        }

        public MainWindow()
        {
            InitializeComponent();
            Server.Started += (sender, s) => this.WriteToSystemLog(s, MessageType.Notification);
            Server.Listening += (sender, s) => this.WriteToSystemLog(s, MessageType.Notification);
            Server.DataRecieved += (sender, s) => this.WriteToSystemLog(String.Format("Recieved Message: {0}",s), MessageType.Notification);
            Server.Exception += (sender, e) => this.WriteToSystemLog(e.Message, MessageType.Exception);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Server.Instance.Launch();
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            this.WriteToSystemLog("Stop...", MessageType.Notification);
            Server.Instance.Stop();
        }

        private void WriteToSystemLog(string message, MessageType type)
        {
            Dispatcher.BeginInvoke(new ThreadStart(() =>
                                                   {
                                                       Color fontColor;

                                                       switch (type)
                                                       {
                                                           case MessageType.Exception:
                                                               fontColor = Colors.Red;
                                                               break;
                                                           case MessageType.Warning:
                                                               fontColor = Colors.Yellow;
                                                               break;
                                                           case MessageType.Notification:
                                                               fontColor = Colors.Green;
                                                               break;
                                                           default:
                                                               fontColor = Colors.Black;
                                                               break;
                                                       }

                                                       var mess = new Run(message) { Foreground = new SolidColorBrush(fontColor) };
                                                       SystemLog.Inlines.Add(mess);
                                                       SystemLog.Inlines.Add(new LineBreak());
                                                   }));

        }
    }
}
