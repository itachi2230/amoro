using System.Windows;
using System.Windows.Input;
using Camara_Service;

namespace Camara_Service
{
    public partial class UpdateFacture : Window
    {
        private Facture _facture;

        public UpdateFacture(Facture facture)
        {
            InitializeComponent();
            _facture = facture;
            LoadFactureDetails();
        }

        private void LoadFactureDetails()
        {
            // Remplir les champs avec les données de la facture
            txtClient.Text = _facture.Client;
            txtTelephone.Text = _facture.Telephone;
            txtTotal.Text = $"{_facture.Total.ToString("N0")} FCFA";
            txtAccompte.Text = $"{_facture.Accompte.ToString("N0")} FCFA";
            txtReliquat.Text = $"{_facture.Reliquat.ToString("N0")} FCFA";

            // Lier les produits au DataGrid
            dataGridProduits.ItemsSource = _facture.Produits;
        }

        private void BtnAjouterAccompte_Click(object sender, RoutedEventArgs e)
        {
            if (double.TryParse(txtMontantAccompte.Text, out double montantAjout))
            {
                

                if (montantAjout + _facture.Accompte > _facture.Total)
                {
                    MessageBox.Show("Le montant ajouté dépasse le total de la facture.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    

                    
                    Utilsv2.MettreAJourAcompteFacture(_facture.Id, montantAjout);
                    _facture.ajouteraccompte(montantAjout);
                    txtAccompte.Text = _facture.Accompte.ToString("F2");
                    txtReliquat.Text = _facture.Reliquat.ToString("F2");
                    MessageBox.Show("Acompte mis à jour avec succès.", "Confirmation", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                MessageBox.Show("Veuillez entrer un montant valide.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnFermer_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }
        private void BtnMaximize_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
                this.WindowState = WindowState.Normal;
            else
                this.WindowState = WindowState.Maximized;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            new HistoriqueWindow("f",_facture.Id).ShowDialog();
        }
    }
}
