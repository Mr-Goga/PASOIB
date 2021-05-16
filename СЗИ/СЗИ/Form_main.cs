using System;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Security.Cryptography;
using System.Data.SQLite;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;



namespace СЗИ
{
    public partial class Form_main : Form
    {
       
        public Form_main(string master_password)
        {
            InitializeComponent();
            _master_password = master_password;
        }
        public string _master_password;
        SQLite _SQLite = new SQLite();
        ThreadWaiter _ThreadWaiter = new ThreadWaiter(1000);
        static public byte[] EncryptStringToBytes_Aes(byte[] plainText, byte[] Key, byte[] IV)
        {
            try
            {
                // Check arguments.
                if (plainText == null || plainText.Length <= 0)
                    throw new ArgumentNullException("plainText");
                if (Key == null || Key.Length <= 0)
                    throw new ArgumentNullException("Key");
                if (IV == null || IV.Length <= 0)
                    throw new ArgumentNullException("IV");
                byte[] encrypted = new byte[plainText.Length];

                // Create an AesManaged object
                // with the specified key and IV.
                using (AesManaged aesAlg = new AesManaged())
                {
                    aesAlg.Key = Key;
                    aesAlg.IV = IV;
                    string tmp = "";
                    char ch;

                    // Create an encryptor to perform the stream transform.
                    ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                    // Create the streams used for encryption.                
                    using (MemoryStream msEncrypt = new MemoryStream())
                    {

                        using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                        {
                            csEncrypt.Write(plainText, 0, plainText.Length);
                            csEncrypt.Close();
                        }

                        encrypted = msEncrypt.ToArray();
                        msEncrypt.Close();
                    }
                }

                // Return the encrypted bytes from the memory stream.
                return encrypted;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                return null;
            }
        }
        static public byte[] DecryptStringFromBytes_Aes(byte[] cipherText, byte[] Key, byte[] IV)
        {
            //Check arguments.
            if (cipherText == null || cipherText.Length <= 0)
                throw new ArgumentNullException("cipherText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("IV");

            // Declare the string used to hold
            // the decrypted text.
            byte[] buffer = new byte[4096];
            byte[] plaintext = null;

            // Create an AesManaged object
            // with the specified key and IV.
            using (AesManaged aesAlg = new AesManaged())
            {
                aesAlg.Key = Key;
                aesAlg.IV = IV;
                // Create a decryptor to perform the stream transform.
                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                // Create the streams used for decryption.
                using (MemoryStream msDecrypt = new MemoryStream(cipherText))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (MemoryStream ms = new MemoryStream())
                        {
                            do
                            {
                                var count = csDecrypt.Read(buffer, 0, buffer.Length);
                                if (count == 0) break;
                                ms.Write(buffer, 0, count);
                            } while (true);
                            plaintext = ms.ToArray();
                        }
                    }
                }

            }
            return plaintext;
        }
        private bool encrypt (string path, string namefile_old)
        {
            try
            {
                using (FileStream fstream = File.OpenRead(path))
                {
                    // преобразуем строку в байты
                    byte[] array = new byte[fstream.Length];
                    // считываем данные
                    fstream.Read(array, 0, array.Length);
                    if (array.Length == 0)
                    {
                        MessageBox.Show("Ошибка! Файл пуст.");
                        return false;
                    }

                }
                    //Читаем данные о файле
                    if (File.Exists("DB.db"))
                {
                    
                    int id_datakey = -1;
                    SQLiteConnection dbConn = new SQLiteConnection("Data Source=DB.db; Version=3;");
                    SQLiteCommand command;
                    dbConn.Open();
                    //Вносим все кроме id_DataKey
                    command = new SQLiteCommand("SELECT * FROM DataKey WHERE namefile_old ='" + namefile_old + "'", dbConn);
                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {

                        if (reader.HasRows) // если есть данные
                        {
                            while (reader.Read())   // построчно считываем данные
                            {
                                id_datakey = reader.GetInt32(0);
                            }
                        }
                    }
                   
                    dbConn.Close();
                    if (id_datakey != -1)
                    {
                        MessageBox.Show("Файл с таким именем уже был зашифрован!");
                        return false;
                    }
                }
                // Создание datakey
                Aes data_aes = Aes.Create();
                int status_extra_key = 0;
                string namefile = namefile_old + ".sec";
                //string namefile_old= label1.Text;
                byte[] encrypted_extrakey;
                byte[] encrypted_extraIV;
                //Проверяем нужно ли сменить имя, если да, меняем
                if (checkBox4.Checked)
                {
                    namefile = _SQLite.Hash(namefile_old) + ".sec";
                }

                //проверка нахождения файла на флешке(директории с exe-шником)
                if ((path.ToLower().IndexOf(Directory.GetCurrentDirectory().ToLower()) == -1) || (File.Exists(Directory.GetCurrentDirectory() + "\\encrypted\\" + namefile)))
                {
                    MessageBox.Show("Невозможно зашифровать данный файл");
                    return false;
                }
                else
                {
                    //Доп пароль на файл
                    if (checkBox2.Checked)
                    {
                        //Введение пароля для файла
                        using (var form = new Form_auth(namefile_old))
                        {
                            //Создание extrakey
                            Aes extra_aes = Aes.Create();
                            var result = form.ShowDialog();
                            if (result == DialogResult.OK)
                            {
                                string val = form.ReturnValue1;          //values preserved after close
                                string password = val;
                                byte[] salt1 = new byte[32];
                                RNGCryptoServiceProvider rngCsp = new RNGCryptoServiceProvider();
                                // Fill the array with a random value.
                                rngCsp.GetBytes(salt1);
                                Rfc2898DeriveBytes k1 = new Rfc2898DeriveBytes(password, salt1, 1000, HashAlgorithmName.SHA256);
                                byte[] extra_key = new byte[32];
                                byte[] extra_IV = new byte[16];
                                extra_key = k1.GetBytes(32);

                                //Считаем IV

                                extra_aes.GenerateIV();
                                extra_IV = extra_aes.IV;
                                extra_aes.Key = extra_key;
                                string password_hash = _SQLite.Hash(password);

                                if (File.Exists("DB.db"))
                                {
                                    SQLiteConnection dbConn = new SQLiteConnection("Data Source=DB.db; Version=3;");
                                    SQLiteCommand command;
                                    dbConn.Open();
                                    //Вносим все кроме id_DataKey
                                    command = new SQLiteCommand("INSERT INTO ExtraKey (id_DataKey, ps_hash, salt, IV) VALUES (22222 ,'" + password_hash + "', '" + _SQLite.bytetostring(salt1) + "', '" + _SQLite.bytetostring(extra_IV) + "');", dbConn);
                                    command.ExecuteNonQuery();
                                    dbConn.Close();
                                    status_extra_key = 1;
                                }
                                else
                                {
                                    MessageBox.Show("Не удалось найти файл БД");
                                    return false;
                                }


                            }
                            else
                            {
                                MessageBox.Show("Отмена шифрования");
                                return false;
                            }
                            //Шифруем ключ datakey
                            encrypted_extrakey = EncryptStringToBytes_Aes(data_aes.Key, extra_aes.Key, extra_aes.IV);
                            encrypted_extraIV = EncryptStringToBytes_Aes(data_aes.IV, extra_aes.Key, extra_aes.IV);
                        }

                    }
                    else
                    {

                        //Шифруем datakey masterkey
                        Aes master_aes = Aes.Create();
                        byte[] salt = new byte[32];
                        if (File.Exists("DB.db"))
                        {
                            SQLiteConnection dbConn = new SQLiteConnection("Data Source=DB.db; Version=3;");
                            SQLiteCommand command;
                            dbConn.Open();
                            command = new SQLiteCommand("SELECT * FROM MasterKey ", dbConn);
                            using (SQLiteDataReader reader = command.ExecuteReader())
                            {

                                if (reader.HasRows) // если есть данные
                                {
                                    while (reader.Read())   // построчно считываем данные
                                    {
                                        salt = _SQLite.stringtobyte(reader.GetString(2));
                                        master_aes.IV = _SQLite.stringtobyte(reader.GetString(3));
                                        Rfc2898DeriveBytes k1 = new Rfc2898DeriveBytes(_master_password, salt, 1000, HashAlgorithmName.SHA256);
                                        master_aes.Key = k1.GetBytes(32);

                                    }
                                }
                            }

                            dbConn.Close();
                        }

                        //Зашифровать мастер ключом дата ключ
                        encrypted_extrakey = EncryptStringToBytes_Aes(data_aes.Key, master_aes.Key, master_aes.IV);
                        encrypted_extraIV = EncryptStringToBytes_Aes(data_aes.IV, master_aes.Key, master_aes.IV);
                    }

                    using (FileStream fstream = File.OpenRead(path))
                    {
                        // преобразуем строку в байты
                        byte[] array = new byte[fstream.Length];
                        // считываем данные
                        fstream.Read(array, 0, array.Length);
                        //Шифрование файла

                        // Encrypt the string to an array of bytes.
                        byte[] encrypted = EncryptStringToBytes_Aes(array, data_aes.Key, data_aes.IV);

                        //Создаем шифрованный файл в encrypted
                        FileStream fstream1 = new FileStream($"{Directory.GetCurrentDirectory()}\\encrypted\\{namefile}", FileMode.OpenOrCreate);
                        // запись массива байтов в файл
                        fstream1.Write(encrypted, 0, encrypted.Length);
                        if (fstream1 != null)
                            fstream1.Close();
                        if (fstream != null)
                            fstream.Close();

                        // Если нужно удалить исходный файл, удаляем
                        if (checkBox3.Checked)
                        {
                            FileInfo fileInf = new FileInfo(path);
                            if (fileInf.Exists)
                            {
                                fileInf.Delete();
                            }
                        }

                        //Сохраняем ключи в БД
                        if (File.Exists("DB.db"))
                        {
                            SQLiteConnection dbConn = new SQLiteConnection("Data Source=DB.db; Version=3;");
                            SQLiteCommand command;
                            dbConn.Open();
                            //Вносим все кроме id_DataKey


                            command = new SQLiteCommand("INSERT INTO DataKey (extra_key, namefile, namefile_old, key, IV) VALUES ('" + status_extra_key + "','" + namefile + "', '" + namefile_old + "', '" + _SQLite.bytetostring(encrypted_extrakey) + "', '" + _SQLite.bytetostring(encrypted_extraIV) + "');", dbConn);
                            command.ExecuteNonQuery();

                            if (checkBox2.Checked)
                            {
                                command = new SQLiteCommand("UPDATE ExtraKey SET id_DataKey = (SELECT ID FROM DataKey WHERE namefile='" + namefile + "') WHERE id_DataKey = 22222;", dbConn);
                                command.ExecuteNonQuery();
                            }
                            dbConn.Close();
                        }
                        else
                        {
                            MessageBox.Show("Не удалось найти файл БД");
                            return false;
                        }

                    }
                    return true;
                }

            }            
            finally
            {
                _ThreadWaiter.RemoveThread();
            }
        }
        private bool decrypt(string path, string namefile)
        {
            try
            {
                int id_datakey = -1;
                int extra_key_status = -22;
                //string namefile= label1.Text;
                string namefile_new = "error";
                byte[] encrypted_datakey = null;
                byte[] encrypted_dataIV = null;
                byte[] salt = new byte[32];
                Aes master_aes = Aes.Create();
                Aes data_aes = Aes.Create();

                //Читаем данные о файле
                if (File.Exists("DB.db"))
                {
                    SQLiteConnection dbConn = new SQLiteConnection("Data Source=DB.db; Version=3;");
                    SQLiteCommand command;
                    dbConn.Open();
                    //Вносим все кроме id_DataKey
                    command = new SQLiteCommand("SELECT * FROM DataKey WHERE namefile ='" + namefile + "'", dbConn);
                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {

                        if (reader.HasRows) // если есть данные
                        {
                            while (reader.Read())   // построчно считываем данные
                            {
                                id_datakey = reader.GetInt32(0);
                                extra_key_status = reader.GetInt32(1);
                                namefile_new = reader.GetString(3);
                                encrypted_datakey = _SQLite.stringtobyte(reader.GetString(4));
                                encrypted_dataIV = _SQLite.stringtobyte(reader.GetString(5));

                            }
                        }
                    }

                    dbConn.Close();
                }

                if ((path.ToLower().IndexOf(Directory.GetCurrentDirectory().ToLower()) == -1) || (File.Exists(Directory.GetCurrentDirectory() + "\\open\\" + namefile_new)) || (id_datakey == -1))
                {
                    MessageBox.Show("Невозможно расшифровать данный файл");
                    return false;
                }
                else
                {
                    //Если datakey зашифрован masterkey
                    if (extra_key_status == 0)
                    {
                        if (File.Exists("DB.db"))
                        {
                            SQLiteConnection dbConn = new SQLiteConnection("Data Source=DB.db; Version=3;");
                            SQLiteCommand command;
                            dbConn.Open();
                            //Вносим все кроме id_DataKey
                            command = new SQLiteCommand("SELECT * FROM MasterKey ", dbConn);
                            using (SQLiteDataReader reader = command.ExecuteReader())
                            {

                                if (reader.HasRows) // если есть данные
                                {
                                    while (reader.Read())   // построчно считываем данные
                                    {
                                        salt = _SQLite.stringtobyte(reader.GetString(2));
                                        master_aes.IV = _SQLite.stringtobyte(reader.GetString(3));
                                        Rfc2898DeriveBytes k1 = new Rfc2898DeriveBytes(_master_password, salt, 1000, HashAlgorithmName.SHA256);
                                        master_aes.Key = k1.GetBytes(32);

                                    }
                                }
                            }
                            dbConn.Close();
                        }
                    }
                    //Если datakey зашифрован extakey
                    else if (extra_key_status == 1)
                    {
                        string ps_hash = "";
                        if (File.Exists("DB.db"))
                        {
                            SQLiteConnection dbConn = new SQLiteConnection("Data Source=DB.db; Version=3;");
                            SQLiteCommand command;
                            dbConn.Open();
                            //Вносим все кроме id_DataKey
                            command = new SQLiteCommand("SELECT * FROM ExtraKey WHERE id_DataKey='" + id_datakey + "'", dbConn);
                            using (SQLiteDataReader reader = command.ExecuteReader())
                            {

                                if (reader.HasRows) // если есть данные
                                {
                                    while (reader.Read())   // построчно считываем данные
                                    {
                                        ps_hash = reader.GetString(2);
                                        salt = _SQLite.stringtobyte(reader.GetString(3));
                                        master_aes.IV = _SQLite.stringtobyte(reader.GetString(4));

                                    }
                                }
                            }
                            dbConn.Close();
                        }
                        //Ввод и проверка пароля
                        using (var form = new Form_auth(namefile))
                        {
                            var result = form.ShowDialog();
                            if (result == DialogResult.OK)
                            {
                                string val = form.ReturnValue1;          //values preserved after close
                                string password = val;
                                if (_SQLite.Hash(password) == ps_hash)
                                {
                                    Rfc2898DeriveBytes k1 = new Rfc2898DeriveBytes(password, salt, 1000, HashAlgorithmName.SHA256);
                                    master_aes.Key = k1.GetBytes(32);
                                }
                                else
                                {
                                    MessageBox.Show("Введен неверный пароль");
                                    return false;
                                }
                            }
                            else
                            {
                                MessageBox.Show("Отмена расшифровки");
                                return false;
                            }

                        }
                    }
                    else
                    {
                        MessageBox.Show("Не удалось расшифровать datakey");
                        return false;
                    }

                    // Расшифровываем datakey
                    data_aes.Key = DecryptStringFromBytes_Aes(encrypted_datakey, master_aes.Key, master_aes.IV);
                    data_aes.IV = DecryptStringFromBytes_Aes(encrypted_dataIV, master_aes.Key, master_aes.IV);
                    using (FileStream fstream = File.OpenRead(path))
                    {
                        // преобразуем строку в байты
                        byte[] array = new byte[fstream.Length];
                        // считываем данные
                        fstream.Read(array, 0, array.Length);
                        byte[] decript = DecryptStringFromBytes_Aes(array, data_aes.Key, data_aes.IV);
                        FileStream fstream1 = new FileStream($"{Directory.GetCurrentDirectory()}\\open\\{namefile_new}", FileMode.OpenOrCreate);
                        // запись массива байтов в файл
                        fstream1.Write(decript, 0, decript.Length);
                        if (fstream1 != null)
                            fstream1.Close();
                        if (fstream != null)
                            fstream.Close();
                    }
                    //Удаление зашифрованного файла
                    if (checkBox3.Checked)
                    {
                        if (File.Exists("DB.db"))
                        {
                            SQLiteConnection dbConn = new SQLiteConnection("Data Source=DB.db; Version=3;");
                            SQLiteCommand command;
                            dbConn.Open();
                            //Вносим все кроме id_DataKey
                            command = new SQLiteCommand("DELETE  FROM ExtraKey WHERE id_DataKey = '" + id_datakey + "'", dbConn);
                            command.ExecuteNonQuery();
                            command = new SQLiteCommand("DELETE  FROM DataKey WHERE id = '" + id_datakey + "'", dbConn);
                            command.ExecuteNonQuery();
                            FileInfo fileInf = new FileInfo(path);
                            if (fileInf.Exists)
                            {
                                fileInf.Delete();
                            }
                            dbConn.Close();
                        }

                    }
                    return true;
                }
            }           
            finally
            {
                _ThreadWaiter.RemoveThread();
            }
        }               
        private void encrypt_catalog (string name)
        {
            string sourceFolder = Directory.GetCurrentDirectory() + "\\open\\" + name; // исходная папка
            string zipFile = Directory.GetCurrentDirectory() + "\\open\\" + name + ".zip"; // сжатый файл
            if (!File.Exists(zipFile))
            {
                ZipFile.CreateFromDirectory(sourceFolder, zipFile);
                encrypt(zipFile, name + ".zip");
                FileInfo fileInf = new FileInfo(zipFile);
                if (fileInf.Exists)
                {
                    fileInf.Delete();
                }
            }
            else
            {
                _ThreadWaiter.RemoveThread();
                MessageBox.Show("Не удалось зашифровать каталог так как файл "+ name + ".zip уже существует");
            }
              
        }
        private void encrypt_allAsync()
        {
            DirectoryInfo dr = new DirectoryInfo(@"open");            
            foreach (var d in dr.GetDirectories())
            {
                _ThreadWaiter.AddThreads(1);
                Task.Run(() => encrypt_catalog(d.Name));
            }       
            foreach (var d in dr.GetFiles())
            {
                _ThreadWaiter.AddThreads(1);
                Task.Run(() => encrypt(Directory.GetCurrentDirectory() + "\\open\\" + d.Name, d.Name));
            }
          _ThreadWaiter.Wait();
        }
        private void decrypt_allAsync()
        {
            DirectoryInfo dr = new DirectoryInfo(@"encrypted");           
            foreach (var d in dr.GetFiles())
            {
                _ThreadWaiter.AddThreads(1);
                Task.Run(() => decrypt(Directory.GetCurrentDirectory() + "\\encrypted\\" + d.Name, d.Name));
            }
            _ThreadWaiter.Wait();
        }
        private void button_Click(object sender, EventArgs e)
        {
            listBox_encrypted.Items.Clear();
            listBox_open.Items.Clear();
            DirectoryInfo dr = new DirectoryInfo(@"open");
            foreach (var d in dr.GetDirectories())
            {
                listBox_open.Items.Add(d.Name);
            }
            foreach (var d in dr.GetFiles())
            {
                listBox_open.Items.Add(d.Name);
            }
            dr = new DirectoryInfo(@"encrypted");
            foreach (var d in dr.GetDirectories())
            {
                listBox_encrypted.Items.Add(d.Name);
            }
            foreach (var d in dr.GetFiles())
            {
                listBox_encrypted.Items.Add(d.Name);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                label1.Text = dlg.SafeFileName;
                label_path.Text = dlg.FileName;
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            label1.Text = "";
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //Шифруем
            if (radioButton1.Checked)
            {
                //Если шифруем всю папку open
                if (checkBox1.Checked)
                {

                    encrypt_allAsync();
                    if (checkBox3.Checked)
                    {
                        DirectoryInfo dr = new DirectoryInfo(Directory.GetCurrentDirectory() + "\\open\\");
                        foreach (var d in dr.GetDirectories())
                        {
                            DirectoryInfo dirInfo = new DirectoryInfo(Directory.GetCurrentDirectory() + "\\open\\" + d.Name);
                            dirInfo.Delete(true);
                        }                       
                    }
                }
                //шифруем только 1 файл
                else if (label1.Text != "")
                {
                    encrypt(label_path.Text, label1.Text);
                }
            }
            if (radioButton2.Checked)
                {
                //Если расшифруем всю папку open
                if (checkBox1.Checked)
                {
                    decrypt_allAsync();
                    DirectoryInfo dr = new DirectoryInfo(@"open");
                    foreach (var d in dr.GetFiles())
                    {
                        if (d.Name.IndexOf(".zip") != -1)
                        {
                            if (!Directory.Exists(Directory.GetCurrentDirectory() + "\\open\\" + d.Name.Replace(".zip", "")))
                            {
                                string path = Directory.GetCurrentDirectory() + "\\open\\" + d.Name;
                                ZipFile.ExtractToDirectory(Directory.GetCurrentDirectory() + "\\open\\" + d.Name, Directory.GetCurrentDirectory() + "\\open\\" + d.Name.Replace(".zip", ""));
                                FileInfo fileInf = new FileInfo(path);
                                if (fileInf.Exists)
                                {
                                    fileInf.Delete();
                                }
                            }
                            else
                            {
                                MessageBox.Show("Каталог с таким имением уже существует!");
                            }
                            
                        }                        
                    }                
                }
                    //расшифруем только 1 файл
                    else if (label1.Text != "")
                    {
                    decrypt(label_path.Text, label1.Text);
                    }
                }            
        }

        private void groupBox3_Enter(object sender, EventArgs e)
        {

        }
    }
}
