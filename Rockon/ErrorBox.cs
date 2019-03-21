using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Rockon
{
    public partial class ErrorBox : Form
    {
        private Exception exception;

        public ErrorBox(string message, Exception exception)
        {
            InitializeComponent();

            this.exception = exception;

            lblTitle.Text = message;
            lblMessage.Text = exception.Message;
            if (exception.Data.Contains("thrower"))
            {
                lblModule.Text = exception.Data["thrower"].ToString();
            }
            else
            {
                lblModule.Text = exception.Source;
            }
            textBox1.Text = exception.ToString();
        }

        private void btnCopy_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(exception.ToString());
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void ErrorBox_Shown(object sender, EventArgs e)
        {
            btnExit.Focus();
        }
    }
}
