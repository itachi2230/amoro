using System.Windows;
using System.Windows.Input;

namespace Camara_Service
{
    public partial class GestionUsersWindow : Window
    {
        public GestionUsersWindow()
        {
            InitializeComponent();
            RafraichirListe();
        }

        private void RafraichirListe()
        {
            UsersDataGrid.ItemsSource = Utilsv2.ChargerUsers();
        }

        private void MakeAdmin_Click(object sender, RoutedEventArgs e)
        {
            ModifierRole("admin");
        }

        private void MakeUser_Click(object sender, RoutedEventArgs e)
        {
            ModifierRole("vendeur");
        }

        private void ModifierRole(string role)
        {
            if (UsersDataGrid.SelectedItem is User selected)
            {
                if (Utilsv2.ChangerRoleUser(selected.id, role))
                {
                    MessageBox.Show($"L'utilisateur {selected.Nom} est maintenant {role}.");
                    RafraichirListe();
                }
            }
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (UsersDataGrid.SelectedItem is User selected)
            {
                var result = MessageBox.Show($"Supprimer {selected.Nom} ?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    if (Utilsv2.SupprimerUser(selected.id))
                    {
                        RafraichirListe();
                    }
                }
            }
        }
        private void Header_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) this.DragMove();
        }

        private void Close_Click(object sender, RoutedEventArgs e) => this.Close();
    }
}