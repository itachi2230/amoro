using System;
using System.Linq;
using System.Windows;


namespace Camara_Service
{
    /// <summary>
    /// Interaction logic for HistoriqueWindowst.xaml
    /// </summary>
    public partial class HistoriqueWindowst : Window
    {
        long id;string nomProduit;
        public HistoriqueWindowst(long produitId, string nomProduit)
        {
            InitializeComponent();
            this.id = produitId;
            this.nomProduit = nomProduit;
            // Met le nom du produit dans le titre
            TitreProduit.Text = $"Historique - {nomProduit}";
            charger();
            
        }
        void charger()
        {
            // Charge l’historique du produit
            var historique = Utilsv2.GetHistoriquePrix(id);

            // Pour afficher aussi le nom du produit dans chaque ligne
            var data = historique.Select(h => new
            {
                h.DateAchat,
                h.Quantite,
                h.PrixAchat,
                NomProduit = nomProduit
            }).ToList();

            HistoriqueDataGrid.ItemsSource = data;
        }

        private void Fermer_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
