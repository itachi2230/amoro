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
    /// Interaction logic for EditerProduitWindow.xaml
    /// </summary>
    public partial class EditerProduitWindow : Window
    {
        long id;
        public EditerProduitWindow()
        {
            InitializeComponent();
        }   
        public EditerProduitWindow(Produit produit)
        {
            InitializeComponent();
            NomTextBox.Text = produit.Nom;
            DescriptionTextBox.Text = produit.Description;
            PrixTextBox.Text = produit.Prix.ToString();
            PrixTextBox_Copy.Text = produit.Prix.ToString();
            TypeComboBox.Text = produit.magasin.ToString();
            QuantiteTextBox.Text = produit.Quantite.ToString();
            this.id = produit.id;

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
                double prix;
                double prixAchat;
                long quantite;
                DateTime? dateExp = ExpirationDatePicker.SelectedDate;
                if (string.IsNullOrEmpty(nom) ||
                    !double.TryParse(PrixTextBox_Copy.Text, out prix) || !long.TryParse(QuantiteTextBox_Copy.Text, out quantite) || !double.TryParse(PrixTextBox1.Text, out prixAchat))
                {
                    MessageBox.Show("Veuillez remplir tous les champs correctement.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                long ancienq= Convert.ToInt64(QuantiteTextBox.Text);
                // Créer un nouvel objet produit
                Produit produit = new Produit(nom, quantite+ancienq, description, magasin, prix,this.id);
                produit.LastModified = DateTime.Now;
                // Ajouter le produit en utilisant la classe Utils 
                if (Utilsv2.UpdateProduit(this.id, produit))
                {
                    Utilsv2.AjouterPrixAchat(produit.id, prixAchat,quantite);
                    // 2. NOUVEAU : Enregistrement du Lot
                    // On n'ajoute un lot que si une quantité positive a été ajoutée ET qu'une date est saisie
                    if (dateExp.HasValue && quantite > 0)
                    {
                        // On passe 'ancienq' comme étant le stock existant avant l'ajout
                        Utilsv2.AjouterLot((int)this.id, (int)quantite, dateExp.Value, (int)ancienq);
                    }
                    // Afficher le message de confirmation
                    SuccessMessageTextBlock.Visibility = Visibility.Visible;

                    // Réinitialiser les champs
                    this.Close();
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
