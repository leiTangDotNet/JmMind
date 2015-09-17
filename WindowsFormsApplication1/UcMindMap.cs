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
         
        private void panelBottom_SizeChanged(object sender, EventArgs e)
        {
            this.panelBottom.Height = 26;
        }

        private void btnZoomIn_Click(object sender, EventArgs e)
        { 
            if(cbZoomValue.SelectedIndex>0)
            {
                cbZoomValue.SelectedIndex = cbZoomValue.SelectedIndex - 1;
            }
        }

        private void btnZoomOut_Click(object sender, EventArgs e)
        {  
            if(cbZoomValue.SelectedIndex< cbZoomValue.Items.Count-1)
            {
                cbZoomValue.SelectedIndex = cbZoomValue.SelectedIndex + 1;
            }
        } 

        private void cbZoomValue_SelectedIndexChanged(object sender, EventArgs e)
        {
            float zoom = Convert.ToInt32(cbZoomValue.Text.Substring(0, cbZoomValue.Text.Length - 1)) / 100.0f;
            this.mindMap.ZoomMap(zoom);

        }  

    }
}
