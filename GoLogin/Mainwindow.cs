using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GoLogin
{
    public partial class MainWindow : Form
    {
        private NetOperation mOperation;
        public MainWindow()
        {
            InitializeComponent();
            mOperation = new NetOperation();
            mOperation.init();
        }

        private void labelUrl_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {

        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox cb = (CheckBox)sender;
            if (!cb.Checked)
                tbPassword.PasswordChar = '*';
            else
            {
                tbPassword.PasswordChar = '\0';
            }
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            if (!mOperation.systemOnline())
                mOperation.startLogin(tbUsername.Text, tbPassword.Text);
            else
            {
                mOperation.startLogout();
            }
        }
    }
}
