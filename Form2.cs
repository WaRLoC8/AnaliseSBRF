using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AnaliseSBRF
{
    public partial class Form2 : Form
    {
        public Form2()
        {
            InitializeComponent();
        }

        private void progressBar1_Click(object sender, EventArgs e)
        {

        }

        public void progressBar1_setvol(int vol)
        {
            progressBar1.Value = vol;
        }

        public void progressBar1_setmax(int vol)
        {
            progressBar1.Maximum = vol;
        }
    }
}
