using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Camara_Service
{
    public partial class ajouterProduit : Window
    {
        public ajouterProduit()
        {
            InitializeComponent();
        }
        // Permet de déplacer la fenêtre
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        // Bouton pour fermer la fenêtre
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // Bouton pour agrandir/réduire la fenêtre
        private void ToggleMaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
            }
            else
            {
                this.WindowState = WindowState.Maximized;
            }
        }
        private void AddProductButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Récupérer les valeurs des champs
                string nom = NomTextBox.Text;
                string description = DescriptionTextBox.Text;
                int magasin = Convert.ToInt32((TypeComboBox.SelectedItem as ComboBoxItem)?.Content.ToString());
                if (TypeComboBox.SelectedItem==null)
                {
                    magasin = 0;
                }
                double prix;
                long quantite;
                if (string.IsNullOrEmpty(nom) ||
                    !double.TryParse(PrixTextBox.Text, out prix) || !long.TryParse(QuantiteTextBox.Text, out quantite))
                {
                    MessageBox.Show("Veuillez remplir tous les champs correctement.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Créer un nouvel objet produit
                Produit produit = new Produit(nom, quantite, description, magasin, prix);
                produit.CreatedAt= DateTime.Now;
                
                // Ajouter le produit en utilisant la classe Utils

                if (Utilsv2.AddProduit(produit, "Stock"))
                {
                    // Afficher le message de confirmation
                    SuccessMessageTextBlock.Visibility = Visibility.Visible;
                    // Réinitialiser les champs
                    NomTextBox.Text = string.Empty;
                    DescriptionTextBox.Text = string.Empty;
                    TypeComboBox.SelectedIndex = -1;
                    PrixTextBox.Text = string.Empty;
                    QuantiteTextBox.Text = string.Empty;
                    // Ici tu peux déjà utiliser produit.id
                    Utilsv2.AjouterPrixAchat(produit.id, produit.Prix, produit.Quantite, "Fournisseur par défaut");
                }
                else
                {
                    MessageBox.Show("Erreur lors de l'ajout du produit.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur: " + ex.Message, "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
