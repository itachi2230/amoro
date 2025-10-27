using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Camara_Service
{
    public class Facture
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string Client { get; set; }
        public ObservableCollection<ProduitFacture> Produits { get; set; } // Liste des produits avec quantité et prix unitaire
        public double Total { get; set; }
        public string Telephone { get; set; }
        public double Reliquat { get; set; }
        public double Accompte { get; set; }
        public string Admin { get; set; }
        public DateTime LastModified { get; set; } = DateTime.UtcNow;
        public string Source { get; set; } = "client"; // ou "server"
        public bool IsSynced { get; set; }
        public Facture(int id, DateTime date, string client, ObservableCollection<ProduitFacture> produits, string admin, string telephone = "", double accompte = 0)
        {
            Id = id;
            Date = date;
            Client = client;
            Produits = produits;
            Telephone = telephone;
            Total = CalculerTotal();
            Accompte = accompte;
            Reliquat = Total - Accompte;
            Admin = admin;
        }

        public double ajouteraccompte(double toAdd)
        {
            Accompte += toAdd;
            Reliquat = Total - Accompte;
            return Reliquat;
        }

        private double CalculerTotal()
        {
            double total = 0;
            foreach (var produit in Produits)
            {
                total += produit.PrixUnitaire * produit.Quantite;
            }
            return total;
        }
    }

    public class ProduitFacture
    {
        public string Nom { get; set; }
        public long Id { get; set; }
        public double PrixUnitaire { get; set; }
        public int Quantite { get; set; }
        public double Total { get; set; }

        public ProduitFacture(string nom, double prixUnitaire, int quantite, long id = 0)
        {
            Nom = nom;
            PrixUnitaire = prixUnitaire;
            Quantite = quantite;
            Total = prixUnitaire * quantite;
            Id = id; //si id different de 0 cest que lobject a ete cree a partir dun produit de la base
        }
    }
}
