using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Camara_Service
{
    public partial class ForgotPasswordWindow : Window
    {
        public ForgotPasswordWindow()
        {
            InitializeComponent();
        }

        private async void Validate_Click(object sender, RoutedEventArgs e)
        {
            string adminCode = AdminCodeBox.Password;
            string username = UsernameBox.Text;

            if (string.IsNullOrWhiteSpace(adminCode) || string.IsNullOrWhiteSpace(username))
            {
                MessageBox.Show("Veuillez remplir tous les champs.");
                return;
            }

            bool isValid = await VerifyAdminCode(adminCode);

            if (isValid)
            {
                // Appeler ta méthode locale pour reset le mot de passe de l’utilisateur
                bool done = Utilsv2.AdminResetPasswordLocal(username, "00000000");
                if (done)
                    MessageBox.Show($"Le mot de passe de {username} a été réinitialisé à 00000000 ✅");
                else
                    MessageBox.Show("Utilisateur introuvable ❌");
            }
            else
            {
                MessageBox.Show("Code admin invalide ❌");
            }
        }

        private async Task<bool> VerifyAdminCode(string adminCode)
        {
            using (var client = new HttpClient())
            {
                var values = new Dictionary<string, string>
                {
                    { "admin_code", adminCode }
                };

                var content = new FormUrlEncodedContent(values);
                var response = await client.PostAsync("http://fxdataedge.com/verify_admin.php", content);
                string result = await response.Content.ReadAsStringAsync();

                return result.Trim() == "OK";
            }
        }
    }
}
