using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Camara_Service
{
    public partial class HistoriqueWindow : Window
    {
        public HistoriqueWindow(string source = "p", int id = 0)
        {
            InitializeComponent();
            ChargerHistorique(source,id);
        }

        private void ChargerHistorique(string source="p",int id= 0)
        {
            List<string> logStock=null;
            switch (source)
            {
                case "p":
                    logStock = Utilsv2.RecupererStockLogs();
                    break;
                case "f":
                    logStock = Utilsv2.RecupererFactureLogs(id);
                    break;
                case "s":
                    logStock = Utilsv2.RecupererLogsSuppressionsFactures();
                    break;
                default:
                    break;
            }
            
            if (logStock.Count!=0)
            {
                foreach (var log in logStock)
                {
                    
                    LogsList.Items.Add(log);
                }
            }
            else
            {
                LogsList.Items.Add("Aucun historique disponible.");
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }
    }

}
