using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MySql.Data.MySqlClient;


namespace ShopForm
{
    public partial class Mysql : Form
    {
        public Mysql()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string connstr = "server=" + ip.Text + ";port=" + port.Text + ";user=" + username.Text + ";password="
                + password.Text + ";database=" + dbname.Text;
            MySqlConnection coon = new MySqlConnection(connstr);
            try
            {
                coon.Open();
                
            }
        }

        private void Mysql_Load(object sender, EventArgs e)
        {

        }
    }
}
