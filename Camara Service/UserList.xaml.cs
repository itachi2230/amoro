using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace Camara_Service
{
    public partial class UserList : Window
    {
        public List<User> Users { get; set; } = new List<User>();

        public UserList()
        {
            InitializeComponent();
            Loaded += UserList_Loaded;
        }

        private async void UserList_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Récupération des utilisateurs
                Users = await Utils.GetUtilisateursAsync();
                UserDataGrid.ItemsSource = Users;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des utilisateurs : {ex.Message}");
            }
        }

        private void CloseWindow_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void DeleteUser_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is User user)
            {
                if (user.Username != MainWindow.currentUser.Username)
                {


                    if (MessageBox.Show($"Voulez-vous vraiment supprimer {user.Username} ?", "Confirmation", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    {
                        try
                        {
                            await Utils.DeleteUserAsync(user.Username);
                            Users.Remove(user);
                            UserDataGrid.Items.Refresh();
                            MessageBox.Show($"{user.Username} a été supprimé avec succès.");
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Erreur lors de la suppression de {user.Username} : {ex.Message}");
                        }
                    }
                }
                else { MessageBox.Show($"Vous ne pouvez pas changer votre propre role"); }
            }
        }

        private async void GrantAdmin_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is User user)
            {
                if (user.Username != MainWindow.currentUser.Username)
                {
                    if (user.Info == "admin")
                    {

                        try
                        {
                            user.Info = "user";
                            await Utils.UpdateUserAsync(user);
                            UserDataGrid.Items.Refresh();
                            MessageBox.Show($"{user.Username} n'est plus administrateur.");
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Erreur lors de la mise à jour de {user.Username} : {ex.Message}");
                        }
                    }
                    else
                    {
                        try
                        {
                            user.Info = "admin";
                            await Utils.UpdateUserAsync(user);
                            UserDataGrid.Items.Refresh();
                            MessageBox.Show($"{user.Username} a été promu administrateur.");
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Erreur lors de la mise à jour de {user.Username} : {ex.Message}");
                        }
                    }
                }
                else { MessageBox.Show($"Vous ne pouvez pas changer votre propre role"); }
            }
        }
    }
}
