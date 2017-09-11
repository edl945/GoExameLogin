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
            richTextBox1.Text = richTextBox1.Text + "初始化结束!";
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
        private void onTimerCheck(object sender, EventArgs e)
        {
            string result = mOperation.updateTimer();
            if (result != null)
            {
                richTextBox1.Text = richTextBox1.Text + result + "\n";
            }
        }
        private void onSlowTimerCheck(object sender, EventArgs e)
        {
            string result = mOperation.updateSlowTimer();
            if (result != null)
            {
                richTextBox1.Text = richTextBox1.Text + result + "\n";
            }
        }
    }
}
