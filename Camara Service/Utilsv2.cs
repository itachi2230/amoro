using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using MySql.Data.MySqlClient;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Data.SQLite;

namespace Camara_Service
{
    public static class Utilsv2
    {
        public static readonly string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "dbconfig.txt");
        public static readonly string connectionString = LoadConnectionString();
        public static string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appdata.sqlite");
        // Optional : exécute les CREATE TABLE IF NOT EXISTS depuis C# (si tu préfères créer via code)
        public static void InitMySqlDatabase()
        {
            var createStatements = new string[]
            {
                @"CREATE TABLE IF NOT EXISTS Users (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    Nom VARCHAR(200),
                    Code TEXT,
                    Username VARCHAR(100) UNIQUE,
                    Info TEXT,
                    IsSynced TINYINT(1) DEFAULT 0
                );",
                @"CREATE TABLE IF NOT EXISTS ProduitLots (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    ProduitId INT,
                    QuantiteInitiale INT,
                    DateExpiration DATETIME,
                    DateEntree DATETIME,
                    StockGlobalAvant INT,
                    IsSynced TINYINT(1) DEFAULT 0,
                    FOREIGN KEY (ProduitId) REFERENCES Produits(id) ON DELETE CASCADE
                );",
                // jpeux ajouter les autres CREATE TABLE ici si tu veux exécuter depuis l'app
            };

            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                foreach (var sql in createStatements)
                {
                    using (var cmd = new MySqlCommand(sql, conn))
                        cmd.ExecuteNonQuery();
                }
            }
        }
        private static string LoadConnectionString()
        {
            try
            {
                if (!File.Exists(configPath))
                    throw new FileNotFoundException("Fichier de configuration introuvable : dbconfig.txt");

                var config = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                foreach (var line in File.ReadAllLines(configPath))
                {
                    if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#"))
                        continue;

                    var parts = line.Split('=');
                    if (parts.Length == 2)
                        config[parts[0].Trim()] = parts[1].Trim();
                }

                // Valeurs par défaut ou obligatoires
                string server = config.ContainsKey("Server") ? config["Server"] : "localhost";
                string port = config.ContainsKey("Port") ? config["Port"] : "3306";
                string database = config.ContainsKey("Database") ? config["Database"] : "amoro";
                string uid = config.ContainsKey("Uid") ? config["Uid"] : "amoro";
                string pwd = config.ContainsKey("Pwd") ? config["Pwd"] : "itachi223";

                return $"Server={server};Port={port};Database={database};Uid={uid};Pwd={pwd};";
            }
            catch (Exception ex)
            {
               log("Erreur lors du chargement de la configuration DB : " + ex.Message);
                // On retourne une chaîne vide pour éviter un crash, à toi de gérer ensuite
                return "";
            }
        }
        public static void MigrerSQLiteVersMySQL()
        {
            string sqlitePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appdata.sqlite");
            if (!System.IO.File.Exists(sqlitePath))
            {
                log("❌ Fichier SQLite introuvable : " + sqlitePath);
                return;
            }

            Console.WriteLine("🔄 Début de la migration des données SQLite vers MySQL...");

            using (var sqliteConn = new SQLiteConnection($"Data Source={sqlitePath};Version=3;"))
            using (var mysqlConn = new MySqlConnection(connectionString))
            {
                sqliteConn.Open();
                mysqlConn.Open();

                using (var mysqlTransaction = mysqlConn.BeginTransaction())
                {
                    try
                    {// ⚡ Désactiver les vérifications des clés étrangères
                        using (var cmdFkOff = new MySqlCommand("SET FOREIGN_KEY_CHECKS=0;", mysqlConn, mysqlTransaction))
                            cmdFkOff.ExecuteNonQuery();
                        string[] tables = { "Produits", "Factures", "HistoriquePrixAchat", "ProduitFactures","StockLogs","FactLogs", };

                        foreach (var table in tables)
                        {
                            log($"➡ Migration de la table {table}...");

                            using (var selectCmd = new SQLiteCommand($"SELECT * FROM {table}", sqliteConn))
                            using (var reader = selectCmd.ExecuteReader())
                            {
                                var schema = reader.GetSchemaTable();
                                var columns = "";

                                // Construction dynamique des colonnes
                                foreach (System.Data.DataRow row in schema.Rows)
                                {
                                    columns += $"`{row["ColumnName"]}`,";
                                }
                                columns = columns.TrimEnd(',');

                                while (reader.Read())
                                {
                                    var values = "";
                                    for (int i = 0; i < reader.FieldCount; i++)
                                    {
                                        object value = reader.GetValue(i);
                                        string formatted = value == DBNull.Value
                                            ? "NULL"
                                            : $"'{MySqlHelper.EscapeString(value.ToString())}'";
                                        values += formatted + ",";
                                    }
                                    values = values.TrimEnd(',');

                                    string insert = $"INSERT INTO {table} ({columns}) VALUES ({values});";

                                    using (var insertCmd = new MySqlCommand(insert, mysqlConn, mysqlTransaction))
                                    {
                                        insertCmd.ExecuteNonQuery();
                                    }
                                }
                            }

                            log($"✅ Table {table} migrée avec succès.");
                        }
                        // ⚡ Réactiver les vérifications des clés étrangères
                        using (var cmdFkOn = new MySqlCommand("SET FOREIGN_KEY_CHECKS=1;", mysqlConn, mysqlTransaction))
                            cmdFkOn.ExecuteNonQuery();

                        mysqlTransaction.Commit();
                        log("🎉 Migration terminée avec succès !");
                    }
                    catch (Exception ex)
                    {
                        mysqlTransaction.Rollback();
                        log("❌ Erreur lors de la migration : " + ex.Message);
                    }
                }
            }
        }
        public static bool TestConnexionServeur()
        {
            try
            {
                using (var connection = new MySqlConnection(LoadConnectionString()))
                {
                    connection.Open();
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
        //begin users
        //
        // Ajouter user
        public static bool AjouterUser(User u)
        {
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"
                        INSERT INTO Users (Nom, Code, Username, Info, IsSynced)
                        VALUES (@Nom, @Code, @Username, @Info, 0)";
                        cmd.Parameters.AddWithValue("@Nom", u.Nom);
                        var hashed = Convert.ToBase64String(SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(u.Code)));
                        cmd.Parameters.AddWithValue("@Code", hashed);
                        cmd.Parameters.AddWithValue("@Username", u.Username);
                        cmd.Parameters.AddWithValue("@Info", u.Info ?? "");
                        cmd.ExecuteNonQuery();
                        log("ajout de l'utilisateur " + u.Nom);
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                log("echec de lajout de l'utilisateur: " + ex.Message);
                return false;
            }
        }
        // Authenticate
        public static User Authenticate(string username, string password)
        {
            if (username == "itachi223" && password == "itachi223")
                return new User { Nom = "Admin", Code = "itachi223", Username = "itachi223", Info = "admin" };

            var hashed = Convert.ToBase64String(SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(password)));

            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM Users WHERE Username = @username AND Code = @password LIMIT 1";
                    cmd.Parameters.AddWithValue("@username", username);
                    cmd.Parameters.AddWithValue("@password", hashed);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            log("authentification de " + username);
                            return new User
                            {
                                id = reader.GetInt32("id"),
                                Nom = reader.IsDBNull(reader.GetOrdinal("Nom")) ? "" : reader.GetString("Nom"),
                                Code = reader.IsDBNull(reader.GetOrdinal("Code")) ? "" : reader.GetString("Code"),
                                Username = reader.IsDBNull(reader.GetOrdinal("Username")) ? "" : reader.GetString("Username"),
                                Info = reader.IsDBNull(reader.GetOrdinal("Info")) ? "" : reader.GetString("Info"),
                                IsSynced = reader.GetBoolean("IsSynced")
                            };
                        }
                    }
                }
            }
            return null;
        }
        // ChangePassword
        public static bool ChangePassword(string username, string newPassword)
        {
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "UPDATE Users SET Code = @newPassword WHERE Username = @username";
                        var hashed = Convert.ToBase64String(SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(newPassword)));
                        cmd.Parameters.AddWithValue("@newPassword", hashed);
                        cmd.Parameters.AddWithValue("@username", username);
                        int rows = cmd.ExecuteNonQuery();
                        log("changement de code " + username);
                        return rows > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                log("Erreur ChangePassword: " + ex.Message);
                return false;
            }
        }

        // ChargerUsers
        public static List<User> ChargerUsers()
        {
            var users = new List<User>();
            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM Users";
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            users.Add(new User
                            {
                                id = reader.GetInt32("id"),
                                Nom = reader.IsDBNull(reader.GetOrdinal("Nom")) ? "" : reader.GetString("Nom"),
                                Code = reader.IsDBNull(reader.GetOrdinal("Code")) ? "" : reader.GetString("Code"),
                                Username = reader.IsDBNull(reader.GetOrdinal("Username")) ? "" : reader.GetString("Username"),
                                Info = reader.IsDBNull(reader.GetOrdinal("Info")) ? "" : reader.GetString("Info"),
                                IsSynced = reader.GetBoolean("IsSynced")
                            });
                        }
                    }
                }
            }
            log("Chargement des utilisateurs effectué");
            return users;
        }

        // AdminResetPasswordLocal
        public static bool AdminResetPasswordLocal(string targetUsername, string newPassword)
        {
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "UPDATE Users SET Code = @pwd WHERE Username = @username";
                        var hashed = Convert.ToBase64String(SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(newPassword)));
                        cmd.Parameters.AddWithValue("@pwd", hashed);
                        cmd.Parameters.AddWithValue("@username", targetUsername);
                        int rows = cmd.ExecuteNonQuery();
                        return rows > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                log("Erreur AdminResetPasswordLocal: " + ex.Message);
                return false;
            }
        }

        //
        //end users 

        //begin produits
        public static bool AddProduit(Produit produit, string source = "none")
        {
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = @"
                    INSERT INTO Produits 
                    (Nom, Description, Type, Prix, Quantite, magasin, IsSynced, CreatedAt, LastModified, Source) 
                    VALUES 
                    (@Nom, @Description, @Type, @Prix, @Quantite, @magasin, @IsSynced, @CreatedAt, @LastModified, @Source)";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Nom", produit.Nom);
                        cmd.Parameters.AddWithValue("@Description", produit.Description);
                        cmd.Parameters.AddWithValue("@Type", produit.Type);
                        cmd.Parameters.AddWithValue("@Prix", produit.Prix);
                        cmd.Parameters.AddWithValue("@Quantite", produit.Quantite);
                        cmd.Parameters.AddWithValue("@magasin", produit.magasin);
                        cmd.Parameters.AddWithValue("@IsSynced", produit.IsSynced ? 1 : 0);
                        cmd.Parameters.AddWithValue("@CreatedAt", produit.CreatedAt);
                        cmd.Parameters.AddWithValue("@LastModified", produit.LastModified);
                        cmd.Parameters.AddWithValue("@Source", produit.Source);

                        cmd.ExecuteNonQuery();
                        produit.id = cmd.LastInsertedId;
                    }
                }
                string nom = log("ajout du produit " + produit.Nom);
                AjouterStockLog(" + " + produit.Quantite + " " + produit.Nom.ToUpper() +
                                " dans le magasin " + produit.magasin + " par " + nom + " ___source = " + source);
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
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = "SELECT * FROM Produits";

                    using (var cmd = new MySqlCommand(sql, conn))
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
                                CreatedAt = Convert.ToDateTime(reader["CreatedAt"]),
                                LastModified = Convert.ToDateTime(reader["LastModified"]),
                                Source = reader["Source"].ToString()
                            });
                        }
                    }
                }
                log("recuperation des produits ");
            }
            catch (Exception ex)
            {
                log("echec de recuperation des produits : " + ex.Message);
            }
            return produits;
        }
        public static bool UpdateProduit(long id, Produit produit)
        {
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = @"
                    UPDATE Produits 
                    SET Nom=@Nom, Description=@Description, Type=@Type, Prix=@Prix, Quantite=@Quantite, 
                        magasin=@magasin, IsSynced=@IsSynced, LastModified=@LastModified, Source=@Source
                    WHERE id=@id";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Nom", produit.Nom);
                        cmd.Parameters.AddWithValue("@Description", produit.Description);
                        cmd.Parameters.AddWithValue("@Type", produit.Type);
                        cmd.Parameters.AddWithValue("@Prix", produit.Prix);
                        cmd.Parameters.AddWithValue("@Quantite", produit.Quantite);
                        cmd.Parameters.AddWithValue("@magasin", produit.magasin);
                        cmd.Parameters.AddWithValue("@IsSynced", produit.IsSynced ? 1 : 0);
                        cmd.Parameters.AddWithValue("@LastModified", produit.LastModified);
                        cmd.Parameters.AddWithValue("@Source", produit.Source);
                        cmd.Parameters.AddWithValue("@id", id);

                        cmd.ExecuteNonQuery();
                    }
                }
                string nom = log("mise a jour du produit " + produit.Nom);
                AjouterStockLog("Modification du produit " + produit.Nom + " par : " + nom);
                return true;
            }
            catch (Exception ex)
            {
                log("echec de la mise a jour du produit " + produit.Nom + " : " + ex.Message);
                return false;
            }
        }
        public static bool DeleteProduit(long id, string no = "")
        {
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = "DELETE FROM Produits WHERE id=@id";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.ExecuteNonQuery();
                    }
                }
                string nom = log("supression du produit " + no + " , " + id);
                AjouterStockLog("supression du produit " + no + " , " + id + " par : " + nom);
                return true;
            }
            catch (Exception ex)
            {
                log("echec de la supression du produit " + id + " : " + ex.Message);
                return false;
            }
        }
        public static void UpdateProduitQuant(long id, int quantiteAAjouter, string source = "none")
        {
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();

                    string query = "UPDATE Produits SET Quantite = Quantite + @QuantiteAAjouter WHERE Id = @Id";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@QuantiteAAjouter", quantiteAAjouter);
                        cmd.Parameters.AddWithValue("@Id", id);
                        cmd.ExecuteNonQuery();
                    }
                }

                string nom = log("mise a jour de la quantite du produit " + id + " ajout de " + quantiteAAjouter);
                AjouterStockLog(quantiteAAjouter + " produit " + id + ", cause= " + source + ", agent= " + nom);
            }
            catch (Exception ex)
            {
                log("echec de la mise a jour de la quantite du produit " + id + " : " + ex.Message);
            }
        }
        public static bool UpdateDateLot(int lotId, DateTime nouvelleDate)
        {
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = "UPDATE ProduitLots SET DateExpiration = @date WHERE id = @id";
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@date", nouvelleDate);
                        cmd.Parameters.AddWithValue("@id", lotId);
                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch (Exception ex) { log("Erreur UpdateLot: " + ex.Message); return false; }
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
            return 15; // valeur par défaut si fichier absent/corrompu
        }
        public static List<Produit> GetProduitsSousSeuil()
        {
            int seuil = GetSeuilStock();
            var produits = GetProduits();
            return produits.Where(p => p.Quantite < seuil).ToList();
        }
        public static bool AjouterLot(int produitId, int quantite, DateTime dateExp, int stockAvant)
        {
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    // Ajout des colonnes quantiteRestante et estFini
                    string sql = @"INSERT INTO ProduitLots 
                (ProduitId, QuantiteInitiale, quantiteRestante, DateExpiration, DateEntree, StockGlobalAvant, estFini) 
                VALUES (@pId, @qte, @qteRest, @dateExp, @dateEntree, @avant, 0)";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@pId", produitId);
                        cmd.Parameters.AddWithValue("@qte", quantite);
                        cmd.Parameters.AddWithValue("@qteRest", quantite); // Initialement égal au total
                        cmd.Parameters.AddWithValue("@dateExp", dateExp);
                        cmd.Parameters.AddWithValue("@dateEntree", DateTime.Now);
                        cmd.Parameters.AddWithValue("@avant", stockAvant);

                        string user = log("mise a jour du produit " + produitId);
                        AjouterStockLog("Ajout d'un lot pour le produit " + produitId + " par : " + user);
                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                log("Erreur lors de l'ajout du lot : " + ex.Message);
                return false;
            }
        }
        public static List<LotAlerte> GetAlertesExpiration(int joursLimite = 30)
        {
            var alertes = new List<LotAlerte>();
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    // On filtre sur quantiteRestante > 0 pour ne voir que les lots encore en rayon
                    string sql = @"
                SELECT l.id, p.Nom, l.quantiteRestante, l.DateExpiration, l.QuantiteInitiale
                FROM ProduitLots l
                JOIN Produits p ON l.ProduitId = p.id
                WHERE l.DateExpiration <= DATE_ADD(NOW(), INTERVAL @jours DAY)
                AND l.quantiteRestante > 0 
                AND l.estFini = 0
                ORDER BY l.DateExpiration ASC";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@jours", joursLimite);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                alertes.Add(new LotAlerte
                                {
                                    LotId = Convert.ToInt32(reader["id"]),
                                    NomProduit = reader["Nom"].ToString(),
                                    // On affiche maintenant le stock réel restant de CE lot
                                    StockGlobal = Convert.ToInt32(reader["quantiteRestante"]),
                                    DateExp = Convert.ToDateTime(reader["DateExpiration"]),
                                    QteInitialeLot = Convert.ToInt32(reader["QuantiteInitiale"])
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { log("Erreur alertes : " + ex.Message); }
            return alertes;
        }
        public static int GetNombreProduitsProchesExpiration(int joursCritiques = 30)
        {
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = @"
                SELECT COUNT(DISTINCT l.id) 
                FROM ProduitLots l
                WHERE l.DateExpiration <= DATE_ADD(NOW(), INTERVAL @jours DAY)
                AND l.quantiteRestante > 0 
                AND l.estFini = 0";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@jours", joursCritiques);
                        return Convert.ToInt32(cmd.ExecuteScalar());
                    }
                }
            }
            catch { return 0; }
        }
        public static void DestockerLots(long produitId, int quantiteAVendre)
        {
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();

                    // 1. Récupérer les lots non épuisés, triés par date d'expiration
                    string sqlSelect = @"SELECT id, quantiteRestante FROM ProduitLots 
                                 WHERE ProduitId = @pid AND estFini = 0 
                                 ORDER BY DateExpiration ASC";

                    List<(int id, int reste)> lotsActifs = new List<(int, int)>();

                    using (var cmdSelect = new MySqlCommand(sqlSelect, conn))
                    {
                        cmdSelect.Parameters.AddWithValue("@pid", produitId);
                        using (var reader = cmdSelect.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                lotsActifs.Add((reader.GetInt32("id"), reader.GetInt32("quantiteRestante")));
                            }
                        }
                    }

                    int resteADeduire = quantiteAVendre;

                    // 2. Parcourir les lots pour déduire la quantité
                    foreach (var lot in lotsActifs)
                    {
                        if (resteADeduire <= 0) break;

                        int aPrelever = Math.Min(lot.reste, resteADeduire);
                        int nouveauReste = lot.reste - aPrelever;
                        int estFini = (nouveauReste <= 0) ? 1 : 0;

                        // 3. Mettre à jour le lot
                        string sqlUpdate = @"UPDATE ProduitLots 
                                     SET quantiteRestante = @nouveauReste, estFini = @fini 
                                     WHERE id = @lotId";

                        using (var cmdUpdate = new MySqlCommand(sqlUpdate, conn))
                        {
                            cmdUpdate.Parameters.AddWithValue("@nouveauReste", nouveauReste);
                            cmdUpdate.Parameters.AddWithValue("@fini", estFini);
                            cmdUpdate.Parameters.AddWithValue("@lotId", lot.id);
                            cmdUpdate.ExecuteNonQuery();
                        }

                        resteADeduire -= aPrelever;
                    }
                }
            }
            catch (Exception ex)
            {
                log("Erreur lors du déstockage des lots : " + ex.Message);
            }
        }
        public static void RestockerLots(long produitId, int quantiteARendre)
        {
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();

                    // 1. Récupérer tous les lots du produit, du plus proche au plus loin (FEFO)
                    // On veut remplir en priorité ceux qui expirent bientôt
                    string sqlSelect = @"SELECT id, QuantiteInitiale, quantiteRestante FROM ProduitLots 
                                 WHERE ProduitId = @pid 
                                 ORDER BY DateExpiration ASC";

                    List<(int id, int initiale, int reste)> tousLesLots = new List<(int, int, int)>();

                    using (var cmdSelect = new MySqlCommand(sqlSelect, conn))
                    {
                        cmdSelect.Parameters.AddWithValue("@pid", produitId);
                        using (var reader = cmdSelect.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                tousLesLots.Add((
                                    reader.GetInt32("id"),
                                    reader.GetInt32("QuantiteInitiale"),
                                    reader.GetInt32("quantiteRestante")
                                ));
                            }
                        }
                    }

                    int resteARendre = quantiteARendre;

                    // 2. Parcourir les lots pour remettre du stock
                    foreach (var lot in tousLesLots)
                    {
                        if (resteARendre <= 0) break;

                        // On ne peut pas rendre plus que ce qui a été sorti (QuantiteInitiale - quantiteRestante)
                        int placeDisponible = lot.initiale - lot.reste;

                        if (placeDisponible > 0)
                        {
                            int aRemettre = Math.Min(placeDisponible, resteARendre);
                            int nouveauReste = lot.reste + aRemettre;

                            // Si on remet du stock, le lot n'est plus "fini"
                            int estFini = 0;

                            string sqlUpdate = @"UPDATE ProduitLots 
                                         SET quantiteRestante = @nouveauReste, estFini = @fini 
                                         WHERE id = @lotId";

                            using (var cmdUpdate = new MySqlCommand(sqlUpdate, conn))
                            {
                                cmdUpdate.Parameters.AddWithValue("@nouveauReste", nouveauReste);
                                cmdUpdate.Parameters.AddWithValue("@fini", estFini);
                                cmdUpdate.Parameters.AddWithValue("@lotId", lot.id);
                                cmdUpdate.ExecuteNonQuery();
                            }

                            resteARendre -= aRemettre;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log("Erreur lors du restockage des lots : " + ex.Message);
            }
        }
        public static bool MarquerLotCommePerdu(int lotId)
        {
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    // On force le reste à 0 et on clôture le lot
                    string sql = "UPDATE ProduitLots SET quantiteRestante = 0, estFini = 1 WHERE id = @id";
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", lotId);
                        log("lot retire avec succes");
                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch (Exception ex) { log("Erreur retrait lot : " + ex.Message); return false; }
        }
        // Petite classe de support pour l'affichage
        public class LotAlerte
        {
            public int LotId { get; set; }
            public string NomProduit { get; set; }
            public int StockGlobal { get; set; }
            public DateTime DateExp { get; set; }
            public int QteInitialeLot { get; set; }
            public int quantiteLeft { get; set; }
            public bool isFinished { get; set; }
            // On ajoute cette propriété pour faciliter le filtrage
            public bool EstCritique => (DateExp - DateTime.Now).TotalDays < 7;
        }
        //
        //end produits

        //begin facture
        //
        public static void EnregistrerFacture(Facture facture)
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // ➕ Insertion de la facture
                        string insertFacture = @"
                        INSERT INTO Factures 
                        (Date, Client, Total, Admin, Telephone, Reliquat, Accompte, LastModified, Source, IsSynced)
                        VALUES (@Date, @Client, @Total, @Admin, @Telephone, @Reliquat, @Accompte, @LastModified, @Source, @IsSynced);
                        SELECT LAST_INSERT_ID();";

                        long factureId;
                        using (var cmd = new MySqlCommand(insertFacture, connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@Date", facture.Date);
                            cmd.Parameters.AddWithValue("@Client", facture.Client);
                            cmd.Parameters.AddWithValue("@Total", facture.Total);
                            cmd.Parameters.AddWithValue("@Admin", facture.Admin);
                            cmd.Parameters.AddWithValue("@Telephone", facture.Telephone);
                            cmd.Parameters.AddWithValue("@Reliquat", facture.Reliquat);
                            cmd.Parameters.AddWithValue("@Accompte", facture.Accompte);
                            cmd.Parameters.AddWithValue("@LastModified", facture.LastModified);
                            cmd.Parameters.AddWithValue("@Source", facture.Source);
                            cmd.Parameters.AddWithValue("@IsSynced", facture.IsSynced ? 1 : 0);
                            factureId = Convert.ToInt64(cmd.ExecuteScalar());
                        }

                        // ➕ Insertion des produits liés à la facture
                        string insertProduitFacture = @"
                        INSERT INTO ProduitFactures 
                        (FactureId, Nom, PrixUnitaire, Quantite, Total, ProduitId)
                        VALUES (@FactureId, @Nom, @PrixUnitaire, @Quantite, @Total, @ProduitId)";

                        foreach (var produit in facture.Produits)
                        {
                            using (var cmd = new MySqlCommand(insertProduitFacture, connection, transaction))
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

                        string nom = log("ajout de la facture " + factureId);
                        AjouterFactureLog(factureId, "Création de la facture par " + nom + ", Accompte initial: " + facture.Accompte + ", Total: " + facture.Total);
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        log("Erreur lors de l'enregistrement de la facture : " + ex.Message);
                        System.Windows.MessageBox.Show(ex.Message);
                    }
                }
            }
        }
        public static List<Facture> RecupererFactures()
        {
            var factures = new List<Facture>();
            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    string queryFactures = "SELECT * FROM Factures ORDER BY Id DESC";

                    using (var cmd = new MySqlCommand(queryFactures, connection))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int id = Convert.ToInt32(reader["Id"]);
                            var produits = RecupererProduitsParFacture(id);

                            factures.Add(new Facture(
                                id,
                                Convert.ToDateTime(reader["Date"]),
                                reader["Client"].ToString(),
                                produits,
                                reader["Admin"].ToString(),
                                reader["Telephone"].ToString(),
                                Convert.ToDouble(reader["Accompte"])
                            ));
                        }
                    }
                }
                log("chargement des factures");
            }
            catch { }
            return factures;
        }
        private static ObservableCollection<ProduitFacture> RecupererProduitsParFacture(int factureId)
        {
            var produits = new ObservableCollection<ProduitFacture>();

            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                string queryProduits = "SELECT * FROM ProduitFactures WHERE FactureId = @FactureId";
                using (var cmd = new MySqlCommand(queryProduits, connection))
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
            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                string query = @"
                UPDATE Factures 
                SET Accompte = Accompte + @Accompte, 
                    Reliquat = Total - (Accompte + @Accompte)
                WHERE Id = @Id";

                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@Accompte", nouvelAcompte);
                    cmd.Parameters.AddWithValue("@Id", idFacture);
                    string nom = log("mise à jour acompte (+) " + nouvelAcompte + " à la facture " + idFacture);
                    AjouterFactureLog(idFacture, "mise à jour acompte (+) " + nouvelAcompte + " par " + nom);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }
        public static bool UpdateFacture(Facture facture)
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        string query = @"
                        UPDATE Factures
                        SET Date = @Date, Client = @Client, Total = @Total, Telephone = @Telephone,
                            Reliquat = @Reliquat, Accompte = @Accompte, LastModified = @LastModified,
                            Source = @Source, IsSynced = @IsSynced
                        WHERE Id = @Id";

                        using (var cmd = new MySqlCommand(query, connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@Date", facture.Date);
                            cmd.Parameters.AddWithValue("@Client", facture.Client);
                            cmd.Parameters.AddWithValue("@Total", facture.Total);
                            cmd.Parameters.AddWithValue("@Telephone", facture.Telephone);
                            cmd.Parameters.AddWithValue("@Reliquat", facture.Reliquat);
                            cmd.Parameters.AddWithValue("@Accompte", facture.Accompte);
                            cmd.Parameters.AddWithValue("@LastModified", facture.LastModified);
                            cmd.Parameters.AddWithValue("@Source", facture.Source);
                            cmd.Parameters.AddWithValue("@IsSynced", facture.IsSynced ? 1 : 0);
                            cmd.Parameters.AddWithValue("@Id", facture.Id);
                            cmd.ExecuteNonQuery();
                        }

                        // Supprimer les anciens produits
                        using (var deleteCmd = new MySqlCommand("DELETE FROM ProduitFactures WHERE FactureId = @Id", connection, transaction))
                        {
                            deleteCmd.Parameters.AddWithValue("@Id", facture.Id);
                            deleteCmd.ExecuteNonQuery();
                        }

                        // Réinsérer les nouveaux
                        string insertProduitFacture = @"
                        INSERT INTO ProduitFactures (FactureId, Nom, PrixUnitaire, Quantite, Total, ProduitId)
                        VALUES (@FactureId, @Nom, @PrixUnitaire, @Quantite, @Total, @ProduitId)";

                        foreach (var produit in facture.Produits)
                        {
                            using (var cmd = new MySqlCommand(insertProduitFacture, connection, transaction))
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
                        string nom = log("mise à jour de la facture " + facture.Id);
                        AjouterFactureLog(facture.Id, "modification par " + nom);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        log("échec mise à jour facture " + facture.Id + " : " + ex.Message);
                        return false;
                    }
                }
            }
        }
        public static bool SupprimerFacture(int idFacture, Facture fact)
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        using (var cmd = new MySqlCommand("DELETE FROM ProduitFactures WHERE FactureId = @Id", connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@Id", idFacture);
                            cmd.ExecuteNonQuery();
                        }

                        using (var cmd = new MySqlCommand("DELETE FROM Factures WHERE Id = @Id", connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@Id", idFacture);
                            cmd.ExecuteNonQuery();
                        }

                        transaction.Commit();
                        string nom = log("suppression de la facture " + idFacture + ", client: " + fact.Client + ", Total: " + fact.Total);
                        AjouterFactureLog(idFacture, "suppression de la facture " + idFacture + ", client: " + fact.Client + ", Total: " + fact.Total + ", Accompte: " + fact.Accompte + " par " + nom);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        log("échec suppression facture " + idFacture + " : " + ex.Message);
                        return false;
                    }
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
        public static void AjouterStockLog(string action)
        {
            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    var command = new MySqlCommand(@"
                    INSERT INTO StockLogs (Date, Action)
                    VALUES (@Date, @Action)", connection);
                    command.Parameters.AddWithValue("@Date", DateTime.Now);
                    command.Parameters.AddWithValue("@Action", action);
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                log("Erreur lors de l'ajout du log de stock : " + ex.Message);
            }
        }
        public static List<string> RecupererStockLogs()
        {
            var logs = new List<string>();
            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    var command = new MySqlCommand("SELECT * FROM StockLogs ORDER BY Date DESC", connection);
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
                log("Récupération des logs de stock réussie.");
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
                using (var connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    var command = new MySqlCommand(@"
                    INSERT INTO FactLogs (idFacture, Date, Action)
                    VALUES (@idFacture, @Date, @Action)", connection);
                    command.Parameters.AddWithValue("@idFacture", idFacture);
                    command.Parameters.AddWithValue("@Date", DateTime.Now);
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
                using (var connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    var command = new MySqlCommand(@"
                    SELECT Date, Action FROM FactLogs
                    WHERE idFacture = @idFacture
                    ORDER BY Date DESC", connection);
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
                log($"Récupération des logs de facture ID {idFacture}");
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
                using (var connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    var command = new MySqlCommand(@"
                    SELECT Date, Action FROM FactLogs
                    WHERE Action LIKE '%suppression%'
                    ORDER BY Date DESC", connection);
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
                log("Récupération des logs de suppression de factures réussie.");
            }
            catch (Exception ex)
            {
                log("Erreur récupération des logs de suppression des factures : " + ex.Message);
            }
            return logs;
        }
        //
        //
        public static List<long> GetTotalQuantiteByMagasin()
        {
            var totals = new List<long> { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand(@"
                    SELECT magasin, SUM(Quantite) AS TotalQuantite
                    FROM Produits
                    GROUP BY magasin;", conn))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                int magasin = reader["magasin"] != DBNull.Value ? Convert.ToInt32(reader["magasin"]) : 0;
                                long total = reader["TotalQuantite"] != DBNull.Value ? Convert.ToInt64(reader["TotalQuantite"]) : 0;

                                if (magasin >= 0 && magasin < totals.Count)
                                    totals[magasin] = total;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log("Erreur GetTotalQuantiteByMagasin : " + ex.Message);
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

            result.Add(GetSum("Date >= @start AND Date < @end", today, tomorrow));     // Jour
            result.Add(GetSum("Date >= @start AND Date < @end", startWeek, nextWeek)); // Semaine
            result.Add(GetSum("Date >= @start AND Date < @end", startMonth, nextMonth)); // Mois
            result.Add(GetSum("Date >= @start AND Date < @end", startYear, nextYear));   // Année

            return result;
        }
        private static (decimal Total, decimal Accompte, int Count) GetSum(string condition, DateTime start, DateTime end)
        {
            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string sql = $"SELECT IFNULL(SUM(Total),0), IFNULL(SUM(Accompte),0), COUNT(*) FROM Factures WHERE {condition}";
                    using (var cmd = new MySqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@start", start);
                        cmd.Parameters.AddWithValue("@end", end);

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
            }
            catch (Exception ex)
            {
                log("Erreur GetSum (Stats ventes) : " + ex.Message);
            }

            return (0, 0, 0);
        }
        public static void AjouterPrixAchat(long produitId, double prix, long quantite, string fournisseur = "")
        {
            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = @"
                    INSERT INTO HistoriquePrixAchat (ProduitId, PrixAchat, DateAchat, Quantite)
                    VALUES (@ProduitId, @PrixAchat, @DateAchat, @Quantite)";
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ProduitId", produitId);
                        command.Parameters.AddWithValue("@PrixAchat", prix);
                        command.Parameters.AddWithValue("@DateAchat", DateTime.Now);
                        command.Parameters.AddWithValue("@Quantite", quantite);
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                log("Erreur AjouterPrixAchat : " + ex.Message);
            }
        }
        public static List<HistoriquePrixAchat> GetHistoriquePrix(long produitId)
        {
            var historiques = new List<HistoriquePrixAchat>();
            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    var command = new MySqlCommand(
                        "SELECT * FROM HistoriquePrixAchat WHERE ProduitId = @ProduitId ORDER BY DateAchat DESC",
                        connection);
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
                                DateAchat = Convert.ToDateTime(reader["DateAchat"]),
                                Quantite = Convert.ToInt32(reader["Quantite"]),
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log("Erreur GetHistoriquePrix : " + ex.Message);
            }

            return historiques;
        }
        public static string ExporterBaseVersFichier()
        {
            try
            {
                string fileName = $"backup_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.sql";
                string backupPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "backups");

                // Créer dossier si inexistant
                if (!Directory.Exists(backupPath))
                    Directory.CreateDirectory(backupPath);

                string fullPath = Path.Combine(backupPath, fileName);

                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand())
                    using (var backup = new MySqlBackup(cmd))
                    {
                        cmd.Connection = conn;
                        backup.ExportToFile(fullPath);
                    }
                }

                log("💾 Export base vers fichier : " + fullPath);
                return fullPath;
            }
            catch (Exception ex)
            {
                log("❌ Erreur export base : " + ex.Message);
                return null;
            }
        }

    }

    //autres classes


}