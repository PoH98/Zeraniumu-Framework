using System;
using System.Windows.Forms;

namespace BotFramework
{
    public partial class SelectProcess : Form
    {
        public int id { get; private set; }
        public SelectProcess()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            id = Convert.ToInt32(maskedTextBox1.Text);
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
