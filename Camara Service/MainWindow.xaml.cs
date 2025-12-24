using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.ComponentModel; //

namespace Camara_Service
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<ProduitFacture> produitsFacture = new List<ProduitFacture>();
        private List<Produit> produitsCharger;
        private List<Produit> _tousLesProduits = new List<Produit>();
        private Facture modifFacture;
        ObservableCollection<ProduitFacture> panierCollection = new ObservableCollection<ProduitFacture>();
        public static User currentUser=null;
        InfoBar infobar;
        public MainWindow()
        {
            InitializeComponent();
            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                dateFacture.Text = DateTime.Now.ToShortDateString();
            currentUser =new User("ghost","ghost","ghost");
            infobar = new InfoBar();
            Grid.SetColumn(infobar, 1);
            dashboard.Children.Add(infobar);
            loadStatistic();
            }
        }
        public MainWindow(User user)
        {
            InitializeComponent();
            currentUser = user;
            infobar = new InfoBar();
            Grid.SetColumn(infobar, 1);
            dashboard.Children.Add(infobar);
            loadStatistic();
            
            dateFacture.Text = DateTime.Now.ToShortDateString();
            
        }
        public void loadStatistic()
        {
            //stats produit
            List<long> liss = Utilsv2.GetTotalQuantiteByMagasin();
             for (int i=0; i<liss.Count();i++)
            {
                if (liss[i] != 0)
                {
                    double percent = liss[i] * 100 / (liss.Sum() > 0 ? liss.Sum() : 1);
                    if (i == 0)
                    {
                        cartstockcontainer.Children.Add(new StockCard("Boutique Amoro ", liss[i].ToString(), percent, percent + "% du stock total"));
                    }
                    else
                    {
                        cartstockcontainer.Children.Add(new StockCard("Magasin " + (i), liss[i].ToString(), percent, percent + "% du stock total"));
                    }
                }
            }
            //
            //stats ventes
            // Récupérer la liste des ventes (Total, Accompte)
            List<(decimal Total, decimal Accompte,int nombre)> ventes = Utilsv2.GetSalesSummary();
            infobar.SetStats(ventes[1], ventes[2], ventes[3]);
            string[] labels = { "Aujourd'hui", "Cette semaine", "Ce mois", "Cette année" };
            for (int i = 0; i < ventes.Count-1; i++)
            {
                var data = ventes[i];
                // Créer un VentesCard
                var card = new VentesCard
                {
                    Title = labels[i],
                    Value = $"{data.nombre}",       // Montant total
                    Progress = (double)(data.Accompte / (data.Total == 0 ? 1 : data.Total) * 100), // Pourcentage accompté
                    LastUpdate = $"Accompte payé: {data.Accompte} FCFA"
                };

                // Ajouter la carte au panel
                cartventecontainer.Children.Add(card);
            }
            
           

        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            new ajouterProduit().ShowDialog();
            LoadStockData();
        }

        // Affiche le Grid des produits en cliquant sur le bouton
        private void ongletProduits_Click(object sender, RoutedEventArgs e)
        {

            conteneurPrincipale.Children.Clear();
            conteneurPrincipale.Children.Add(GridProduits);
            LoadStockData();
        }

        // Méthode pour charger les produits
        private void LoadStockData()
        {
            _tousLesProduits = Utilsv2.GetProduits();
            AppliquerFiltresStock(); // On appelle la centralisation du filtrage
        }

        // Ajouter un produit
        private void AjouterProduit_Click(object sender, RoutedEventArgs e)
        {
            var ajouterProduitWindow = new ajouterProduit(); // Nouvelle fenêtre pour ajouter un produit
            if (ajouterProduitWindow.ShowDialog() == true)
            {
                LoadStockData(); // Recharge les données après ajout
            }
        }

        // Éditer un produit
        private void EditerProduit_Click(object sender, RoutedEventArgs e)
        {
            var produit = (Produit)((Button)sender).DataContext; // Récupère le produit de la ligne
            var editerProduitWindow = new EditerProduitWindow(produit); // Ouvre une fenêtre pour éditer le produit
            if (editerProduitWindow.ShowDialog() == true)
            {
                LoadStockData(); // Recharge les données après modification
            }
        }

        // Supprimer un produit
        private void SupprimerProduit_Click(object sender, RoutedEventArgs e)
        {
            var produit = (Produit)((Button)sender).DataContext;
            if (MessageBox.Show($"Voulez-vous vraiment supprimer {produit.Nom} ?", "Confirmation", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                Utilsv2.DeleteProduit(produit.id,produit.Quantite+ " "+produit.Nom); // Méthode dans Utils pour supprimer le produit
                LoadStockData(); // Recharge les données après suppression
            }
        }

        // Actualiser les données
        private void ActualiserStock_Click(object sender, RoutedEventArgs e)
        {
            LoadStockData(); // Recharge les données
        }
        private void GridProduits_Loaded(object sender, RoutedEventArgs e)
        {
            _tousLesProduits = Utilsv2.GetProduits();
            AppliquerFiltresStock();
            RechercheStockTxt.TextChanged += RechercheStockTxt_TextChanged;
            FiltreTypeComboBox.SelectionChanged += FiltreTypeComboBox_SelectionChanged;
        }
        private void AppliquerFiltresStock()
        {
            if (_tousLesProduits == null) return;

            var resultat = _tousLesProduits.AsEnumerable();

            // Filtre par Magasin (ComboBox)
            if (FiltreTypeComboBox != null && FiltreTypeComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                string selectedType = selectedItem.Content.ToString();
                if (selectedType != "Tous les magasins")
                {
                    string magasinCible = (selectedType == "Boutique Amoro") ? "0" : selectedType;
                    int magId = Convert.ToInt32(magasinCible);
                    resultat = resultat.Where(p => p.magasin == magId);
                }
            }

             //Filtre par Recherche (TextBox)
            if (RechercheStockTxt != null && !string.IsNullOrWhiteSpace(RechercheStockTxt.Text))
            {
                string recherche = RechercheStockTxt.Text.ToLower().Trim();
                resultat = resultat.Where(p =>
                    p.Nom.ToLower().Contains(recherche) ||
                    (p.Description != null && p.Description.ToLower().Contains(recherche))
                );
            }

            StockDataGrid.ItemsSource = resultat.ToList();
        }
        // 4. Les événements qui déclenchent le filtrage
        private void RechercheStockTxt_TextChanged(object sender, TextChangedEventArgs e)
        {
            AppliquerFiltresStock();
        }
        private void FiltreTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AppliquerFiltresStock();
        }
        // Méthode pour ajouter du texte dans une cellule de la grille
        private void AddTextToGrid(Grid grid, string text, int row, int column)
        {
            TextBlock textBlock = new TextBlock
            {
                Text = text,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetRow(textBlock, row);
            Grid.SetColumn(textBlock, column);
            grid.Children.Add(textBlock);
        }
        private void OngletDashboard_Click(object sender, RoutedEventArgs e)
        {
            // GridProduits.Visibility = Visibility.Collapsed;
            // dashboard.Visibility = Visibility.Visible;

            conteneurPrincipale.Children.Clear();
            cartventecontainer.Children.Clear();
            cartstockcontainer.Children.Clear();
            conteneurPrincipale.Children.Add(dashboard);
            loadStatistic();
        }
        // Gérer les changements dans la sélection
        private void ProduitComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            
            if (ProduitComboBox.SelectedItem != null)
            {
                var selectedProductName = ((Produit)ProduitComboBox.SelectedItem).Nom;
                var produitSelectionne = produitsCharger.FirstOrDefault(p => p.Nom == selectedProductName);
                if (produitSelectionne != null)
                {
                    PrixUnitaireTextBox.Text = produitSelectionne.Prix.ToString();
                }
            }
        }
        private void GridFactures_Loaded(object sender, RoutedEventArgs e)
        {
            produitsCharger = Utilsv2.GetProduits();
            ProduitComboBox.ItemsSource = produitsCharger;
            ProduitComboBox.DisplayMemberPath = "NomEtMagasin";
           // ProduitComboBox.DisplayMemberPath = "Nom";
            panierCollection.CollectionChanged += MaCollection_CollectionChanged;

        }
        private void MaCollection_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
              CalculerTotal();
        }
        private void ProduitComboBox_KeyUp(object sender, KeyEventArgs e)
        {
            try
            {
                string searchText = ProduitComboBox.Text.ToLower();
                string[] parties = searchText.Split(new string[] { " - " }, StringSplitOptions.RemoveEmptyEntries);

                // La première partie (index 0) sera le Nom
                string nomSeul = parties[0].Trim();

                // Exemple de vérification (facultatif mais recommandé)
                if (parties.Length > 0)
                {
                    nomSeul = parties[0].Trim();
                }

                var filteredList = produitsCharger.Where(p => p.Nom.ToLower().Contains(nomSeul)).ToList<Produit>();
                ProduitComboBox.ItemsSource = filteredList;
                ProduitComboBox.DisplayMemberPath = "NomEtMagasin";
                //ProduitComboBox.DisplayMemberPath = "Nom";
                ProduitComboBox.IsDropDownOpen = true; // Ouvrir la liste déroulante automatiquement
            }
            catch { }
        }
        private void AjouterProduitfacture_Click(object sender, RoutedEventArgs e)
        {
            // Récupérer les données du formulaire
            var nomProduit = ProduitComboBox.Text;

            var quantite = int.TryParse(QuantiteTextBox.Text, out var qte) ? qte : 0;
            var prixUnitaire = double.TryParse(PrixUnitaireTextBox.Text, out var prix) ? prix : 0.0;
            try
            {
                if (((Produit)ProduitComboBox.SelectedItem).Quantite < quantite)
                {
                    MessageBox.Show("La quantité de " + nomProduit + " est insuffisante il ne reste que " + ((Produit)ProduitComboBox.SelectedItem).Quantite);
                    return;
                }
            }
            catch { }
            if (string.IsNullOrWhiteSpace(nomProduit) || quantite <= 0 || prixUnitaire <= 0)
            {
                MessageBox.Show("Veuillez remplir correctement les informations du produit.");
                return;
            }

            // Créer un ProduitFacture et l'ajouter à la liste du panier
            var produitFacture = new ProduitFacture(nomProduit, prixUnitaire, quantite);
            if (ProduitComboBox.SelectedItem != null)
            {
                produitFacture.Id=((Produit)ProduitComboBox.SelectedItem).id;
            }
            // Ajouter le produit au DataGrid (supposons que votre DataGrid est lié à une ObservableCollection<ProduitFacture>)
            panierCollection.Add(produitFacture);
            PanierDataGrid.ItemsSource=panierCollection;// panierCollection est la source de données liée à PanierDataGrid

            // Réinitialiser les champs du formulaire pour un nouvel ajout
            ProduitComboBox.Text = string.Empty;
            QuantiteTextBox.Text = "1";
            PrixUnitaireTextBox.Text = string.Empty;
            ProduitComboBox.SelectedItem = null;
            // Mettre à jour le total si nécessaire
            CalculerTotal();
        }
        private void CalculerTotal()
        {
            var total = panierCollection.Sum(p => p.PrixUnitaire * p.Quantite);
            TotalTextBlock.Text = $"{total:N2} FCFA";
        }
        private void PanierDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
           CalculerTotal();
        }
        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validation des champs obligatoires
                if (string.IsNullOrWhiteSpace(nomclient.Text))
                {
                    MessageBox.Show("Le nom du client est obligatoire.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(telephoneClient.Text))
                {
                    MessageBox.Show("Le numéro de téléphone est obligatoire.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(dateFacture.Text) || !DateTime.TryParse(dateFacture.Text, out DateTime date))
                {
                    MessageBox.Show("La date est invalide ou manquante.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(accompteText.Text) || !double.TryParse(accompteText.Text, out double accompte))
                {
                    MessageBox.Show("Le montant de l'acompte est invalide ou manquant.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (panierCollection == null || panierCollection.Count == 0)
                {
                    MessageBox.Show("Le panier est vide. Ajoutez des produits avant de créer une facture.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Calcul du total de la facture
                double totalFacture = panierCollection.Sum(p => p.PrixUnitaire * p.Quantite);
                if (accompte > totalFacture)
                {
                    MessageBox.Show("L'acompte ne peut pas dépasser le montant total de la facture.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Création de l'objet Facture
                Facture facture = new Facture(
                    0,
                    date,
                    nomclient.Text.Trim(),
                    panierCollection,
                    currentUser.Nom,
                    telephoneClient.Text.Trim(),
                    accompte
                );

                // Enregistrement de la facture dans le cas direct
                if ((Button)sender != updatFacturebtnv)
                {
                    Utilsv2.EnregistrerFacture(facture);
                    //maintenant on doit mettre ajour la quantite des produits
                    foreach (ProduitFacture pdf in panierCollection)
                    {
                        if (pdf.Id != 0)
                        {
                            // 1.  méthode pour le stock global (total produit)
                            Utilsv2.UpdateProduitQuant(pdf.Id, pdf.Quantite * -1);

                            // 2. On ajoute la gestion chirurgicale des lots
                            Utilsv2.DestockerLots(pdf.Id, pdf.Quantite);
                        }
                    }
                }
                else if (modifFacture != null)
                {
                    facture.Id = modifFacture.Id;
                    Utilsv2.UpdateFacture(facture);

                    // 1. On remet dans le stock les produits de l'ancienne version de la facture
                    foreach (ProduitFacture pdf in modifFacture.Produits)
                    {
                        if (pdf.Id != 0)
                        {
                            Utilsv2.UpdateProduitQuant(pdf.Id, pdf.Quantite); // Stock global
                            Utilsv2.RestockerLots(pdf.Id, pdf.Quantite);     // Stock par lots (Ordre Expiration)
                        }
                    }

                    // 2. On enlève les produits de la nouvelle version (modifiée)
                    foreach (ProduitFacture pdf in facture.Produits)
                    {
                        if (pdf.Id != 0)
                        {
                            Utilsv2.UpdateProduitQuant(pdf.Id, pdf.Quantite * -1); // Stock global
                            Utilsv2.DestockerLots(pdf.Id, pdf.Quantite);          // Stock par lots (Ordre Expiration)
                        }
                    }
                }
                else { }
                updatFacturebtn.Visibility = Visibility.Hidden;
              
                IconSuccess.Visibility = Visibility.Visible;
                //
                
                // Réinitialisation des champs après succès
                ReinitialiserChamps();
                await Task.Delay(2000);
                IconSuccess.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                // Gestion des erreurs inattendues
                MessageBox.Show($"Une erreur s'est produite lors de l'enregistrement de la facture : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        // Méthode pour réinitialiser les champs après l'enregistrement
        private void ReinitialiserChamps()
        {
            nomclient.Text = string.Empty;
            telephoneClient.Text = string.Empty;
            dateFacture.Text = DateTime.Now.ToString("yyyy-MM-dd");
            accompteText.Text = string.Empty;
            panierCollection.Clear();
            CalculerTotal();
        }
        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            conteneurPrincipale.Children.Clear();
            conteneurPrincipale.Children.Add(GridFactures);
        }
        private void HistoriqueFacturesOnglet(object sender, RoutedEventArgs e)
        {
            conteneurPrincipale.Children.Clear();
            conteneurPrincipale.Children.Add(HistoriqueFacturesGrid);
            ChargerHistoriqueFactures();

        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            conteneurPrincipale.Children.Clear();
            conteneurPrincipale.Children.Add(dashboard);
            var produitsSousSeuil = Utilsv2.GetProduitsSousSeuil();
            if (produitsSousSeuil.Any())
            {
                var fenetre = new StockWarningWindow(produitsSousSeuil);
                fenetre.ShowDialog();
            }
        }
        private void ChargerHistoriqueFactures()
        {
            try
            {
                // Récupération des factures depuis Utils
                var factures = Utilsv2.RecupererFactures();

                // Lier les factures au DataGrid
                FacturesDataGrid.ItemsSource = factures;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des factures : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void RechercheFacture_TextChanged(object sender, TextChangedEventArgs e)
        {
            string recherche = RechercheFacture.Text.ToLower();
            var facturesFiltrees = Utilsv2.RecupererFactures()
                .Where(f => f.Client.ToLower().Contains(recherche) || f.Id.ToString().Contains(recherche))
                .ToList();

            FacturesDataGrid.ItemsSource = facturesFiltrees;
        }
        private void FiltrerFacturesBtn_Click(object sender, RoutedEventArgs e)
        {
            DateTime? dateDebut = DateDebut.SelectedDate;
            DateTime? dateFin = DateFin.SelectedDate;

            if (dateDebut == null || dateFin == null)
            {
                MessageBox.Show("Veuillez sélectionner les deux dates.", "Information", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (dateDebut > dateFin)
            {
                MessageBox.Show("La date de début ne peut pas être après la date de fin.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var facturesFiltrees = Utilsv2.RecupererFactures()
                .Where(f => f.Date >= dateDebut && f.Date <= dateFin)
                .ToList();

            FacturesDataGrid.ItemsSource = facturesFiltrees;
        }
        private void ReinitialiserFiltres()
        {
            RechercheFacture.Text = string.Empty;
            DateDebut.SelectedDate = null;
            DateFin.SelectedDate = null;

            FacturesDataGrid.ItemsSource = Utilsv2.RecupererFactures();
        }
        private void FacturesDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (FacturesDataGrid.SelectedItem != null)
            {
                (new UpdateFacture((Facture)(FacturesDataGrid.SelectedItem))).ShowDialog();
                ChargerHistoriqueFactures();
            }
        }
        private void FacturesDataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && FacturesDataGrid.SelectedItem is Facture selectedFacture)
            {
                // Demander une confirmation avant suppression
                MessageBoxResult result = MessageBox.Show(
                    $"Voulez-vous vraiment supprimer la facture ID {selectedFacture.Id} ?",
                    "Confirmation de suppression",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        // Appeler la méthode de suppression dans la classe Utils
                        bool isDeleted = Utilsv2.SupprimerFacture(selectedFacture.Id,selectedFacture);

                        if (isDeleted)
                        {
                            //je remet dans le stock les produit de la facture
                            foreach (ProduitFacture pdf in selectedFacture.Produits)
                            {
                                 Utilsv2.UpdateProduitQuant(pdf.Id, pdf.Quantite,"suppresion de facture");
                            }
                            // Retirer la facture supprimée de la liste affichée en rechargeant
                            ChargerHistoriqueFactures();

                            MessageBox.Show($"Facture ID {selectedFacture.Id} supprimée avec succès.", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            MessageBox.Show($"Échec de la suppression de la facture ID {selectedFacture.Id}.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Erreur lors de la suppression : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }

                // Empêcher la propagation de l'événement Delete
                e.Handled = true;
            }
        }
        private void LoadCurrentUser()
        {
            // Exemple d'authentification utilisateur
            textUsername.Text = currentUser.Username;

            // Afficher le panneau administrateur si c'est un admin
            adminPanel.Visibility = currentUser.Info == "Administrateur" ? Visibility.Visible : Visibility.Collapsed;
        }
        /// Modifier le mot de passe de l'utilisateur connecté.
        /// </summary>
        private void BtnChangePassword_Click(object sender, RoutedEventArgs e)
        {
            string oldPassword = txtOldPassword.Password;
            string newPassword = txtNewPassword.Password;

            if (string.IsNullOrWhiteSpace(oldPassword) || string.IsNullOrWhiteSpace(newPassword))
            {
                MessageBox.Show("Veuillez remplir tous les champs.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (Utilsv2.Authenticate(currentUser.Username, oldPassword) == null )
            {
                MessageBox.Show("L'ancien mot de passe est incorrect.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (Utilsv2.ChangePassword(currentUser.Username, newPassword))
            {
                MessageBox.Show("Mot de passe modifié avec succès.", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                txtOldPassword.Clear();
                txtNewPassword.Clear();
            }
            else
            {
                MessageBox.Show("Erreur lors de la modification du mot de passe.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        /// <summary>
        /// Valide le mot de passe administrateur pour afficher le formulaire de création d'utilisateur.
        /// </summary>
        private void BtnAdminValidate_Click(object sender, RoutedEventArgs e)
        {
            string adminPassword = txtAdminPassword.Password;

            if (Utilsv2.Authenticate(currentUser.Username, adminPassword) == null)
            {
                MessageBox.Show("Mot de passe administrateur incorrect.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            createUserPanel.Visibility = Visibility.Visible;
        }
        /// <summary>
        /// Crée un nouvel utilisateur.
        /// </summary>
        private void BtnCreateUser_Click(object sender, RoutedEventArgs e)
        {
            string newName = txtNewUserName.Text;
            string newUsername = txtNewUsername.Text;
            string newPassword = txtNewUserPassword.Password;


            if (string.IsNullOrWhiteSpace(newName) || string.IsNullOrWhiteSpace(newUsername) || string.IsNullOrWhiteSpace(newPassword))
            {
                MessageBox.Show("Veuillez remplir tous les champs.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            User newUser = new User(newName, newUsername, newPassword);

            if (Utilsv2.AjouterUser(newUser))
            {
                MessageBox.Show($"Utilisateur '{newUsername}' créé avec succès.", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                txtNewUserName.Clear();
                txtNewUsername.Clear();
                txtNewUserPassword.Clear();
                createUserPanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                MessageBox.Show("Ce nom d'utilisateur existe déjà.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void CloseWindow(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private void OngletParametres1_Click(object sender, RoutedEventArgs e)
        {
            conteneurPrincipale.Children.Clear();
            conteneurPrincipale.Children.Add(gridSettings);
        }
        private void settingloaded(object sender, RoutedEventArgs e)
        {
            textUsername.Text = currentUser.Nom;
            if (currentUser.Nom == "Admin") { adminPanel.Visibility = Visibility.Visible; }
        }
        private void ExporterFactureBtn_Copy_Click(object sender, RoutedEventArgs e)
        {
            if (!(FacturesDataGrid.SelectedItem is Facture selectedFacture))
            {
                MessageBox.Show("Veuillez sélectionner une facture à imprimer.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            //supprimer le nom de magasin avant impression
            if (selectedFacture?.Produits == null) return;

            foreach (var produit in selectedFacture.Produits)
            {
                if (string.IsNullOrWhiteSpace(produit.Nom))
                    continue;

                // Cherche un motif du type " - Magasin 12" à la fin
                int index = produit.Nom.LastIndexOf(" - M ", StringComparison.OrdinalIgnoreCase);
                if (index > -1)
                {
                    // On supprime cette partie
                    produit.Nom = produit.Nom.Substring(0, index).Trim();
                }
            }
            // Chemin dans Documents
            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string fileName = $"Facture_{selectedFacture.Id}_{SanitizeFileName(selectedFacture.Client)}.pdf";
            string filePath = System.IO.Path.Combine(documentsPath, fileName);
            try
            {
                // Exporter en PDF

                ExporterFactureEnPdf(selectedFacture, filePath);

                // Ouvrir et imprimer automatiquement
                if (File.Exists(filePath))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(filePath) { UseShellExecute = true });

                }
                else
                {
                    MessageBox.Show("Le fichier PDF n'a pas été trouvé après l'exportation.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'ouverture ou impression : {ex.Message}");
            }
        }
        private string SanitizeFileName(string name)
        {
            foreach (char c in System.IO.Path.GetInvalidFileNameChars())
            {
                name = name.Replace(c, '_');
            }
            return name;
        }
        public static string NumberToWords(long number)
        {
            if (number == 0)
                return "zéro";

            if (number < 0)
                return "moins " + NumberToWords(Math.Abs(number));

            string words = "";

            // Millions
            if ((number / 1000000) > 0)
            {
                long millions = number / 1000000;
                words += NumberToWords(millions) + " million";
                if (millions > 1) words += "s";
                words += " ";
                number %= 1000000;
            }

            // Milliers
            if ((number / 1000) > 0)
            {
                long milliers = number / 1000;
                if (milliers == 1)
                    words += "mille ";
                else
                    words += NumberToWords(milliers) + " mille ";
                number %= 1000;
            }

            // Centaines
            if ((number / 100) > 0)
            {
                long centaines = number / 100;
                if (centaines == 1)
                    words += "cent";
                else
                    words += NumberToWords(centaines) + " cent";

                number %= 100;

                if (number == 0 && centaines > 1)
                    words += "s"; // pluriel si rien après
                else if (number > 0)
                    words += " "; // espace si nombre continue
            }

            // Unités et dizaines
            if (number > 0)
            {
                string[] unitsMap = { "zéro", "un", "deux", "trois", "quatre", "cinq", "six", "sept", "huit", "neuf",
                              "dix", "onze", "douze", "treize", "quatorze", "quinze", "seize" };
                string[] tensMap = { "", "", "vingt", "trente", "quarante", "cinquante", "soixante" };

                if (number < 17)
                {
                    words += unitsMap[number];
                }
                else if (number < 20)
                {
                    words += "dix-" + unitsMap[number - 10];
                }
                else if (number < 70)
                {
                    long tens = number / 10;
                    long units = number % 10;
                    words += tensMap[tens];
                    if (units == 1)
                        words += " et un";
                    else if (units > 1)
                        words += "-" + unitsMap[units];
                }
                else if (number < 80) // 70-79
                {
                    long units = number - 60;
                    if (units == 11)
                        words += "soixante et onze";
                    else
                        words += "soixante-" + NumberToWords(units);
                }
                else if (number < 100) // 80-99
                {
                    long units = number - 80;
                    words += "quatre-vingt";
                    if (units == 0)
                        words += "s";
                    else if (units == 1)
                        words += "-un";
                    else
                        words += "-" + NumberToWords(units);
                }
            }

            return words.Trim();
        }
        public static void ExporterFactureEnPdf(Facture facture, string cheminFichier)
        {
            // Marges normales pour le contenu
            float margeGauche = 40, margeDroite = 40, margeHaut = 60, margeBas = 40;
            Document doc = new Document(PageSize.A4, margeGauche, margeDroite, margeHaut, margeBas);
            PdfWriter writer = PdfWriter.GetInstance(doc, new FileStream(cheminFichier, FileMode.Create));
            doc.Open();

            // Couleurs et polices
            BaseColor bleuFonce = new BaseColor(0, 70, 130);
            BaseColor bleuClair = new BaseColor(230, 240, 255);
            BaseColor grisClair = new BaseColor(245, 245, 245);

            var fontNormal = FontFactory.GetFont(FontFactory.HELVETICA, 10, BaseColor.BLACK);
            var fontGras = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12, BaseColor.BLACK);
            var fontBlanc = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, BaseColor.WHITE);

            // Insertion des shapes et logo directement sur PdfContentByte
            var cb = writer.DirectContent;

            
            
            // Logo
            iTextSharp.text.Image logo = iTextSharp.text.Image.GetInstance("logo2.png");
            logo.ScaleToFit(150,100);
            logo.SetAbsolutePosition(0, doc.PageSize.Height - margeHaut - 50); // juste sous le bord haut
            
            cb.AddImage(logo);

            // head
            iTextSharp.text.Image head = iTextSharp.text.Image.GetInstance("head.png");
            head.ScaleToFit(400, 100);
            head.SetAbsolutePosition(doc.PageSize.Width/2 -150, doc.PageSize.Height- 130); // juste sous le bord haut

            cb.AddImage(head);

            // ==== En-tête ====
            PdfPTable header = new PdfPTable(3) { WidthPercentage = 100 };
            header.SetWidths(new float[] { 11f,60f, 25f });
            header.AddCell(new PdfPCell(new Phrase("\n", fontNormal))
            {
                Border = Rectangle.NO_BORDER,
            });
            Chunk chunkDoit = new Chunk("DOIT :", fontGras);
            chunkDoit.SetUnderline(0.8f, -2f); // 0.8f = épaisseur, -2f = position (distance sous le texte)

            Chunk chunkClient = new Chunk("  "+facture.Client, fontGras);

            Phrase phrase = new Phrase();
            phrase.Add("\n\n\n\n\n\nFacture N° " + facture.Id + "\n\n");
            phrase.Add(chunkDoit);
            phrase.Add(chunkClient);

            PdfPCell infosFacture = new PdfPCell(phrase)
            {
                Border = Rectangle.NO_BORDER,
                HorizontalAlignment = Element.ALIGN_LEFT
            };
            header.AddCell(infosFacture);
           
            header.AddCell(new PdfPCell(new Phrase($"\n\n\n\n\n\nDate : {facture.Date:dd/MM/yyyy}\n\n", fontGras))
            {
                Border = Rectangle.NO_BORDER,
            });

            doc.Add(header);
            doc.Add(new Paragraph("\n"));

            // ==== Tableau Produits ====
            PdfPTable tableProduits = new PdfPTable(4) { WidthPercentage = 100 };
            tableProduits.SetWidths(new float[] { 50f, 15f, 15f, 20f });

            string[] headers = { "Désignation", "Qté", "P.U", "Montant" };
            foreach (var h in headers)
            {
                PdfPCell cell = new PdfPCell(new Phrase(h, fontGras))
                {
                    BackgroundColor = bleuClair,
                    HorizontalAlignment = Element.ALIGN_CENTER,
                    Padding = 5
                };
                tableProduits.AddCell(cell);
            }

            bool altern = false;
            foreach (var p in facture.Produits)
            {
                BaseColor bg = altern ? bleuClair : BaseColor.WHITE;
                tableProduits.AddCell(CelluleTexteBg(p.Nom, fontNormal, bg));
                tableProduits.AddCell(CelluleTexteBg(p.Quantite.ToString(), fontNormal, bg, Element.ALIGN_CENTER));
                tableProduits.AddCell(CelluleTexteBg($"{p.PrixUnitaire:N0} ", fontNormal, bg, Element.ALIGN_RIGHT));
                tableProduits.AddCell(CelluleTexteBg($"{p.Total:N0} ", fontNormal, bg, Element.ALIGN_RIGHT));
                altern = !altern;
            }

            doc.Add(tableProduits);
            doc.Add(new Paragraph("\n"));

            // ==== Totaux ====
            PdfPTable tableTotaux = new PdfPTable(2) { WidthPercentage = 40, HorizontalAlignment = Element.ALIGN_RIGHT };
            tableTotaux.SetWidths(new float[] { 50f, 50f });

            tableTotaux.AddCell(CelluleTexteBg("Total", fontGras, grisClair));
            tableTotaux.AddCell(CelluleTexteBg($"{facture.Total:N0} CFA", fontGras, grisClair, Element.ALIGN_RIGHT));

            tableTotaux.AddCell(CelluleTexteBg("Accompte", fontNormal, BaseColor.WHITE));
            tableTotaux.AddCell(CelluleTexteBg($"{facture.Accompte:N0} CFA", fontNormal, BaseColor.WHITE, Element.ALIGN_RIGHT));

            tableTotaux.AddCell(CelluleTexteBg("Reliquat", fontNormal, grisClair));
            tableTotaux.AddCell(CelluleTexteBg($"{facture.Reliquat:N0} CFA", fontNormal, grisClair, Element.ALIGN_RIGHT));

            doc.Add(tableTotaux);

            PdfPTable tableTexte = new PdfPTable(1);
            tableTexte.WidthPercentage = 53; // 50% de la page
            tableTexte.HorizontalAlignment = Element.ALIGN_LEFT;

            PdfPCell cellTexte = new PdfPCell(new Phrase(
                "\nArrêté la présente facture à la somme de :  " +
                NumberToWords(Convert.ToInt64(facture.Total))+ " FCFA", fontGras))
            {
                Border = Rectangle.NO_BORDER,
                HorizontalAlignment = Element.ALIGN_LEFT
            };

            tableTexte.AddCell(cellTexte);
            doc.Add(tableTexte);
            doc.Close();
        }
        private static PdfPCell CelluleTexteBg(string texte, Font font, BaseColor bg, int align = Element.ALIGN_LEFT)
        {
            return new PdfPCell(new Phrase(texte, font))
            {
                BackgroundColor = bg,
                HorizontalAlignment = align,
                Padding = 5
            };
        }
        private void FacturesDataGrid_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (FacturesDataGrid.SelectedItem != null)
            {
                insererfact((Facture)FacturesDataGrid.SelectedItem);
            }
            conteneurPrincipale.Children.Clear();
            conteneurPrincipale.Children.Add(GridFactures);
            updatFacturebtn.Visibility = Visibility.Visible;
        }
        private void insererfact(Facture fact) //remplir le panier avec une facture fourni
        {
            modifFacture = fact;
            nomclient.Text = fact.Client;
            telephoneClient.Text = fact.Telephone;
            dateFacture.Text = fact.Date.ToShortDateString();
            accompteText.Text = fact.Accompte.ToString();

            panierCollection.Clear();
            foreach (ProduitFacture pdr in fact.Produits)
            {
                panierCollection.Add(pdr);
            }
            PanierDataGrid.ItemsSource = panierCollection;
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            new HistoriqueWindow().ShowDialog();
        }

        private void ExporterFactureBtn_Copy1_Click(object sender, RoutedEventArgs e)
        {
            new HistoriqueWindow("s").ShowDialog();
        }

        private void ExporterFactureBtn_Copy2_Click(object sender, RoutedEventArgs e)
        {
            if (FacturesDataGrid.SelectedItem is Facture selectedFacture)
            {
                // Demander une confirmation avant suppression
                MessageBoxResult result = MessageBox.Show(
                $"Voulez-vous vraiment supprimer la facture ID {selectedFacture.Id} ?",
                "Confirmation de suppression",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        // Appeler la méthode de suppression dans la classe Utils
                        bool isDeleted = Utilsv2.SupprimerFacture(selectedFacture.Id, selectedFacture);

                        if (isDeleted)
                        {
                            //je remet dans le stock les produit de la facture
                            foreach (ProduitFacture pdf in selectedFacture.Produits)
                            {
                                Utilsv2.UpdateProduitQuant(pdf.Id, pdf.Quantite, "suppresion de facture");
                            }
                            // Retirer la facture supprimée de la liste affichée en rechargeant
                            ChargerHistoriqueFactures();

                            MessageBox.Show($"Facture ID {selectedFacture.Id} supprimée avec succès.", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            MessageBox.Show($"Échec de la suppression de la facture ID {selectedFacture.Id}.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Erreur lors de la suppression : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }

                // Empêcher la propagation de l'événement Delete
                e.Handled = true;
            }
    }

        private void StockDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (StockDataGrid.SelectedItem is Produit produit)
            {
                HistoriqueWindowst fenetre = new HistoriqueWindowst(produit.id, produit.Nom);
                fenetre.ShowDialog();
            }

        }
        private void BtnGestionUsers_Click(object sender, RoutedEventArgs e)
        {
            // On vérifie si l'utilisateur actuel est admin
            if (MainWindow.currentUser.Info.ToLower() == "admin")
            {
                GestionUsersWindow win = new GestionUsersWindow();
                win.ShowDialog();
            }
            else
            {
                MessageBox.Show("Accès réservé aux administrateurs ! 🚫");
            }
        }
    }
   
}
