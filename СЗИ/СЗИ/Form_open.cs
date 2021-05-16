using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Data.SQLite;

namespace СЗИ
{
    public partial class Form_open : Form
    {
        public Form_open()
        {
            InitializeComponent();
        }
        SQLite _SQLite = new SQLite();

        private void button1_Click(object sender, EventArgs e)
        {
            if(textBox1.Text != textBox2.Text)
            {
                MessageBox.Show("Пароли не совпадают");
                textBox1.Text = "";
                textBox2.Text = "";
            }
            else if (!File.Exists("DB.db")&&(textBox1.Text==textBox2.Text))
            {
                 _SQLite.createDB(textBox2.Text);
                MessageBox.Show("База данных успешно создана!");
                this.Hide();
                var formMain = new Form_main(textBox2.Text);
                formMain.Closed += (s, args) => this.Close();
                formMain.Show();
            }
            else if (File.Exists("DB.db") && (textBox1.Text == textBox2.Text))
            {
                string hashpass="";
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
                            hashpass = reader.GetString(1);      
                        }
                    }
                }
                dbConn.Close();
                if(_SQLite.Hash(textBox1.Text)== hashpass)
                {
                    this.Hide();
                    var formMain = new Form_main(textBox2.Text);
                    formMain.Closed += (s, args) => this.Close();
                    formMain.Show();
                }
                else
                {
                    MessageBox.Show("Введен неверный пароль");
                    textBox1.Text = "";
                    textBox2.Text = "";
                }
            }
                        

            
           
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
