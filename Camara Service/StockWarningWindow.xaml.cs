using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace Camara_Service
{
    /// <summary>
    /// Interaction logic for StockWarningWindow.xaml
    /// </summary>
    public partial class StockWarningWindow : Window
    {
        public StockWarningWindow(List<Produit> produitsSousSeuil)
        {
            InitializeComponent();
            ProduitsList.ItemsSource = produitsSousSeuil;
        }

        private void Fermer_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
