using System;

namespace Camara_Service
{
    public class User
    {   

        public string Nom { get; set; }
        public long id { get; set; }
        public string Code { get; set; }
        public string Username { get; set; }
        public string Info { get; set; }
        public bool IsSynced { get; set; }

        public User() { }
        public User(string nom, string username, string code, string info = "")
        {
            Nom = nom;
            Code = code;
            Username = username;
            Info = info;
        }
    }
}
