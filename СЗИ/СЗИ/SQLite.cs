using System;
using System.Text;
using System.Data.SQLite;
using System.IO;
using System.Security.Cryptography;
namespace СЗИ
{
    class SQLite
    {
        public string bytetostring(byte[] data)
        {
            string Arraystring = data[0].ToString();

            for (int i = 1; i < data.Length; i++)
            {
                Arraystring += "," + data[i].ToString();
            }
                return Arraystring;
        }

        public byte[] stringtobyte(string data)
        {
            string[] tokens = data.Split(',');

            byte[] myItems = Array.ConvertAll<string, byte>(tokens, byte.Parse);
            return myItems;
        }
        public string Hash(string input)
        {
            using (SHA1Managed sha1 = new SHA1Managed())
            {
                var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(input));
                var sb = new StringBuilder(hash.Length * 2);
                foreach (byte b in hash)
                {
                    sb.Append(b.ToString("x2"));
                }
                return sb.ToString();
            }
        }
        public bool createDB (string password)
        {
            const String dbFileName = "DB.db";
            byte[] salt1 = new byte[32];
            RNGCryptoServiceProvider rngCsp = new RNGCryptoServiceProvider();            
            // Fill the array with a random value.
            rngCsp.GetBytes(salt1);
            Rfc2898DeriveBytes k1 = new Rfc2898DeriveBytes(password, salt1, 1000, HashAlgorithmName.SHA256);
            byte[] master_key = new byte[32];
            byte[] master_IV = new byte[16];
            master_key = k1.GetBytes(32);
            
            //Считаем IV
            Aes aes = Aes.Create();
            aes.GenerateIV();
            master_IV = aes.IV;

            string password_hash= Hash(password);



            FileInfo fileInf = new FileInfo(dbFileName);
            //SQLiteConnection dbConn = new SQLiteConnection();
            SQLiteConnection dbConn = new SQLiteConnection("Data Source=" + dbFileName + ";Version=3;");
            SQLiteCommand command;            
            if (!File.Exists("DB.db"))
            {
                SQLiteConnection.CreateFile(dbFileName);
                //dbConn = new SQLiteConnection("Data Source=" + dbFileName + ";Version=3;");            
                dbConn.Open();
                command = new SQLiteCommand("CREATE TABLE IF NOT EXISTS MasterKey(id INTEGER PRIMARY KEY, ps_hash TEXT, salt TEXT, IV TEXT);", dbConn);
                command.ExecuteNonQuery();
                command = new SQLiteCommand("INSERT INTO MasterKey (id, ps_hash, salt,IV) VALUES (1 ,'" + password_hash + "', '" + bytetostring(salt1) + "', '" + bytetostring(master_IV) + "');", dbConn);
                command.ExecuteNonQuery();
                command = new SQLiteCommand("CREATE TABLE IF NOT EXISTS DataKey(id INTEGER PRIMARY KEY AUTOINCREMENT,extra_key INTEGER, namefile TEXT,namefile_old TEXT, key TEXT, IV TEXT);", dbConn);
                command.ExecuteNonQuery();
                command = new SQLiteCommand("CREATE TABLE IF NOT EXISTS ExtraKey(id INTEGER PRIMARY KEY AUTOINCREMENT,id_DataKey INTEGER, ps_hash TEXT, salt TEXT, IV TEXT);", dbConn);
                command.ExecuteNonQuery();

            }

            dbConn.Close();
            string path = Directory.GetCurrentDirectory();
            string subpath = @"open";
            DirectoryInfo dirInfo = new DirectoryInfo(path);
            if (!dirInfo.Exists)
            {
                dirInfo.Create();
            }
            dirInfo.CreateSubdirectory(subpath);
            
            subpath = @"encrypted";
            dirInfo = new DirectoryInfo(path);
            if (!dirInfo.Exists)
            {
                dirInfo.Create();
            }
            dirInfo.CreateSubdirectory(subpath);
            return true;
        }
    }
}
