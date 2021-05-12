using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace СЗИ
{
    public partial class Form_auth : Form
    {
        
        public Form_auth(string text)
        {           
            InitializeComponent();
            this.label1.Text = "Введите пароль для " + text;
        }
        
        public string ReturnValue1 { get; set; }
        private void button1_Click(object sender, EventArgs e)
        {
            if (textBox2.Text == textBox1.Text)
            {
                this.ReturnValue1 = textBox1.Text;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                MessageBox.Show("Пароли не совпадают!");
                textBox1.Text = "";
                textBox2.Text = "";
            }
            
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.DialogResult=DialogResult.Cancel;
            this.Close();
        }
    }
}
