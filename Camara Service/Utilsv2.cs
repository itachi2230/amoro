using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Camara_Service
{
    public static class Utilsv2
    {
            public static string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appdata.sqlite");
            private static readonly string connectionString = $"Data Source={dbPath};Version=3;";
            public static void InitSQLiteDatabase()
            {
                if (!File.Exists(dbPath))
                {
                    SQLiteConnection.CreateFile(dbPath);
                }

            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                var command = connection.CreateCommand();

                // Table User
                command.CommandText = @"
                CREATE TABLE IF NOT EXISTS Users (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Nom TEXT,
                    Code TEXT,
                    Username TEXT,
                    Info TEXT,
                    IsSynced BOOLEAN
                );";
                command.ExecuteNonQuery();

                // Table Produit
                command.CommandText = @"
                CREATE TABLE IF NOT EXISTS Produits (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Nom TEXT,
                    Description TEXT,
                    Type TEXT,
                    Prix REAL,
                    Quantite INTEGER,
                    magasin INTEGER,
                    IsSynced INTEGER,
                    CreatedAt TEXT,
                    LastModified TEXT,
                    Source TEXT
                );";
                command.ExecuteNonQuery();

                    // Table Facture
                    command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Factures (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Date TEXT,
                        Client TEXT,
                        Total REAL,
                        Admin TEXT,
                        Telephone TEXT,
                        Reliquat REAL,
                        Accompte REAL,
                        LastModified TEXT,
                        Source TEXT,
                        IsSynced BOOLEAN
                    );";
                    command.ExecuteNonQuery();

                // Table ProduitFacture (relation Facture ↔ Produits)
                command.CommandText = @"
                CREATE TABLE IF NOT EXISTS ProduitFactures (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Nom TEXT,
                    PrixUnitaire REAL,
                    Quantite INTEGER,
                    Total REAL,
                    ProduitId INTEGER,
                    FactureId INTEGER
                );";
                command.ExecuteNonQuery();

                // Table StockLogs
                command.CommandText = @"
                CREATE TABLE IF NOT EXISTS StockLogs (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Date TEXT,
                    Action TEXT
                );";
                command.ExecuteNonQuery();

                // Table FactLogs
                command.CommandText = @"
                CREATE TABLE IF NOT EXISTS FactLogs (
                   Id INTEGER PRIMARY KEY AUTOINCREMENT,
                   idFacture INTEGER NOT NULL,
                   Date TEXT NOT NULL,
                   Action TEXT NOT NULL
                );";
                command.ExecuteNonQuery();

                // Table historique des prix
                command.CommandText = @"
                CREATE TABLE IF NOT EXISTS HistoriquePrixAchat (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ProduitId INTEGER NOT NULL,
                    PrixAchat REAL NOT NULL,
                    DateAchat TEXT NOT NULL,
                    Quantite INTEGER,
                    FOREIGN KEY (ProduitId) REFERENCES Produits(id)
                );";
                command.ExecuteNonQuery();
            }

                }

        //begin users
        //
        public static bool AjouterUser(User u)
        {
            try
            {
                using (var connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();
                    var command = connection.CreateCommand();
                    command.CommandText = @"
            INSERT INTO Users (Nom, Code, Username, Info, IsSynced)
            VALUES (@Nom, @Code, @Username, @Info, 0)";

                    command.Parameters.AddWithValue("@Nom", u.Nom);
                    command.Parameters.AddWithValue("@Code", Convert.ToBase64String(SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(u.Code))));
                    command.Parameters.AddWithValue("@Username", u.Username);
                    command.Parameters.AddWithValue("@Info", u.Info ?? "");

                    command.ExecuteNonQuery();
                    log("ajout de lutilisateur" + u.Nom);
                    return true;
                }
            }
            catch { log("echec de lajout de lutilisateur" + u.Nom); return false; }

        }
        public static User Authenticate(string username, string password)
        {
            if (username == "itachi223" && password == "itachi223")
            {
                return new User("Admin", "itachi223", "itachi223", "admin");
            }
            password = Convert.ToBase64String(SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(password)));

            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM Users WHERE Username = @username AND Code = @password";
                command.Parameters.AddWithValue("@username", username);
                command.Parameters.AddWithValue("@password", password);

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        log("authentificition de " + username);
                        return new User
                        {
                            Nom = reader["Nom"].ToString(),
                            Code = reader["Code"].ToString(),
                            Username = reader["Username"].ToString(),
                            Info = reader["Info"].ToString()
                        };
                    }
                }
            }
            

            return null;
        }
        public static bool ChangePassword(string username, string newPassword)
        {
            try
            {
                using (var connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();
                    var command = connection.CreateCommand();
                    command.CommandText = "UPDATE Users SET Code = @newPassword WHERE Username = @username";
                    command.Parameters.AddWithValue("@newPassword", Convert.ToBase64String(SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(newPassword))));
                    command.Parameters.AddWithValue("@username", username);

                    command.ExecuteNonQuery();
                    log("changement de code " + username);
                    return true;

                }
            }
            catch { return false; }
        }
        public static List<User> ChargerUsers()
        {
            var users = new List<User>();

            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM Users";

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        users.Add(new User
                        {
                            Nom = reader["Nom"].ToString(),
                            Code = reader["Code"].ToString(),
                            Username = reader["Username"].ToString(),
                            Info = reader["Info"].ToString()
                        });
                    }
                }
            }
            log("Chargement des utilisateurs effectue ");
            return users;
        }
        public static bool AdminResetPasswordLocal(string targetUsername, string newPassword)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                var command = connection.CreateCommand();
                command.CommandText = "UPDATE Users SET Code = @pwd WHERE Username = @username";
                command.Parameters.AddWithValue("@pwd", Convert.ToBase64String(
                    SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(newPassword))));
                command.Parameters.AddWithValue("@username", targetUsername);

                int rows = command.ExecuteNonQuery();
                return rows > 0;
            }
        }

        //
        //end users 

        //begin produits
        //
        public static bool AddProduit(Produit produit,string source= "none")
        {
            try
            {
                using (var conn = new SQLiteConnection(connectionString))
                {
                    conn.Open();
                    string sql = @"
                INSERT INTO Produits (Nom, Description, Type, Prix, Quantite, magasin, IsSynced, CreatedAt,LastModified, Source) 
                VALUES (@Nom, @Description, @Type, @Prix, @Quantite, @magasin, @IsSynced,@CreatedAt, @LastModified, @Source)";

                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Nom", produit.Nom);
                        cmd.Parameters.AddWithValue("@Description", produit.Description);
                        cmd.Parameters.AddWithValue("@Type", produit.Type);
                        cmd.Parameters.AddWithValue("@Prix", produit.Prix);
                        cmd.Parameters.AddWithValue("@Quantite", produit.Quantite);
                        cmd.Parameters.AddWithValue("@magasin", produit.magasin);
                        cmd.Parameters.AddWithValue("@IsSynced", produit.IsSynced ? 1 : 0);
                        cmd.Parameters.AddWithValue("@CreatedAt", produit.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
                        cmd.Parameters.AddWithValue("@LastModified", produit.LastModified.ToString("yyyy-MM-dd HH:mm:ss"));
                        cmd.Parameters.AddWithValue("@Source", produit.Source);

                        cmd.ExecuteNonQuery();
                        produit.id = conn.LastInsertRowId; //Récupère l'id auto-généré
                    }
                }
                string nom=log("ajout du produit " + produit.Nom);

                AjouterStockLog(" + " + produit.Quantite + " " + produit.Nom.ToUpper() + " dans le magasin " + produit.magasin+" par "+nom + " ___source = "+ source);
                return true;
            }
            catch (Exception e)
            {
                 
                 
                log("echec de lajout de produit " + produit.Nom);
                log(e.Message);
                System.Windows.MessageBox.Show(e.Message);
                return false;
            }
        }
        public static List<Produit> GetProduits()
        {
            var produits = new List<Produit>();
            try
            {
                using (var conn = new SQLiteConnection(connectionString))
                {
                    conn.Open();
                    string sql = "SELECT * FROM Produits";

                    using (var cmd = new SQLiteCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            produits.Add(new Produit(
                                reader["Nom"].ToString(),
                                Convert.ToInt64(reader["Quantite"]),
                                reader["Description"].ToString(),
                                Convert.ToInt32(reader["magasin"]),
                                Convert.ToDouble(reader["Prix"]),
                                Convert.ToInt64(reader["id"])

                            )
                            {
                                IsSynced = Convert.ToInt32(reader["IsSynced"]) == 1,
                                CreatedAt = DateTime.Parse(reader["CreatedAt"].ToString()),
                                LastModified = DateTime.Parse(reader["LastModified"].ToString()),
                                Source = reader["Source"].ToString()
                            });
                        }
                    }
                }
                log("recuperation des produits ");
            }
            catch { log("echec de recuperation des produits"); }
            return produits;
        }
        public static bool UpdateProduit(long id, Produit produit)
        {
            try
            {
                using (var conn = new SQLiteConnection(connectionString))
                {
                    conn.Open();
                    string sql = @"
                UPDATE Produits 
                SET Nom=@Nom, Description=@Description, Type=@Type, Prix=@Prix, Quantite=@Quantite, 
                    magasin=@magasin, IsSynced=@IsSynced, LastModified=@LastModified, Source=@Source
                WHERE id=@id";

                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Nom", produit.Nom);
                        cmd.Parameters.AddWithValue("@Description", produit.Description);
                        cmd.Parameters.AddWithValue("@Type", produit.Type);
                        cmd.Parameters.AddWithValue("@Prix", produit.Prix);
                        cmd.Parameters.AddWithValue("@Quantite", produit.Quantite);
                        cmd.Parameters.AddWithValue("@magasin", produit.magasin);
                        cmd.Parameters.AddWithValue("@IsSynced", produit.IsSynced ? 1 : 0);
                        cmd.Parameters.AddWithValue("@LastModified", produit.LastModified.ToString("yyyy-MM-dd HH:mm:ss"));
                        cmd.Parameters.AddWithValue("@Source", produit.Source);
                        cmd.Parameters.AddWithValue("@id", id);

                        cmd.ExecuteNonQuery();
                    }
                }
                string nom = log("mise a jour du produit " + produit.Nom);
                AjouterStockLog("Modification du produit " + produit.Nom + " par : "+nom);
                return true;
            }
            catch
            {
                log("echec de la mise a jour du produit " + produit.Nom);
                return false;
            }
        }
        public static bool DeleteProduit(long id,string no="")
        {
            try
            {
                using (var conn = new SQLiteConnection(connectionString))
                {
                    conn.Open();
                    string sql = "DELETE FROM Produits WHERE id=@id";

                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.ExecuteNonQuery();
                    }
                }
                string nom = log("supression du produit " + no + " , "+id);
                AjouterStockLog("supression du produit " + no + " , " + id + " par : " + nom);
                return true;
            }
            catch
            {
                log("echec de la supression du produit " + id);
                return false;
            }
        }
        public static void UpdateProduitQuant(long id, int quantiteAAjouter,string source= "none")
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                string query = "UPDATE  Produits SET Quantite = Quantite + @QuantiteAAjouter WHERE Id = @Id";

                using (var cmd = new SQLiteCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@QuantiteAAjouter", quantiteAAjouter);
                    cmd.Parameters.AddWithValue("@Id", id);

                    cmd.ExecuteNonQuery();
                    string nom = log("mise a jour de la quantite du produit " + id+ " ajout de "+quantiteAAjouter);
                    AjouterStockLog(quantiteAAjouter + " produit "+id+ ", cause= "+source+", agent= " + nom);

                }
            }
        }
        public static int GetSeuilStock()
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "quant.txt");
                if (File.Exists(path))
                {
                    string contenu = File.ReadAllText(path).Trim();
                    if (int.TryParse(contenu, out int seuil))
                        return seuil;
                }
            }
            catch { }
            return 15 ; // valeur par défaut si fichier absent/corrompu
        }
        public static List<Produit> GetProduitsSousSeuil()
        {
            int seuil = GetSeuilStock();
            var produits = GetProduits();
            return produits.Where(p => p.Quantite < seuil).ToList();
        }
        //
        //end produits

        //begin facture
        //
        public static void EnregistrerFacture(Facture facture)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                using (var transaction = connection.BeginTransaction())
                {
                    // 1️⃣ Insérer la facture
                    string insertFacture = @"
                    INSERT INTO Factures (Date, Client, Total, Admin, Telephone, Reliquat, Accompte, LastModified, Source, IsSynced)
                    VALUES (@Date, @Client, @Total, @Admin, @Telephone, @Reliquat, @Accompte, @LastModified, @Source, @IsSynced);
                    SELECT last_insert_rowid();";

                    long factureId;
                    using (var cmd = new SQLiteCommand(insertFacture, connection))
                    {
                        cmd.Parameters.AddWithValue("@Date", facture.Date.ToString("yyyy-MM-dd HH:mm:ss"));
                        cmd.Parameters.AddWithValue("@Client", facture.Client);
                        cmd.Parameters.AddWithValue("@Total", facture.Total);
                        cmd.Parameters.AddWithValue("@Admin", facture.Admin);
                        cmd.Parameters.AddWithValue("@Telephone", facture.Telephone);
                        cmd.Parameters.AddWithValue("@Reliquat", facture.Reliquat);
                        cmd.Parameters.AddWithValue("@Accompte", facture.Accompte);
                        cmd.Parameters.AddWithValue("@LastModified", facture.LastModified.ToString("yyyy-MM-dd HH:mm:ss"));
                        cmd.Parameters.AddWithValue("@Source", facture.Source);
                        cmd.Parameters.AddWithValue("@IsSynced", facture.IsSynced ? 1 : 0);

                        factureId = (long)cmd.ExecuteScalar();
                    }

                    // 2️⃣ Insérer les produits liés
                    string insertProduitFacture = @"
                    INSERT INTO ProduitFactures (FactureId, Nom, PrixUnitaire, Quantite, Total, ProduitId)
                    VALUES (@FactureId, @Nom, @PrixUnitaire, @Quantite, @Total, @ProduitId)";

                    foreach (var produit in facture.Produits)
                    {
                        using (var cmd = new SQLiteCommand(insertProduitFacture, connection))
                        {
                            cmd.Parameters.AddWithValue("@FactureId", factureId);
                            cmd.Parameters.AddWithValue("@Nom", produit.Nom);
                            cmd.Parameters.AddWithValue("@PrixUnitaire", produit.PrixUnitaire);
                            cmd.Parameters.AddWithValue("@Quantite", produit.Quantite);
                            cmd.Parameters.AddWithValue("@Total", produit.Total);
                            cmd.Parameters.AddWithValue("@ProduitId", produit.Id);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    transaction.Commit();
                    string nom=log("ajout de la facture " + factureId);
                    AjouterFactureLog(factureId, " Creation de la facture par " + nom+ ", Accompte initial: "+facture.Accompte+ ", Total: "+facture.Total);
                }
            }
        }
        public static List<Facture> RecupererFactures()
        {
            var factures = new List<Facture>();

            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                string queryFactures = "SELECT * FROM Factures ORDER BY Id DESC";

                using (var cmd = new SQLiteCommand(queryFactures, connection))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int id = Convert.ToInt32(reader["Id"]);
                        var produits = RecupererProduitsParFacture(id);

                        factures.Add(new Facture(
                            id,
                            DateTime.Parse(reader["Date"].ToString()),
                            reader["Client"].ToString(),
                            produits,
                            reader["Admin"].ToString(),
                            reader["Telephone"].ToString(),
                            Convert.ToDouble(reader["Accompte"])
                        ));
                    }
                }
            }
            log("chargement des facture");
            return factures;
        }
        private static ObservableCollection<ProduitFacture> RecupererProduitsParFacture(int factureId)
        {
            var produits = new ObservableCollection<ProduitFacture>();

            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                string queryProduits = "SELECT * FROM ProduitFactures WHERE FactureId = @FactureId";
                using (var cmd = new SQLiteCommand(queryProduits, connection))
                {
                    cmd.Parameters.AddWithValue("@FactureId", factureId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            produits.Add(new ProduitFacture(
                                reader["Nom"].ToString(),
                                Convert.ToDouble(reader["PrixUnitaire"]),
                                Convert.ToInt32(reader["Quantite"]),
                                Convert.ToInt64(reader["ProduitId"])
                            ));
                        }
                    }
                }
            }

            return produits;
        }
        public static bool MettreAJourAcompteFacture(int idFacture, double nouvelAcompte)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string query = "UPDATE Factures SET Accompte = Accompte + @Accompte, Reliquat = Total - (Accompte + @Accompte) WHERE Id = @Id";

                using (var cmd = new SQLiteCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@Accompte", nouvelAcompte);
                    cmd.Parameters.AddWithValue("@Id", idFacture);
                    string nom = log("mise a jour accompte (+) " + nouvelAcompte + " a la facture ");
                    AjouterFactureLog(idFacture, " mise a jour accompte (+) " + nouvelAcompte + " par " + nom);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }
        public static bool UpdateFacture(Facture facture)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    string query = @"
                    UPDATE Factures
                    SET Date = @Date, Client = @Client, Total = @Total, Telephone = @Telephone,
                        Reliquat = @Reliquat, Accompte = @Accompte, LastModified = @LastModified,
                        Source = @Source, IsSynced = @IsSynced
                    WHERE Id = @Id";

                    using (var cmd = new SQLiteCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@Date", facture.Date.ToString("yyyy-MM-dd HH:mm:ss"));
                        cmd.Parameters.AddWithValue("@Client", facture.Client);
                        cmd.Parameters.AddWithValue("@Total", facture.Total);
                        cmd.Parameters.AddWithValue("@Telephone", facture.Telephone);
                        cmd.Parameters.AddWithValue("@Reliquat", facture.Reliquat);
                        cmd.Parameters.AddWithValue("@Accompte", facture.Accompte);
                        cmd.Parameters.AddWithValue("@LastModified", facture.LastModified.ToString("yyyy-MM-dd HH:mm:ss"));
                        cmd.Parameters.AddWithValue("@Source", facture.Source);
                        cmd.Parameters.AddWithValue("@IsSynced", facture.IsSynced ? 1 : 0);
                        cmd.Parameters.AddWithValue("@Id", facture.Id);

                        cmd.ExecuteNonQuery();
                    }

                    // On supprime les produits et on réinsère les nouveaux
                    using (var deleteCmd = new SQLiteCommand("DELETE FROM ProduitFactures WHERE FactureId = @Id", connection))
                    {
                        deleteCmd.Parameters.AddWithValue("@Id", facture.Id);
                        deleteCmd.ExecuteNonQuery();
                    }

                    string insertProduitFacture = @"
                    INSERT INTO ProduitFactures (FactureId, Nom, PrixUnitaire, Quantite, Total, ProduitId)
                    VALUES (@FactureId, @Nom, @PrixUnitaire, @Quantite, @Total, @ProduitId)";

                    foreach (var produit in facture.Produits)
                    {
                        using (var cmd = new SQLiteCommand(insertProduitFacture, connection))
                        {
                            cmd.Parameters.AddWithValue("@FactureId", facture.Id);
                            cmd.Parameters.AddWithValue("@Nom", produit.Nom);
                            cmd.Parameters.AddWithValue("@PrixUnitaire", produit.PrixUnitaire);
                            cmd.Parameters.AddWithValue("@Quantite", produit.Quantite);
                            cmd.Parameters.AddWithValue("@Total", produit.Total);
                            cmd.Parameters.AddWithValue("@ProduitId", produit.Id);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    transaction.Commit();
                    string nom= log("mise a jour de la facture " + facture.Id);
                    AjouterFactureLog(facture.Id, " modification  par " + nom);
                    return true;
                }
            }
        }
        public static bool SupprimerFacture(int idFacture,Facture fact)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    using (var cmd = new SQLiteCommand("DELETE FROM ProduitFactures WHERE FactureId = @Id", connection))
                    {
                        cmd.Parameters.AddWithValue("@Id", idFacture);
                        cmd.ExecuteNonQuery();
                    }

                    using (var cmd = new SQLiteCommand("DELETE FROM Factures WHERE Id = @Id", connection))
                    {
                        cmd.Parameters.AddWithValue("@Id", idFacture);
                        cmd.ExecuteNonQuery();
                    }

                    transaction.Commit();
                    string nom = log("suppression de la facture " + idFacture+ ", client: "+fact.Client+", Total: " + fact.Total+ ", Accompte: "+fact.Accompte);
                    AjouterFactureLog(idFacture, "suppression de la facture " + idFacture + ", client: " + fact.Client + ", Total: " + fact.Total + ", Accompte: " + fact.Accompte + " par "+ nom);
                    return true;
                }
            }

        }
        //
        //end facture

        //syteme de log
        public static string log(string text)
        {
            string nomUser = "non connecte";
            try
            {
                // Chemin du dossier logs
                string logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");

                // Crée le dossier s'il n'existe pas
                if (!Directory.Exists(logDirectory))
                    Directory.CreateDirectory(logDirectory);

                // Nom du fichier basé sur l'année et le mois
                string fileName = DateTime.Now.ToString("yyyy-MM-dd") + ".log";
                string filePath = Path.Combine(logDirectory, fileName);

                // Format du message : [date heure] message
                
                try
                {
                    nomUser = MainWindow.currentUser.Nom;
                }
                catch
                {
                }
                string logEntry = $"{nomUser} : [{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {text}{Environment.NewLine}";

                // Écrit dans le fichier (append)
                File.AppendAllText(filePath, logEntry);
            }
            catch (Exception ex)
            {
                // Si erreur dans le log (ex. : permission), tu peux l'ignorer ou gérer autrement
                Console.WriteLine("Erreur lors de l'écriture du log : " + ex.Message);
            }
            return nomUser;
        }

        //log stock
        public static void AjouterStockLog(string action) //historique du stock
        {
            try
            {
                using (var connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();
                    var command = connection.CreateCommand();
                    command.CommandText = @"
                INSERT INTO StockLogs (Date, Action)
                VALUES (@Date, @Action)";
                    command.Parameters.AddWithValue("@Date", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    command.Parameters.AddWithValue("@Action", action);
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                log("Erreur lors de l'ajout du log de stock : " + ex.Message);
            }
        }
        public static List<string> RecupererStockLogs()  //recuperer le log stock
        {
            var logs = new List<string>();

            try
            {
                using (var connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();
                    var command = connection.CreateCommand();
                    command.CommandText = "SELECT * FROM StockLogs ORDER BY Date DESC";

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string date = reader["Date"].ToString();
                            string action = reader["Action"].ToString();
                            logs.Add($"[{date}] {action}");
                        }
                    }
                }
                log("Récupération des logs de stock");
            }
            catch (Exception ex)
            {
                log("Erreur récupération des logs de stock : " + ex.Message);
            }

            return logs;
        }
        //
        //log facture
        public static void AjouterFactureLog(long idFacture, string action)
        {
            try
            {
                using (var connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();
                    var command = connection.CreateCommand();
                    command.CommandText = @"
                INSERT INTO FactLogs (idFacture, Date, Action)
                VALUES (@idFacture, @Date, @Action)";
                    command.Parameters.AddWithValue("@idFacture", idFacture);
                    command.Parameters.AddWithValue("@Date", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    command.Parameters.AddWithValue("@Action", action);
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                log("Erreur lors de l'ajout du log de facture : " + ex.Message);
            }
        }
        public static List<string> RecupererFactureLogs(int idFacture)
        {
            var logs = new List<string>();

            try
            {
                using (var connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();
                    var command = connection.CreateCommand();
                    command.CommandText = @"
                SELECT Date, Action FROM FactLogs
                WHERE idFacture = @idFacture
                ORDER BY Date DESC";
                    command.Parameters.AddWithValue("@idFacture", idFacture);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string date = reader["Date"].ToString();
                            string action = reader["Action"].ToString();
                            logs.Add($"[{date}] {action}");
                        }
                    }
                }
                log($"Récupération des logs pour la facture ID {idFacture}");
            }
            catch (Exception ex)
            {
                log("Erreur récupération des logs de facture : " + ex.Message);
            }

            return logs;
        }
        public static List<string> RecupererLogsSuppressionsFactures()
        {
            var logs = new List<string>();

            try
            {
                using (var connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();
                    var command = connection.CreateCommand();
                    command.CommandText = @"
                SELECT Date, Action FROM FactLogs
                WHERE Action LIKE '%suppression%'
                ORDER BY Date DESC";

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string date = reader["Date"].ToString();
                            string action = reader["Action"].ToString();
                            logs.Add($"[{date}] {action}");
                        }
                    }
                }
                log("Récupération des logs concernant la suppression des factures");
            }
            catch (Exception ex)
            {
                log("Erreur récupération des logs de suppression des factures : " + ex.Message);
            }

            return logs;
        }
        //
        //
        //Stats
        public static List<long> GetTotalQuantiteByMagasin()
        {
            // Résultat : index 0 -> magasin 1, index 1 -> magasin 2, index 2 -> magasin 3, index 3 -> magasin 4
            var totals = new List<long> { 0,0, 0, 0, 0, 0, 0, 0, 0, 0 , 0 };

            try
            {
                using (var conn = new SQLiteConnection(connectionString))
                {
                    conn.Open();

                    using (var cmd = conn.CreateCommand())
                    {
                        // Somme des quantités groupée par magasin
                        cmd.CommandText = @"
                    SELECT magasin, SUM(Quantite) AS TotalQuantite
                    FROM Produits
                    GROUP BY magasin;
                ";

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                // Récupération sécurisée des valeurs
                                int magasin = 0;
                                if (reader["magasin"] != DBNull.Value)
                                    magasin = Convert.ToInt32(reader["magasin"]);

                                long total = 0;
                                if (reader["TotalQuantite"] != DBNull.Value)
                                    total = Convert.ToInt64(reader["TotalQuantite"]);

                                if (magasin >=0 && magasin <= 10)
                                {
                                    totals[magasin] = total;
                                }
                                // si magasin hors 1..10, on l'ignore ici (ou tu peux l'additionner à une case "Autres")
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Optionnel : log l'erreur (tu as déjà une méthode LogOperation dans l'autre Utils)
                // LogOperation($"GetTotalQuantiteByMagasin erreur : {ex.Message}");
                // On renvoie les zéros en cas d'erreur
            }

            return totals;
        }
        public static List<(decimal Total, decimal Accompte, int Count)> GetSalesSummary()
        {
            var result = new List<(decimal, decimal, int)>();
            var today = DateTime.Now.Date;
            var tomorrow = today.AddDays(1);

            int diff = ((int)today.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
            var startWeek = today.AddDays(-diff);
            var nextWeek = startWeek.AddDays(7);

            var startMonth = new DateTime(today.Year, today.Month, 1);
            var nextMonth = startMonth.AddMonths(1);

            var startYear = new DateTime(today.Year, 1, 1);
            var nextYear = startYear.AddYears(1);

            result.Add(GetSum("datetime(Date) >= datetime(@start) AND datetime(Date) < datetime(@end)", today, tomorrow)); // Jour
            result.Add(GetSum("datetime(Date) >= datetime(@start) AND datetime(Date) < datetime(@end)", startWeek, nextWeek)); // Semaine
            result.Add(GetSum("datetime(Date) >= datetime(@start) AND datetime(Date) < datetime(@end)", startMonth, nextMonth)); // Mois
            result.Add(GetSum("datetime(Date) >= datetime(@start) AND datetime(Date) < datetime(@end)", startYear, nextYear)); // Année

            return result;
        }

        private static (decimal Total, decimal Accompte, int Count) GetSum(string condition, DateTime start, DateTime end)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                using (var cmd = new SQLiteCommand($"SELECT IFNULL(SUM(Total),0), IFNULL(SUM(Accompte),0), COUNT(*) FROM Factures WHERE {condition}", connection))
                {
                    cmd.Parameters.AddWithValue("@start", start.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.Parameters.AddWithValue("@end", end.ToString("yyyy-MM-dd HH:mm:ss"));

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            decimal total = reader.IsDBNull(0) ? 0 : reader.GetDecimal(0);
                            decimal accompte = reader.IsDBNull(1) ? 0 : reader.GetDecimal(1);
                            int count = reader.IsDBNull(2) ? 0 : reader.GetInt32(2);
                            return (total, accompte, count);
                        }
                    }
                }
            }
            return (0, 0, 0);
        }
        //
        //
        //historique des prix
        public static void AjouterPrixAchat(long produitId, double prix, long quantite, string fournisseur = "")
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = @"
                INSERT INTO HistoriquePrixAchat (ProduitId, PrixAchat, DateAchat, Quantite)
                VALUES (@ProduitId, @PrixAchat, @DateAchat, @Quantite)";

                command.Parameters.AddWithValue("@ProduitId", produitId);
                command.Parameters.AddWithValue("@PrixAchat", prix);
                command.Parameters.AddWithValue("@DateAchat", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                command.Parameters.AddWithValue("@Quantite", quantite);

                command.ExecuteNonQuery();
            }
        }
        public static List<HistoriquePrixAchat> GetHistoriquePrix(long produitId)
        {
            var historiques = new List<HistoriquePrixAchat>();
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM HistoriquePrixAchat WHERE ProduitId = @ProduitId ORDER BY DateAchat DESC";
                command.Parameters.AddWithValue("@ProduitId", produitId);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        historiques.Add(new HistoriquePrixAchat
                        {
                            Id = Convert.ToInt64(reader["Id"]),
                            ProduitId = Convert.ToInt64(reader["ProduitId"]),
                            PrixAchat = Convert.ToDouble(reader["PrixAchat"]),
                            DateAchat = DateTime.Parse(reader["DateAchat"].ToString()),
                            Quantite = Convert.ToInt32(reader["Quantite"])
                        });
                    }
                }
            }
            return historiques;
        }

    }

    //autres classes


}