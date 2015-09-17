using System;
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

        private void toolBtnRelate_Click(object sender, EventArgs e)
        {
            this.mindMap.RelateSubjectLink();
        }


        private void toolBtnExpandTwoSide_Click(object sender, EventArgs e)
        {
            this.mindMap.ViewModel = MapViewModel.ExpandTwoSides;
        }

        private void toolBtnExpandRight_Click(object sender, EventArgs e)
        {
            this.mindMap.ViewModel = MapViewModel.ExpandRightSide;
        }

        private void tooBtnTreeMap_Click(object sender, EventArgs e)
        {
            this.mindMap.ViewModel = MapViewModel.TreeMap;
        }

        private void toolBtnStructure_Click(object sender, EventArgs e)
        {
            this.mindMap.ViewModel = MapViewModel.Structure;
        }


        private void toolCbZoom_TextChanged(object sender, EventArgs e)
        {
            this.mindMap.ZoomMap(float.Parse(this.toolCbZoom.Text));
        }

        private void panelBottom_SizeChanged(object sender, EventArgs e)
        {
            this.panelBottom.Height = 26;
        }

        private void btnZoomIn_Click(object sender, EventArgs e)
        {
            int value = this.trackBarZoomValue.Value - 50;
            if(value > this.trackBarZoomValue.Minimum)
            { 
                this.trackBarZoomValue.Value = value;
            }
        }

        private void btnZoomOut_Click(object sender, EventArgs e)
        {
            int value = this.trackBarZoomValue.Value + 50;
            if (value <this.trackBarZoomValue.Maximum)
            { 
                this.trackBarZoomValue.Value = value;
            }
        }

        private void btnZoomNormal_Click(object sender, EventArgs e)
        {
            this.trackBarZoomValue.Value = 100;
        }
         
        private void trackBarZoomValue_ValueChanged(object sender, EventArgs e)
        {
            this.lblZoomValue.Text = this.trackBarZoomValue.Value + "%";
            float zoom = this.trackBarZoomValue.Value * 1.0f / 100;


            this.mindMap.ZoomMap(zoom);

        }


         
         

    }
}
