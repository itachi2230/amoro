using System;
using System.IO;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Windows;
using System.Media; // Pour SoundPlayer
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Camara_Service
{
    public partial class InfoBar : UserControl
    {
        private string filePath = Utilsv2.dbPath;
        private string backupUrl = "https://www.fxdataedge.com/public/amora.php";

        // Correction : On définit un vrai fichier pour stocker la date de sauvegarde
        private string heuresauveg = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "last_sync.bin");
        private bool aDejaParle = false; // Empêche de répéter l'alerte
        public InfoBar()
        {
            InitializeComponent();
            Loaded += DashboardBar_Loaded;
        }

        private void ChargerBackupUrl()
        {
            try
            {
                string configPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "serv.bin");
                if (File.Exists(configPath))
                {
                    string contenu = File.ReadAllText(configPath).Trim();
                    if (!string.IsNullOrWhiteSpace(contenu))
                    {
                        backupUrl = contenu;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erreur chargement serv.bin : " + ex.Message);
            }
        }

        private void DashboardBar_Loaded(object sender, RoutedEventArgs e)
        {
            ChargerBackupUrl();

            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += (s, ev) =>
            {
                ClockText.Text = DateTime.Now.ToString("HH:mm:ss");
            };
            timer.Start();

            try
            {
                // On vérifie si le fichier de date existe
                if (File.Exists(heuresauveg))
                {
                    string dateStr = File.ReadAllText(heuresauveg);
                    LastSyncText.Text = dateStr;

                    if (DateTime.TryParse(dateStr, out DateTime lastSync))
                    {
                        // Sync auto si plus de 24h
                        if ((DateTime.Now - lastSync).TotalDays >= 7)
                        {
                            CloudSync_Click(null, null);
                        }

                        // Rappel si plus de 30 jours
                        if ((DateTime.Now - lastSync).TotalDays >= 30)
                        {
                            ReminderWindow reminder = new ReminderWindow();
                            reminder.ShowDialog();
                        }
                    }
                }
                else
                {
                    LastSyncText.Text = "Jamais";
                }
            }
            catch { }

            ActualiserAlertes();
        }

        private async void CloudSync_Click(object sender, RoutedEventArgs e)
        {
            Utilsv2.log("tentative de sauvegarde...");
            try
            {
                ShowOverlay();
                // On passe le nom de l'utilisateur actuel
                await UploadDatabaseAsync(MainWindow.currentUser?.Nom ?? "Admin");
            }
            catch (Exception ex)
            {
                Utilsv2.log("Erreur globale CloudSync: " + ex.Message);
            }
            finally
            {
                HideOverlay();
                await Task.Delay(3000);
                Overlay2.Visibility = Visibility.Collapsed;
                IconSuccess.Visibility = Visibility.Collapsed;
                IconError.Visibility = Visibility.Collapsed;
            }
        }

        public async Task UploadDatabaseAsync(string username)
        {
            // 1. Vérification Connexion
            if (!HasInternetAccess())
            {
                AfficherErreur("Pas de connexion Internet.");
                return;
            }

            // 2. Vérification Serveur (Extraction du domaine pour le ping)
            Uri uri = new Uri(backupUrl);
            if (!await IsServerOnline(uri.Host))
            {
                AfficherErreur("Serveur distant inaccessible.");
                return;
            }

            try
            {
                ShowOverlay();
                Etat.Text = "📦 Génération du dump SQL...";

                // 🔥 Appel de votre méthode d'exportation
                string sqlFilePath = Utilsv2.ExporterBaseVersFichier();

                if (string.IsNullOrEmpty(sqlFilePath) || !File.Exists(sqlFilePath))
                {
                    AfficherErreur("Échec de la génération du fichier SQL.");
                    return;
                }

                Etat.Text = "☁️ Envoi vers le serveur...";

                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromMinutes(5); // Sécurité pour gros fichiers

                    using (var form = new MultipartFormDataContent())
                    {
                        byte[] fileBytes = File.ReadAllBytes(sqlFilePath);
                        string fileName = $"backup_{username}_{DateTime.Now:yyyyMMdd_HHmmss}.sql";

                        var fileContent = new ByteArrayContent(fileBytes);
                        form.Add(fileContent, "file", fileName);

                        var response = await client.PostAsync(backupUrl, form);

                        if (response.IsSuccessStatusCode)
                        {
                            string dateNow = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                            File.WriteAllText(heuresauveg, dateNow);

                            HideOverlay();
                            Overlay2.Visibility = Visibility.Visible;
                            etat2.Text = "✅ Sauvegarde réussie";
                            IconSuccess.Visibility = Visibility.Visible;
                            LastSyncText.Text = dateNow;
                            Utilsv2.log("Sauvegarde SQL terminée avec succès.");
                        }
                        else
                        {
                            AfficherErreur($"Erreur serveur: {response.StatusCode}");
                        }
                    }
                }

                // Optionnel : Supprimer le fichier SQL temporaire après envoi
               // try { File.Delete(sqlFilePath); } catch { }
            }
            catch (Exception ex)
            {
                AfficherErreur($"Erreur: {ex.Message}");
                Utilsv2.log("Exception Upload: " + ex.Message);
            }
        }

        private void AfficherErreur(string message)
        {
            HideOverlay();
            Overlay2.Visibility = Visibility.Visible;
            etat2.Text = "❌ " + message;
            IconError.Visibility = Visibility.Visible;
            IconSuccess.Visibility = Visibility.Collapsed;
        }

        // --- MÉTHODES UTILITAIRES GARDÉES ---

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
            catch { return false; }
        }
        private async Task<bool> IsServerOnline(string host)
        {
            try
            {
                using (var ping = new Ping())
                {
                    var reply = await ping.SendPingAsync(host, 3000);
                    return reply.Status == IPStatus.Success;
                }
            }
            catch { return false; }
        }
        private void ShowOverlay() { Overlay.Visibility = Visibility.Visible; Overlay.Opacity = 1; }
        private void HideOverlay() { Overlay.Visibility = Visibility.Collapsed; }
        public void SetStats((decimal montant, decimal accompte, int factures) thisweek, (decimal montant, decimal accompte, int factures) thismonth, (decimal montant, decimal accompte, int factures) thisyear)
        {
            TodaySales.Text = $"{thisweek.factures} Factures";
            TodayAmount.Text = $"total: {thisweek.montant:N0} FCFA";
            TodayAmount_Copy.Text = $"accompte: {thisweek.accompte:N0} FCFA";
            TodayAmount_Copy1.Text = $"dette: {thisweek.montant - thisweek.accompte:N0} FCFA";

            YesterdaySales.Text = $"{thismonth.factures} Factures";
            YesterdayAmount.Text = $"total: {thismonth.montant:N0} FCFA";
            YesterdayAmount_Copy.Text = $"accompte: {thismonth.accompte:N0} FCFA";
            YesterdayAmount_Copy1.Text = $"dette: {thismonth.montant - thismonth.accompte:N0} FCFA";
        }
        private void ActualiserAlertes()
        {
            try
            {
                int nb = Utilsv2.GetNombreProduitsProchesExpiration(30);

                if (nb > 0)
                {
                    NbProduitsExpires.Text = $"{nb} Produit(s)";
                    NbProduitsExpires.Foreground = new SolidColorBrush(Colors.White);
                    AlerteBorder.BorderBrush = new SolidColorBrush(Colors.Red);
                    AlerteBorder.BorderThickness = new Thickness(1);

                    // Animation de clignotement
                    DoubleAnimation blink = new DoubleAnimation(1, 0.4, TimeSpan.FromSeconds(0.8))
                    {
                        AutoReverse = true,
                        RepeatBehavior = RepeatBehavior.Forever
                    };
                    AlerteBorder.BeginAnimation(UIElement.OpacityProperty, blink);

                    // --- ALERTE AUDIO (Fichier WAV) ---
                    if (!aDejaParle)
                    {
                        aDejaParle = true;
                        Task.Run(() => {
                            try
                            {
                                string soundPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "alerteexpiration.wav");

                                if (File.Exists(soundPath))
                                {
                                    using (SoundPlayer player = new SoundPlayer(soundPath))
                                    {
                                        player.Play(); // Joue le son sans bloquer l'interface
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Utilsv2.log("Erreur lecture son: " + ex.Message);
                            }
                        });
                    }
                }
                else
                {
                    NbProduitsExpires.Text = "Aucune alerte";
                    AlerteBorder.BorderThickness = new Thickness(0);
                    AlerteBorder.BeginAnimation(UIElement.OpacityProperty, null);
                    aDejaParle = false; // On réinitialise si le stock est nettoyé
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
    }
}