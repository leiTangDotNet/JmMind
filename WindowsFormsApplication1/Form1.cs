using JsmMind;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class Form1 : Form
    {
        JsmMind.MindDocView calendarView1 = null;
        public Form1()
        {
            InitializeComponent();
            this.calendarView1 = new MindDocView(); 
            panel2.Controls.Add(this.calendarView1);
            this.calendarView1.Dock = DockStyle.Fill;
        }
         

        int month = 7;
        private void Form1_Load(object sender, EventArgs e)
        {
           
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            using(OpenFileDialog ofd=new OpenFileDialog())
            {
                if(ofd.ShowDialog()== System.Windows.Forms.DialogResult.OK)
                {
                    this.calendarView1.SelectedSubject.AddImage(ofd.FileName); 
                }
            }
        } 
    }
}