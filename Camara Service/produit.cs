using System;
using System.Collections.Generic;

namespace Camara_Service
{
    public class Produit
    {
        public string   Nom { get; set; }
        public string Description { get; set; }
        public string Type { get; set; }
        public double Prix { get; set; }
        public long Quantite { get; set; }
        public long id { get; set; }
        public int magasin { get; set; }
        public bool IsSynced { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastModified { get; set; } = DateTime.UtcNow;
        public string Source { get; set; } = "client"; // ou "server"

        public Produit(string nom, long quantite = 0, string description = "", int magasin = 1, double prix = 0, long id = 0)
        {
            Nom = nom;
            Description = description;
            this.magasin = magasin;
            Prix = prix;
            Quantite = quantite;
            this.id = id;
            this.magasin = magasin;
            IsSynced = false;
            Type = "autre";
        }
        public string NomEtMagasin
        {
            get { return $"{Nom} - M {magasin}"; }
        }


    }
    public class HistoriquePrixAchat
    {
        public long Id { get; set; }
        public long ProduitId { get; set; }
        public double PrixAchat { get; set; }
        public DateTime DateAchat { get; set; }
        public int Quantite { get; set; }
    }
}
