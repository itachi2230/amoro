using FirebaseAdmin;
using Google.Cloud.Firestore;
using Google.Apis.Auth.OAuth2;
using System;

public static class FirebaseConfig
{
    public static void InitializeFirebase()
    {
        FirebaseApp.Create(new AppOptions
        {
            Credential = GoogleCredential.FromFile("google-services.json")
        });
    }

    public static FirestoreDb GetFirestoreDb()
    {
        try
        {
            string path = "google-services.json";
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", path);
            return FirestoreDb.Create("amora-bd70d");
            //return FirestoreDb.Create("gestion-solaire-yaya");
        }
        catch (Exception ex)
        {
            throw new Exception($"Erreur lors de la connexion à Firestore : {ex.Message}", ex);
        }
    }
}
