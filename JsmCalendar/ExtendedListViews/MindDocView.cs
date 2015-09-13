// MindDocView class
// Author: tang lei
// Email: tanglei331@hotmail.com
//  
////////////////////////////////////////////////////////

using System;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.ComponentModel.Design.Serialization;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Design;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace JsmMind
{
    #region Enumerations
    public enum MapViewModel { ExpandTwoSides, ExpandRightSide, TreeMap, Structure };
	#endregion

	#region Event Stuff
	#endregion

    #region SubjectPicture
    [Serializable]
    public class SubjectPicture
    {
        private Image subjectImage;
        private Rectangle rectArea;

        public SubjectPicture(string url)
        {
            subjectImage = new Bitmap(url);
        }

        public SubjectPicture(Image img)
        {
            subjectImage = img;
        }

        public Image SubjectImage
        {
            get { return subjectImage; }
            set { subjectImage = value; }
        }


        public Rectangle RectArea
        {
            get { return rectArea; }
            set { rectArea = value; }
        }

    }
    #endregion
    #region Subject
    /// <summary>
    /// �������
    /// </summary>
    [Serializable]
    public class SubjectBase
    {
        private string title = "����";
        private string content = "";

        protected int level = 0;

        private Font font = new Font("΢���ź�", 10.0f);
        private Color foreColor = Color.Black;
        private Color backColor = Color.White;
        private Color borderColor = Color.Green;
        private Color selectColor = Color.SteelBlue;
        private Color activeColor = Color.LightSteelBlue;

        protected Point centerPoint = new Point(0, 0);
        protected int width = 0;
        protected int height = 0;
        protected int marginWidth = 20;
        protected int marginHeight = 10;
        protected int branchLinkWidth = 30;
        protected int branchSplitHeight = 12;

        protected int leftBuffer = 30;

        protected bool expanded = true;
        protected bool visible = true;
        private Rectangle rectAreas;
        private Rectangle rectTitle;
         
        private Rectangle rectCollapse;

        private Rectangle rectPlusLeft; 
        private Rectangle rectPlusTop; 
        private Rectangle rectPlusRight; 
        private Rectangle rectPlusBottom;


        protected SubjectBase parentSubject = null;
        protected SubjectBase relateSubject = null;


        protected List<SubjectBase> childSubjects = new List<SubjectBase>();
        private List<SubjectBase> floatSubjects = new List<SubjectBase>();


        protected List<SubjectPicture> subjectPictures = new List<SubjectPicture>();

        private int moveX = -10000;

        protected int scalceWidth = 0;

         
        public SubjectBase DeepClone()
        {
            System.Runtime.Serialization.Formatters.Binary.BinaryFormatter bFormatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            MemoryStream stream = new MemoryStream();
            bFormatter.Serialize(stream, this);
            stream.Seek(0, SeekOrigin.Begin);

            SubjectBase subject = (SubjectBase)bFormatter.Deserialize(stream);
            return subject;
        }


        #region ����
        /// <summary>
        /// ���������µ�����������λ��
        /// (ע���÷���ֻ������ǰ����������⣬
        /// ʵ����������������Ҫ�ٻ�ȡ����������ø÷���ʵ������)
        /// </summary>
        public virtual void AdjustPosition()
        {
            int hb = this.branchSplitHeight;  //����߶�
            int hs = 0;   //����߶ȣ���������������
            int count = this.ChildSubjects.Count;
            if (count == 0) return;
            if (this.expanded == false) return;
            int totalHeight = 0;
            for (int i = 0; i < count; i++)
            {
                SubjectBase childSubject = this.ChildSubjects[i];
                hs = childSubject.GetTotalHeight();
                totalHeight += hs;
                totalHeight += hb;
            }
            totalHeight -= hb;

            int top = this.CenterPoint.Y - totalHeight / 2;
            if(top<this.height/2)
            {
                top = this.height / 2;
            }
            for (int i = 0; i < count; i++)
            {
                SubjectBase childSubject = this.ChildSubjects[i];
                hs = childSubject.GetTotalHeight();
                int x =  childSubject.centerPoint.X + childSubject.moveX; 
                if (childSubject.moveX==-10000) 
                {
                    x = this.CenterPoint.X + (this.Width / 2) + this.BranchLinkWidth + childSubject.width / 2 + childSubject.leftBuffer;

                    if(childSubject.relateSubject!=null) //�͹����ڵ�ͬһλ��
                    {
                        x = childSubject.relateSubject.centerPoint.X + childSubject.width / 2;
                    }
                } 
                int y = top + hs / 2;

                if(childSubject.GetType()==typeof(AttachSubject))
                { 

                } 
                childSubject.CenterPoint = new Point(x, y);
                
                top = top + hs + hb;
                 
                foreach(SubjectBase chidsub in childSubject.childSubjects)
                {
                    chidsub.moveX = childSubject.moveX + childSubject.scalceWidth;
                }
                childSubject.moveX = 0;
                childSubject.scalceWidth = 0;
                childSubject.AdjustPosition();
            }
        }

        /// <summary>
        /// ���������С
        /// </summary>
        public void AdjustSubjectSize()
        {
            Image bmp=new Bitmap(200,200);
            Graphics g = Graphics.FromImage(bmp);

            int w = 0;
            int h = 0;
            foreach(SubjectPicture subjectPicture in subjectPictures)
            {
                w += subjectPicture.SubjectImage.Width;
                w += 5;

                if(subjectPicture.SubjectImage.Height>h)
                {
                    h = subjectPicture.SubjectImage.Height;
                }
            }
            //�����ı�
            SizeF size = g.MeasureString(this.title,this.font);
            w += (int)size.Width;
            if(size.Height>h)
            {
                h = (int)size.Height;
            }
            int dx = (this.marginWidth + w + this.marginWidth - this.width)/2;
            int dy = (this.marginHeight + h + this.marginHeight - this.height)/2;
            this.width = this.marginWidth + w + this.marginWidth;
            this.height = this.marginHeight + h + this.marginHeight;
            this.moveX = dx;
            this.scalceWidth = dx;  //���ӿ��
            if(this.parentSubject!=null)
            {
                this.parentSubject.AdjustPosition();
            } 
            SubjectBase topSubject = this.GetTopSubect();
            if (topSubject != null)
            {
                topSubject.AdjustPosition();
            }
        }


        /// <summary>
        /// �Ƿ�չ������
        /// </summary>
        /// <param name="isExpand">�Ƿ�չ��</param>
        public virtual void CollapseChildSubject(bool isExpand)
        {
            this.expanded = isExpand;
            if (this.parentSubject != null)
            {
                this.parentSubject.AdjustPosition();
            }
            SubjectBase topSubject = this.GetTopSubect();
            if (topSubject != null)
            {
                topSubject.AdjustPosition();
            }
        } 

        /// <summary>
        /// �ƶ�����λ��
        /// </summary>
        /// <param name="movePoint">�ƶ��������λ��</param>
        public virtual void MoveSubjectPosition(Point movePoint)
        { 
            SubjectBase parentSubject = this.ParentSubject;
            //�����ƶ���Χ
            if(this.parentSubject!=null)
            {
                //�ϳ�����
                if(Math.Abs(movePoint.X - this.parentSubject.CenterPoint.X)>500)
                {
                    SubjectBase topSubject = this.GetTopSubect();
                    this.ParentSubject = null; 
                    topSubject.FloatSubjects.Add(this);
                    this.SetTitleStyle();
                }
                else
                {
                    int x = this.parentSubject.CenterPoint.X + (this.parentSubject.Width / 2) + this.parentSubject.BranchLinkWidth + this.width / 2 + 30;
                    if (x > movePoint.X)
                    {
                        movePoint.X = x;
                    }

                    bool bUp = movePoint.Y < this.centerPoint.Y;
                    for (int i = 0; i < this.parentSubject.childSubjects.Count; i++)
                    {
                        SubjectBase subject = this.parentSubject.childSubjects[i];

                        if (movePoint.Y < subject.centerPoint.Y)
                        {
                            if (subject != this)
                            {

                                if (bUp)
                                {
                                    this.parentSubject.childSubjects.Remove(this);
                                    this.parentSubject.childSubjects.Insert(i, this);
                                }
                                else
                                {
                                    this.parentSubject.childSubjects.Remove(this);
                                    this.parentSubject.childSubjects.Insert(i - 1, this);
                                }
                            }
                            else
                            {

                            }
                            break;
                        }
                        //�ƶ������
                        if (i == this.parentSubject.childSubjects.Count - 1)
                        {
                            if (movePoint.Y > subject.centerPoint.Y)
                            {
                                this.parentSubject.childSubjects.Remove(this);
                                this.parentSubject.childSubjects.Add(this);
                            }
                        }

                    }

                    if (this.GetType() == typeof(TitleSubject))
                    {
                        movePoint.Y = this.CenterPoint.Y;
                    }
                } 
            } 
            //�Ƿ���ק�����������ڵ�
            int dX = movePoint.X - this.centerPoint.X;
            int dY = movePoint.Y - this.centerPoint.Y;
            this.CenterPoint = new Point(movePoint.X, movePoint.Y); 
            foreach(SubjectBase childSub in this.childSubjects)
            {
                childSub.moveX = dX;
            } 
            this.moveX = 0;

            this.AdjustPosition();
            if (parentSubject != null)
                parentSubject.AdjustPosition();
        }


        /// <summary>
        /// ��������
        /// </summary>
        /// <param name="subject"></param>
        public void InsertSubject(SubjectBase subject,int position)
        { 
            subject.parentSubject = this;
            subject.SetTitleStyle();

            if (!this.childSubjects.Contains(subject))
            {
                if (this.childSubjects.Count == 0 || position >= this.childSubjects.Count || position<0)
                {
                    this.childSubjects.Add(subject); 
                }
                else
                {
                    this.childSubjects.Insert(position, subject);
                }
                this.AdjustPosition();

                SubjectBase topSubject = this.GetTopSubect();
                if (topSubject != null)
                {
                    topSubject.AdjustPosition();
                } 
            }  
        } 

        /// <summary>
        /// ����ͼƬ
        /// </summary>
        /// <param name="url">����ͼƬ·��</param>
        public void AddImage(string url)
        {
            SubjectPicture subjectPicture = new SubjectPicture(url);
            this.subjectPictures.Add(subjectPicture);
            this.AdjustSubjectSize();
        }  

        /// <summary>
        /// ��ȡ������ʾ�ܸ߶ȣ����������⣩
        /// </summary>
        /// <returns></returns>
        public virtual int GetTotalHeight()
        {
            int hb = 0;
            if(this.childSubjects.Count<1 || this.expanded==false)
            {
                hb = this.height;
            }
            else
            {
                SubjectBase firstSubject=this.childSubjects[0];
                SubjectBase lastSubject =this.childSubjects[this.childSubjects.Count-1];
                 
                int minHeight = firstSubject.centerPoint.Y - firstSubject.height / 2;
                int maxHeight = lastSubject.centerPoint.Y + lastSubject.height / 2;

                while(firstSubject!=null)
                {
                    if (firstSubject.childSubjects.Count == 0) break;
                    if (firstSubject.expanded == false) break;
                    firstSubject = firstSubject.childSubjects[0]; 
                    int minPos = firstSubject.centerPoint.Y - firstSubject.height / 2;
                    if(minPos<minHeight)
                    {
                        minHeight = minPos;
                    } 
                }
                while(lastSubject!=null)
                {
                    if (lastSubject.childSubjects.Count == 0) break;
                    if (lastSubject.expanded == false) break;
                    lastSubject = lastSubject.childSubjects[lastSubject.childSubjects.Count - 1];
                    int maxPos = lastSubject.centerPoint.Y + lastSubject.height / 2;
                    if(maxPos>maxHeight)
                    {
                        maxHeight = maxPos;
                    }
                }

                hb = maxHeight - minHeight;
                if(hb<this.height)
                {
                    hb = this.height;
                }
            }
            return hb;
        }
         
        /// <summary>
        /// ����������ʽ
        /// </summary>
        public void SetTitleStyle()
        {
            int lv = this.ParentSubject != null ? this.ParentSubject.Level + 1 : 0;
            if (lv > 3) lv = 3;
            if (this.level == lv)
            {
                return;
            } 
            this.level = lv;

            if (this.GetType() == typeof(TitleSubject))
            {
                switch (this.level)
                {
                    case 0: 
                        this.Width = 133;
                        this.Height = 35;
                        this.MarginWidth = 20;
                        this.MarginHeight = 10;
                        this.BackColor = Color.WhiteSmoke;
                        this.ForeColor = Color.DimGray;
                        this.BorderColor = Color.DimGray;
                        this.Font = new Font("΢���ź�", 12.0f);
                        this.Title = "��������";
                        this.BranchLinkWidth = 20;
                        this.BranchSplitHeight = 24;
                        break;
                    case 1:
                        this.Width = 93;
                        this.Height = 35;
                        this.marginWidth = 20;
                        this.marginHeight = 6;
                        this.BackColor = Color.AliceBlue;
                        this.ForeColor = Color.DimGray;
                        this.BorderColor = Color.SteelBlue;
                        this.Font = new Font("΢���ź�", 12.0f);
                        this.Title = "����";
                        break;
                    case 2:
                        this.Width = 83;
                        this.Height = 25;
                        this.marginWidth = 15;
                        this.marginHeight = 3;
                        this.BackColor = Color.Honeydew;
                        this.ForeColor = Color.DimGray;
                        this.BorderColor = Color.Green;
                        this.Font = new Font("΢���ź�", 10.0f);
                        this.Title = "������";

                        break;
                    case 3:
                        this.Width = 63;
                        this.Height = 20;
                        this.marginWidth = 5;
                        this.marginHeight = 2;
                        this.BackColor = Color.White;
                        this.ForeColor = Color.DimGray;
                        this.BorderColor = Color.Transparent;
                        this.Font = new Font("΢���ź�", 8.0f);
                        this.Title = "������";
                        break;
                }
            } 
            foreach (SubjectBase childSubject in ChildSubjects)
            {
                childSubject.SetTitleStyle();
            }
        }
         

        /// <summary>
        /// ��ȡ��������
        /// </summary>
        /// <returns></returns>
        protected SubjectBase GetTopSubect()
        {
            SubjectBase topSubject = this;
            while(topSubject.parentSubject!=null)
            {
                topSubject = topSubject.parentSubject; 
            } 
            return topSubject;
        }
        public void TranslateLightColor()
        {
            foreColor = Color.FromArgb(128, foreColor);
            backColor = Color.FromArgb(64, backColor);
            borderColor = Color.FromArgb(64, borderColor);
        }
        #endregion

        #region ����
        public string Title
        {
            get { return title; }
            set { title = value; }
        } 
        public string Content
        {
            get { return content; }
            set { content = value; }
        }

        public int Level
        {
            get { return level; } 
        }
        
        public Font Font
        {
            get { return font; }
            set { font = value; }
        }
        public Color ForeColor
        {
            get { return foreColor; }
            set { foreColor = value; }
        }
        public Color BackColor
        {
            get { return backColor; }
            set { backColor = value; }
        }
        public Color BorderColor
        {
            get { return borderColor; }
            set { borderColor = value; }
        }
        public Color SelectColor
        {
            get { return selectColor; }
            set { selectColor = value; }
        }
        public Color ActiveColor
        {
            get { return activeColor; }
            set { activeColor = value; }
        }
         
        public Point CenterPoint
        {
            get { return centerPoint; }
            set { centerPoint = value; }
        }
        public int Width
        {
            get { return width; }
            set { width = value; }
        }
        public int Height
        {
            get { return height; }
            set { height = value; }
        } 
        public int MarginWidth
        {
            get { return marginWidth; }
            set { marginWidth = value; }
        }
        
        public int MarginHeight
        {
            get { return marginHeight; }
            set { marginHeight = value; }
        }
        public int BranchLinkWidth
        {
            get { return branchLinkWidth; }
            set { branchLinkWidth = value; }
        } 
        public int BranchSplitHeight
        {
            get { return branchSplitHeight; }
            set { branchSplitHeight = value; }
        } 
        public bool Expanded
        {
            get { return expanded; } 
        } 
        public bool Visible
        {
            get { return visible; }
            set { visible = value; }
        }

        public Rectangle RectAreas
        {
            get { return rectAreas; }
            set { rectAreas = value; }
        }
        public Rectangle RectTitle
        {
            get { return rectTitle; }
            set { rectTitle = value; }
        }

        public Rectangle RectCollapse
        {
            get { return rectCollapse; }
            set { rectCollapse = value; }
        }

        public Rectangle RectPlusLeft
        {
            get { return rectPlusLeft; }
            set { rectPlusLeft = value; }
        }
        public Rectangle RectPlusTop
        {
            get { return rectPlusTop; }
            set { rectPlusTop = value; }
        }
        public Rectangle RectPlusRight
        {
            get { return rectPlusRight; }
            set { rectPlusRight = value; }
        }
        public Rectangle RectPlusBottom
        {
            get { return rectPlusBottom; }
            set { rectPlusBottom = value; }
        }
         
        public SubjectBase ParentSubject
        {
            get { return parentSubject; }
            set
            {
                if (parentSubject!=null)
                {
                    if(parentSubject.childSubjects.Contains(this))
                    {
                        parentSubject.childSubjects.Remove(this);
                    }
                } 

                parentSubject = value;
                if (parentSubject!=null)
                    parentSubject.InsertSubject(this,parentSubject.childSubjects.Count);
            }
        }

        public SubjectBase RelateSubject
        {
            get { return relateSubject; }
            set { relateSubject = value; }
        }  

        public List<SubjectBase> ChildSubjects
        {
            get { return childSubjects; }
            set { childSubjects = value; }
        } 

        public List<SubjectBase> FloatSubjects
        {
            get { return floatSubjects; }
            set { floatSubjects = value; }
        }
        public List<SubjectPicture> SubjectPictures
        {
            get { return subjectPictures; }
            set { subjectPictures = value; }
        } 
        public int MoveX
        {
            get { return moveX; }
            set { moveX = value; }
        }
        #endregion


    }

    /// <summary>
    /// ��������
    /// </summary>
    [Serializable]
    public class CenterSubject:SubjectBase
    {
        public CenterSubject()
        {
            this.Width = 133;
            this.Height = 45;
            this.MarginWidth = 20;
            this.MarginHeight = 10;
            this.BackColor = Color.WhiteSmoke;
            this.ForeColor = Color.DimGray;
            this.BorderColor = Color.DimGray;
            this.Font = new Font("΢���ź�", 14.0f);
            this.Title = "��������";
            this.BranchLinkWidth = 20;
            this.BranchSplitHeight=24;

            this.level = 0;
        } 
    }
    /// <summary>
    /// ��Ҫ����
    /// </summary>
    [Serializable]
    public class TitleSubject : SubjectBase
    {
        public TitleSubject(SubjectBase fatherSubject)
        {
            this.ParentSubject = fatherSubject; 
        }

        public TitleSubject(SubjectBase brotherSubject,bool insertUp)
        {
            SubjectBase fatherSubject = brotherSubject.ParentSubject;
            if (fatherSubject == null)
            {
                return;
            }

            int index = fatherSubject.ChildSubjects.IndexOf(brotherSubject);
            fatherSubject.InsertSubject(this, index + (insertUp ? 0 : 1));
        }   
    }

    /// <summary>
    /// ��ע
    /// </summary>
    [Serializable]
    public class AttachSubject:SubjectBase
    {  
        public AttachSubject(SubjectBase subject)
        { 
            this.Width = 63;
            this.Height = 30;
            this.MarginWidth = 10;
            this.MarginHeight = 5;
            this.BackColor = Color.LightYellow;
            this.ForeColor = Color.DimGray;
            this.BorderColor = Color.Orange;
            this.Font = new Font("΢���ź�", 10.0f);
            this.Title = "��ע"; 
            //�����ֵ�����
            SubjectBase fatherSubject = subject.ParentSubject;
            if(fatherSubject!=null)
            {
                this.relateSubject = subject;
                int index = fatherSubject.ChildSubjects.IndexOf(subject); 
                fatherSubject.InsertSubject(this, index); //Ĭ�ϲ��뵽�������������
            }

        }
         
        /// <summary>
        /// ��ȡ������ʾ�ܸ߶ȣ����������⣩
        /// </summary>
        /// <returns></returns>
        public override int GetTotalHeight()
        { 
            int hb=base.GetTotalHeight();
            SubjectBase relateSubject = this.relateSubject;
            if(this.MoveX!=-10000)
            {
                int hd = Math.Abs(this.CenterPoint.Y - relateSubject.CenterPoint.Y) - this.ParentSubject.BranchSplitHeight-2;
                if (hb < hd)
                {
                    hb = hd;
                }

            }
          
            return hb;
        }
    }


    #endregion
       

	#region MindDocView
	/// <summary>
	/// MindDocView provides a hybrid listview whos first
	/// column can behave as a treeview. This control extends
	/// ContainerListView, allowing subitems to contain 
	/// controls.
	/// </summary>`
	public class MindDocView : JsmMind.ContainerListView
	{
		#region Events 
        public delegate void SubjectEventHandler(object sender, SubjectBase e);

        [Description("Occurs after the Subject is double click")]
        public event SubjectEventHandler SubjectDoubleClick;

        [Description("Occurs after the Subject is double click")]
        public event SubjectEventHandler SubjectClick; 
		#endregion

		#region Variables 
		protected int indent = 19;
		protected int itemheight = 20;
        protected int itemwidth = 20;
        protected int taskHeight = 20;
        protected int lfWidth = 60;
        protected int ltHeight = 0;
        protected int colCount = 7;
        private int collaspRadius = 6;
		protected bool showlines = false, showrootlines = false, showplusminus = true;

		protected ListDictionary pmRects;
        protected ListDictionary nodeRowRects; 

		protected bool alwaysShowPM = false;

		protected Bitmap bmpMinus, bmpPlus;
            
        private List<SubjectBase> subjectNodes;

        private int maxWidth = 0;
          
		private bool mouseActivate = false;

		private bool allCollapsed = false;
         

        //����ѡ�еĽڵ�; 
        private SubjectBase selectedSubject = null;

        private SubjectBase activeSubject = null;
        private SubjectBase collapseSubject = null;
        private SubjectBase editSubject = null;

        private SubjectBase centerProject = null;
         
        private SubjectBase dragSubject = null;
        private int dragStartPx = 0;
        private int dragStartPy = 0;
         
        private int selectPlus = 0;  //1,2,3,4�ֱ��ʾLef,Top,Right,Bottom,0��ʾδѡ��
          
        private Color tempBackColor;
        private Color tempForeColor;


        private int selectionStartLine = 0;
        private int selectionEndLine = 0;

        private DateTime currentTime=DateTime.Now; 
        private MapViewModel mapViewModel = MapViewModel.ExpandTwoSides;
         
        TextBox txtNode = null;
        Image imgRender = null;
        System.Windows.Forms.Timer timerTxt;
         
		#endregion

		#region Constructor
        public MindDocView() : base()
        { 
            subjectNodes = new List<SubjectBase>();

            centerProject = new CenterSubject();
            centerProject.CenterPoint = new Point(this.Width / 2, this.Height / 2);
            subjectNodes.Add(centerProject);

            nodeRowRects = new ListDictionary();
            pmRects = new ListDictionary();
             
            txtNode = new TextBox();
            txtNode.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            txtNode.MouseDown += txtNode_MouseDown;
            txtNode.MouseUp += txtNode_MouseUp;
            txtNode.MouseMove += txtNode_MouseMove;
            txtNode.KeyDown += txtNode_KeyDown;
            txtNode.Leave += txtNode_Leave;

            timerTxt = new Timer();
            timerTxt.Interval = 1000;
            timerTxt.Enabled = false;
            timerTxt.Tick += timerTxt_Tick;
            imgRender = new Bitmap(100, 100);
            currentTime = DateTime.Now;
            // Use reflection to load the
            // embedded bitmaps for the
            // styles plus and minus icons
            Assembly myAssembly = Assembly.GetAssembly(Type.GetType("JsmMind.MindDocView"));
            ////string filename = Application.StartupPath + @"\Image\tv_minus.bmp";
            ////bmpMinus = new Bitmap(filename);  yixun
            ////bmpMinus = new Bitmap(Application.StartupPath + @"\Image\tv_plus.bmp");
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MindDocView));
           //// bmpMinus = ((System.Drawing.Bitmap)(resources.GetObject("tv_minus.bmp")));
        }


        #endregion

		#region Properties 

		[
		Category("Behavior"),
		Description("Determins wether an item is activated or expanded by a double click."),
		DefaultValue(false)
		]
		public bool MouseActivte
		{
			get { return mouseActivate; }
			set { mouseActivate = value; }
		}

		[
		Category("Behavior"),
		Description("Specifies wether to always show plus/minus signs next to each node."),
		DefaultValue(false)
		]
		public bool AlwaysShowPlusMinus
		{
			get { return alwaysShowPM; }
			set { alwaysShowPM = value; }
		} 

		[Browsable(false)]
		public override ContainerListViewItemCollection Items
		{
			get { return items; }
		}

		[
		Category("Behavior"),
		Description("The indentation of child nodes in pixels."),
		DefaultValue(19)
		]
		public int Indent
		{
			get { return indent; }
			set { indent = value; }
		} 
		[
		Category("Behavior"),
		Description("Indicates wether lines are shown between sibling nodes and between parent and child nodes."),
		DefaultValue(false)
		]
		public bool ShowLines
		{
			get { return showlines; }
			set { showlines = value; }
		}

		[
		Category("Behavior"),
		Description("Indicates wether lines are shown between root nodes."),
		DefaultValue(false)
		]
		public bool ShowRootLines
		{
			get { return showrootlines; }
			set { showrootlines = value; }
		}

		[
		Category("Behavior"),
		Description("Indicates wether plus/minus signs are shown next to parent nodes."),
		DefaultValue(true)
		]
		public bool ShowPlusMinus
		{
			get { return showplusminus; }
			set { showplusminus = value; }
		}  

        public int SelectionStartLine
        {
            get { return selectionStartLine; }
            set { selectionStartLine = value; }
        }

        public int SelectionEndLine
        {
            get { return selectionEndLine; }
            set { selectionEndLine = value; }
        }

        public Color TempBackColor
        {
            get { return tempBackColor; }
            set { tempBackColor = value; }
        }

        public Color TempForeColor
        {
            get { return tempForeColor; }
            set { tempForeColor = value; }
        } 
        [
       Category("Behavior"),
       Description("define the map viewmodel .")
       ]
        public MapViewModel MapViewMode
        {
            get { return mapViewModel; }
            set
            {
                mapViewModel = value;
            }
        }

        public SubjectBase SelectedSubject
        {
            get { return selectedSubject; }
            set { selectedSubject = value; }
        }
		#endregion

        #region Method


        public void AddAttachNote(SubjectBase subject)
        {
            SubjectBase attachSubject = new AttachSubject(subject);
            this.subjectNodes.Add(attachSubject);

        }

        #endregion
        #region Overrides
        public override bool PreProcessMessage(ref Message msg)
		{
			if (msg.Msg == WM_KEYDOWN)
            {
                Keys keyData = ((Keys)(int)msg.WParam) | ModifierKeys;
                Keys keyCode = ((Keys)(int)msg.WParam);

                if (keyCode == Keys.Left)	// collapse current node or move up to parent
                {
                }
                else if (keyCode == Keys.Right) // expand current node or move down to first child
                {
                }

                else if (keyCode == Keys.Up)
                {
                }
                else if (keyCode == Keys.Down)
                {
                    Invalidate();
                    return true;
                }						
			}

			return base.PreProcessMessage(ref msg);
		}

		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);

            if (centerProject != null && subjectNodes.Count<2)
            { 
                centerProject.CenterPoint = new Point(this.Width / 2, this.Height / 2); 
            }
		}

         
        System.Text.RegularExpressions.Regex regNumber = new System.Text.RegularExpressions.Regex(@"^[-]?\d+[.]?\d*$");
        
		protected override void OnMouseDown(MouseEventArgs e)
		{
			base.OnMouseDown(e);

            HideTxtBox();
             
            if (e.Button == MouseButtons.Left)
            {   
                if (e.Y > headerBuffer) //���������������ѡ�������¼�
                { 
                    //����ѡ��״̬
                    if (selectedSubject != null)
                    {
                        //�Ƿ�����չ��ť
                        int plus = PlusInArea(e, selectedSubject);
                        if (plus > 0) 
                        {
                            SubjectBase subject = null;
                            switch (plus)
                            {
                                case 1:
                                    subject = new TitleSubject(selectedSubject);
                                    break;
                                case 2:
                                    subject = new TitleSubject(selectedSubject,true);
                                    break;
                                case 3:
                                    subject = new TitleSubject(selectedSubject);
                                    break;
                                case 4:
                                    subject = new TitleSubject(selectedSubject,false);
                                    break;
                            }
                            if (subject != null)
                            {
                                subjectNodes.Add(subject);
                                selectedSubject = subject;
                            }
                            Invalidate();
                            return;
                        }

                        if(e.Clicks==2)
                        { 
                            //�Ƿ�������
                            if (TitleInArea(e, selectedSubject))
                            {
                                txtNode.Tag = selectedSubject;
                                ShowTxtBox();
                                return;
                            } 
                        }
                    }

                    //�Ƿ����۵���ť
                    SubjectBase collaspSubject = CollaspeInArea(e);
                    if (collapseSubject != null)
                    {
                        collapseSubject.CollapseChildSubject(!collapseSubject.Expanded);
                        Invalidate();
                        return;
                    } 

                    //�Ƿ�ѡ������
                    selectedSubject = null;
                    SubjectBase subjectNode = SubjectInArea(e);
                    if (subjectNode != null)
                    {
                        selectedSubject = subjectNode;
                        if (SubjectClick != null)
                            SubjectClick(this, subjectNode);

                        //��ק��ʼ
                        dragSubject = subjectNode.DeepClone();
                        dragSubject.TranslateLightColor();
                        dragStartPx = e.X;
                        dragStartPy = e.Y;
                    }
                } 
                Invalidate();

            } 
		}
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            activeSubject = null;
            collapseSubject = null;
            selectPlus = 0;
            if (e.Y > headerBuffer)
            { 
                //������ק����
                if(dragSubject!=null)  
                {
                    SubjectBase nowSubject = selectedSubject;
                     
                    int scaleX = e.X - dragStartPx;
                    int scaleY = e.Y - dragStartPy;
                    if ((Math.Abs(scaleX) > 10 || Math.Abs(scaleY) > 10) && nowSubject != null)
                    {
                        dragSubject.CenterPoint = new Point(nowSubject.CenterPoint.X + scaleX, nowSubject.CenterPoint.Y + scaleY);

                        //�Ƿ��ƶ�����������
                        SubjectBase subjectNode = SubjectInArea(e);
                        if (subjectNode != null)
                        {
                            activeSubject = subjectNode; 
                            Cursor.Current = Cursors.Hand;
                        }
                        else
                        {
                            Cursor.Current = Cursors.Default;
                        }
                    }

                }
                else //��������ƶ������,��չ��ť���۵���ť
                {
                    SubjectBase subjectNode = SubjectInArea(e);
                    if (subjectNode != null)  //����ƶ��������� 
                    {
                        activeSubject = subjectNode;  
                    }
                    else if (CollaspeInArea(e) != null)//����ƶ����۵���ť�� 
                    {
                        subjectNode = CollaspeInArea(e);
                        if (subjectNode != null)
                        {
                            collapseSubject = subjectNode;
                        }
                    }  
                    else  
                    {
                        
                    }

                    if (selectedSubject != null)  
                    { 
                        //����ѡ��״̬
                        int plus = PlusInArea(e, selectedSubject); //����ƶ�����չ��ť��
                        selectPlus = plus;

                        if (TitleInArea(e, selectedSubject))  //����Ƿ��ƶ���������
                        {
                            //Cursor.Current = Cursors.IBeam;
                        }
                        else
                        { 
                            //Cursor.Current = Cursors.Default;
                        }
                    }
                     
                } 
                Invalidate(); 
            }  
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
             
            if(dragSubject!=null)
            {
                //��ǰ��ק�����ʵ������
                SubjectBase nowSubject = selectedSubject;

                int scaleX = e.X - dragStartPx;
                int scaleY = e.Y - dragStartPy;
                //����������������ק
                if ((Math.Abs(scaleX) > 10 || Math.Abs(scaleY) > 10) && nowSubject != null)
                { 
                    //�Ƿ���ק������������
                    SubjectBase subjectNode = SubjectInArea(e);
                    if (subjectNode != null) //1.��������Ϊ�ƶ�����ĸ�����
                    {
                        //�ж��Ƿ��ƶ����Լ���������
                        bool isDragChild = false;
                        bool isDragSelf = false;
                        bool isDragParent = false;
                        SubjectBase subjectTemp = subjectNode.ParentSubject;
                        while (subjectTemp!=null)
                        {
                            if(subjectTemp == nowSubject)
                            {
                                isDragChild = true;
                                break;
                            }
                            subjectTemp = subjectTemp.ParentSubject;
                        }
                        if (nowSubject == subjectNode)
                        {
                            isDragSelf = true;
                        }
                        if(nowSubject.ParentSubject==subjectNode)
                        {
                            isDragParent = true;
                        }
                        if (isDragChild == false && isDragSelf==false && isDragParent==false)
                        {
                            if(centerProject.FloatSubjects.Contains(nowSubject))
                            {
                                centerProject.FloatSubjects.Remove(nowSubject);
                            }
                            nowSubject.MoveX = -10000;
                            nowSubject.ParentSubject = subjectNode;   
                        }
                    }
                    else  //2.�ƶ���ק����λ��
                    {
                        nowSubject.MoveSubjectPosition(dragSubject.CenterPoint);
                    } 
                }

                dragSubject = null;
                dragStartPx = 0;
                dragStartPy = 0;

                Cursor.Current = Cursors.Default;
            }
        }
        protected override void OnKeyUp(KeyEventArgs e)
		{
			base.OnKeyUp(e);

			if (e.KeyCode == Keys.F5)
			{
                if (allCollapsed)
                {
                    //ExpandAll();  
                }
                else
                {
                    //CollapseAll(); 
                }
			}
		}
 
        protected override void DrawHeaders(Graphics g, Rectangle r)
        {
            // render column headers and trailing column header

            g.Clip = new Region(new Rectangle(r.Left, r.Top, r.Width, r.Top + headerBuffer + 4));

            g.FillRectangle(new SolidBrush(Color.DarkGray), r.Left, r.Top, r.Width - 2, headerBuffer);

        }

        protected override void DrawRows(Graphics g, Rectangle r)
        {
            // render item rows
            int i;
            int maxrend = ClientRectangle.Height / itemheight + 1;


            AdjustScrollbars();
            RenderSubject(centerProject, g, r, 0);

            for (i = 0; i < centerProject.FloatSubjects.Count; i++)
            {
                RenderSubject(centerProject.FloatSubjects[i], g, r, 0);
            }
            if (dragSubject != null)
            {
                RenderDragSubject(dragSubject, g, r, 0);
            }
        }
        protected override void DrawExtra(Graphics g, Rectangle r)
        {
            base.DrawExtra(g, r);
        }

        protected override void DrawBackground(Graphics g, Rectangle r)
        {
            base.DrawBackground(g, r);  
        }


        private void RenderSubject(SubjectBase subjectNode, Graphics g, Rectangle r, int level)
        {
            int lb = 0;
            int hb = headerBuffer;
            int left = r.Left + subjectNode.CenterPoint.X - subjectNode.Width / 2+lb-hscrollBar.Value;
            int top = r.Left + subjectNode.CenterPoint.Y - subjectNode.Height / 2 + hb - vscrollBar.Value;
            string title = subjectNode.Title;
            Rectangle sr = new Rectangle(left, top, subjectNode.Width, subjectNode.Height);
            //���������������
            subjectNode.RectAreas = sr;
             
            //�������������� 
            if(subjectNode.Expanded)
            {
                foreach (SubjectBase childSubject in subjectNode.ChildSubjects)
                {
                    RenderSubject(childSubject, g, r, level + 1);
                } 
            }

            //��ʼ����
            g.SmoothingMode = SmoothingMode.AntiAlias;

            //1.���������� 
            if (subjectNode.ParentSubject != null)
            {
                RenderSubjectParentLink(g, r, subjectNode);
            }   
            //2.�Ȼ�������ķ�֧�����ߺ��۵���ť���ٻ�������������ʾ��ǰ  
            RenderSubjectBranchLink(g, r, subjectNode);

            //3.������������  
            RenderSubjectTitle(g, r, subjectNode);

            g.SmoothingMode = SmoothingMode.Default; 
        }

        private void RenderDragSubject(SubjectBase subjectNode, Graphics g, Rectangle r, int level)
        {
            SubjectBase nowSubject =selectedSubject;
            if(nowSubject==null || subjectNode==null) return;
            if (subjectNode.CenterPoint == nowSubject.CenterPoint) return;
            int lb = 0;
            int hb = headerBuffer;
            int left = r.Left + subjectNode.CenterPoint.X - subjectNode.Width / 2 + lb;
            int top = r.Left + subjectNode.CenterPoint.Y - subjectNode.Height / 2 + hb;
            string title = subjectNode.Title;
            Rectangle sr = new Rectangle(left, top, subjectNode.Width, subjectNode.Height);
             

            
            
            //���������������
            subjectNode.RectAreas = sr;
             


            //��ʼ����
            g.SmoothingMode = SmoothingMode.AntiAlias;


            if (nowSubject.ParentSubject != null && Math.Abs(subjectNode.CenterPoint.X - nowSubject.ParentSubject.CenterPoint.X) < 500)
            {
                //1.���������� 
                if (subjectNode.ParentSubject != null)
                {
                    RenderSubjectParentLink(g, r, subjectNode);
                }  
            } 

            //3.������������  
            RenderSubjectTitle(g, r, subjectNode);

            g.SmoothingMode = SmoothingMode.Default;
        }


        private void RenderSubjectParentLink(Graphics g, Rectangle r, SubjectBase subject)
        {
            if (subject.ParentSubject == null) return;
            g.Clip = new System.Drawing.Region(r);
            if (mapViewModel == MapViewModel.ExpandTwoSides)
            {
                if (subject.GetType() == typeof(AttachSubject))
                {
                    RenderLinkAttach(g, r, subject);
                }
                else
                {
                    RenderLinkBranch(g, r, subject);
                }
            }
        }

        private void RenderSubjectBranchLink(Graphics g, Rectangle r, SubjectBase subject)
        {
            if (subject.ChildSubjects.Count == 0) return;
            g.Clip = new System.Drawing.Region(r);

            //�Ȼ�������ķ�֧�����ߺ��۵���ť���ٻ�������������ʾ��ǰ  
            int x1 = subject.RectAreas.Left + subject.RectAreas.Width;
            int y1 = subject.RectAreas.Top + subject.RectAreas.Height / 2;

            int x2 = x1 + subject.BranchLinkWidth;
            int y2 = y1;

            Color penColor = subject.BorderColor == Color.Transparent ? subject.ForeColor : subject.BorderColor;
            Pen penLine = new Pen(penColor, 1.0f);
            //���Ʒ�֧����
            g.DrawLine(penLine, x1, y1, x2, y2);

            //�����۵���ť 
            Color colorCollapseF = penColor;
            Color colorCollapseB = Color.White;
            if (collapseSubject == subject)
            {
                colorCollapseF = Color.Blue;
                colorCollapseB = Color.LightSteelBlue;
            }

            int collaspradius = collaspRadius;
            Rectangle srCollaspe = new Rectangle(x2 - collaspradius, y2 - collaspradius, collaspradius * 2, collaspradius * 2);
            subject.RectCollapse = srCollaspe;
            g.FillEllipse(new SolidBrush(colorCollapseB), srCollaspe);
            g.DrawEllipse(new Pen(colorCollapseF, 1.0f), srCollaspe);

            g.DrawLine(new Pen(Color.DimGray, 2.0f), srCollaspe.Left + 3, srCollaspe.Top + srCollaspe.Height / 2, srCollaspe.Right - 3, srCollaspe.Top + srCollaspe.Height / 2);

            if (subject.Expanded == false)
            {
                g.DrawLine(new Pen(Color.DimGray, 2.0f), srCollaspe.Left + srCollaspe.Width / 2, srCollaspe.Top + 3, srCollaspe.Left + srCollaspe.Width / 2, srCollaspe.Bottom - 3);
            }
        }

        private void RenderSubjectTitle(Graphics g, Rectangle r, SubjectBase subject)
        {
            string title = subject.Title;
            Font f = subject.Font;
            Pen p = new Pen(subject.BorderColor, 1.0f);
            Rectangle sr = subject.RectAreas;
            g.Clip = new Region(new Rectangle(sr.Left - 10, sr.Top - 10, sr.Width + 20 + subject.BranchLinkWidth, sr.Height + 20));

            int radius = subject.Height / 6;
            if (selectedSubject == subject)  //����ѡ��߿����չ
            {
                Rectangle activeArea = new Rectangle(sr.Left - 3, sr.Top - 6, sr.Width + 7, sr.Height + 10);

                //������չ
                RenderPlus(g, activeArea, subject);

                //���Ʊ߿�ͱ���
                FillRoundRectangle(g, new SolidBrush(subject.SelectColor), activeArea, radius);
                g.FillRectangle(Brushes.White, sr.Left - 1, sr.Top - 1, sr.Width + 2, sr.Height + 2);

            }
            else if (activeSubject == subject) //����ѡ�б߿�
            {
                Rectangle activeArea = new Rectangle(sr.Left - 3, sr.Top - 6, sr.Width + 7, sr.Height + 10);
                FillRoundRectangle(g, new SolidBrush(subject.ActiveColor), activeArea, radius);
                g.FillRectangle(Brushes.White, sr.Left - 1, sr.Top - 1, sr.Width + 2, sr.Height + 2);
            }

            if(subject.GetType()== typeof(AttachSubject))
            {
                FillAttachRectangle(g, new SolidBrush(subject.BackColor), sr, radius);
                DrawAttachRectangle(g, p, sr, radius);
            }
            else
            {
                FillRoundRectangle(g, new SolidBrush(subject.BackColor), sr, radius);
                DrawRoundRectangle(g, p, sr, radius);
            }

            //����ͼ��
            int lb = subject.MarginWidth;
            for (int i = 0; i < subject.SubjectPictures.Count; i++)
            {
                Image img = subject.SubjectPictures[i].SubjectImage;

                g.DrawImage(img, (float)(sr.Left + lb), (float)(sr.Top + sr.Height / 2 - img.Height / 2 + 1));
                lb += (img.Width + 5);
            }

            SizeF size = g.MeasureString(title, f);
            //�����ı�
            float x_title = (float)(sr.Left + sr.Width / 2 - size.Width / 2); //���л���
            float y_title = (float)(sr.Top + sr.Height / 2 - Math.Floor(size.Height) / 2 + 1);
            if (lb != subject.MarginWidth) 
            {
                x_title = (float)(sr.Left + lb);//��ͼ������
            }

            g.DrawString(title, f, new SolidBrush(subject.ForeColor), x_title, y_title);
            Rectangle srTitle = new Rectangle((int)x_title, (int)y_title, (int)size.Width, (int)size.Height);
            subject.RectTitle = srTitle; 
        }

        private void RenderLinkBranch(Graphics g, Rectangle r,SubjectBase subject)
        {
            Pen penLine = new Pen(subject.BorderColor == Color.Transparent ? subject.ForeColor : subject.BorderColor, 1.0f);
            SubjectBase parentSubject = subject.ParentSubject; 
            int branchLinkWidth = parentSubject.BranchLinkWidth;

            //�������֧���ӵ�
            int x1 = parentSubject.RectAreas.Left + parentSubject.RectAreas.Width + branchLinkWidth;
            int y1 = parentSubject.RectAreas.Top + parentSubject.RectAreas.Height / 2; 
            //��������������ʼ��
            int x2 = subject.RectAreas.Left;
            int y2 = subject.RectAreas.Top + subject.RectAreas.Height / 2;
             
            if (x1 == x2 || y1 == y2)
            {
                g.DrawLine(penLine, x1, y1, x2, y2);
                return;
            } 
            Rectangle rect = new Rectangle(x1, y1, x2 - x1, y2 - y1);
            //ֱ�߻��Ʒ���(0,1,2,3�ֱ��ʾ�Ծ������Ͻǿ�ʼ����һ�������
            int direction = 0;
            if(x1<x2)
            {
                if(y1<y2)
                { 
                    rect = new Rectangle(x1, y1, x2 - x1, y2 - y1);
                    direction=2;
                }
                else
                {
                    rect = new Rectangle(x1, y2, x2 - x1, -(y2 - y1));
                    direction = 3;
                }
            }
            else
            {
                if (y1<y2)
                {
                    rect = new Rectangle(x2, y1, -(x2 - x1), y2 - y1);
                    direction = 1;
                }
                else
                { 
                    rect = new Rectangle(x2, y1, -(x2 - x1), -(y2 - y1));
                    direction = 0;
                }

            } 
            int roration = rect.Height / 4;
            if (roration > 16) roration = 16;
            if (roration < 4) roration = 4;
            DrawRoundLine(g, penLine, rect, roration, direction);
        }
        private void RenderLinkAttach(Graphics g, Rectangle r, SubjectBase subject)
        {
            Pen penLine = new Pen(subject.BorderColor == Color.Transparent ? subject.ForeColor : subject.BorderColor, 1.0f);
            SubjectBase relateSubject =subject.RelateSubject;
            //�����������ĵ�
            int x1 = relateSubject.CenterPoint.X;
            int y1 = relateSubject.CenterPoint.Y;

            int x2 = subject.CenterPoint.X;
            int y2 = subject.CenterPoint.Y;


            int xa = 0;
            int ya = 0;
            int xb = 0;
            int yb = 0;
            int xc = 0;
            int yc = 0;
            if (x1 < x2)
            {
                if (y1 < y2)
                {
                    xa = x1;
                    ya = y1 + relateSubject.Height/2+2;

                    xb = x2-5;
                    yb = y2-subject.Height/2;

                    xc = xb + 10;
                    yc = yb;
                }
                else
                {
                    xa = x1;
                    ya = y1-relateSubject.Height/2;

                    xb = x2-5;
                    yb = y2+subject.Height/2;

                    xc = xb + 10;
                    yc = yb;
                }
            }
            else
            {
                if (y1 < y2)
                {
                    xa = x1;
                    ya = y1+relateSubject.Height/2+2;

                    xb = x2+5;
                    yb = y2 - subject.Height/2;


                    xc = xb - 10;
                    yc = yb;
                }
                else
                {
                    xa = x1;
                    ya = y1-relateSubject.Height/2;

                    xb = x2+5;
                    yb = y2 +subject.Height/2;

                    xc = xb - 10;
                    yc = yb;
                }

            } 
            //g.DrawLine(penLine, xa, ya, xb, yb);


            GraphicsPath roundedRect = new GraphicsPath();
            roundedRect.AddLine(xa, ya, xb, yb);
            roundedRect.AddLine(xa, ya, xc, yc);
            roundedRect.CloseFigure();
            g.DrawPath(penLine, roundedRect);
        }

        private void RenderPlus(Graphics g, Rectangle area, SubjectBase subject)
        {
            Rectangle sr = new Rectangle(area.Left - 10, area.Top - 10, area.Width + 20, area.Height + 20);
            g.Clip = new Region(sr);

            Pen penPlus = new Pen(new SolidBrush(Color.White), 2.0f);

            Rectangle pmLeft = new Rectangle(area.Left - 8, area.Top + area.Height / 2 - 10, 40, 20);
            subject.RectPlusLeft = new Rectangle(pmLeft.Left,pmLeft.Top,pmLeft.Width/2,pmLeft.Height);
            RenderPlusLeft(g, pmLeft, penPlus, selectPlus == 1 ? Color.LightSteelBlue : Color.SteelBlue);

            if(subject.GetType()==typeof(TitleSubject))
            { 
                Rectangle pmTop = new Rectangle(area.Left + area.Width / 2 - 10, area.Top - 8, 20, 40);
                subject.RectPlusTop = new Rectangle(pmTop.Left, pmTop.Top, pmTop.Width, pmTop.Height / 2);
                RenderPlusTop(g, pmTop, penPlus, selectPlus == 2 ? Color.LightSteelBlue : Color.SteelBlue); 
            }

            Rectangle pmRight = new Rectangle(area.Left + area.Width - (40 - 8) - 1, area.Top + area.Height / 2 - 10, 40, 20);
            subject.RectPlusRight = new Rectangle(pmRight.Left + pmRight.Width / 2, pmRight.Top, pmRight.Width / 2, pmRight.Height);
            RenderPlusRight(g, pmRight, penPlus, selectPlus == 3 ? Color.LightSteelBlue : Color.SteelBlue);


            if (subject.GetType() == typeof(TitleSubject))
            {
                Rectangle pmBottom = new Rectangle(area.Left + area.Width / 2 - 10, area.Top + area.Height - (40 - 8) - 1, 20, 40);
                subject.RectPlusBottom = new Rectangle(pmBottom.Left, pmBottom.Top + pmBottom.Height / 2, pmBottom.Width, pmBottom.Height / 2);
                RenderPlusBottom(g, pmBottom, penPlus, selectPlus == 4 ? Color.LightSteelBlue : Color.SteelBlue);
            }
        }

        private void RenderPlusLeft(Graphics g, Rectangle pmLeft, Pen penPlus, Color backColr)
        {
            g.FillEllipse(new SolidBrush(backColr), pmLeft);
            g.DrawLine(penPlus, pmLeft.Left + 2, pmLeft.Top + pmLeft.Height / 2, pmLeft.Left + 8, pmLeft.Top + pmLeft.Height / 2);
            g.DrawLine(penPlus, pmLeft.Left + 5, pmLeft.Top + pmLeft.Height / 2 - 3, pmLeft.Left + 5, pmLeft.Top + pmLeft.Height / 2 + 3);
        }

        private void RenderPlusRight(Graphics g, Rectangle pmRight, Pen penPlus, Color backColr)
        {
            g.FillEllipse(new SolidBrush(backColr), pmRight);
            g.DrawLine(penPlus, pmRight.Right - 8, pmRight.Top + pmRight.Height / 2, pmRight.Right - 2, pmRight.Top + pmRight.Height / 2);
            g.DrawLine(penPlus, pmRight.Right - 5, pmRight.Top + pmRight.Height / 2 - 3, pmRight.Right - 5, pmRight.Top + pmRight.Height / 2 + 3); 
        }

        private void RenderPlusTop(Graphics g, Rectangle pmTop, Pen penPlus, Color backColr)
        {
            g.FillEllipse(new SolidBrush(backColr), pmTop);
            g.DrawLine(penPlus, pmTop.Left + 7, pmTop.Top + 5, pmTop.Left + 13, pmTop.Top + 5);
            g.DrawLine(penPlus, pmTop.Left + 10, pmTop.Top + 2, pmTop.Left + 10, pmTop.Top + 8); 
        }

        private void RenderPlusBottom(Graphics g, Rectangle pmBottom, Pen penPlus, Color backColr)
        {
            g.FillEllipse(new SolidBrush(backColr), pmBottom);
            g.DrawLine(penPlus, pmBottom.Left + 7, pmBottom.Bottom - 5, pmBottom.Left + 13, pmBottom.Bottom - 5);
            g.DrawLine(penPlus, pmBottom.Left + 10, pmBottom.Bottom - 2, pmBottom.Left + 10, pmBottom.Bottom - 8); 
        }

        private void DrawAttachRectangle(Graphics g, Pen pen, Rectangle rect, int cornerRadius)
        {
            using (GraphicsPath path = CreateRoundedRectanglePath(rect, cornerRadius))
            {
                g.DrawPath(pen, path);
                int edge = cornerRadius * 2;

                Rectangle rectEdge = new Rectangle(rect.Right - edge, rect.Top, edge, edge);
                //���Ƹ�ע������۵�����
                g.FillRectangle(Brushes.White,rectEdge.Left,rectEdge.Top-1,rectEdge.Width+1,rectEdge.Height);

                g.DrawLine(pen, rectEdge.Left, rectEdge.Top, rectEdge.Left, rectEdge.Bottom);
                g.DrawLine(pen, rectEdge.Left, rectEdge.Top, rectEdge.Right, rectEdge.Bottom);
                g.DrawLine(pen, rectEdge.Left, rectEdge.Bottom, rectEdge.Right, rectEdge.Bottom);  
            }
        }

        private void FillAttachRectangle(Graphics g, Brush brush, Rectangle rect, int cornerRadius)
        {
            using (GraphicsPath path = CreateRoundedRectanglePath(rect, cornerRadius/2))
            {
                g.FillPath(brush, path); 
            }
        }

        private void DrawRoundRectangle(Graphics g, Pen pen, Rectangle rect, int cornerRadius)
        {
            using (GraphicsPath path = CreateRoundedRectanglePath(rect, cornerRadius))
            {
                g.DrawPath(pen, path);
            }
        }

        private void FillRoundRectangle(Graphics g, Brush brush, Rectangle rect, int cornerRadius)
        {
            using (GraphicsPath path = CreateRoundedRectanglePath(rect, cornerRadius))
            {
                g.FillPath(brush, path);
            }
        }
        private GraphicsPath CreateRoundedRectanglePath(Rectangle rect, int cornerRadius)
        {
            GraphicsPath roundedRect = new GraphicsPath();
            roundedRect.AddArc(rect.X, rect.Y, cornerRadius * 2, cornerRadius * 2, 180, 90);
            roundedRect.AddLine(rect.X + cornerRadius, rect.Y, rect.Right - cornerRadius * 2, rect.Y);
            roundedRect.AddArc(rect.X + rect.Width - cornerRadius * 2, rect.Y, cornerRadius * 2, cornerRadius * 2, 270, 90);
            roundedRect.AddLine(rect.Right, rect.Y + cornerRadius * 2, rect.Right, rect.Y + rect.Height - cornerRadius * 2);
            roundedRect.AddArc(rect.X + rect.Width - cornerRadius * 2, rect.Y + rect.Height - cornerRadius * 2, cornerRadius * 2, cornerRadius * 2, 0, 90);
            roundedRect.AddLine(rect.Right - cornerRadius * 2, rect.Bottom, rect.X + cornerRadius * 2, rect.Bottom);
            roundedRect.AddArc(rect.X, rect.Bottom - cornerRadius * 2, cornerRadius * 2, cornerRadius * 2, 90, 90);
            roundedRect.AddLine(rect.X, rect.Bottom - cornerRadius * 2, rect.X, rect.Y + cornerRadius * 2);
            roundedRect.CloseFigure();
            return roundedRect;
        }
         
        /// <summary>
        /// ���ƻ���
        /// </summary>
        /// <param name="g"></param>
        /// <param name="pen"></param>
        /// <param name="rect"></param>
        /// <param name="cornerRadius">Բ�ǰ뾶</param>
        /// <param name="direction">ֱ�߻��Ʒ���(0,1,2,3�ֱ��ʾ�Ծ������Ͻǿ�ʼ����һ�������)</param>
        private void DrawRoundLine(Graphics g, Pen pen, Rectangle rect, int cornerRadius,int direction)
        {
            using (GraphicsPath path = CreateRoundedLinePath(rect, cornerRadius,direction))
            {
                g.DrawPath(pen, path);
            }
        }
        private GraphicsPath CreateRoundedLinePath(Rectangle rect, int cornerRadius,int direction)
        {
            GraphicsPath roundedRect = new GraphicsPath();
            if(direction==0)
            { 
                roundedRect.AddLine(rect.X, rect.Y, rect.Right - cornerRadius * 2, rect.Y);
                roundedRect.AddArc(rect.X + rect.Width - cornerRadius * 2, rect.Y, cornerRadius * 2, cornerRadius * 2, 270, 90);
                roundedRect.AddLine(rect.Right, rect.Y + cornerRadius * 2, rect.Right, rect.Y + rect.Height);

            } 
            else if( direction==1)
            {
                roundedRect.AddLine(rect.Right, rect.Y , rect.Right, rect.Y + rect.Height - cornerRadius * 2);
                roundedRect.AddArc(rect.X + rect.Width - cornerRadius * 2, rect.Y + rect.Height - cornerRadius * 2, cornerRadius * 2, cornerRadius * 2, 0, 90);
                roundedRect.AddLine(rect.Right - cornerRadius * 2, rect.Bottom, rect.X + cornerRadius * 2, rect.Bottom);
                
            }
            else if(direction==2)
            {
                roundedRect.AddLine(rect.Right, rect.Bottom, rect.X + cornerRadius * 2, rect.Bottom);
                roundedRect.AddArc(rect.X, rect.Bottom - cornerRadius * 2, cornerRadius * 2, cornerRadius * 2, 90, 90);
                roundedRect.AddLine(rect.X, rect.Bottom - cornerRadius * 2, rect.X, rect.Y); 
            }
            else if(direction==3)
            {
                roundedRect.AddLine(rect.X, rect.Bottom , rect.X, rect.Y + cornerRadius * 2);
                roundedRect.AddArc(rect.X, rect.Y, cornerRadius * 2, cornerRadius * 2, 180, 90);
                roundedRect.AddLine(rect.X + cornerRadius, rect.Y, rect.Right, rect.Y); 
            }
            else
            {
                roundedRect.AddArc(rect.X, rect.Y, cornerRadius * 2, cornerRadius * 2, 180, 90);
                roundedRect.AddLine(rect.X + cornerRadius, rect.Y, rect.Right - cornerRadius * 2, rect.Y);
                roundedRect.AddArc(rect.X + rect.Width - cornerRadius * 2, rect.Y, cornerRadius * 2, cornerRadius * 2, 270, 90);
                roundedRect.AddLine(rect.Right, rect.Y + cornerRadius * 2, rect.Right, rect.Y + rect.Height - cornerRadius * 2);
                roundedRect.AddArc(rect.X + rect.Width - cornerRadius * 2, rect.Y + rect.Height - cornerRadius * 2, cornerRadius * 2, cornerRadius * 2, 0, 90);
                roundedRect.AddLine(rect.Right - cornerRadius * 2, rect.Bottom, rect.X + cornerRadius * 2, rect.Bottom);
                roundedRect.AddArc(rect.X, rect.Bottom - cornerRadius * 2, cornerRadius * 2, cornerRadius * 2, 90, 90);
                roundedRect.AddLine(rect.X, rect.Bottom - cornerRadius * 2, rect.X, rect.Y + cornerRadius * 2);

            }
           
            //roundedRect.CloseFigure();
            return roundedRect;
        } 
		#endregion
        

        #region TextBoxEdit 
        void txtNode_Leave(object sender, EventArgs e)
        { 
        }
        void txtNode_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter || e.KeyCode== Keys.Escape)
            {
                HideTxtBox();
                return;
            }
            Graphics g = Graphics.FromImage(imgRender);
            SizeF size = g.MeasureString(txtNode.Text, txtNode.Font);

            int w = (int)size.Width + 5;
            txtNode.Width = w;

            //if (this.txtNode.Tag != null && this.txtNode.AccessibleDescription != null && this.txtNode.AccessibleDescription == "Save")
            //{
            //    SubjectBase subject = txtNode.Tag as SubjectBase;
            //    subject.Title = this.txtNode.Text;
            //    subject.AdjustSubjectSize();
            //    Invalidate();
            //}
        }

        void txtNode_MouseMove(object sender, MouseEventArgs e)
        {
            //if (muiltTxtDown)
            //{
            //    if (e.Y > txtNode.Height || e.Y < 0)
            //    {
            //        if (txtNode.Visible)
            //        {
            //            txtNode.Visible = false; 
            //        }
            //    }
            //}
        }

        void txtNode_MouseUp(object sender, MouseEventArgs e)
        {
            //muiltTxtDown = false;
        }

        void txtNode_MouseDown(object sender, MouseEventArgs e)
        {
            //muiltTxtDown = true;
        }

        void timerTxt_Tick(object sender, EventArgs e)
        { 
            timerTxt.Enabled = false;
            ShowTxtBox();
        }

        private void ShowTxtBox()
        {
            if (txtNode.Tag == null) return;
            SubjectBase subject = txtNode.Tag as SubjectBase;
            Rectangle r = subject.RectTitle;

            txtNode.AccessibleDescription = "Save";
            txtNode.Text = subject.Title;
            int left = r.Left+3;
            int top = r.Top < headerBuffer ? headerBuffer : r.Top;
            int height = r.Height - (r.Top < headerBuffer ? headerBuffer - r.Top : 0);
            txtNode.Location = new Point(left, top);

            txtNode.MinimumSize = new System.Drawing.Size(r.Width - 6, height);
            txtNode.Font = subject.Font;
            txtNode.Multiline = false;
            txtNode.ClientSize = new Size(r.Width - 6, height);
            txtNode.BorderStyle = System.Windows.Forms.BorderStyle.None;
            txtNode.BackColor = subject.BackColor;
            txtNode.Parent = this;
            txtNode.Select(txtNode.Text.Length, 0);
            txtNode.Visible = true;
            txtNode.Focus(); 
        }
        private void HideTxtBox()
        {
            if (this.txtNode.Tag != null && this.txtNode.AccessibleDescription != null && this.txtNode.AccessibleDescription == "Save")
            {
                SubjectBase subject = txtNode.Tag as SubjectBase;
                subject.Title = this.txtNode.Text;
                subject.AdjustSubjectSize();
            }

            this.txtNode.Tag = null;
            this.txtNode.AccessibleDescription = "";
            if (this.txtNode.Visible)
            {
                if (this.Controls.Contains(txtNode))
                {
                    this.Controls.Remove(txtNode);
                }
                this.txtNode.Visible = false;
            }

        }
        #endregion
         
		#region Helper Functions

        private int vsize, hsize;
        public override void AdjustScrollbars()
        {
            if(mapViewModel== MapViewModel.ExpandTwoSides)
            {
                int allRowsHeight = GetSubjectMaxHeight(centerProject);
                allRowsHeight += 20;
                int allColsWidth = GetSubjectMaxWidth(centerProject);
                allColsWidth += 20; 
                vsize = vscrollBar.Width; 
                vscrollBar.Left = this.ClientRectangle.Left + this.ClientRectangle.Width - vscrollBar.Width;
                vscrollBar.Top = this.ClientRectangle.Top + headerBuffer;
                vscrollBar.Height = this.ClientRectangle.Height - hsize - headerBuffer;
                vscrollBar.Maximum = allRowsHeight;
                vscrollBar.LargeChange = (this.ClientRectangle.Height - headerBuffer - hsize - 4 > 0 ? this.ClientRectangle.Height - headerBuffer - hsize - 4 : 0);
                if (allRowsHeight > this.ClientRectangle.Height - headerBuffer - 4 - hsize)
                {
                    vscrollBar.Show();
                    vsize = vscrollBar.Width;
                }
                else
                {
                    vscrollBar.Hide();
                    vscrollBar.Value = 0;
                    vsize = 0;
                }

                hsize = hscrollBar.Height; 
                hscrollBar.Left = this.ClientRectangle.Left;
                hscrollBar.Top = this.ClientRectangle.Top + this.ClientRectangle.Height - hscrollBar.Height;
                hscrollBar.Width = this.ClientRectangle.Width - vsize;
                hscrollBar.LargeChange = (this.ClientRectangle.Width - vsize - 4 > 0 ? this.ClientRectangle.Width - vsize - 4 : 0);
                hscrollBar.Maximum = allColsWidth;
                if (allColsWidth > this.ClientRectangle.Width - 4 - vsize)
                {
                    hscrollBar.Show();
                    hsize = hscrollBar.Height;
                }
                else
                {
                    hscrollBar.Hide();
                    hscrollBar.Value = 0;
                    hsize = 0;
                } 
                 
            }
            else
            {

                vscrollBar.Hide();
                vscrollBar.Value = 0;
                vsize = 0;
            }
        }

        private int GetSubjectMaxWidth(SubjectBase subject)
        {
            return GetSubjectMaxWidth(subject, 0);
        }
        private int GetSubjectMaxWidth(SubjectBase subject,int max)
        { 
            int maxWidth=max;
            if(subject.Expanded)
            { 
                foreach (SubjectBase n in subject.ChildSubjects)
                {
                    if (n.Expanded)
                    {
                        maxWidth = GetSubjectMaxWidth(n, maxWidth);
                    }

                    if (n.CenterPoint.X + n.Width / 2 > maxWidth)
                    {
                        maxWidth = n.CenterPoint.X + n.Width / 2;
                    }
                }
            }

            if (subject.CenterPoint.X + subject.Width / 2 > maxWidth)
            {
                maxWidth = subject.CenterPoint.X + subject.Width / 2;
            }
            return maxWidth;
        }

        
        private int GetSubjectMaxHeight(SubjectBase subject)
        {
            return GetSubjectMaxHeight(subject, 0);
        }
        private int GetSubjectMaxHeight(SubjectBase subject, int max)
        {
            int maxHeight = max;
            if (subject.Expanded)
            {
                foreach (SubjectBase n in subject.ChildSubjects)
                {
                    if (n.Expanded)
                    {
                        maxHeight = GetSubjectMaxHeight(n, maxHeight);
                    }

                    if (n.CenterPoint.Y + n.Height / 2 > maxHeight)
                    {
                        maxHeight = n.CenterPoint.Y + n.Height / 2;
                    }
                }
            }

            if (subject.CenterPoint.Y + subject.Height / 2 > maxHeight)
            {
                maxHeight = subject.CenterPoint.Y + subject.Height / 2;
            }
            return maxHeight;
        } 
        private int GetChildSubjectCount(SubjectBase node)
        {
            int rs = 0;
            foreach (SubjectBase n in node.ChildSubjects)
            {
                rs++;
                if (n.Expanded)
                {
                    rs = rs + GetChildSubjectCount(n);
                }
            }
            return rs;
        }
   
        private bool SubjectInCenterArea(MouseEventArgs e)
        {
            SubjectBase taskEvent = centerProject;
            Rectangle r = taskEvent.RectAreas;
            if (r.Left <= e.X && r.Left + r.Width >= e.X
                    && r.Top <= e.Y && r.Top + r.Height >= e.Y)
            {
                return true;
            }
            return false;
        }
        private SubjectBase SubjectInArea(MouseEventArgs e)
        {
            foreach (SubjectBase taskEvent in subjectNodes)
            {  
                Rectangle r = taskEvent.RectAreas;
                if (r.Left <= e.X && r.Left + r.Width >= e.X
                        && r.Top <= e.Y && r.Top + r.Height >= e.Y)
                {
                    return taskEvent;
                }
            }
            return null;
        }

        private SubjectBase CollaspeInArea(MouseEventArgs e)
        {
            foreach (SubjectBase taskEvent in subjectNodes)
            {
                Rectangle r = taskEvent.RectCollapse;
                if (r.Left <= e.X && r.Left + r.Width >= e.X
                        && r.Top <= e.Y && r.Top + r.Height >= e.Y)
                {
                    return taskEvent;
                }
            }
            return null;
        }

        private bool TitleInArea(MouseEventArgs e, SubjectBase taskEvent)
        {
            Rectangle r = taskEvent.RectTitle;
            if (r.Left <= e.X && r.Left + r.Width >= e.X
                    && r.Top <= e.Y && r.Top + r.Height >= e.Y)
            {
                return true;
            }
            return false;
        }

        private int PlusInArea(MouseEventArgs e,SubjectBase taskEvent)
        {
            selectPlus = 0;
            Rectangle r = taskEvent.RectPlusLeft;
            if (r.Left <= e.X && r.Left + r.Width >= e.X
                    && r.Top <= e.Y && r.Top + r.Height >= e.Y)
            {
                selectPlus = 1; 
            }
            r = taskEvent.RectPlusTop;
            if (r.Left <= e.X && r.Left + r.Width >= e.X
                    && r.Top <= e.Y && r.Top + r.Height >= e.Y)
            {
                selectPlus = 2; 
            }
            r = taskEvent.RectPlusRight;
            if (r.Left <= e.X && r.Left + r.Width >= e.X
                    && r.Top <= e.Y && r.Top + r.Height >= e.Y)
            {
                selectPlus = 3; 
            }
            r = taskEvent.RectPlusBottom;
            if (r.Left <= e.X && r.Left + r.Width >= e.X
                    && r.Top <= e.Y && r.Top + r.Height >= e.Y)
            {
                selectPlus = 4; 
            }
            return selectPlus;
        }
	 

        #endregion  
         
	}
	#endregion
     
}
