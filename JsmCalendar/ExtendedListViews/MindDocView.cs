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
     
    #region Subject
    /// <summary>
    /// 主题基类
    /// </summary>
    [Serializable]
    public class SubjectBase
    {
        private string title = "主题";
        private string content = "";

        protected int level = 0;

        private Font font = new Font("微软雅黑", 10.0f);
        private Color foreColor = Color.Black;
        private Color backColor = Color.White;
        private Color borderColor = Color.Green;
        private Color selectColor = Color.SteelBlue;
        private Color activeColor = Color.LightSteelBlue;

        private Point centerPoint = new Point(0, 0);
        private int width = 0;
        private int height = 0;
        private int branchLinkWidth = 30;
        private int branchSplitHeight = 12;

        private bool expanded = true; 
        private bool visible = true;
        private Rectangle rectAreas;
        private Rectangle rectCollapse;


        private SubjectBase parentSubject = null;

        private List<SubjectBase> childSubjects = new List<SubjectBase>();


        public SubjectBase DeepClone()
        {
            System.Runtime.Serialization.Formatters.Binary.BinaryFormatter bFormatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            MemoryStream stream = new MemoryStream();
            bFormatter.Serialize(stream, this);
            stream.Seek(0, SeekOrigin.Begin);

            SubjectBase subject = (SubjectBase)bFormatter.Deserialize(stream);
            return subject;
        }


        #region 方法
        /// <summary>
        /// 调整主题下的所有子主题位置
        /// (注：该方法只调整当前主题的子主题，
        /// 实现其它主题联动需要再获取顶层主题调用该方法实现联动)
        /// </summary>
        public virtual void AdjustPosition()
        {
            int hb = this.branchSplitHeight;  //间隔高度
            int hs = 0;   //主题高度，包含所有子主题
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
                int x = this.CenterPoint.X + (this.Width / 2) + this.BranchLinkWidth + childSubject.width / 2 + 30;
                 
                if(childSubject.CenterPoint.X>x)
                {
                    x = childSubject.centerPoint.X;
                }
                int y = top + hs / 2;
                childSubject.CenterPoint = new Point(x, y);

                top = top + hs+ hb;
                childSubject.AdjustPosition();
            } 
        }

        /// <summary>
        /// 是否展开主题
        /// </summary>
        /// <param name="isExpand">是否展开</param>
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
        /// 移动主题位置
        /// </summary>
        /// <param name="movePoint">移动后的中心位置</param>
        public virtual void MoveSubjectPosition(Point movePoint)
        { 

            //控制移动范围
            if(this.parentSubject!=null)
            {
                int x=this.parentSubject.CenterPoint.X + (this.parentSubject.Width / 2) + this.parentSubject.BranchLinkWidth + this.width / 2 + 30;
                if(x>movePoint.X)
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
                    //移动到最后
                    if (i == this.parentSubject.childSubjects.Count - 1)
                    {
                        if (movePoint.Y > subject.centerPoint.Y)
                        {
                            this.parentSubject.childSubjects.Remove(this);
                            this.parentSubject.childSubjects.Add(this);
                        }
                    }

                }

                movePoint.Y = this.CenterPoint.Y;
            } 
            //是否拖拽到其他其他节点
            this.CenterPoint = new Point(movePoint.X, movePoint.Y);
            this.AdjustPosition();   
            if(this.parentSubject!=null)
                this.parentSubject.AdjustPosition();
        }

        /// <summary>
        /// 获取主题显示总高度（所有子主题）
        /// </summary>
        /// <returns></returns>
        public int GetTotalHeight()
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
        /// 设置主题样式
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
            switch (this.level)
            {
                case 1:
                    this.Width = 93;
                    this.Height = 40;
                    this.BackColor = Color.AliceBlue;
                    this.ForeColor = Color.DimGray;
                    this.BorderColor = Color.SteelBlue;
                    this.Font = new Font("微软雅黑", 12.0f);
                    this.Title = "主题";
                    break;
                case 2:
                    this.Width = 83;
                    this.Height = 25;
                    this.BackColor = Color.Honeydew;
                    this.ForeColor = Color.DimGray;
                    this.BorderColor = Color.Green;
                    this.Font = new Font("微软雅黑", 10.0f);
                    this.Title = "子主题";

                    break;
                case 3:
                    this.Width = 63;
                    this.Height = 20;
                    this.BackColor = Color.White;
                    this.ForeColor = Color.DimGray;
                    this.BorderColor = Color.Transparent;
                    this.Font = new Font("微软雅黑", 8.0f);
                    this.Title = "子主题"; 
                    break; 
                case 4:
                    this.Width = 73;
                    this.Height = 40;
                    this.BackColor = Color.LightYellow;
                    this.ForeColor = Color.DimGray;
                    this.BorderColor = Color.Orange;
                    this.Font = new Font("微软雅黑", 10.0f);
                    this.Title = "附注";
                    break;
                default: 
                    this.Width = 63;
                    this.Height = 20;
                    this.BackColor = Color.White;
                    this.ForeColor = Color.DimGray;
                    this.BorderColor = Color.Transparent;
                    this.Font = new Font("微软雅黑", 8.0f);
                    this.Title = "子主题";
                    break;
            }

            foreach (SubjectBase childSubject in ChildSubjects)
            {
                childSubject.SetTitleStyle();
            }
        }

        /// <summary>
        /// 获取顶层主题
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

        #region 属性
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
        public Rectangle RectCollapse
        {
            get { return rectCollapse; }
            set { rectCollapse = value; }
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
                SetTitleStyle(); 
                if (!parentSubject.childSubjects.Contains(this))
                {
                    parentSubject.childSubjects.Add(this);
                    parentSubject.AdjustPosition();

                    SubjectBase topSubject = parentSubject.GetTopSubect();
                    if(topSubject!=null)
                    {
                        topSubject.AdjustPosition();
                    }


                }
            }
        }

        public List<SubjectBase> ChildSubjects
        {
            get { return childSubjects; }
            set { childSubjects = value; }
        }
        #endregion
    }

    /// <summary>
    /// 中心主题
    /// </summary>
    [Serializable]
    public class CenterSubject:SubjectBase
    {
        public CenterSubject()
        {
            this.Width = 133;
            this.Height = 50;
            this.BackColor = Color.WhiteSmoke;
            this.ForeColor = Color.DimGray;
            this.BorderColor = Color.DimGray;
            this.Font = new Font("微软雅黑", 14.0f);
            this.Title = "中心主题";
            this.BranchLinkWidth = 20;
            this.BranchSplitHeight=24;

            this.level = 0;
        }

        public override void AdjustPosition()
        { 
            base.AdjustPosition(); 
        }

    }
    /// <summary>
    /// 主要主题
    /// </summary>
    [Serializable]
    public class TitleSubject : SubjectBase
    {
        public TitleSubject(SubjectBase fatherSubject)
        {
            this.ParentSubject = fatherSubject; 
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
         

        //设置选中的节点; 
        private SubjectBase selectedSubject = null;
        private SubjectBase activeSubject = null;
        private SubjectBase collapseSubject = null;

        private SubjectBase centerProject = null;
         
        private SubjectBase dragSubject = null;
        private int dragStartPx = 0;
        private int dragStartPy = 0;

        private Color tempBackColor;
        private Color tempForeColor;


        private int selectionStartLine = 0;
        private int selectionEndLine = 0;

        private DateTime currentTime=DateTime.Now; 
        private MapViewModel mapViewModel = MapViewModel.ExpandTwoSides;
         
        TextBox txtNode = null;
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
                if (e.Y > headerBuffer) //点击标题以下区域，选择任务事件
                { 
                    bool addTitle = true;
                    if (addTitle && e.Clicks == 2)
                    {
                        SubjectBase subject = null;
                        if (SubjectInCenterArea(e))
                        {
                            subject = new TitleSubject(centerProject);
                        }
                        else if (SubjectInArea(e) != null)
                        {
                            SubjectBase clickSubject = SubjectInArea(e);
                            subject = new TitleSubject(clickSubject);
                        }
                        else
                        {
                            //浮动标题
                            //subject = new TitleBatchSubject();
                            //subject.CenterPoint = new Point(e.X, e.Y); 
                        }
                        if(subject!=null)
                            subjectNodes.Add(subject);
                    }

                    selectedSubject = null;
                    SubjectBase subjectNode = SubjectInArea(e);
                    if (subjectNode != null)
                    {
                        selectedSubject = subjectNode;

                        dragSubject = subjectNode.DeepClone();
                        dragSubject.TranslateLightColor();
                        dragStartPx = e.X;
                        dragStartPy = e.Y;
                    }

                    SubjectBase collaspSubject = CollaspeInArea(e);
                    if(collapseSubject!=null)
                    {
                        collapseSubject.CollapseChildSubject(!collapseSubject.Expanded);
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
            if (e.Y > headerBuffer)
            { 
                //处理拖拽主题
                if(dragSubject!=null)  
                {
                    SubjectBase nowSubject = selectedSubject;
                     
                    int scaleX = e.X - dragStartPx;
                    int scaleY = e.Y - dragStartPy;
                    if ((Math.Abs(scaleX) > 10 || Math.Abs(scaleY) > 10) && nowSubject != null)
                    {
                        dragSubject.CenterPoint = new Point(nowSubject.CenterPoint.X + scaleX, nowSubject.CenterPoint.Y + scaleY);

                        //是否移动到其他主题
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
                else //处理鼠标移动活动主题和折叠按钮
                {
                    SubjectBase subjectNode = SubjectInArea(e);
                    if (subjectNode != null)
                    {
                        activeSubject = subjectNode; 
                    }
                    else
                    {
                        subjectNode = CollaspeInArea(e);
                        if (subjectNode != null)
                        {
                            collapseSubject = subjectNode;
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
                //当前拖拽主题的实际主题
                SubjectBase nowSubject = selectedSubject;

                int scaleX = e.X - dragStartPx;
                int scaleY = e.Y - dragStartPy;
                //处理非中心主题的拖拽
                if ((Math.Abs(scaleX) > 10 || Math.Abs(scaleY) > 10) && nowSubject != null)
                { 
                    //是否拖拽到其他主题上
                    SubjectBase subjectNode = SubjectInArea(e);
                    if (subjectNode != null) //1.将主题作为移动主题的父主题
                    {
                        //判断是否移动到自己的子主题
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
                            nowSubject.ParentSubject = subjectNode; 
                        }
                    }
                    else  //2.移动拖拽主题位置
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
			int maxrend = ClientRectangle.Height/itemheight+1;


            AdjustScrollbars();
            RenderSubject(centerProject, g, r, 0);
            //for(i=0;i<subjectNodes.Count;i++)
            //{
            //    RenderSubject(subjectNodes[i],g,r,0);
            //}
            if(dragSubject!=null)
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
            //绘制主题绘制区域
            subjectNode.RectAreas = sr;
             
            //绘制所有子主题 
            if(subjectNode.Expanded)
            {
                foreach (SubjectBase childSubject in subjectNode.ChildSubjects)
                {
                    RenderSubject(childSubject, g, r, level + 1);
                } 
            }

            //开始绘制
            g.SmoothingMode = SmoothingMode.AntiAlias; 
            //1.绘制连接线 
            if (subjectNode.ParentSubject != null)
            {
                RenderSubjectParentLink(g, r, subjectNode);
            }   
            //2.先绘制主题的分支连接线和折叠按钮，再绘制主题内容显示最前  
            RenderSubjectBranchLink(g, r, subjectNode);

            //3.绘制主题内容  
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
            //绘制主题绘制区域
            subjectNode.RectAreas = sr;
             
            //开始绘制
            g.SmoothingMode = SmoothingMode.AntiAlias;
            //1.绘制连接线 
            if (subjectNode.ParentSubject != null)
            {
                RenderSubjectParentLink(g, r, subjectNode);
            } 

            //3.绘制主题内容  
            RenderSubjectTitle(g, r, subjectNode);

            g.SmoothingMode = SmoothingMode.Default;
        }
         
         
        private void RenderSubjectParentLink(Graphics g, Rectangle r, SubjectBase subject)
        {
            if (subject.ParentSubject == null) return; 
            g.Clip = new System.Drawing.Region(r);
            SubjectBase parentSubject = subject.ParentSubject;

            int branchLinkWidth = parentSubject.BranchLinkWidth;

            Pen penLine = new Pen(subject.BorderColor == Color.Transparent ? subject.ForeColor : subject.BorderColor, 1.0f);
            if(mapViewModel== MapViewModel.ExpandTwoSides)
            {
                //父主题连接线起始点
                int x1 = parentSubject.RectAreas.Left + parentSubject.RectAreas.Width;
                int y1 = parentSubject.RectAreas.Top + parentSubject.RectAreas.Height / 2;
                //父主题分支连接点
                int x2 = x1 + branchLinkWidth;
                int y2 = y1;
                //子主体连接线起始点
                int x3 = subject.RectAreas.Left;
                int y3 = subject.RectAreas.Top + subject.RectAreas.Height / 2;

                RenderLink(g, r,penLine, x2, y2, x3, y3);
            }
        }

        private void RenderSubjectBranchLink(Graphics g, Rectangle r, SubjectBase subject)
        {
            if (subject.ChildSubjects.Count == 0) return;
            g.Clip = new System.Drawing.Region(r);

            //先绘制主题的分支连接线和折叠按钮，再绘制主题内容显示最前  
            int x1 = subject.RectAreas.Left + subject.RectAreas.Width;
            int y1 = subject.RectAreas.Top + subject.RectAreas.Height / 2;

            int x2 = x1 + subject.BranchLinkWidth;
            int y2 = y1;

            Color penColor = subject.BorderColor == Color.Transparent ? subject.ForeColor : subject.BorderColor;
            Pen penLine = new Pen(penColor, 1.0f);
            //绘制分支主线
            g.DrawLine(penLine, x1, y1, x2, y2);

            //绘制折叠按钮 
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
            if (selectedSubject == subject)  //绘制选择边框和扩展
            {
                Rectangle activeArea = new Rectangle(sr.Left - 3, sr.Top - 6, sr.Width + 7, sr.Height + 10);

                //绘制扩展
                RenderPlus(g, activeArea);

                //绘制边框和背景
                FillRoundRectangle(g, new SolidBrush(subject.SelectColor), activeArea, radius);
                g.FillRectangle(Brushes.White, sr.Left - 1, sr.Top - 1, sr.Width + 2, sr.Height + 2);

            }
            else if (activeSubject == subject) //绘制选中边框
            {
                Rectangle activeArea = new Rectangle(sr.Left - 3, sr.Top - 6, sr.Width + 7, sr.Height + 10);
                FillRoundRectangle(g, new SolidBrush(subject.ActiveColor), activeArea, radius);
                g.FillRectangle(Brushes.White, sr.Left - 1, sr.Top - 1, sr.Width + 2, sr.Height + 2);
            }

            FillRoundRectangle(g, new SolidBrush(subject.BackColor), sr, radius);
            DrawRoundRectangle(g, p, sr, radius);

            SizeF size = g.MeasureString(title, f);
            g.DrawString(title, f, new SolidBrush(subject.ForeColor), (float)(sr.Left + sr.Width / 2 - size.Width / 2), (float)(sr.Top + sr.Height / 2 - Math.Floor(size.Height) / 2 + 1));
             

        }

        private void RenderLink(Graphics g, Rectangle r, Pen penLine, int x1, int y1, int x2, int y2)
        {
            if (x1 == x2 || y1 == y2)
            {
                g.DrawLine(penLine, x1, y1, x2, y2);
                return;
            }


            Rectangle rect = new Rectangle(x1, y1, x2 - x1, y2 - y1);
            //直线绘制方向(0,1,2,3分别表示以矩形左上角开始从哪一个点绘制
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

        private void RenderPlus(Graphics g,Rectangle area)
        {
            Rectangle sr = new Rectangle(area.Left - 10, area.Top - 10, area.Width + 20, area.Height + 20);
            g.Clip = new Region(sr); 

            Pen penPlus = new Pen(new SolidBrush(Color.White), 2.0f);

            Rectangle pmLeft = new Rectangle(area.Left - 8, area.Top + area.Height / 2 - 10,40,20);
            g.FillEllipse(new SolidBrush(Color.SteelBlue), pmLeft);

            g.DrawLine(penPlus, pmLeft.Left + 2, pmLeft.Top + pmLeft.Height / 2, pmLeft.Left + 8, pmLeft.Top + pmLeft.Height / 2);
            g.DrawLine(penPlus, pmLeft.Left + 5, pmLeft.Top + pmLeft.Height / 2-3, pmLeft.Left + 5, pmLeft.Top + pmLeft.Height / 2+3);

            Rectangle pmRight = new Rectangle(area.Left+area.Width-(40-8)-1, area.Top + area.Height / 2 - 10, 40, 20);
            g.FillEllipse(new SolidBrush(Color.SteelBlue), pmRight);

            g.DrawLine(penPlus, pmRight.Right - 8, pmRight.Top + pmRight.Height / 2, pmRight.Right -2, pmRight.Top + pmRight.Height / 2);
            g.DrawLine(penPlus, pmRight.Right - 5, pmRight.Top + pmRight.Height / 2-3, pmRight.Right - 5, pmRight.Top + pmRight.Height / 2+3);


            Rectangle pmTop = new Rectangle(area.Left + area.Width/2-10, area.Top - 8, 20, 40);
            g.FillEllipse(new SolidBrush(Color.SteelBlue), pmTop);
             
            g.DrawLine(penPlus, pmTop.Left + 7, pmTop.Top + 5, pmTop.Left + 13, pmTop.Top + 5);
            g.DrawLine(penPlus, pmTop.Left + 10, pmTop.Top + 2, pmTop.Left + 10, pmTop.Top + 8);


            Rectangle pmBottom = new Rectangle(area.Left + area.Width / 2 - 10, area.Top +area.Height-(40-8)-1, 20, 40);
            g.FillEllipse(new SolidBrush(Color.SteelBlue), pmBottom);

            g.DrawLine(penPlus, pmBottom.Left + 7, pmBottom.Bottom -5, pmBottom.Left + 13, pmBottom.Bottom -5);
            g.DrawLine(penPlus, pmBottom.Left + 10, pmBottom.Bottom - 2, pmBottom.Left + 10, pmBottom.Bottom - 8);

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
        /// 绘制弧形
        /// </summary>
        /// <param name="g"></param>
        /// <param name="pen"></param>
        /// <param name="rect"></param>
        /// <param name="cornerRadius">圆角半径</param>
        /// <param name="direction">直线绘制方向(0,1,2,3分别表示以矩形左上角开始从哪一个点绘制)</param>
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
            }
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
            //if (txtNode.Tag == null) return;
            //TaskEventNode task = txtNode.Tag as TaskEventNode;
            //Rectangle r = task.SelectedTitleArea;

            //txtNode.AccessibleDescription = "Save";
            //txtNode.Text = task.Title;
            //int left = r.Left;
            //int top = r.Top < headerBuffer ? headerBuffer : r.Top;
            //int height = r.Height - (r.Top < headerBuffer ? headerBuffer - r.Top : 0);
            //txtNode.Location=new Point(left,top);
            //if (calendarViewMode == CalendarViewModel.Month || calendarViewMode== CalendarViewModel.Year)
            //{
            //    txtNode.Multiline = false;
            //    txtNode.ClientSize = new Size(r.Width - 6, height);
            //}
            //else if(calendarViewMode== CalendarViewModel.TimeSpan)
            //{
            //    txtNode.Multiline = false;
            //    txtNode.ClientSize = new Size(r.Width - 2, height);
            //}
            //else if (calendarViewMode == CalendarViewModel.Week || calendarViewMode== CalendarViewModel.WorkWeek || calendarViewMode == CalendarViewModel.Day || calendarViewMode== CalendarViewModel.MonthWeek)
            //{
            //    txtNode.Multiline = true;
            //    txtNode.ClientSize = new Size(r.Width - 2, height - 2);

            //}
            //txtNode.Parent = this;
            //txtNode.Select(txtNode.Text.Length, 0);
            //txtNode.Visible = true;
            //txtNode.Focus(); 
        }
        private void HideTxtBox()
        {
            //if(this.txtNode.Tag!=null && this.txtNode.AccessibleDescription!=null && this.txtNode.AccessibleDescription == "Save")
            //{ 
            //    TaskEventNode task = txtNode.Tag as TaskEventNode;
            //    task.Title = this.txtNode.Text;
            //}

            //this.txtNode.Tag = null;
            //this.txtNode.AccessibleDescription = "";
            //if(this.txtNode.Visible)
            //{  
            //    if (this.Controls.Contains(txtNode))
            //    {
            //        this.Controls.Remove(txtNode);
            //    }
            //    this.txtNode.Visible = false;
            //}

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
	 

        #endregion  
         
	}
	#endregion
     
}
