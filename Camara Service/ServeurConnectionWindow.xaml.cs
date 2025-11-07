using System.Windows;
using System.Net;
using System.Net.Sockets;
namespace Camara_Service
{
    public partial class ServerConnectionWindow : Window
    {
        public ServerConnectionWindow()
        {
            InitializeComponent();
            monip.Text="Mon IP : "+ GetLocalIPAddress();
        }
        public static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork) // IPv4
                {
                    return ip.ToString();
                }
            }
            return "IP non trouvée";
        }
        private void RetryConnection_Click(object sender, RoutedEventArgs e)
        {
            if (Utilsv2.TestConnexionServeur())
            {
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Connexion toujours impossible.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void OpenConfig_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("notepad.exe", Utilsv2.configPath);
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
