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
        UcMindMap ucMindMap = null;
        public Form1()
        {
            InitializeComponent();
            this.ucMindMap = new UcMindMap();
            panel2.Controls.Add(this.ucMindMap);
            this.ucMindMap.Dock = DockStyle.Fill;
        } 
    }
}