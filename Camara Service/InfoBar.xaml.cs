using System;
using System.IO;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Camara_Service
{
    /// <summary>
    /// Interaction logic for InfoBar.xaml
    /// </summary>
    public partial class InfoBar : UserControl
    {
        private string filePath = Utilsv2.dbPath;
        private string backupUrl = "https://www.fxdataedge.com/public/amora.php"; // Ton script côté serveur
        string heuresauveg = "jamais";
        public InfoBar()
        {
            InitializeComponent();
            Loaded += DashboardBar_Loaded;
            
        }
        private void ChargerBackupUrl()
        {
            try
            {
                string filePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "serv.bin");
                if (File.Exists(filePath))
                {
                    string contenu = File.ReadAllText(filePath).Trim();
                    if (!string.IsNullOrWhiteSpace(contenu))
                    {
                        backupUrl = contenu;
                    }
                }
            }
            catch (Exception ex)
            {
                // Optionnel : log ou ignore
                Console.WriteLine("Erreur chargement serv.bin : " + ex.Message);
            }
        }
        private void DashboardBar_Loaded(object sender, RoutedEventArgs e)
        {
            ChargerBackupUrl();
        
        // Lancer l'horloge en temps réel
        DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += (s, ev) =>
            {
                // Affiche HH:mm:ss
                ClockText.Text = DateTime.Now.ToString("HH:mm:ss");
            };
            timer.Start();
            try
            {
                if (File.Exists(heuresauveg))
                {
                    string date = File.ReadAllText(heuresauveg);
                    LastSyncText.Text = date;

                    if (DateTime.TryParse(date, out DateTime lastSync))
                    {
                        if ((DateTime.Now - lastSync).TotalDays >= 1)
                        {
                            CloudSync_Click(null, null);
                        }

                        if ((DateTime.Now - lastSync).TotalDays >= 7)
                        {
                            ReminderWindow reminder = new ReminderWindow();
                            reminder.ShowDialog();
                        }
                    }
                }
            }
            catch { }
            ActualiserAlertes();

        }
        public void SetStats(( decimal montant, decimal accompte, int factures) thisweek, (decimal montant, decimal accompte, int factures) thismonth, (decimal montant, decimal accompte, int factures) thisyear)
        {
            TodaySales.Text = $"{thisweek.factures} Factures";
            TodayAmount.Text = $"total: {thisweek.montant:N0} FCFA";
            TodayAmount_Copy.Text = $"accompte: {thisweek.accompte:N0} FCFA";
            TodayAmount_Copy1.Text = $"dette: {thisweek.montant-thisweek.accompte:N0} FCFA";

            YesterdaySales.Text = $"{thismonth.factures} Factures";
            YesterdayAmount.Text = $"total: {thismonth.montant:N0} FCFA";
            YesterdayAmount_Copy.Text = $"accompte: {thismonth.accompte:N0} FCFA";
            YesterdayAmount_Copy1.Text = $"dette: {thismonth.montant-thismonth.accompte:N0} FCFA";

        }

        private void ActualiserAlertes()
        {
            try
            {
                // On récupère le nombre de produits expirant dans 30 jours
                int nb = Utilsv2.GetNombreProduitsProchesExpiration(30);

                if (nb > 0)
                {
                    NbProduitsExpires.Text = $"{nb} Produit(s)";
                    NbProduitsExpires.Foreground = new SolidColorBrush(Colors.White);
                    AlerteBorder.BorderBrush = new SolidColorBrush(Colors.Red);
                    AlerteBorder.BorderThickness = new Thickness(1);

                    // Animation optionnelle pour attirer l'oeil
                    DoubleAnimation blink = new DoubleAnimation(1, 0.4, TimeSpan.FromSeconds(0.8))
                    {
                        AutoReverse = true,
                        RepeatBehavior = RepeatBehavior.Forever
                    };
                    AlerteBorder.BeginAnimation(UIElement.OpacityProperty, blink);
                }
                else
                {
                    NbProduitsExpires.Text = "Aucune alerte";
                    AlerteBorder.BorderThickness = new Thickness(0);
                    AlerteBorder.BeginAnimation(UIElement.OpacityProperty, null); // Stop animation
                }
            }
            catch { }
        }

        // Action lors du clic sur le bloc
        private void AlerteBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
           ListeExpirationWindow win = new ListeExpirationWindow();
           win.ShowDialog();
            
        }
        private async void CloudSync_Click(object sender, RoutedEventArgs e)
        {
            Utilsv2.log("tentative de sauvegarde...");
            try
            {
                // Affiche l'overlay
                ShowOverlay();

                // Upload de la base
                await UploadDatabaseAsync(MainWindow.currentUser.Nom);
               
                // Affiche Overlay2
               
            }
            catch 
            {
                
            }
            finally
            {
                // Masque l'overlay
                HideOverlay();
                await Task.Delay(3000);
                Overlay2.Visibility = Visibility.Collapsed;
                IconSuccess.Visibility = Visibility.Collapsed;
                IconError.Visibility = Visibility.Collapsed;
            }
        }
        private bool HasInternetAccess()
        {
            try
            {
                using (var ping = new Ping())
                {
                    var reply = ping.Send("8.8.8.8", 2000);
                    return reply.Status == IPStatus.Success;
                }
            }
            catch
            {
                return false;
            }
        }
        private async Task<bool> IsServerOnline(string url)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(5);
                    var response = await client.GetAsync(url);
                    return response.IsSuccessStatusCode;
                }
            }
            catch
            {
                return false;
            }
        }
        private void ShowOverlay()
        {
            Overlay.Visibility = Visibility.Visible;
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300));
            Overlay.BeginAnimation(UIElement.OpacityProperty, fadeIn);
        }
        private void HideOverlay()
        {
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(300));
            fadeOut.Completed += (s, e) => Overlay.Visibility = Visibility.Collapsed;
            Overlay.BeginAnimation(UIElement.OpacityProperty, fadeOut);
        }
       
        public async Task UploadDatabaseAsync(string username)
        {
            // Vérifications standard
            if (!HasInternetAccess())
            {
                HideOverlay();
                Overlay2.Visibility = Visibility.Visible;
                etat2.Text = "Pas de connexion Internet.";
                IconSuccess.Visibility = Visibility.Collapsed;
                IconError.Visibility = Visibility.Visible;
                Utilsv2.log("echec, pas de connexion internet.");
                return;
            }

            if (!await IsServerOnline("https://www.fxdataedge.com"))
            {
                HideOverlay();
                Overlay2.Visibility = Visibility.Visible;
                etat2.Text = "Serveur hors ligne.";
                IconSuccess.Visibility = Visibility.Collapsed;
                IconError.Visibility = Visibility.Visible;
                Utilsv2.log("echec, serveur hors ligne.");
                return;
            }

            try
            {
                ShowOverlay();
                Etat.Text = "📦 Génération du backup SQL...";

                // 🔥 Génère le .sql au lieu du .sqlite
                string sqlFilePath = Utilsv2.ExporterBaseVersFichier();

                if (!File.Exists(sqlFilePath))
                {
                    HideOverlay();
                    Overlay2.Visibility = Visibility.Visible;
                    etat2.Text = "Erreur : Fichier SQL non généré.";
                    IconSuccess.Visibility = Visibility.Collapsed;
                    IconError.Visibility = Visibility.Visible;
                    Utilsv2.log("Erreur génération SQL.");
                    return;
                }

                Etat.Text = "☁️ Envoi de la sauvegarde au serveur...";

                using (var client = new HttpClient())
                using (var form = new MultipartFormDataContent())
                using (var fileStream = File.OpenRead(sqlFilePath))
                {
                    string fileName = $"backup_{username}_{DateTime.Now:yyyyMMdd_HHmmss}.sql";

                    var fileContent = new StreamContent(fileStream);
                    form.Add(fileContent, "file", fileName);

                    var response = await client.PostAsync(backupUrl, form);

                    if (response.IsSuccessStatusCode)
                    {
                        HideOverlay();
                        Overlay2.Visibility = Visibility.Visible;
                        etat2.Text = "✅ Sauvegarde réussie";
                        IconSuccess.Visibility = Visibility.Visible;
                        IconError.Visibility = Visibility.Collapsed;
                        Utilsv2.log("Sauvegarde terminée.");

                        try { File.WriteAllText(heuresauveg, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")); } catch { }
                        LastSyncText.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    }
                    else
                    {
                        HideOverlay();
                        Overlay2.Visibility = Visibility.Visible;
                        etat2.Text = $"❌ Erreur serveur : {response.StatusCode}";
                        IconSuccess.Visibility = Visibility.Collapsed;
                        IconError.Visibility = Visibility.Visible;
                        Utilsv2.log("Echec de la sauvegarde (erreur serveur).");
                    }
                }
            }
            catch (Exception ex)
            {
                HideOverlay();
                Overlay2.Visibility = Visibility.Visible;
                etat2.Text = $"❌ Erreur : {ex.Message}";
                IconSuccess.Visibility = Visibility.Collapsed;
                IconError.Visibility = Visibility.Visible;
                Utilsv2.log("Erreur sauvegarde : " + ex.Message);
            }
        }

    }
}
