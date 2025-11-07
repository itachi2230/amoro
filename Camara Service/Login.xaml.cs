using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
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
    /// Interaction logic for Login.xaml
    /// </summary>
    public partial class Login : Window
    {
        
        public Login()
        {
            InitializeComponent();
            try
            {
                Utilsv2.InitMySqlDatabase();
            }catch(Exception e) { Utilsv2.log(e.Message); }
            if (!Utilsv2.TestConnexionServeur())
            {
                var win = new ServerConnectionWindow();
                bool? result = win.ShowDialog();
                if (result != true) return; // si l'utilisateur n'a pas reconnecté → on arrête ici
            }

        }
        private void CloseWindow_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                Connexion_Click(sender, e); // Appelle la méthode de connexion
            }
        }
        private void Connexion_Click(object sender, RoutedEventArgs e)
        {
           
            string username = UsernameTextBox.Text.Trim();
            string password = PassworddBox.Password.Trim(); 
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Veuillez entrer un nom d'utilisateur et un mot de passe.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Charger la liste des utilisateurs existants
            var users = Utilsv2.ChargerUsers();

            // Si aucun utilisateur n'existe, créer le premier utilisateur
            if (users.Count == 0)
            {
                var firstUser = new User("Admin", username, password, "admin");
                Utilsv2.AjouterUser(firstUser);

                // Ouvrir la fenêtre principale avec le premier utilisateur
                OpenMainWindow(firstUser);
                return;
            }

            // Vérifier les informations de connexion
            var loggedInUser = Utilsv2.Authenticate(username, password);
            if (loggedInUser != null )
            {
                OpenMainWindow(loggedInUser);
            }
            else
            {
                incorect.Visibility = Visibility.Visible;
            }
        }

        private void OpenMainWindow(User user)
        {
            MainWindow mainWindow = new MainWindow(user);
            mainWindow.Show();
            this.Close(); // Fermer la fenêtre de connexion
        }

        private void Incorect_MouseUp(object sender, MouseButtonEventArgs e)
        {
            new ForgotPasswordWindow().ShowDialog();
        }

        private void TextBlock_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (UsernameTextBox.Text == "itachi223")
            {
                Utilsv2.MigrerSQLiteVersMySQL();
            }
        }
    }
}
