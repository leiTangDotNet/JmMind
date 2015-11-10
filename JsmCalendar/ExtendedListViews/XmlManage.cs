using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace JsmMind
{
    class XmlManage
    {
        public static string SaveSubject(SubjectBase centerSubject, List<SubjectBase> floatSubjects,MapTheme theme,MapViewModel viewmodel)
        {
            XmlDocument doc = new XmlDocument();
            XmlElement root = doc.CreateElement("JmMap");
              
            CreateObjectAttribute(doc, root, "Theme", theme.ToString());
            CreateObjectAttribute(doc, root, "ViewModel", viewmodel.ToString());

            SaveSubjectInfo(doc,root, centerSubject);
            foreach(SubjectBase floatSubject in floatSubjects)
            {
                SaveSubjectInfo(doc, root, floatSubject);
            }

            doc.AppendChild(root);
            string ss = doc.InnerXml;
            return ss;
        }


        private static void SaveSubjectInfo(XmlDocument doc, XmlElement root, SubjectBase subject)
        {
            //Create Subject Element
            XmlElement nodeSubject = CreateObjectElement(doc, subject);

            //Subject'PictureInfo 
            XmlElement nodePictures= doc.CreateElement("SujectPictrues");
            foreach(SubjectPicture subjectPicture in subject.SubjectPictures)
            {
                XmlElement nodePicture = CreateObjectElement(doc, subjectPicture);
                nodePictures.AppendChild(nodePicture);
            }
            nodeSubject.AppendChild(nodePictures);


            //Save Subject'ChildSubject 
            XmlElement nodeChildSubjects = doc.CreateElement("ChildSubjects"); 
            foreach(SubjectBase childSubject in subject.ChildSubjects)
            {
                SaveSubjectInfo(doc, nodeChildSubjects, childSubject);
            }
            nodeSubject.AppendChild(nodeChildSubjects);

            root.AppendChild(nodeSubject);
        }

        private static XmlElement CreateObjectElement(XmlDocument doc, object ob)
        {
            Type type = ob.GetType();
            System.Reflection.PropertyInfo[] array_properyInfo = type.GetProperties();

            XmlElement obElement = doc.CreateElement(type.Name);

            //Save Subject Slef Attribute
            foreach (System.Reflection.PropertyInfo property in array_properyInfo)
            {
                string name = property.Name;
                string value = GetPropertyValue(property, ob);

                XmlAttribute nodeAtt = doc.CreateAttribute(name);
                XmlText nodeText = doc.CreateTextNode(value);
                nodeAtt.AppendChild(nodeText);
                obElement.Attributes.Append(nodeAtt);
            }
            return obElement; 
        } 
        private static void CreateObjectAttribute(XmlDocument doc, XmlElement root, string name, string value)
        {
            XmlAttribute nodeAtt = doc.CreateAttribute(name);
            XmlText nodeText = doc.CreateTextNode(value);
            nodeAtt.AppendChild(nodeText);
            root.Attributes.Append(nodeAtt);
        }

        public static SubjectBase ReadSubject(string xml, List<SubjectBase> floatSubjects, out MapTheme theme,out MapViewModel viewmodel)
        {
            XmlDocument NexusDocument = new XmlDocument();

            NexusDocument.LoadXml(xml);

            theme = MapTheme.Default;
            viewmodel = MapViewModel.ExpandRightSide;
            //Read Map Attributes
            foreach (XmlAttribute att in NexusDocument.DocumentElement.Attributes)
            {
                string name = att.Name;
                string value = att.Value; 
                if(name=="Theme")
                {
                    theme = (MapTheme)Enum.Parse(typeof(MapTheme), value);
                }
                else if (name == "ViewModel")
                {
                    viewmodel = (MapViewModel)Enum.Parse(typeof(MapViewModel), value);
                }
            }
            //Read CenterSubject,FloatSubject
            SubjectBase centetSubject = null; 
            foreach (XmlNode node in NexusDocument.DocumentElement.ChildNodes)
            {  
                if(node.Name=="CenterSubject")
                {
                    centetSubject = ReadSubjectInfo(node);
                }
                else
                {
                    SubjectBase floatSubject = ReadSubjectInfo(node);
                    floatSubjects.Add(floatSubject);
                }
            }
            return centetSubject;
        }
        private static SubjectBase ReadSubjectInfo(XmlNode xmlNode)
        {
            XmlNode nodeSubject = xmlNode;
            //Read SubjectInfo
            object subjectObject = CreateElementObject(nodeSubject);

            SubjectBase subject = subjectObject as SubjectBase;
            //Read Picture Info
            foreach (XmlNode node in xmlNode.ChildNodes)
            {
                if(node.Name== "SujectPictrues")
                {
                    foreach(XmlNode nodePicture in node.ChildNodes)
                    {
                        object pictureObject = CreateElementObject(nodePicture);
                        SubjectPicture subjectPicture = pictureObject as SubjectPicture;
                        subject.SubjectPictures.Add(subjectPicture);
                    }
                } 
                if (node.Name == "ChildSubjects")
                {
                    foreach (XmlNode nodeChildSubject in node.ChildNodes)
                    { 
                        SubjectBase childsubject  = ReadSubjectInfo(nodeChildSubject);
                        subject.ChildSubjects.Add(childsubject);
                        childsubject.ParentSubject = subject;
                    } 
                } 
            }
            return subject; 
        }

        private static object CreateElementObject(XmlNode nodeObject)
        { 
            Assembly assembly = typeof(XmlManage).Assembly;
            object ob = assembly.CreateInstance("JsmMind." + nodeObject.Name);
            foreach (XmlAttribute att in nodeObject.Attributes)
            {
                string name = att.Name;
                string value = att.Value;

                Type type = ob.GetType();
                System.Reflection.PropertyInfo propertyInfo = type.GetProperty(name);
                if (propertyInfo != null)
                {
                    SetPropertyValue(propertyInfo, ob, value);
                }
            }
            return ob;
        }


        public static string GetPropertyValue(PropertyInfo propertyInfo, object ob)
        {
            string value = "";
            if (propertyInfo != null)
            {
                if(propertyInfo.PropertyType==typeof(Color))
                { 
                    Color color = (Color)(propertyInfo.GetValue(ob, null));
                    value = color.ToArgb().ToString();
                }
                else if(propertyInfo.PropertyType== typeof(Font))
                {
                    Font font = (Font)(propertyInfo.GetValue(ob, null));
                    value = string.Format("{0},{1},{2},{3}", font.Name, font.Size.ToString(), font.Bold?"1":"0", font.Italic?"1":"0");
                }
                else if (propertyInfo.PropertyType == typeof(Image))
                {
                    Image image = (Image)(propertyInfo.GetValue(ob, null));
                    byte[] imageBuffer = Helpers.ImageHelper.ImageToBytes(image);
                    value = System.Convert.ToBase64String(imageBuffer);
                }
                else
                {
                    value = propertyInfo.GetValue(ob, null) == null ? "" : propertyInfo.GetValue(ob, null).ToString();
                }
            }


            return value;
        }

        public static void SetPropertyValue(PropertyInfo propertyInfo, object columnInput, string value)
        {
            if (propertyInfo != null)
            {
                if (propertyInfo.PropertyType == typeof(Point))
                { 
                    List<string> ss = ArrParser(value, "[0-9]{1,}", "[0-9]{1,}");
                    Point pt = new Point(Convert.ToInt32(ss[0]), Convert.ToInt32(ss[1]));
                    propertyInfo.SetValue(columnInput, pt, null);
                }
                else if (propertyInfo.PropertyType == typeof(Size))
                {
                    List<string> ss = ArrParser(value, "[0-9]{1,}", "[0-9]{1,}");
                    Size sz = new Size(Convert.ToInt32(ss[0]), Convert.ToInt32(ss[1]));
                    propertyInfo.SetValue(columnInput, sz, null);
                }
                else if (propertyInfo.PropertyType == typeof(Rectangle))
                { 
                    List<string> ss = ArrParser(value, "[0-9]{1,}", "[0-9]{1,}");
                    Rectangle rc = new Rectangle(Convert.ToInt32(ss[0]), Convert.ToInt32(ss[1]), Convert.ToInt32(ss[2]), Convert.ToInt32(ss[3]));
                    propertyInfo.SetValue(columnInput, rc, null);
                }
                else if(propertyInfo.PropertyType==typeof(Color))
                {
                    Color color = Color.FromArgb(Convert.ToInt32(value));
                    propertyInfo.SetValue(columnInput, color, null); 
                }
                else if(propertyInfo.PropertyType== typeof(Font))
                {
                    FontStyle fs = FontStyle.Regular;
                    //ss 0,1,2,3分别是fontName,fontSize,isBlod,isItalic
                    string[] ss = value.Split(new char[] { ',' }, StringSplitOptions.None);// ArrParser(value, "[0-9]{1,}", "[0-9]{1,}");
                    if (ss[2] == "1") fs |= FontStyle.Bold;
                    if (ss[3] == "1") fs |= FontStyle.Italic;

                    Font font = new Font(ss[0], float.Parse(ss[1]), fs);
                    propertyInfo.SetValue(columnInput, font, null);
                }
                else if (propertyInfo.PropertyType == typeof(DateTime))
                {
                    propertyInfo.SetValue(columnInput, DateTime.Parse(value), null);
                }
                else if (propertyInfo.PropertyType == typeof(float))
                {
                    float f = Convert.ToSingle(value);
                    propertyInfo.SetValue(columnInput, f, null);
                }
                else if (propertyInfo.PropertyType == typeof(int))
                {
                    propertyInfo.SetValue(columnInput, Convert.ToInt32(value), null);
                }
                else if (propertyInfo.PropertyType == typeof(bool))
                {
                    propertyInfo.SetValue(columnInput, Convert.ToBoolean(value), null);
                }
                else if (propertyInfo.PropertyType == typeof(string))
                {
                    propertyInfo.SetValue(columnInput, value, null);
                }
                else
                {
                    //默认处理
                    //propertyInfo.SetValue(columnInput, null, null);
                }
            }
        }
        public static List<string> ArrParser(string txt, string pattern, string SplitTxt = ".*?")
        {
            List<string> mylist = new List<string>();
            MatchCollection mc = Regex.Matches(txt, pattern, RegexOptions.IgnoreCase);
            for (int i = 0; i < mc.Count; i++)
            {
                string itemtxt = mc[i].ToString();
                pattern = pattern.Replace(SplitTxt, "‖");
                string[] list = pattern.Split('‖');
                for (int j = 0; j < list.Length; j++)
                {
                    Regex regex = new Regex(@list[j], RegexOptions.IgnoreCase);
                    itemtxt = regex.Replace(itemtxt, "");
                } mylist.Add(itemtxt);
            }
            return mylist;
        }
    } 
}
