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
    public enum MapLinkStyle { RectLine, RoundLine, Curve, Cycle, Angle };
    public enum MapTheme { Default, Classics, Bubble, Lemo, Black };
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
    /// 主题基类
    /// </summary>
    [Serializable]
    public class SubjectBase
    {
        public static float zoomValue = 1;
        static public List<SubjectBase> FloatSubjects = new List<SubjectBase>();
        static public MapViewModel viewMode = MapViewModel.ExpandRightSide;
        static public MindDocTheme mindDocThem = new MindDocThemeDefault();
        private string title = "主题";
        private string content = "";

        protected int level = 0;

        protected int direction = 1;


        private Font font = new Font("微软雅黑", 10.0f);
        private Color foreColor = Color.Black;
        private Color backColor = Color.White;
        private Color borderColor = Color.Green;
        private Color selectColor = Color.SteelBlue;
        private Color activeColor = Color.LightSteelBlue;
        private MapLinkStyle lineStyle = MapLinkStyle.RoundLine;

        private float zoom = 1;

        protected Point centerPoint = new Point(0, 0);
        protected int width = 0;
        protected int height = 0;
        protected int marginWidth = 20;
        protected int marginHeight = 10;
        protected int branchLinkWidth = 30;
        protected int branchSplitHeight = 12;
        protected int imageSplitWidth = 5;
        protected bool equalWidth = false;

        protected int collapseRadius = 6;


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
        private SubjectBase linkSubject = null;




        protected List<SubjectBase> childSubjects = new List<SubjectBase>();


        protected List<SubjectPicture> subjectPictures = new List<SubjectPicture>();

        private int moveX = -10000;
        private int moveY = -10000;


        private bool dragOut = false;

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


        #region 方法
        /// <summary>
        /// 调整主题下的所有子主题位置
        /// (注：该方法只调整当前主题的子主题
        /// 实现其它主题联动需要再获取顶层主题调用该方法实现联动)
        /// </summary>
        public virtual void AdjustPosition()
        {
            if (viewMode == MapViewModel.ExpandTwoSides)
            {
                if (this.GetType() == typeof(CenterSubject))
                {
                    List<SubjectBase> lstLeftSubjecs = new List<SubjectBase>();
                    List<SubjectBase> lstRightSubjecs = new List<SubjectBase>();

                    foreach (SubjectBase subject in this.ChildSubjects)
                    {
                        if (subject.direction == 1)
                        {
                            lstRightSubjecs.Add(subject);
                        }
                        else
                        {
                            lstLeftSubjecs.Add(subject);
                        }
                    }

                    if (lstLeftSubjecs.Count > 0)
                        AdjustExpandTowSides(lstLeftSubjecs, -1);
                    if (lstRightSubjecs.Count > 0)
                        AdjustExpandTowSides(lstRightSubjecs, 1);
                }
                else
                {
                    AdjustExpandTowSides();
                }


            }
            else if (viewMode == MapViewModel.ExpandRightSide)
            {
                AdjustExpandRightSide();
            }
            else if (viewMode == MapViewModel.TreeMap)
            {
                AdjustTreeMap();
            }
            else if (viewMode == MapViewModel.Structure)
            {
                AdjustStructure();
            }
        }
        private void AdjustExpandTowSides(List<SubjectBase> lstSubjecs = null, int side = 0)
        {
            if (lstSubjecs == null)
            {
                lstSubjecs = this.ChildSubjects;
            }

            int d = 1;  //扩展方向
            int hb = this.branchSplitHeight;  //间隔高度
            int hs = 0;   //主题高度，包含所有子主题
            int count = lstSubjecs.Count;
            if (count == 0) return;
            if (this.expanded == false) return;
            int totalHeight = 0;
            for (int i = 0; i < count; i++)
            {
                SubjectBase childSubject = lstSubjecs[i];
                hs = childSubject.GetTotalHeight();
                totalHeight += hs;
                totalHeight += hb;
            }
            totalHeight -= hb;

            int top = this.CenterPoint.Y - totalHeight / 2;
            if (top < this.height / 2)
            {
                top = this.height / 2;
            }
            for (int i = 0; i < count; i++)
            {
                SubjectBase childSubject = lstSubjecs[i];
                //使用父主题方向
                d = side != 0 ? side : this.direction;
                hs = childSubject.GetTotalHeight();
                int x = childSubject.centerPoint.X + childSubject.moveX;
                if (childSubject.moveX == -10000)
                {
                    x = this.CenterPoint.X + d * (this.Width / 2) + d * this.BranchLinkWidth + d * (childSubject.width / 2) + d * childSubject.leftBuffer;

                    if (childSubject.relateSubject != null) //和关联节点同一位置
                    {
                        x = childSubject.relateSubject.centerPoint.X + d * (childSubject.width / 2);
                    }
                }
                int y = top + hs / 2;

                childSubject.CenterPoint = new Point(x, y);

                top = top + hs + hb;

                foreach (SubjectBase chidsub in childSubject.childSubjects)
                {
                    chidsub.moveX = childSubject.moveX + childSubject.scalceWidth;
                    chidsub.direction = d;
                }
                childSubject.moveX = 0;
                childSubject.scalceWidth = 0;
                childSubject.AdjustPosition();
            }
        }


        private void AdjustExpandRightSide()
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
            if (top < this.height / 2)
            {
                top = this.height / 2;
            }
            for (int i = 0; i < count; i++)
            {
                SubjectBase childSubject = this.ChildSubjects[i];
                hs = childSubject.GetTotalHeight();
                int x = childSubject.centerPoint.X + childSubject.moveX;
                if (childSubject.moveX == -10000)
                {
                    x = this.CenterPoint.X + (this.Width / 2) + this.BranchLinkWidth + childSubject.width / 2 + childSubject.leftBuffer;

                    if (childSubject.relateSubject != null) //和关联节点同一位置
                    {
                        x = childSubject.relateSubject.centerPoint.X + childSubject.width / 2;
                    }
                }
                int y = top + hs / 2;

                childSubject.CenterPoint = new Point(x, y);

                top = top + hs + hb;

                foreach (SubjectBase chidsub in childSubject.childSubjects)
                {
                    chidsub.moveX = childSubject.moveX + childSubject.scalceWidth;
                }
                childSubject.moveX = 0;
                childSubject.scalceWidth = 0;
                childSubject.AdjustPosition();
            }
        }

        private void AdjustTreeMap()
        {
            int hb = this.branchSplitHeight;  //间隔高度
            int hs = 0;   //主题高度，包含所有子主题
            int count = this.ChildSubjects.Count;
            if (count == 0) return;
            if (this.expanded == false) return;
            int top = this.centerPoint.Y + this.height / 2 + this.BranchLinkWidth + hb;
            for (int i = 0; i < count; i++)
            {
                SubjectBase childSubject = this.ChildSubjects[i];
                hs = childSubject.GetTotalHeight();
                int x = childSubject.centerPoint.X + childSubject.moveX;
                if (childSubject.moveX == -10000)
                {
                    if (this.level == 0)
                    {
                        x = this.CenterPoint.X + (this.Width / 2) + this.BranchLinkWidth + childSubject.width / 2 + childSubject.leftBuffer;
                    }
                    else
                    {
                        x = this.CenterPoint.X - (this.Width / 2) + this.BranchLinkWidth / 2 + childSubject.Width / 2 + childSubject.leftBuffer;
                    }
                    if (childSubject.relateSubject != null) //和关联节点同一位置
                    {
                        x = childSubject.relateSubject.centerPoint.X + childSubject.width / 2;
                    }
                }
                int y = top + childSubject.height / 2;

                if (childSubject.GetType() == typeof(AttachSubject))
                {

                }
                childSubject.CenterPoint = new Point(x, y);

                top = top + hs + hb;

                foreach (SubjectBase chidsub in childSubject.childSubjects)
                {
                    chidsub.moveX = childSubject.moveX + childSubject.scalceWidth;
                }
                childSubject.moveX = 0;
                childSubject.scalceWidth = 0;
                childSubject.AdjustPosition();
            }
        }

        private void AdjustStructure()
        {
            int hb = this.branchSplitHeight;  //间隔高度
            int hs = 0;   //主题高度，包含所有子主题
            int count = this.ChildSubjects.Count;
            if (count == 0) return;
            if (this.expanded == false) return;

            int top = 0;
            if (this.level == 0)
            {
                int totalWidth = 0;
                int ws = 0;
                for (int i = 0; i < count; i++)
                {
                    SubjectBase childSubject = this.ChildSubjects[i];
                    ws = childSubject.GetTotalWidth();
                    totalWidth += ws;
                    totalWidth += hb;
                }
                totalWidth -= hb;


                int left = this.CenterPoint.X - totalWidth / 2;
                if (left < this.Width / 2)
                {
                    left = this.Width / 2;
                }

                for (int i = 0; i < count; i++)
                {
                    SubjectBase childSubject = this.ChildSubjects[i];
                    ws = childSubject.GetTotalWidth();
                    int y = childSubject.centerPoint.Y + childSubject.moveY;
                    if (childSubject.moveY == -10000)
                    {
                        y = this.CenterPoint.Y + (this.Height / 2) + this.BranchLinkWidth + childSubject.height / 2 + childSubject.leftBuffer;

                        if (childSubject.relateSubject != null) //和关联节点同一位置
                        {
                            y = childSubject.relateSubject.centerPoint.Y + childSubject.height / 2;
                        }
                    }

                    int x = left + childSubject.width / 2;
                    childSubject.CenterPoint = new Point(x, y);

                    left = left + ws + hb;

                    foreach (SubjectBase chidsub in childSubject.childSubjects)
                    {
                        chidsub.moveY = childSubject.moveY;
                    }
                    childSubject.moveY = 0;
                    childSubject.scalceWidth = 0;
                    childSubject.AdjustPosition();
                }

            }
            else
            {
                top = this.centerPoint.Y + this.height / 2 + this.BranchLinkWidth + hb;
                for (int i = 0; i < count; i++)
                {
                    SubjectBase childSubject = this.ChildSubjects[i];
                    hs = childSubject.GetTotalHeight();
                    int x = childSubject.centerPoint.X + childSubject.moveX;
                    //if (childSubject.moveX == -10000)
                    //{
                    if (this.level == 0)
                    {
                        x = this.CenterPoint.X + (this.Width / 2) + this.BranchLinkWidth + childSubject.width / 2 + childSubject.leftBuffer;
                    }
                    else
                    {
                        x = this.CenterPoint.X - (this.Width / 2) + this.BranchLinkWidth / 2 + childSubject.Width / 2 + childSubject.leftBuffer;
                    }
                    if (childSubject.relateSubject != null) //和关联节点同一位置
                    {
                        x = childSubject.relateSubject.centerPoint.X + childSubject.width / 2;
                    }
                    //} 




                    int y = top + childSubject.height / 2;

                    if (childSubject.GetType() == typeof(AttachSubject))
                    {

                    }
                    childSubject.CenterPoint = new Point(x, y);

                    top = top + hs + hb;

                    foreach (SubjectBase chidsub in childSubject.childSubjects)
                    {
                        chidsub.moveX = childSubject.moveX + childSubject.scalceWidth;
                    }
                    childSubject.moveX = 0;
                    childSubject.scalceWidth = 0;
                    childSubject.AdjustPosition();
                }

            }
        }

        /// <summary>
        /// 调整主题大小
        /// </summary>
        public void AdjustSubjectSize(bool bAdjustPosition = true)
        {
            Image bmp = new Bitmap(200, 200);
            Graphics g = Graphics.FromImage(bmp);

            int w = 0;
            int h = 0;
            foreach (SubjectPicture subjectPicture in subjectPictures)
            {
                w += subjectPicture.SubjectImage.Width;
                w += this.imageSplitWidth;

                if (subjectPicture.SubjectImage.Height > h)
                {
                    h = subjectPicture.SubjectImage.Height;
                }
            }
            //绘制文本
            SizeF size = g.MeasureString(this.title, this.font);
            w += (int)size.Width;
            if (size.Height > h)
            {
                h = (int)size.Height;
            }
            int dx = this.direction * (this.marginWidth + w + this.marginWidth - this.width) / 2;
            int dy = (this.marginHeight + h + this.marginHeight - this.height) / 2;
            this.width = this.marginWidth + w + this.marginWidth;
            this.height = this.equalWidth ? this.width : this.marginHeight + h + this.marginHeight;

            if (bAdjustPosition)
            {
                this.moveX = dx;
                this.scalceWidth = dx;  //增加宽度
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
        }

        /// <summary>
        /// 移动主题位置
        /// </summary>
        /// <param name="movePoint">移动后的中心位置</param>
        public virtual void MoveSubjectPosition(Point movePoint)
        {
            SubjectBase parentSubject = this.ParentSubject;

            int dX = movePoint.X - this.centerPoint.X;
            int dY = movePoint.Y - this.centerPoint.Y;
            //拖出主题
            if (this.DragOut)
            {
                SubjectBase topSubject = this.GetTopSubect();
                this.ParentSubject = null;
                FloatSubjects.Add(this);
                this.SetTitleStyle();

                this.CenterPoint = new Point(movePoint.X, movePoint.Y);

                foreach (SubjectBase childSub in this.childSubjects)
                {
                    childSub.moveX = -10000;
                    childSub.moveY = -10000;
                }
                this.moveX = 0;
                this.moveY = 0;

                this.AdjustPosition();
                while (parentSubject != null)
                {
                    parentSubject.AdjustPosition();
                    parentSubject = parentSubject.ParentSubject;
                }

                this.DragOut = false;
                return;
            }

            //移动范围
            if (this.parentSubject != null)
            {
                if (viewMode == MapViewModel.Structure && this.Level == 1)  //组织结构图第一层主题
                {
                    int y = this.parentSubject.CenterPoint.Y + (this.parentSubject.height / 2) + this.parentSubject.BranchLinkWidth + this.height / 2 + this.leftBuffer;
                    if (y > movePoint.Y)
                    {
                        movePoint.Y = y;
                    }

                    bool bLeft = movePoint.X < this.centerPoint.X;
                    for (int i = 0; i < this.parentSubject.childSubjects.Count; i++)
                    {
                        SubjectBase subject = this.parentSubject.childSubjects[i];

                        if (subject != this)
                        {

                            if (movePoint.X < subject.centerPoint.X)
                            {
                                if (bLeft)
                                {
                                    this.parentSubject.childSubjects.Remove(this);
                                    this.parentSubject.childSubjects.Insert(i, this);
                                }
                                else
                                {
                                    this.parentSubject.childSubjects.Remove(this);
                                    this.parentSubject.childSubjects.Insert(i - 1, this);
                                }

                                break;
                            }
                            //移动到最后
                            if (i == this.parentSubject.childSubjects.Count - 1)
                            {
                                if (movePoint.X > subject.centerPoint.X)
                                {
                                    this.parentSubject.childSubjects.Remove(this);
                                    this.parentSubject.childSubjects.Add(this);
                                }
                            }
                        }
                    }

                }
                else
                {
                    if (this.direction == 1)
                    {
                        int x = this.parentSubject.CenterPoint.X + (this.parentSubject.Width / 2) + this.parentSubject.BranchLinkWidth + this.width / 2 + this.leftBuffer;
                        if (x > movePoint.X)
                        {
                            movePoint.X = x;
                        }
                    }
                    else if (direction == -1)
                    {
                        int x = this.parentSubject.CenterPoint.X - (this.parentSubject.Width / 2) - this.parentSubject.BranchLinkWidth - this.width / 2 - this.leftBuffer;
                        if (x < movePoint.X)
                        {
                            movePoint.X = x;
                        }
                    }


                    bool bUp = movePoint.Y < this.centerPoint.Y;
                    for (int i = 0; i < this.parentSubject.childSubjects.Count; i++)
                    {
                        SubjectBase subject = this.parentSubject.childSubjects[i];

                        if (subject != this && subject.direction == this.direction)
                        {
                            if (movePoint.Y < subject.centerPoint.Y)
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

                                break;
                            }
                        }

                        //移动到最后
                        if (i == this.parentSubject.childSubjects.Count - 1)
                        {
                            //找到同方向最后一个主题

                            for (int l = this.parentSubject.childSubjects.Count - 1; l >= 0; l--)
                            {
                                SubjectBase lastSubject = this.parentSubject.childSubjects[l];
                                if (lastSubject.direction == this.direction)
                                {
                                    if (movePoint.Y > lastSubject.centerPoint.Y)
                                    {
                                        this.parentSubject.childSubjects.Remove(this);
                                        this.parentSubject.childSubjects.Add(this);
                                    }
                                }
                            }
                        }
                    }

                    if (this.GetType() == typeof(TitleSubject))
                    {
                        movePoint.Y = this.CenterPoint.Y;
                    }
                }
            }
            dX = movePoint.X - this.centerPoint.X;
            dY = movePoint.Y - this.centerPoint.Y;
            this.CenterPoint = new Point(movePoint.X, movePoint.Y);

            foreach (SubjectBase childSub in this.childSubjects)
            {
                childSub.moveX = dX;
                childSub.moveY = dY;
            }
            this.moveX = 0;
            this.moveY = 0;
            this.AdjustPosition();

            while (parentSubject != null)
            {
                parentSubject.AdjustPosition();
                parentSubject = parentSubject.ParentSubject;
            }
        }

        public void ResortChildSubject()
        {
            this.moveX = -10000;
            this.moveY = -10000;
            for (int i = 0; i < this.ChildSubjects.Count; i++)
            {
                SubjectBase childSubject = this.ChildSubjects[i];
                childSubject.ResortChildSubject();
                childSubject.AdjustPosition();
            }
            mindDocThem.SetThemeStyle(this);
            this.AdjustSubjectSize(false);
            this.AdjustPosition();
        }

        /// <summary>
        /// 插入主题
        /// </summary>
        /// <param name="subject"></param>
        public void InsertSubject(SubjectBase subject, int position)
        {
            subject.parentSubject = this;
            subject.SetTitleStyle();

            if (!this.childSubjects.Contains(subject))
            {
                if (this.expanded == false)
                {
                    this.expanded = true;
                }
                if (this.childSubjects.Count == 0 || position >= this.childSubjects.Count || position < 0)
                {
                    this.childSubjects.Add(subject);
                }
                else
                {
                    this.childSubjects.Insert(position, subject);
                }
                this.AdjustPosition();

                SubjectBase parentSubject = this.ParentSubject;
                while (parentSubject != null)
                {
                    parentSubject.AdjustPosition();
                    parentSubject = parentSubject.ParentSubject;
                }

                //SubjectBase topSubject = this.GetTopSubect();
                //if (topSubject != null)
                //{
                //    topSubject.AdjustPosition();
                //} 
            }
        }

        /// <summary>
        /// 移除主题
        /// </summary>
        /// <param name="subject"></param>
        public void RemoveSubject(SubjectBase subject)
        {
            if (this.ChildSubjects.Contains(subject))
            {
                this.ChildSubjects.Remove(subject);
                this.AdjustPosition();

                SubjectBase parentSubject = this.ParentSubject;
                while (parentSubject != null)
                {
                    parentSubject.AdjustPosition();
                    parentSubject = parentSubject.ParentSubject;
                }

                //SubjectBase topSubject = this.GetTopSubect();
                //if (topSubject != null)
                //{
                //    topSubject.AdjustPosition();
                //}
            }
        }

        /// <summary>
        /// 增加图片
        /// </summary>
        /// <param name="url">本地图片路径</param>
        public void AddImage(string url)
        {
            SubjectPicture subjectPicture = new SubjectPicture(url);
            this.subjectPictures.Add(subjectPicture);
            this.AdjustSubjectSize();
        }

        /// <summary>
        /// 按比例缩放主题
        /// </summary>
        public void ZoomValue()
        {
            this.font = new Font("微软雅黑", this.font.Size / this.zoom * zoomValue);

            this.centerPoint = new Point((int)(this.centerPoint.X / this.zoom * zoomValue), (int)(this.centerPoint.Y / this.zoom * zoomValue));
            this.width = (int)(this.width / this.zoom * zoomValue);
            this.height = (int)(this.height / this.zoom * zoomValue);
            this.marginWidth = (int)(this.marginWidth / this.zoom * zoomValue);
            this.marginHeight = (int)(this.marginHeight / this.zoom * zoomValue);
            this.branchLinkWidth = (int)(this.branchLinkWidth / this.zoom * zoomValue);
            this.branchSplitHeight = (int)(this.branchSplitHeight / this.zoom * zoomValue);
            this.imageSplitWidth = (int)(this.imageSplitWidth / this.zoom * zoomValue);
            this.leftBuffer = (int)(this.leftBuffer / this.zoom * zoomValue);
            this.collapseRadius = (int)(this.collapseRadius / this.zoom * zoomValue);

            this.zoom = zoomValue;
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

        public virtual int[] GetPlusVisible()
        {
            int[] plus = new int[4];
            plus[0] = 1;
            plus[1] = 1;
            plus[2] = 1;
            plus[3] = 1;
            return plus;
        }

        /// <summary>
        /// 获取主题显示总高度（所有子主题）
        /// </summary>
        /// <returns></returns>
        public virtual int GetTotalHeight()
        {
            int hb = 0;
            if (this.childSubjects.Count < 1)
            {
                hb = this.height;
            }
            else
            {
                int min = GetSubjectMinHeight(this, this.centerPoint.Y);
                int max = GetSubjectMaxHeight(this, 0);
                hb = max - min;
                if (hb < this.height)
                {
                    hb = this.height;
                }
            }
            return hb;
        }

        private int GetSubjectMinHeight(SubjectBase subject, int min)
        {
            int minHeight = min;
            if (subject.Expanded)
            {
                foreach (SubjectBase n in subject.ChildSubjects)
                {
                    if (n.Expanded)
                    {
                        minHeight = GetSubjectMinHeight(n, minHeight);
                    }

                    if (n.CenterPoint.Y - n.Height / 2 < minHeight)
                    {
                        minHeight = n.CenterPoint.Y - n.Height / 2;
                    }
                }
            }

            if (subject.CenterPoint.Y - subject.Height / 2 < minHeight)
            {
                minHeight = subject.CenterPoint.Y - subject.Height / 2;
            }
            return minHeight;
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
                    else
                    {
                        if ((viewMode == MapViewModel.TreeMap || viewMode == MapViewModel.Structure) && n.childSubjects.Count > 0)
                        {
                            maxHeight = n.CenterPoint.Y + n.Height / 2 + n.branchLinkWidth + n.CollapseRadius;
                        }
                    }

                    if (n.CenterPoint.Y + n.Height / 2 > maxHeight)
                    {
                        maxHeight = n.CenterPoint.Y + n.Height / 2;
                    }
                }
            }
            else
            {
                if ((viewMode == MapViewModel.TreeMap || viewMode == MapViewModel.Structure) && subject.childSubjects.Count > 0)
                {
                    maxHeight = subject.CenterPoint.Y + subject.Height / 2 + subject.branchLinkWidth + subject.collapseRadius;
                }
            }

            if (subject.CenterPoint.Y + subject.Height / 2 > maxHeight)
            {
                maxHeight = subject.CenterPoint.Y + subject.Height / 2;
            }
            return maxHeight;
        }


        /// <summary>
        /// 获取主题显示总宽度（所有子主题）
        /// </summary>
        /// <returns></returns>
        public virtual int GetTotalWidth()
        {
            int hb = 0;
            if (this.childSubjects.Count < 1)
            {
                hb = this.width;
            }
            else
            {
                int min = GetSubjectMinWidth(this, this.centerPoint.X);
                int max = GetSubjectMaxWidth(this, 0);
                hb = max - min;
                if (hb < this.width)
                {
                    hb = this.width;
                }
            }
            return hb;
        }

        private int GetSubjectMinWidth(SubjectBase subject, int min)
        {
            int minWidth = min;
            if (subject.Expanded)
            {
                foreach (SubjectBase n in subject.ChildSubjects)
                {
                    if (n.Expanded)
                    {
                        minWidth = GetSubjectMinWidth(n, minWidth);
                    }

                    if (n.CenterPoint.X - n.Width / 2 < minWidth)
                    {
                        minWidth = n.CenterPoint.X - n.Width / 2;
                    }
                }
            }

            if (subject.CenterPoint.X - subject.Width / 2 < minWidth)
            {
                minWidth = subject.CenterPoint.X - subject.Width / 2;
            }
            return minWidth;
        }

        private int GetSubjectMaxWidth(SubjectBase subject, int max)
        {
            int maxWidth = max;
            if (subject.Expanded)
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


        /// <summary>
        /// 设置主题样式
        /// </summary>
        public void SetTitleStyle()
        {
            this.zoom = 1;
            int lv = this.ParentSubject != null ? this.ParentSubject.Level + 1 : 0;
            if (lv > 3) lv = 3;
            if (this.level == lv)
            {
                return;
            }
            this.level = lv;

            mindDocThem.SetThemeStyle(this);
            this.AdjustSubjectSize(false);
            this.ZoomValue();
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
            while (topSubject.parentSubject != null)
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
            set { level = value; }
        }

        public int Direction
        {
            get { return direction; }
            set { direction = value; }
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
        public MapLinkStyle LineStyle
        {
            get { return lineStyle; }
            set { lineStyle = value; }
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

        public int ImageSplitWidth
        {
            get { return imageSplitWidth; }
            set { imageSplitWidth = value; }
        }

        public bool EqualWidth
        {
            get { return equalWidth; }
            set { equalWidth = value; }
        }

        public int CollapseRadius
        {
            get { return collapseRadius; }
            set { collapseRadius = value; }
        }


        public bool Expanded
        {
            get { return expanded; }
            set { expanded = value; }
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
                if (parentSubject != null)
                {
                    if (parentSubject.childSubjects.Contains(this))
                    {
                        parentSubject.childSubjects.Remove(this);
                    }
                }

                parentSubject = value;
                if (parentSubject != null)
                    parentSubject.InsertSubject(this, parentSubject.childSubjects.Count);
            }
        }

        public SubjectBase RelateSubject
        {
            get { return relateSubject; }
            set { relateSubject = value; }
        }

        public SubjectBase LinkSubject
        {
            get { return linkSubject; }
            set { linkSubject = value; }
        }
        public List<SubjectBase> ChildSubjects
        {
            get { return childSubjects; }
            set { childSubjects = value; }
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
        public int MoveY
        {
            get { return moveY; }
            set { moveY = value; }
        }
        public bool DragOut
        {
            get { return dragOut; }
            set { dragOut = value; }
        }
        #endregion


    }

    /// <summary>
    /// 中心主题
    /// </summary>
    [Serializable]
    public class CenterSubject : SubjectBase
    {
        public CenterSubject()
        {
            mindDocThem.SetThemeStyle(this);
            this.AdjustSubjectSize(false);
        }

        public override int[] GetPlusVisible()
        {
            int[] plus = new int[4];

            plus[0] = 0;
            plus[1] = 0;
            plus[2] = 0;
            plus[3] = 0;
            if (viewMode == MapViewModel.ExpandTwoSides)
            {
                plus[0] = 1;
                plus[2] = 1;
            }
            else if (viewMode == MapViewModel.ExpandRightSide)
            {
                plus[2] = 1;
            }
            else if (viewMode == MapViewModel.Structure)
            {
                plus[3] = 1;
            }
            else if (viewMode == MapViewModel.TreeMap)
            {
                plus[3] = 1;
            }
            return plus;
        }
    }
    /// <summary>
    /// 主要主题
    /// </summary>
    [Serializable]
    public class TitleSubject : SubjectBase
    {
        public TitleSubject()
        {

        }
        /// <summary>
        /// 初始化主题
        /// </summary>
        /// <param name="subject">点击主题</param>
        /// <param name="plus">点击方向(1,2,3,4分别表示左，上，右，下)</param>
        public TitleSubject(SubjectBase subject, int plus)
        {
            if (subject.GetType() == typeof(CenterSubject))
            {
                if (viewMode == MapViewModel.ExpandTwoSides)
                {
                    this.Direction = plus == 1 ? -1 : 1;
                }
                this.ParentSubject = subject;
            }
            else if (subject.GetType() == typeof(TitleSubject))
            {
                //是否子主题
                bool bAddChild = true;
                int insertPos = 0; //插入位置

                if (viewMode == MapViewModel.Structure)
                {
                    bAddChild = plus == 2 || plus == 4; //上下添加子主题
                    insertPos = plus == 1 ? 0 : 1;
                }
                else
                {
                    bAddChild = plus == 1 || plus == 3; //左右添加子主题
                    insertPos = plus == 2 ? 0 : 1;

                }

                if (bAddChild)  //子主题
                {
                    this.ParentSubject = subject;
                }
                else  //兄弟主题
                {
                    SubjectBase brotherSubject = subject;
                    SubjectBase fatherSubject = brotherSubject.ParentSubject;
                    if (fatherSubject == null)
                    {
                        return;
                    }
                    if (fatherSubject.GetType() == typeof(CenterSubject))
                    {
                        this.Direction = brotherSubject.Direction;
                    }

                    int index = fatherSubject.ChildSubjects.IndexOf(brotherSubject);
                    fatherSubject.InsertSubject(this, index + insertPos);
                }
            }
        }

        /// <summary>
        /// 初始化主题
        /// </summary>
        /// <param name="brotherSubject">同层主题</param>
        /// <param name="insertUp">是否向上插入</param>
        public TitleSubject(SubjectBase brotherSubject, bool insertUp)
        {

        }
    }

    /// <summary>
    /// 附注
    /// </summary>
    [Serializable]
    public class AttachSubject : SubjectBase
    {
        public AttachSubject()
        {

        }
        public AttachSubject(SubjectBase subject)
        {
            this.Width = 63;
            this.Height = 30;
            this.MarginWidth = 10;
            this.MarginHeight = 5;
            this.BackColor = Color.LightYellow;
            this.ForeColor = Color.DimGray;
            this.BorderColor = Color.Orange;
            this.Font = new Font("微软雅黑", 10.0f);
            this.Title = "附注";
            //插入兄弟主题
            SubjectBase fatherSubject = subject.ParentSubject;
            if (fatherSubject != null)
            {
                this.relateSubject = subject;
                int index = fatherSubject.ChildSubjects.IndexOf(subject);
                fatherSubject.InsertSubject(this, index); //默认插入到关联主题的上面
            }

        }

        /// <summary>
        /// 获取主题显示总高度（所有子主题）
        /// </summary>
        /// <returns></returns>
        public override int GetTotalHeight()
        {
            int hb = base.GetTotalHeight();
            SubjectBase relateSubject = this.relateSubject;
            if (this.MoveX != -10000)
            {
                int hd = Math.Abs(this.CenterPoint.Y - relateSubject.CenterPoint.Y) - this.ParentSubject.BranchSplitHeight - 2;
                if (hb < hd)
                {
                    hb = hd;
                }

            }

            return hb;
        }
    }


    #endregion
    #region Theme

    //public enum MapTheme { Default, Classics, Bubble, Lemo, Black };

    public class ThemeFactory
    {
        public static MindDocTheme CreateTheme(MapTheme theme)
        {
            MindDocTheme mindDocTheme = null;
            switch (theme)
            {
                case MapTheme.Default:
                    mindDocTheme = new MindDocThemeDefault();
                    break;
                case MapTheme.Classics:
                    mindDocTheme = new MindDocThemeClassics();
                    break;
                case MapTheme.Bubble:
                    mindDocTheme = new MindDocThemeBubble();
                    break;
                case MapTheme.Lemo:
                    mindDocTheme = new MindDocThemeLemo();
                    break;
                case MapTheme.Black:
                    mindDocTheme = new MindDocThemeBlack();
                    break;
            }
            return mindDocTheme;
        }
    }

    public class MindDocTheme
    {
        /// <summary>
        /// 设置主题样式
        /// </summary>
        public void SetThemeStyle(SubjectBase subject)
        {
            if (subject.GetType() == typeof(CenterSubject))
            {
                SetCenterSubjectStyle(subject);
                return;
            }

            switch (subject.Level)
            {
                case 0:
                    SetTitleStyleLevel0(subject);
                    break;
                case 1:
                    SetTitleStyleLevel1(subject);
                    break;
                case 2:
                    SetTitleStyleLevel2(subject);
                    break;
                case 3:
                    SetTitleStyleLevel3(subject);
                    break;
            }
        }


        protected virtual void SetCenterSubjectStyle(SubjectBase subject)
        {
            subject.MarginWidth = 20;
            subject.MarginHeight = 10;

            subject.BackColor = Color.WhiteSmoke;
            subject.ForeColor = Color.DimGray;
            subject.BorderColor = Color.DimGray;
            subject.Font = new Font("微软雅黑", 14.0f);

            subject.BranchLinkWidth = 20;
            subject.BranchSplitHeight = 24;
            subject.EqualWidth = false;
            subject.LineStyle = MapLinkStyle.RoundLine;

            subject.Title = subject.Title == "主题" ? "中心主题" : subject.Title;
        }

        protected virtual void SetTitleStyleLevel0(SubjectBase subject)
        {
            subject.MarginWidth = 20;
            subject.MarginHeight = 10;

            subject.BackColor = Color.WhiteSmoke;
            subject.ForeColor = Color.DimGray;
            subject.BorderColor = Color.DimGray;
            subject.Font = new Font("微软雅黑", 12.0f);
            subject.LineStyle = MapLinkStyle.RoundLine;

            subject.Title = (subject.Title == "主题" || subject.Title == "子主题") ? "浮动主题" : subject.Title;
        }

        protected virtual void SetTitleStyleLevel1(SubjectBase subject)
        {
            subject.MarginWidth = 20;
            subject.MarginHeight = 6;

            subject.BackColor = Color.AliceBlue;
            subject.ForeColor = Color.DimGray;
            subject.BorderColor = Color.SteelBlue;
            subject.Font = new Font("微软雅黑", 12.0f);
            subject.LineStyle = MapLinkStyle.RoundLine;

            subject.Title = (subject.Title == "主题" || subject.Title == "子主题") ? "主题" : subject.Title;
        }

        protected virtual void SetTitleStyleLevel2(SubjectBase subject)
        {
            subject.MarginWidth = 15;
            subject.MarginHeight = 3;

            subject.BackColor = Color.Honeydew;
            subject.ForeColor = Color.DimGray;
            subject.BorderColor = Color.Green;
            subject.Font = new Font("微软雅黑", 10.0f);
            subject.LineStyle = MapLinkStyle.RoundLine;

            subject.Title = (subject.Title == "主题" || subject.Title == "子主题") ? "子主题" : subject.Title;
        }

        protected virtual void SetTitleStyleLevel3(SubjectBase subject)
        {
            subject.MarginWidth = 5;
            subject.MarginHeight = 2;

            subject.BackColor = Color.White;
            subject.ForeColor = Color.DimGray;
            subject.BorderColor = Color.Transparent;
            subject.Font = new Font("微软雅黑", 8.0f);
            subject.LineStyle = MapLinkStyle.RoundLine;

            subject.Title = (subject.Title == "主题" || subject.Title == "子主题") ? "子主题" : subject.Title;
        }


    }

    public class MindDocThemeDefault : MindDocTheme
    {
        public MindDocThemeDefault()
        {

        }


    }

    public class MindDocThemeClassics : MindDocTheme
    {
        public MindDocThemeClassics()
        {
        }
        protected override void SetCenterSubjectStyle(SubjectBase subject)
        {
            subject.MarginWidth = 20;
            subject.MarginHeight = 10;

            subject.BackColor = Color.LightSteelBlue;
            subject.ForeColor = Color.DimGray;
            subject.BorderColor = Color.LightGray;
            subject.Font = new Font("微软雅黑", 14.0f, FontStyle.Bold);

            subject.BranchLinkWidth = 20;
            subject.BranchSplitHeight = 24;

            subject.EqualWidth = false;
            subject.LineStyle = MapLinkStyle.Curve;

            subject.Title = subject.Title == "主题" ? "中心主题" : subject.Title;
        }

        protected override void SetTitleStyleLevel0(SubjectBase subject)
        {
            subject.MarginWidth = 20;
            subject.MarginHeight = 10;

            subject.BackColor = Color.WhiteSmoke;
            subject.ForeColor = Color.DimGray;
            subject.BorderColor = Color.DimGray;
            subject.Font = new Font("微软雅黑", 12.0f);
            subject.LineStyle = MapLinkStyle.Curve;

            subject.Title = (subject.Title == "主题" || subject.Title == "子主题") ? "浮动主题" : subject.Title;
        }

        protected override void SetTitleStyleLevel1(SubjectBase subject)
        {
            subject.MarginWidth = 20;
            subject.MarginHeight = 6;

            subject.BackColor = Color.FromArgb(65, Color.LightSteelBlue);
            subject.ForeColor = Color.DimGray;
            subject.BorderColor = Color.Gray;
            subject.Font = new Font("微软雅黑", 12.0f, FontStyle.Bold);
            subject.LineStyle = MapLinkStyle.Curve;

            subject.Title = (subject.Title == "主题" || subject.Title == "子主题") ? "主题" : subject.Title;
        }

        protected override void SetTitleStyleLevel2(SubjectBase subject)
        {
            subject.MarginWidth = 5;
            subject.MarginHeight = 3;

            subject.BackColor = Color.Transparent;
            subject.ForeColor = Color.DimGray;
            subject.BorderColor = Color.Transparent;
            subject.Font = new Font("微软雅黑", 10.0f);

            subject.LineStyle = MapLinkStyle.Curve;

            subject.Title = (subject.Title == "主题" || subject.Title == "子主题") ? "子主题" : subject.Title;
        }

        protected override void SetTitleStyleLevel3(SubjectBase subject)
        {
            subject.MarginWidth = 5;
            subject.MarginHeight = 2;

            subject.BackColor = Color.White;
            subject.ForeColor = Color.DimGray;
            subject.BorderColor = Color.Transparent;
            subject.Font = new Font("微软雅黑", 8.0f);

            subject.LineStyle = MapLinkStyle.Curve;

            subject.Title = (subject.Title == "主题" || subject.Title == "子主题") ? "子主题" : subject.Title;
        }
    }

    public class MindDocThemeBubble : MindDocTheme
    {
        public MindDocThemeBubble()
        {

        }
        protected override void SetCenterSubjectStyle(SubjectBase subject)
        {
            subject.MarginWidth = 20;
            subject.MarginHeight = 10;

            subject.BackColor = Color.DarkOrange;
            subject.ForeColor = Color.SaddleBrown;
            subject.BorderColor = Color.OrangeRed;
            subject.Font = new Font("微软雅黑", 14.0f, FontStyle.Bold);

            subject.BranchLinkWidth = 20;
            subject.BranchSplitHeight = 24;

            subject.EqualWidth = true;
            subject.LineStyle = MapLinkStyle.Curve;

            subject.Title = subject.Title == "主题" ? "中心主题" : subject.Title;
        }

        protected override void SetTitleStyleLevel0(SubjectBase subject)
        {
            subject.MarginWidth = 20;
            subject.MarginHeight = 10;

            subject.BackColor = Color.WhiteSmoke;
            subject.ForeColor = Color.DimGray;
            subject.BorderColor = Color.DimGray;
            subject.Font = new Font("微软雅黑", 12.0f);
            subject.LineStyle = MapLinkStyle.Curve;

            subject.Title = (subject.Title == "主题" || subject.Title == "子主题") ? "浮动主题" : subject.Title;
        }

        protected override void SetTitleStyleLevel1(SubjectBase subject)
        {
            subject.MarginWidth = 20;
            subject.MarginHeight = 6;

            subject.BackColor = Color.FromArgb(180, Color.DodgerBlue);
            subject.ForeColor = Color.MidnightBlue;
            subject.BorderColor = Color.Gray;
            subject.Font = new Font("微软雅黑", 12.0f, FontStyle.Bold);
            subject.LineStyle = MapLinkStyle.RectLine;

            subject.Title = (subject.Title == "主题" || subject.Title == "子主题") ? "主题" : subject.Title;
        }

        protected override void SetTitleStyleLevel2(SubjectBase subject)
        {
            subject.MarginWidth = 5;
            subject.MarginHeight = 3;

            subject.BackColor = Color.Transparent;
            subject.ForeColor = Color.DimGray;
            subject.BorderColor = Color.Transparent;
            subject.Font = new Font("微软雅黑", 10.0f);

            subject.LineStyle = MapLinkStyle.RectLine;

            subject.Title = (subject.Title == "主题" || subject.Title == "子主题") ? "子主题" : subject.Title;
        }

        protected override void SetTitleStyleLevel3(SubjectBase subject)
        {
            subject.MarginWidth = 5;
            subject.MarginHeight = 2;

            subject.BackColor = Color.Transparent;
            subject.ForeColor = Color.DimGray;
            subject.BorderColor = Color.Transparent;
            subject.Font = new Font("微软雅黑", 8.0f);

            subject.LineStyle = MapLinkStyle.RectLine;

            subject.Title = (subject.Title == "主题" || subject.Title == "子主题") ? "子主题" : subject.Title;
        }
    }

    public class MindDocThemeLemo : MindDocTheme
    {
        public MindDocThemeLemo()
        {

        }
        protected override void SetCenterSubjectStyle(SubjectBase subject)
        {
            subject.MarginWidth = 20;
            subject.MarginHeight = 10;

            subject.BackColor = Color.LimeGreen;
            subject.ForeColor = Color.WhiteSmoke;
            subject.BorderColor = Color.DarkGreen;
            subject.Font = new Font("微软雅黑", 14.0f, FontStyle.Bold);

            subject.BranchLinkWidth = 20;
            subject.BranchSplitHeight = 24;

            subject.EqualWidth = false;
            subject.LineStyle = MapLinkStyle.RectLine;

            subject.Title = subject.Title == "主题" ? "中心主题" : subject.Title;
        }

        protected override void SetTitleStyleLevel0(SubjectBase subject)
        {
            subject.MarginWidth = 20;
            subject.MarginHeight = 10;

            subject.BackColor = Color.FromArgb(102, 205, 0);
            subject.ForeColor = Color.DimGray;
            subject.BorderColor = Color.DimGray;
            subject.Font = new Font("微软雅黑", 12.0f);
            subject.LineStyle = MapLinkStyle.RectLine;

            subject.Title = (subject.Title == "主题" || subject.Title == "子主题") ? "浮动主题" : subject.Title;
        }

        protected override void SetTitleStyleLevel1(SubjectBase subject)
        {
            subject.MarginWidth = 20;
            subject.MarginHeight = 6;

            subject.BackColor = Color.YellowGreen;
            subject.ForeColor = Color.WhiteSmoke;
            subject.BorderColor = Color.Gray;
            subject.Font = new Font("微软雅黑", 12.0f, FontStyle.Bold);
            subject.LineStyle = MapLinkStyle.RectLine;

            subject.Title = (subject.Title == "主题" || subject.Title == "子主题") ? "主题" : subject.Title;
        }

        protected override void SetTitleStyleLevel2(SubjectBase subject)
        {
            subject.MarginWidth = 5;
            subject.MarginHeight = 3;

            subject.BackColor = Color.Transparent;
            subject.ForeColor = Color.DimGray;
            subject.BorderColor = Color.Transparent;
            subject.Font = new Font("微软雅黑", 10.0f);

            subject.LineStyle = MapLinkStyle.RectLine;

            subject.Title = (subject.Title == "主题" || subject.Title == "子主题") ? "子主题" : subject.Title;
        }

        protected override void SetTitleStyleLevel3(SubjectBase subject)
        {
            subject.MarginWidth = 5;
            subject.MarginHeight = 2;

            subject.BackColor = Color.Transparent;
            subject.ForeColor = Color.DimGray;
            subject.BorderColor = Color.Transparent;
            subject.Font = new Font("微软雅黑", 8.0f);

            subject.LineStyle = MapLinkStyle.RectLine;

            subject.Title = (subject.Title == "主题" || subject.Title == "子主题") ? "子主题" : subject.Title;
        }
    }

    public class MindDocThemeBlack : MindDocTheme
    {
        public MindDocThemeBlack()
        {

        }
        protected override void SetCenterSubjectStyle(SubjectBase subject)
        {
            subject.MarginWidth = 20;
            subject.MarginHeight = 10;

            subject.BackColor = Color.FromArgb(16,78,137);
            subject.ForeColor = Color.WhiteSmoke;
            subject.BorderColor = Color.LightSteelBlue;
            subject.Font = new Font("微软雅黑", 14.0f, FontStyle.Bold);

            subject.BranchLinkWidth = 20;
            subject.BranchSplitHeight = 24;

            subject.EqualWidth = false;
            subject.LineStyle = MapLinkStyle.Curve;

            subject.Title = subject.Title == "主题" ? "中心主题" : subject.Title;
        }

        protected override void SetTitleStyleLevel0(SubjectBase subject)
        {
            subject.MarginWidth = 20;
            subject.MarginHeight = 10;

            subject.BackColor = Color.WhiteSmoke;
            subject.ForeColor = Color.DimGray;
            subject.BorderColor = Color.DimGray;
            subject.Font = new Font("微软雅黑", 12.0f);
            subject.LineStyle = MapLinkStyle.Curve;

            subject.Title = (subject.Title == "主题" || subject.Title == "子主题") ? "浮动主题" : subject.Title;
        }

        protected override void SetTitleStyleLevel1(SubjectBase subject)
        {
            subject.MarginWidth = 20;
            subject.MarginHeight = 6;

            subject.BackColor = Color.FromArgb(200, Color.DimGray);
            subject.ForeColor = Color.WhiteSmoke;
            subject.BorderColor = Color.Gray;
            subject.Font = new Font("微软雅黑", 12.0f, FontStyle.Bold);
            subject.LineStyle = MapLinkStyle.RectLine;

            subject.Title = (subject.Title == "主题" || subject.Title == "子主题") ? "主题" : subject.Title;
        }

        protected override void SetTitleStyleLevel2(SubjectBase subject)
        {
            subject.MarginWidth = 5;
            subject.MarginHeight = 3;

            subject.BackColor = Color.Transparent;
            subject.ForeColor = Color.Gray;
            subject.BorderColor = Color.Transparent;
            subject.Font = new Font("微软雅黑", 10.0f);

            subject.LineStyle = MapLinkStyle.RectLine;

            subject.Title = (subject.Title == "主题" || subject.Title == "子主题") ? "子主题" : subject.Title;
        }

        protected override void SetTitleStyleLevel3(SubjectBase subject)
        {
            subject.MarginWidth = 5;
            subject.MarginHeight = 2;

            subject.BackColor = Color.Transparent;
            subject.ForeColor = Color.Gray;
            subject.BorderColor = Color.Transparent;
            subject.Font = new Font("微软雅黑", 8.0f);

            subject.LineStyle = MapLinkStyle.RectLine;

            subject.Title = (subject.Title == "主题" || subject.Title == "子主题") ? "子主题" : subject.Title;
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
        private List<SubjectBase> subjectNodes;

        private bool mouseActivate = false;
        private bool allCollapsed = false;

        //设置选中的节点; 
        private SubjectBase selectedSubject = null;
        private SubjectBase activeSubject = null;
        private SubjectBase collapseSubject = null;
        private SubjectBase linkSubject = null;
        private SubjectBase centerProject = null;
        private SubjectBase dragSubject = null;

        private int dragStartPx = 0;
        private int dragStartPy = 0;

        private int selectPlus = 0;  //1,2,3,4分别表示Lef,Top,Right,Bottom,0表示未选中

        private bool openLinkSubject = false;

        TextBox txtNode = null;
        Image imgRender = null;
        System.Windows.Forms.Timer timerTxt;
        private MapTheme theme = MapTheme.Default;
        Dictionary<string, Image> backGroundImg = new Dictionary<string, Image>();
        #endregion

        #region Constructor
        public MindDocView()
            : base()
        {
            subjectNodes = new List<SubjectBase>();

            centerProject = new CenterSubject();
            centerProject.CenterPoint = new Point(this.Width / 2, this.Height / 2);
            subjectNodes.Add(centerProject);

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

            // Use reflection to load the
            // embedded bitmaps for the
            // styles plus and minus icons
            //Assembly myAssembly = Assembly.GetAssembly(Type.GetType("JsmMind.MindDocView")); 
            //System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MindDocView));
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

        [Browsable(false)]
        public override ContainerListViewItemCollection Items
        {
            get { return items; }
        }

        [
        Category("Behavior"),
        Description("The theme of MapView."),
        DefaultValue(MapTheme.Default)
        ]
        public MapTheme Theme
        {
            get { return theme; }
            set
            {
                theme = value;
                SubjectBase.mindDocThem = ThemeFactory.CreateTheme(theme);

                centerProject.ResortChildSubject();
            }
        }
        public Dictionary<string, Image> BackgroundImg
        {
            get { return backGroundImg; }
            set { backGroundImg = value; }
        }


        //[
        //Category("Behavior"),
        //Description("The indentation of child nodes in pixels."),
        //DefaultValue(19)
        //]
        //public int Indent
        //{
        //    get { return indent; }
        //    set { indent = value; }
        //}     

        public SubjectBase SelectedSubject
        {
            get { return selectedSubject; }
            set { selectedSubject = value; }
        }
        public MapViewModel ViewModel
        {
            get { return SubjectBase.viewMode; }
            set
            {
                SubjectBase.viewMode = value;

                centerProject.ResortChildSubject();
            }
        }
        #endregion

        #region Method


        public void AddAttachNote(SubjectBase subject)
        {
            SubjectBase attachSubject = new AttachSubject(subject);
            this.subjectNodes.Add(attachSubject);

        }

        public void RelateSubjectLink()
        {
            openLinkSubject = true;
            linkSubject = null;
        }

        public void ZoomMap(float zoom)
        {
            SubjectBase.zoomValue = zoom;
            foreach (SubjectBase subject in subjectNodes)
            {
                subject.ZoomValue();
            }
            Invalidate();
        }

        public void AdjustMapCenter()
        {
            int h = this.centerProject.GetTotalHeight();
            int w = this.centerProject.GetTotalWidth();

            int x = this.Width / 2 - w / 2;
            int y = this.Height / 2 - h / 2;

            if (x < centerProject.Width) x = centerProject.Width;

            if (y < centerProject.Height) y = centerProject.Height;

            this.centerProject.MoveSubjectPosition(new Point(x, y));
            Invalidate();
        }

        public void SaveAs(string fileName)
        {
            string xml = GetXml();
            byte[] zipBuffer = System.Text.Encoding.UTF8.GetBytes(xml);
            FileStream fs = new FileStream(fileName, FileMode.Create, FileAccess.Write);
            fs.Write(zipBuffer, 0, zipBuffer.Length);
            fs.Close();
        }

        public void ReadAs(string fileName)
        {
            FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);

            byte[] bBuffer = new byte[fs.Length];
            fs.Read(bBuffer, 0, (int)fs.Length);
            string xml = System.Text.Encoding.UTF8.GetString(bBuffer);
            fs.Close();
            SetXml(xml);
        }
        public string GetXml()
        {
            string xml = XmlManage.SaveSubject(centerProject, SubjectBase.FloatSubjects, theme, ViewModel);
            return xml;
        }
        public void SetXml(string xml)
        {
            ClearSubject();
            List<SubjectBase> floatSubjects = new List<SubjectBase>();
            MapTheme mapTheme = MapTheme.Default;
            MapViewModel mapViewModel = MapViewModel.ExpandRightSide;
            SubjectBase subject = XmlManage.ReadSubject(xml, floatSubjects, out mapTheme, out mapViewModel);
            centerProject = subject;
            SubjectBase.FloatSubjects = floatSubjects;
            Theme = mapTheme;
            ViewModel = mapViewModel;
            subjectNodes.Clear();
            AddSubjectNode(centerProject);
            foreach (SubjectBase floatSubject in SubjectBase.FloatSubjects)
            {
                AddSubjectNode(floatSubject);
            }
            Invalidate();
        }
        private void AddSubjectNode(SubjectBase subject)
        {
            subjectNodes.Add(subject);
            foreach(SubjectBase childSubject in subject.ChildSubjects)
            {
                AddSubjectNode(childSubject);
            }
        }

        public void ClearSubject()
        {
            subjectNodes.Clear();
            SubjectBase.FloatSubjects.Clear();

            centerProject = new CenterSubject();
            centerProject.CenterPoint = new Point(this.Width / 2, this.Height / 2);
            subjectNodes.Add(centerProject);
            Invalidate();
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

            if (centerProject != null && subjectNodes.Count < 2)
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
                    //主题选中状态
                    if (selectedSubject != null)
                    {
                        //是否点击扩展按钮
                        int plus = PlusInArea(e, selectedSubject);
                        if (plus > 0)
                        {
                            SubjectBase subject = null;
                            subject = new TitleSubject(selectedSubject, plus);
                            if (subject != null)
                            {
                                subjectNodes.Add(subject);
                                selectedSubject = subject;
                            }
                            Invalidate();
                            return;
                        }

                        if (e.Clicks == 2)
                        {
                            //是否点击标题
                            if (TitleInArea(e, selectedSubject))
                            {
                                txtNode.Tag = selectedSubject;
                                ShowTxtBox();
                                return;
                            }
                        }
                    }

                    //是否点击折叠按钮
                    SubjectBase collaspSubject = CollaspeInArea(e);
                    if (collapseSubject != null)
                    {
                        collapseSubject.CollapseChildSubject(!collapseSubject.Expanded);
                        Invalidate();
                        return;
                    }

                    //是否选中主题
                    selectedSubject = null;
                    SubjectBase subjectNode = SubjectInArea(e);
                    if (subjectNode != null)
                    {
                        selectedSubject = subjectNode;
                        if (SubjectClick != null)
                            SubjectClick(this, subjectNode);


                        if (linkSubject != null)
                        {
                            subjectNode.LinkSubject = linkSubject;
                            linkSubject = null;
                        }

                        if (openLinkSubject)
                        {
                            linkSubject = subjectNode;
                            openLinkSubject = false;
                        }



                        //拖拽开始
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
                //处理拖拽主题
                if (dragSubject != null)
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
                else //处理鼠标移动活动主题,扩展按钮和折叠按钮
                {
                    SubjectBase subjectNode = SubjectInArea(e);
                    if (subjectNode != null)  //鼠标移动在主题上 
                    {
                        activeSubject = subjectNode;
                    }
                    else if (CollaspeInArea(e) != null)//鼠标移动在折叠按钮上 
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
                        //主题选中状态
                        int plus = PlusInArea(e, selectedSubject); //鼠标移动在扩展按钮上
                        selectPlus = plus;

                        if (TitleInArea(e, selectedSubject))  //鼠标是否移动到标题上
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

            if (dragSubject != null)
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
                        while (subjectTemp != null)
                        {
                            if (subjectTemp == nowSubject)
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
                        if (nowSubject.ParentSubject == subjectNode)
                        {
                            isDragParent = true;
                        }
                        if (isDragChild == false && isDragSelf == false && isDragParent == false)
                        {
                            if (SubjectBase.FloatSubjects.Contains(nowSubject))
                            {
                                SubjectBase.FloatSubjects.Remove(nowSubject);
                            }
                            nowSubject.MoveX = -10000;
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

            if (selectedSubject != null)
            {
                switch (e.KeyCode)
                {
                    case Keys.Delete:
                        if (selectedSubject.GetType() != typeof(CenterSubject))
                        {
                            SubjectBase subject = selectedSubject;
                            if (SubjectBase.FloatSubjects.Contains(subject))  //浮动主题
                            {
                                SubjectBase.FloatSubjects.Remove(subject);
                            }
                            else  //主题
                            {
                                SubjectBase parentSubject = subject.ParentSubject;

                                if (parentSubject != null)
                                {
                                    parentSubject.RemoveSubject(subject);
                                }
                            }

                            if (this.subjectNodes.Contains(subject))
                            {
                                this.subjectNodes.Remove(subject);
                            }
                        }
                        break;
                    case Keys.Left:
                        if (selectedSubject.ParentSubject != null)
                        {
                            selectedSubject = selectedSubject.ParentSubject;
                        }
                        break;
                    case Keys.Right:
                        if (selectedSubject.ChildSubjects.Count > 0)
                        {
                            selectedSubject = selectedSubject.ChildSubjects[0];
                        }
                        break;
                    case Keys.Up:
                        if (selectedSubject.ParentSubject != null)
                        {
                            int index = selectedSubject.ParentSubject.ChildSubjects.IndexOf(selectedSubject);
                            if (index > 0)
                            {
                                selectedSubject = selectedSubject.ParentSubject.ChildSubjects[index - 1];
                            }
                        }
                        break;
                    case Keys.Down:
                        if (selectedSubject.ParentSubject != null)
                        {
                            int index = selectedSubject.ParentSubject.ChildSubjects.IndexOf(selectedSubject);
                            if (index < selectedSubject.ParentSubject.ChildSubjects.Count - 1)
                            {
                                selectedSubject = selectedSubject.ParentSubject.ChildSubjects[index + 1];
                            }
                        }
                        break;
                }
                Invalidate();
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
            AdjustScrollbars();
            RenderSubject(centerProject, g, r, 0);
            for (i = 0; i < SubjectBase.FloatSubjects.Count; i++)
            {
                RenderSubject(SubjectBase.FloatSubjects[i], g, r, 0);
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
            g.FillRectangle(new SolidBrush(BackColor), r);
            if (theme == MapTheme.Bubble || theme == MapTheme.Black)
            { 
                if (backGroundImg.ContainsKey(theme.ToString()))
                {
                    Image img = backGroundImg[theme.ToString()];
                    int w = img.Width;
                    int h = img.Height;
                    for (int i = 0; i < this.Width; i += w)
                    {
                        for (int j = 0; j < this.Height; j += h)
                        {
                            g.DrawImage(backGroundImg[theme.ToString()], i, j);
                        }
                    }

                }
            }
        }


        private void RenderSubject(SubjectBase subjectNode, Graphics g, Rectangle r, int level)
        {
            int lb = 0;
            int hb = headerBuffer;
            int left = r.Left + subjectNode.CenterPoint.X - subjectNode.Width / 2 + lb - hscrollBar.Value;
            int top = r.Left + subjectNode.CenterPoint.Y - subjectNode.Height / 2 + hb - vscrollBar.Value;
            string title = subjectNode.Title;
            Rectangle sr = new Rectangle(left, top, subjectNode.Width, subjectNode.Height);
            //绘制主题绘制区域
            subjectNode.RectAreas = sr;

            //绘制所有子主题 
            if (subjectNode.Expanded)
            {
                foreach (SubjectBase childSubject in subjectNode.ChildSubjects)
                {
                    RenderSubject(childSubject, g, r, level + 1);
                }
            }

            //开始绘制
            g.SmoothingMode = SmoothingMode.AntiAlias;

            if(subjectNode.Title=="备份")
            {

            }
            //1.绘制连接线 
            if (subjectNode.ParentSubject != null)
            {
                RenderSubjectParentLink(g, r, subjectNode);
            }
            if (subjectNode.LinkSubject != null)
            {
                RenderSubjectRelateLink(g, r, subjectNode);
            }
            //2.先绘制主题的分支连接线和折叠按钮，再绘制主题内容显示最前  
            RenderSubjectBranchLink(g, r, subjectNode);

            //3.绘制主题内容  
            RenderSubjectTitle(g, r, subjectNode);

            g.SmoothingMode = SmoothingMode.Default;
        }

        private void RenderDragSubject(SubjectBase subjectNode, Graphics g, Rectangle r, int level)
        {
            SubjectBase nowSubject = selectedSubject;
            if (nowSubject == null || subjectNode == null) return;
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


            if (nowSubject.GetType() == typeof(TitleSubject) && nowSubject.ParentSubject != null &&
                (Math.Abs(subjectNode.CenterPoint.X - nowSubject.CenterPoint.X) > 400 || Math.Abs(subjectNode.CenterPoint.Y - nowSubject.ParentSubject.CenterPoint.Y) > 300))
            {
                //移除主题
                nowSubject.DragOut = true;
            }
            else
            {

                //绘制连接线 
                if (subjectNode.ParentSubject != null)
                {
                    RenderSubjectParentLink(g, r, subjectNode);
                }

                nowSubject.DragOut = false;
            }

            //绘制主题内容  
            RenderSubjectTitle(g, r, subjectNode);

            g.SmoothingMode = SmoothingMode.Default;
        }

        private void RenderSubjectParentLink(Graphics g, Rectangle r, SubjectBase subject)
        {
            if (subject.ParentSubject == null) return;
            g.Clip = new System.Drawing.Region(r);

            if (subject.GetType() == typeof(AttachSubject))
            {
                RenderLinkAttach(g, r, subject);
            }
            else
            {
                RenderLinkBranch(g, r, subject);
            }
        }

        private void RenderSubjectRelateLink(Graphics g, Rectangle r, SubjectBase subject)
        {
            if (subject.LinkSubject == null) return;
            g.Clip = new System.Drawing.Region(r);

            RenderLinkRelate(g, r, subject);
        }

        /// <summary>
        /// 绘制分支线和折叠按钮
        /// </summary>
        private void RenderSubjectBranchLink(Graphics g, Rectangle r, SubjectBase subject)
        {
            if (subject.ChildSubjects.Count == 0) return;

            if (theme != MapTheme.Default && subject.GetType() == typeof(CenterSubject)) return;

            g.Clip = new System.Drawing.Region(r);


            //先绘制主题的分支连接线和折叠按钮，再绘制主题内容显示最前  
            int x1 = 0;
            int y1 = 0;
            int x2 = 0;
            int y2 = 0;
            if (ViewModel == MapViewModel.ExpandTwoSides)
            {
                bool bDrawRight = false;
                bool bDrawLeft = false;
                if (subject.GetType() == typeof(CenterSubject))
                {
                    foreach (SubjectBase childSubject in subject.ChildSubjects)
                    {
                        if (childSubject.Direction == 1)
                        {
                            bDrawRight = true;
                        }
                        if (childSubject.Direction == -1)
                        {
                            bDrawLeft = true;
                        }
                        if (bDrawRight && bDrawLeft) break;
                    }
                }
                else
                {
                    bDrawRight = subject.Direction == 1;
                    bDrawLeft = subject.Direction == -1;
                }

                if (bDrawRight)
                {
                    x1 = subject.RectAreas.Left + subject.RectAreas.Width;
                    y1 = subject.RectAreas.Top + subject.RectAreas.Height / 2;

                    x2 = x1 + subject.BranchLinkWidth;
                    y2 = y1;

                    RendrBranchLink(g, r, subject, x1, y1, x2, y2);
                }
                if (bDrawLeft)
                {
                    x1 = subject.RectAreas.Left;
                    y1 = subject.RectAreas.Top + subject.RectAreas.Height / 2;

                    x2 = x1 - subject.BranchLinkWidth;
                    y2 = y1;

                    RendrBranchLink(g, r, subject, x1, y1, x2, y2);
                }
                return;
            }

            if (ViewModel == MapViewModel.ExpandRightSide)
            {
                x1 = subject.RectAreas.Left + subject.RectAreas.Width;
                y1 = subject.RectAreas.Top + subject.RectAreas.Height / 2;

                x2 = x1 + subject.BranchLinkWidth;
                y2 = y1;
            }
            else if (ViewModel == MapViewModel.TreeMap)
            {
                x1 = subject.RectAreas.Left + subject.BranchLinkWidth / 2;
                y1 = subject.RectAreas.Top + subject.RectAreas.Height;

                x2 = x1;
                y2 = y1 + subject.BranchLinkWidth;
            }
            else if (ViewModel == MapViewModel.Structure)
            {
                if (subject.Level == 0)
                {
                    x1 = subject.RectAreas.Left + subject.Width / 2;
                    y1 = subject.RectAreas.Top + subject.RectAreas.Height;

                    x2 = x1;
                    y2 = y1 + subject.BranchLinkWidth;
                }
                else
                {
                    x1 = subject.RectAreas.Left + subject.BranchLinkWidth / 2;
                    y1 = subject.RectAreas.Top + subject.RectAreas.Height;

                    x2 = x1;
                    y2 = y1 + subject.BranchLinkWidth;
                }
            }
            RendrBranchLink(g, r, subject, x1, y1, x2, y2);
        }

        private void RendrBranchLink(Graphics g, Rectangle r, SubjectBase subject, int x1, int y1, int x2, int y2)
        {
            Color penColor = subject.BorderColor.A==0 ? subject.ForeColor : subject.BorderColor;
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

            int collaspradius = subject.CollapseRadius;
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
            g.Clip = new Region(new Rectangle(sr.Left - (int)(10 * SubjectBase.zoomValue), sr.Top - (int)(10 * SubjectBase.zoomValue), sr.Width + (int)(20 * SubjectBase.zoomValue) + subject.BranchLinkWidth, sr.Height + (int)(20 * SubjectBase.zoomValue)));

            int radius = subject.Height / 6;
            if (selectedSubject == subject)  //绘制选择边框和扩展
            {
                Rectangle activeArea = new Rectangle(sr.Left - (int)(3 * SubjectBase.zoomValue), sr.Top - (int)(6 * SubjectBase.zoomValue), sr.Width + (int)(7 * SubjectBase.zoomValue), sr.Height + (int)(10 * SubjectBase.zoomValue));

                //绘制扩展
                RenderPlus(g, activeArea, subject);

                //绘制边框和背景   
                FillRoundRectangle(g, new SolidBrush(subject.SelectColor), activeArea, radius);
                g.FillRectangle(Brushes.White, sr.Left - 1, sr.Top - 1, sr.Width + 2, sr.Height + 2);

            }
            else if (activeSubject == subject) //绘制选中边框
            {
                Rectangle activeArea = new Rectangle(sr.Left - (int)(3 * SubjectBase.zoomValue), sr.Top - (int)(6 * SubjectBase.zoomValue), sr.Width + (int)(7 * SubjectBase.zoomValue), sr.Height + (int)(10 * SubjectBase.zoomValue));

                FillRoundRectangle(g, new SolidBrush(subject.ActiveColor), activeArea, radius);
                g.FillRectangle(Brushes.White, sr.Left - 1, sr.Top - 1, sr.Width + 2, sr.Height + 2);
            }

            if (subject.GetType() == typeof(AttachSubject))
            {
                FillAttachRectangle(g, new SolidBrush(subject.BackColor), sr, radius);
                DrawAttachRectangle(g, p, sr, radius);
            }
            else
            {
                if (theme == MapTheme.Bubble && subject.GetType() == typeof(CenterSubject))
                {
                    g.FillEllipse(new SolidBrush(subject.BackColor), sr);
                    g.DrawEllipse(p, sr);
                }
                else
                {
                    FillRoundRectangle(g, new SolidBrush(subject.BackColor), sr, radius);
                    DrawRoundRectangle(g, p, sr, radius);
                }
            }

            //绘制图像
            int lb = subject.MarginWidth;
            for (int i = 0; i < subject.SubjectPictures.Count; i++)
            {
                Image img = subject.SubjectPictures[i].SubjectImage;

                g.DrawImage(img, (float)(sr.Left + lb), (float)(sr.Top + sr.Height / 2 - img.Height / 2 + 1));
                lb += (img.Width + subject.ImageSplitWidth);
            }

            SizeF size = g.MeasureString(title, f);
            //绘制文本
            float x_title = (float)(sr.Left + sr.Width / 2 - size.Width / 2); //居中绘制
            float y_title = (float)(sr.Top + sr.Height / 2 - Math.Floor(size.Height) / 2 + 1);
            if (lb != subject.MarginWidth)
            {
                x_title = (float)(sr.Left + lb);//在图标后绘制
            }

            g.DrawString(title, f, new SolidBrush(subject.ForeColor), x_title, y_title);
            Rectangle srTitle = new Rectangle((int)x_title, (int)y_title, (int)size.Width, (int)size.Height);
            subject.RectTitle = srTitle;
        }

        private void RenderLinkBranch(Graphics g, Rectangle r, SubjectBase subject)
        {
            if(subject.BorderColor.A==0)
            {

            }
            Pen penLine = new Pen(subject.BorderColor.A==0 ? subject.ForeColor : subject.BorderColor, 1.0f);
            SubjectBase parentSubject = subject.ParentSubject;
            int branchLinkWidth = parentSubject.BranchLinkWidth;


            //父主题分支连接点
            int x1 = 0;
            int y1 = 0;
            //子主体连接线起始点
            int x2 = 0;
            int y2 = 0;

            if (ViewModel == MapViewModel.ExpandTwoSides)
            {
                if (subject.Direction == 1)
                {
                    //父主题分支连接点
                    x1 = parentSubject.RectAreas.Left + parentSubject.RectAreas.Width + branchLinkWidth;
                    y1 = parentSubject.RectAreas.Top + parentSubject.RectAreas.Height / 2;
                    //子主体连接线起始点
                    x2 = subject.RectAreas.Left;
                    y2 = subject.RectAreas.Top + subject.RectAreas.Height / 2;
                }
                else
                {
                    //父主题分支连接点
                    x1 = parentSubject.RectAreas.Left - branchLinkWidth;
                    y1 = parentSubject.RectAreas.Top + parentSubject.RectAreas.Height / 2;
                    //子主体连接线起始点
                    x2 = subject.RectAreas.Left + subject.Width;
                    y2 = subject.RectAreas.Top + subject.RectAreas.Height / 2;

                }

            }
            if (ViewModel == MapViewModel.ExpandRightSide)
            {
                //父主题分支连接点
                x1 = parentSubject.RectAreas.Left + parentSubject.RectAreas.Width + branchLinkWidth;
                y1 = parentSubject.RectAreas.Top + parentSubject.RectAreas.Height / 2;
                //子主体连接线起始点
                x2 = subject.RectAreas.Left;
                y2 = subject.RectAreas.Top + subject.RectAreas.Height / 2;
            }
            else if (ViewModel == MapViewModel.TreeMap)
            {
                //父主题分支连接点
                x1 = parentSubject.RectAreas.Left + branchLinkWidth / 2;
                y1 = parentSubject.RectAreas.Top + parentSubject.RectAreas.Height + branchLinkWidth;
                //子主体连接线起始点
                x2 = subject.RectAreas.Left;
                y2 = subject.RectAreas.Top + subject.RectAreas.Height / 2;
            }
            else if (ViewModel == MapViewModel.Structure)
            {
                if (parentSubject.Level == 0)
                {
                    //父主题分支连接点
                    x1 = parentSubject.RectAreas.Left + parentSubject.Width / 2;
                    y1 = parentSubject.RectAreas.Top + parentSubject.RectAreas.Height + branchLinkWidth;
                    //子主体连接线起始点
                    x2 = subject.RectAreas.Left + subject.Width / 2;
                    y2 = subject.RectAreas.Top;

                }
                else
                {
                    //父主题分支连接点
                    x1 = parentSubject.RectAreas.Left + branchLinkWidth / 2;
                    y1 = parentSubject.RectAreas.Top + parentSubject.RectAreas.Height + branchLinkWidth;
                    //子主体连接线起始点
                    x2 = subject.RectAreas.Left;
                    y2 = subject.RectAreas.Top + subject.RectAreas.Height / 2;

                }
            }
            //应用主题样式
            if (theme != MapTheme.Default && parentSubject.GetType() == typeof(CenterSubject))
            {
                //父主题分支连接点
                x1 = parentSubject.RectAreas.Left + parentSubject.RectAreas.Width / 2;
                y1 = parentSubject.RectAreas.Top + parentSubject.RectAreas.Height / 2;
            }

            if (x1 == x2 || y1 == y2)
            {
                g.DrawLine(penLine, x1, y1, x2, y2);
                return;
            }
            Rectangle rect = new Rectangle(x1, y1, x2 - x1, y2 - y1);
            //直线绘制方向(0,1,2,3分别表示以矩形左上角开始从哪一个点绘制
            int direction = 0;
            if (x1 < x2)
            {
                if (y1 < y2)
                {
                    rect = new Rectangle(x1, y1, x2 - x1, y2 - y1);
                    direction = 2;
                }
                else
                {
                    rect = new Rectangle(x1, y2, x2 - x1, -(y2 - y1));
                    direction = 3;
                }
            }
            else
            {
                if (y1 < y2)
                {
                    rect = new Rectangle(x2, y1, -(x2 - x1), y2 - y1);
                    direction = 1;
                }
                else
                {
                    rect = new Rectangle(x2, y2, -(x2 - x1), -(y2 - y1));
                    direction = 0;
                }

            }

            if (ViewModel == MapViewModel.ExpandTwoSides)
            {
            }
            else if (ViewModel == MapViewModel.ExpandRightSide)
            {
            }
            else if (ViewModel == MapViewModel.TreeMap)
            {

            }
            else if (ViewModel == MapViewModel.Structure)
            {
                if (parentSubject.Level == 0)
                {
                    if (direction == 1)
                    {
                        direction = 3;
                    }
                    else if (direction == 2)
                    {
                        direction = 0;
                    }
                }
            }


            if (parentSubject.LineStyle == MapLinkStyle.RoundLine)
            {
                int roration = rect.Height / 4;
                if (roration > 16) roration = 16;
                if (roration < 4) roration = 4;
                DrawRoundLine(g, penLine, rect, roration, direction);
            }
            else if (parentSubject.LineStyle == MapLinkStyle.RectLine)
            {
                DrawRectLine(g, penLine, rect, direction);
            }
            else if (parentSubject.LineStyle == MapLinkStyle.Curve)
            {
                DrawCurveLine(g, penLine, rect, 3, direction);
            }

        }
        private void RenderLinkRelate(Graphics g, Rectangle r, SubjectBase subject)
        {
            Pen penLine = new Pen(Color.Goldenrod, 1.0f);
            penLine.DashStyle = DashStyle.Dash;
            SubjectBase parentSubject = subject.LinkSubject;
            int branchLinkWidth = parentSubject.BranchLinkWidth;

            //父主题分支连接点
            int x1 = parentSubject.CenterPoint.X;
            int y1 = parentSubject.CenterPoint.Y;
            //子主体连接线起始点
            int x2 = subject.CenterPoint.X;
            int y2 = subject.CenterPoint.Y;

            if (x1 == x2 || y1 == y2)
            {
                g.DrawLine(penLine, x1, y1, x2, y2);
                return;
            }


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
                    xa = parentSubject.CenterPoint.X + parentSubject.RectAreas.Width / 2;
                    ya = parentSubject.CenterPoint.Y;

                    xb = subject.CenterPoint.X;
                    yb = subject.CenterPoint.Y - subject.Height / 2;
                }
                else
                {
                    xa = parentSubject.CenterPoint.X + parentSubject.RectAreas.Width / 2;
                    ya = parentSubject.CenterPoint.Y;

                    xb = subject.CenterPoint.X;
                    yb = subject.CenterPoint.Y + subject.Height / 2;
                }
            }
            else
            {
                if (y1 < y2)
                {
                    xb = parentSubject.CenterPoint.X + parentSubject.RectAreas.Width / 2;
                    yb = parentSubject.CenterPoint.Y;

                    xa = subject.CenterPoint.X;
                    ya = subject.CenterPoint.Y - subject.Height / 2;
                }
                else
                {
                    xb = parentSubject.CenterPoint.X + parentSubject.RectAreas.Width / 2;
                    yb = parentSubject.CenterPoint.Y;

                    xa = subject.CenterPoint.X;
                    ya = subject.CenterPoint.Y + subject.Height / 2;
                }
            }

            xc = xa + (xb - xa) / 2;
            yc = ya + (yb - ya) / 4;

            List<Point> points = new List<Point>();
            points.Add(new Point(xa, ya));
            points.Add(new Point(xc, yc));
            //points.Add(new Point(xd, yd));
            points.Add(new Point(xb, yb));

            List<Point> curvePoints = JsmMind.Helpers.CurveTools.ParaCurveFitting(points);

            g.DrawLines(penLine, curvePoints.ToArray());
        }
        private void RenderLinkAttach(Graphics g, Rectangle r, SubjectBase subject)
        {
            Pen penLine = new Pen(subject.BorderColor.A==0? subject.ForeColor : subject.BorderColor, 1.0f);
            SubjectBase relateSubject = subject.RelateSubject;
            //管理主题中心点
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
                    ya = y1 + relateSubject.Height / 2 + 2;

                    xb = x2 - 5;
                    yb = y2 - subject.Height / 2;

                    xc = xb + 10;
                    yc = yb;
                }
                else
                {
                    xa = x1;
                    ya = y1 - relateSubject.Height / 2;

                    xb = x2 - 5;
                    yb = y2 + subject.Height / 2;

                    xc = xb + 10;
                    yc = yb;
                }
            }
            else
            {
                if (y1 < y2)
                {
                    xa = x1;
                    ya = y1 + relateSubject.Height / 2 + 2;

                    xb = x2 + 5;
                    yb = y2 - subject.Height / 2;


                    xc = xb - 10;
                    yc = yb;
                }
                else
                {
                    xa = x1;
                    ya = y1 - relateSubject.Height / 2;

                    xb = x2 + 5;
                    yb = y2 + subject.Height / 2;

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
            Rectangle sr = new Rectangle(area.Left - (int)(10 * SubjectBase.zoomValue), area.Top - (int)(10 * SubjectBase.zoomValue), area.Width + (int)(20 * SubjectBase.zoomValue), area.Height + (int)(20 * SubjectBase.zoomValue));
            g.Clip = new Region(sr);

            Pen penPlus = new Pen(new SolidBrush(Color.White), 2.0f);
            int[] plus = subject.GetPlusVisible();

            if (plus[0] == 1)
            {
                //绘制左边Plus
                Rectangle pmLeft = new Rectangle(area.Left - (int)(8 * SubjectBase.zoomValue), area.Top + area.Height / 2 - (int)(10 * SubjectBase.zoomValue), (int)(40 * SubjectBase.zoomValue), (int)(20 * SubjectBase.zoomValue));
                subject.RectPlusLeft = new Rectangle(pmLeft.Left, pmLeft.Top, (int)(10 * SubjectBase.zoomValue), pmLeft.Height);
                g.Clip = new Region(subject.RectPlusLeft);  //防止超出边界  
                RenderPlusLeft(g, pmLeft, penPlus, selectPlus == 1 ? Color.LightSteelBlue : Color.SteelBlue);
            }
            if (plus[2] == 1)
            {
                //绘制右边边Plus
                Rectangle pmRight = new Rectangle(area.Left + area.Width - (int)(((40 - 8) + 1) * SubjectBase.zoomValue), area.Top + area.Height / 2 - (int)(10 * SubjectBase.zoomValue), (int)(40 * SubjectBase.zoomValue), (int)(20 * SubjectBase.zoomValue));
                subject.RectPlusRight = new Rectangle(pmRight.Right - (int)(10 * SubjectBase.zoomValue), pmRight.Top, (int)(10 * SubjectBase.zoomValue), pmRight.Height);
                g.Clip = new Region(subject.RectPlusRight);  //防止超出边界           
                RenderPlusRight(g, pmRight, penPlus, selectPlus == 3 ? Color.LightSteelBlue : Color.SteelBlue);
            }
            if (plus[1] == 1)
            {
                //绘制上边Plus

                Rectangle pmTop = new Rectangle(area.Left + area.Width / 2 - (int)(10 * SubjectBase.zoomValue), area.Top - (int)(8 * SubjectBase.zoomValue), (int)(20 * SubjectBase.zoomValue), (int)(40 * SubjectBase.zoomValue));
                subject.RectPlusTop = new Rectangle(pmTop.Left, pmTop.Top, pmTop.Width, (int)(7 * SubjectBase.zoomValue));

                g.Clip = new Region(subject.RectPlusTop);  //防止超出边界
                RenderPlusTop(g, pmTop, penPlus, selectPlus == 2 ? Color.LightSteelBlue : Color.SteelBlue);
            }
            if (plus[3] == 1)
            {
                //绘制下边Plus
                Rectangle pmBottom = new Rectangle(area.Left + area.Width / 2 - (int)(10 * SubjectBase.zoomValue), area.Top + area.Height - (int)(((40 - 8) + 1) * SubjectBase.zoomValue), (int)(20 * SubjectBase.zoomValue), (int)(40 * SubjectBase.zoomValue));
                subject.RectPlusBottom = new Rectangle(pmBottom.Left, pmBottom.Bottom - (int)(7 * SubjectBase.zoomValue), pmBottom.Width, (int)(7 * SubjectBase.zoomValue));

                g.Clip = new Region(subject.RectPlusBottom);  //防止超出边界
                RenderPlusBottom(g, pmBottom, penPlus, selectPlus == 4 ? Color.LightSteelBlue : Color.SteelBlue);
            }

            g.Clip = new Region(sr);

        }

        private void RenderPlusLeft(Graphics g, Rectangle pmLeft, Pen penPlus, Color backColr)
        {
            g.FillEllipse(new SolidBrush(backColr), pmLeft);
            g.DrawLine(penPlus, pmLeft.Left + (int)(2 * SubjectBase.zoomValue), pmLeft.Top + pmLeft.Height / 2, pmLeft.Left + (int)(8 * SubjectBase.zoomValue), pmLeft.Top + pmLeft.Height / 2);
            g.DrawLine(penPlus, pmLeft.Left + (int)(5 * SubjectBase.zoomValue), pmLeft.Top + pmLeft.Height / 2 - (int)(3 * SubjectBase.zoomValue), pmLeft.Left + (int)(5 * SubjectBase.zoomValue), pmLeft.Top + pmLeft.Height / 2 + (int)(3 * SubjectBase.zoomValue));
        }

        private void RenderPlusRight(Graphics g, Rectangle pmRight, Pen penPlus, Color backColr)
        {
            g.FillEllipse(new SolidBrush(backColr), pmRight);
            g.DrawLine(penPlus, pmRight.Right - (int)(8 * SubjectBase.zoomValue), pmRight.Top + pmRight.Height / 2, pmRight.Right - (int)(2 * SubjectBase.zoomValue), pmRight.Top + pmRight.Height / 2);
            g.DrawLine(penPlus, pmRight.Right - (int)(5 * SubjectBase.zoomValue), pmRight.Top + pmRight.Height / 2 - (int)(3 * SubjectBase.zoomValue), pmRight.Right - (int)(5 * SubjectBase.zoomValue), pmRight.Top + pmRight.Height / 2 + (int)(3 * SubjectBase.zoomValue));
        }

        private void RenderPlusTop(Graphics g, Rectangle pmTop, Pen penPlus, Color backColr)
        {
            g.FillEllipse(new SolidBrush(backColr), pmTop);
            g.DrawLine(penPlus, pmTop.Left + (int)(7 * SubjectBase.zoomValue), pmTop.Top + (int)(5 * SubjectBase.zoomValue), pmTop.Left + (int)(13 * SubjectBase.zoomValue), pmTop.Top + (int)(5 * SubjectBase.zoomValue));
            g.DrawLine(penPlus, pmTop.Left + (int)(10 * SubjectBase.zoomValue), pmTop.Top + (int)(2 * SubjectBase.zoomValue), pmTop.Left + (int)(10 * SubjectBase.zoomValue), pmTop.Top + (int)(8 * SubjectBase.zoomValue));
        }

        private void RenderPlusBottom(Graphics g, Rectangle pmBottom, Pen penPlus, Color backColr)
        {
            g.FillEllipse(new SolidBrush(backColr), pmBottom);
            g.DrawLine(penPlus, pmBottom.Left + (int)(7 * SubjectBase.zoomValue), pmBottom.Bottom - (int)(5 * SubjectBase.zoomValue), pmBottom.Left + (int)(13 * SubjectBase.zoomValue), pmBottom.Bottom - (int)(5 * SubjectBase.zoomValue));
            g.DrawLine(penPlus, pmBottom.Left + (int)(10 * SubjectBase.zoomValue), pmBottom.Bottom - (int)(2 * SubjectBase.zoomValue), pmBottom.Left + (int)(10 * SubjectBase.zoomValue), pmBottom.Bottom - (int)(8 * SubjectBase.zoomValue));
        }

        private void DrawAttachRectangle(Graphics g, Pen pen, Rectangle rect, int cornerRadius)
        {
            using (GraphicsPath path = CreateRoundedRectanglePath(rect, cornerRadius))
            {
                g.DrawPath(pen, path);
                int edge = cornerRadius * 2;

                Rectangle rectEdge = new Rectangle(rect.Right - edge, rect.Top, edge, edge);
                //绘制附注主题的折叠区域
                g.FillRectangle(Brushes.White, rectEdge.Left, rectEdge.Top - 1, rectEdge.Width + 1, rectEdge.Height);

                g.DrawLine(pen, rectEdge.Left, rectEdge.Top, rectEdge.Left, rectEdge.Bottom);
                g.DrawLine(pen, rectEdge.Left, rectEdge.Top, rectEdge.Right, rectEdge.Bottom);
                g.DrawLine(pen, rectEdge.Left, rectEdge.Bottom, rectEdge.Right, rectEdge.Bottom);
            }
        }

        private void FillAttachRectangle(Graphics g, Brush brush, Rectangle rect, int cornerRadius)
        {
            using (GraphicsPath path = CreateRoundedRectanglePath(rect, cornerRadius / 2))
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

            if (cornerRadius < 2)
            {
                roundedRect.AddLine(rect.X, rect.Y, rect.Right, rect.Y);
                roundedRect.AddLine(rect.X, rect.Y, rect.X, rect.Bottom);
                roundedRect.AddLine(rect.X, rect.Bottom, rect.Right, rect.Bottom);
                roundedRect.AddLine(rect.Right, rect.Bottom, rect.Right, rect.Y);

                return roundedRect;
            }
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
        /// 绘制圆角线
        /// </summary>
        /// <param name="g"></param>
        /// <param name="pen"></param>
        /// <param name="rect"></param>
        /// <param name="cornerRadius">圆角半径</param>
        /// <param name="direction">直线绘制方向(0,1,2,3分别表示以矩形左上角开始从哪一个点绘制)</param>
        private void DrawRoundLine(Graphics g, Pen pen, Rectangle rect, int cornerRadius, int direction)
        {
            using (GraphicsPath path = CreateRoundedLinePath(rect, cornerRadius, direction))
            {
                g.DrawPath(pen, path);
            }
        }
        private GraphicsPath CreateRoundedLinePath(Rectangle rect, int cornerRadius, int direction)
        {
            GraphicsPath roundedRect = new GraphicsPath();
            if (direction == 0)
            {
                roundedRect.AddLine(rect.X, rect.Y, rect.Right - cornerRadius * 2, rect.Y);
                roundedRect.AddArc(rect.X + rect.Width - cornerRadius * 2, rect.Y, cornerRadius * 2, cornerRadius * 2, 270, 90);
                roundedRect.AddLine(rect.Right, rect.Y + cornerRadius * 2, rect.Right, rect.Y + rect.Height);

            }
            else if (direction == 1)
            {
                roundedRect.AddLine(rect.Right, rect.Y, rect.Right, rect.Y + rect.Height - cornerRadius * 2);
                roundedRect.AddArc(rect.X + rect.Width - cornerRadius * 2, rect.Y + rect.Height - cornerRadius * 2, cornerRadius * 2, cornerRadius * 2, 0, 90);
                roundedRect.AddLine(rect.X, rect.Bottom, rect.Right - cornerRadius * 2, rect.Bottom);

            }
            else if (direction == 2)
            {
                roundedRect.AddLine(rect.Right, rect.Bottom, rect.X + cornerRadius * 2, rect.Bottom);
                roundedRect.AddArc(rect.X, rect.Bottom - cornerRadius * 2, cornerRadius * 2, cornerRadius * 2, 90, 90);
                roundedRect.AddLine(rect.X, rect.Bottom - cornerRadius * 2, rect.X, rect.Y);
            }
            else if (direction == 3)
            {
                roundedRect.AddLine(rect.X, rect.Bottom, rect.X, rect.Y + cornerRadius * 2);
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
        /// <summary>
        /// 绘制矩形线
        /// </summary>
        /// <param name="g"></param>
        /// <param name="pen"></param>
        /// <param name="rect"></param>
        /// <param name="cornerRadius"></param>
        /// <param name="direction">绘制方向(0,1,2,3分别表示以矩形左上角开始从哪一个点绘制)</param>
        private void DrawRectLine(Graphics g, Pen pen, Rectangle rect, int direction)
        {
            if (direction == 3)
            {
                Point pt1 = new Point(rect.Left, rect.Bottom);
                Point pt2 = new Point(rect.Left, rect.Top);
                Point pt3 = new Point(rect.Right, rect.Top);
                g.DrawLines(pen, new Point[] { pt1, pt2, pt3 });
            }
            else if (direction == 2)
            {
                Point pt1 = new Point(rect.Left, rect.Top);
                Point pt2 = new Point(rect.Left, rect.Bottom);
                Point pt3 = new Point(rect.Right, rect.Bottom);
                g.DrawLines(pen, new Point[] { pt1, pt2, pt3 });
            }
            else if (direction == 1)
            {
                Point pt1 = new Point(rect.Left, rect.Bottom);
                Point pt2 = new Point(rect.Right, rect.Bottom);
                Point pt3 = new Point(rect.Right, rect.Top);
                g.DrawLines(pen, new Point[] { pt1, pt2, pt3 });
            }
            else if (direction == 0)
            {
                Point pt1 = new Point(rect.Left, rect.Top);
                Point pt2 = new Point(rect.Right, rect.Top);
                Point pt3 = new Point(rect.Right, rect.Bottom);
                g.DrawLines(pen, new Point[] { pt1, pt2, pt3 });
            }
        }

        /// <summary>
        /// 绘制弧线
        /// </summary>
        /// <param name="g"></param>
        /// <param name="pen"></param>
        /// <param name="rect"></param>
        /// <param name="cornerRadius">弧线半径</param>
        /// <param name="direction">绘制方向(0,1,2,3分别表示以矩形左上角开始从哪一个点绘制)</param>
        private void DrawCurveLine(Graphics g, Pen pen, Rectangle rect, int cornerRadius, int direction)
        {
            if (direction == 3)
            {
                Point pt1 = new Point(rect.Left, rect.Bottom);
                Point pt2 = new Point(rect.Left, rect.Bottom - rect.Height / 3);
                Point pt3 = new Point(rect.Left + rect.Width / 3, rect.Top);
                Point pt4 = new Point(rect.Right, rect.Top);
                g.DrawBezier(pen, pt1, pt2, pt3, pt4);
            }
            else if (direction == 2)
            {
                Point pt1 = new Point(rect.Left, rect.Top);
                Point pt2 = new Point(rect.Left, rect.Top + rect.Height / 3);
                Point pt3 = new Point(rect.Left + rect.Width / 3, rect.Bottom);
                Point pt4 = new Point(rect.Right, rect.Bottom);
                g.DrawBezier(pen, pt1, pt2, pt3, pt4);
            }
            else if (direction == 1)
            {
                Point pt1 = new Point(rect.Left, rect.Bottom);
                Point pt2 = new Point(rect.Right - rect.Width / 3, rect.Bottom);
                Point pt3 = new Point(rect.Right, rect.Top + rect.Height / 3);
                Point pt4 = new Point(rect.Right, rect.Top);
                g.DrawBezier(pen, pt1, pt2, pt3, pt4);
            }
            else if (direction == 0)
            {
                Point pt1 = new Point(rect.Left, rect.Top);
                Point pt2 = new Point(rect.Right - rect.Width / 3, rect.Top);
                Point pt3 = new Point(rect.Right, rect.Bottom - rect.Height / 3);
                Point pt4 = new Point(rect.Right, rect.Bottom);
                g.DrawBezier(pen, pt1, pt2, pt3, pt4);
            }
        }

        #endregion

        #region TextBoxEdit
        void txtNode_Leave(object sender, EventArgs e)
        {
        }
        void txtNode_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Escape)
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
            int left = r.Left + 3;
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

        private int GetSubjectMaxWidth(SubjectBase subject)
        {
            int maxWidth = GetSubjectMaxWidth(subject, 0);

            foreach (SubjectBase n in SubjectBase.FloatSubjects)
            {
                int m = GetSubjectMaxWidth(n, 0);
                if (m > maxWidth)
                {
                    maxWidth = m;
                }
            }
            return maxWidth;
        }
        private int GetSubjectMaxWidth(SubjectBase subject, int max)
        {
            int maxWidth = max;
            if (subject.Expanded)
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
            int maxHeight = GetSubjectMaxHeight(subject, 0);

            foreach (SubjectBase n in SubjectBase.FloatSubjects)
            {
                int m = GetSubjectMaxHeight(n, 0);
                if (m > maxHeight)
                {
                    maxHeight = m;
                }
            }
            return maxHeight;
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

        private int PlusInArea(MouseEventArgs e, SubjectBase taskEvent)
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
