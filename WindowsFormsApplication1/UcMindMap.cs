﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using JsmMind;

namespace WindowsFormsApplication1
{
    public partial class UcMindMap : UserControl
    {
        public JsmMind.MindDocView mindMap = null;
        public UcMindMap()
        {
            InitializeComponent();
            this.mindMap = new JsmMind.MindDocView();
            this.panelMap.Controls.Add(this.mindMap);
            this.mindMap.Dock = DockStyle.Fill;

            this.mindMap.SubjectClick += mindMap_SubjectClick;

            this.richTextBox1.TextChanged += richTextBox1_TextChanged;

            this.splitContainer1.Panel2Collapsed = true;

        }

        void richTextBox1_TextChanged(object sender, EventArgs e)
        {
            SubjectBase subject = this.mindMap.SelectedSubject;
            if (subject == null) return;

            subject.Content = this.richTextBox1.Text;
        }

        void mindMap_SubjectClick(object sender, JsmMind.SubjectBase e)
        {
            SubjectBase subject = this.mindMap.SelectedSubject;
            if (subject == null) return;

            this.lblTitle.Text = subject.Title;
            this.richTextBox1.Text = subject.Content;
        }

        private void toolBtnAddImage_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    this.mindMap.SelectedSubject.AddImage(ofd.FileName);
                }
            }

        }


        private void toolBtnAttachNote_Click(object sender, EventArgs e)
        {

            SubjectBase subject = this.mindMap.SelectedSubject;
            if (subject == null) return;

            this.mindMap.AddAttachNote(subject);
        } 

        private void toolBtnRemark_Click(object sender, EventArgs e)
        {
            this.splitContainer1.Panel2Collapsed = !this.splitContainer1.Panel2Collapsed;
        }

    }
}
