using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Linq;

namespace Camara_Service
{
    public partial class ListeExpirationWindow : Window
    {
        private List<Utilsv2.LotAlerte> _fullList = new List<Utilsv2.LotAlerte>();

        public ListeExpirationWindow()
        {
            InitializeComponent();
            // On attend que les composants soient chargés pour éviter les NullReference
            this.Loaded += (s, e) => ChargerDonnees();
        }

        private void ChargerDonnees()
        {
            if (FiltreDelai == null) return;

            int jours = 30;
            if (FiltreDelai.SelectedItem is ComboBoxItem selectedItem)
            {
                string content = selectedItem.Content.ToString();
                if (content.Contains("90")) jours = 90;
                else if (content.Contains("Tous")) jours = 9999;
            }

            // Récupération ultra-rapide grâce aux nouvelles colonnes SQL
            _fullList = Utilsv2.GetAlertesExpiration(jours);
            AppliquerFiltres();
        }

        private void AppliquerFiltres()
        {
            if (RechercheTxt == null || ExpirationGrid == null || _fullList == null) return;

            var filtered = _fullList.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(RechercheTxt.Text))
            {
                string recherche = RechercheTxt.Text.ToLower();
                filtered = filtered.Where(a => a.NomProduit != null &&
                                             a.NomProduit.ToLower().Contains(recherche));
            }

            ExpirationGrid.ItemsSource = filtered.ToList();
        }

        // --- GESTION DES PERTES / RETRAITS MANUELS ---
        private void BtnFermerLot_Click(object sender, RoutedEventArgs e)
        {
            // Récupération du lot lié à la ligne cliquée
            var button = sender as Button;
            var lot = button?.DataContext as Utilsv2.LotAlerte;

            if (lot != null)
            {
                var result = MessageBox.Show(
                    $"Voulez-vous masquer ce lot de '{lot.NomProduit}' ?\nIl ne s'affichera plus dans les alertes.",
                    "Masquer le lot",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    // On met à jour uniquement la table ProduitLots
                    // La quantité globale du produit ne bougera pas
                    if (Utilsv2.MarquerLotCommePerdu(lot.LotId))
                    {
                        // On recharge la liste pour faire disparaître la ligne
                        ChargerDonnees();
                    }
                    else
                    {
                        MessageBox.Show("Erreur lors de la mise à jour du lot.");
                    }
                }
            }
        }
        // --- LOGIQUE D'INTERFACE ---
        private void RechercheTxt_TextChanged(object sender, TextChangedEventArgs e) => AppliquerFiltres();
        private void FiltreDelai_SelectionChanged(object sender, SelectionChangedEventArgs e) => ChargerDonnees();

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed) DragMove();
        }

        private void Close_Click(object sender, RoutedEventArgs e) => this.Close();
        private void Minimize_Click(object sender, RoutedEventArgs e) => this.WindowState = WindowState.Minimized;
    }
}