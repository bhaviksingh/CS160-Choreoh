using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Kinect;
using Coding4Fun.Kinect.Wpf;
using System.Windows.Media.Effects;

namespace Choreoh
{
    /// <summary>
    /// Interaction logic for RadialMenu.xaml
    /// </summary>
    public partial class RadialMenu : UserControl
    {
        private RadialAnimate radialAnimate;
        private String lastHovering = "";
        private double radialX = 0;
        private double radialY = 0;
        private double cursorX = 0;
        private double cursorY = 0;
        public RadialMenu()
        {
            InitializeComponent();
            radialAnimate = new RadialAnimate();
            this.Effect = shadowEffect;
            setSize(300);
        }

        private void setSize(int diameter)
        {

        }

        public int getDiameter()
        {
            return 319;
        }


        #region Properties
        public static readonly DependencyProperty TopTextProperty = DependencyProperty.Register(
            "TopText", typeof(string), typeof(RadialMenu), new PropertyMetadata(""));
        public string TopText
        {
            get { return (string)this.GetValue(TopTextProperty); }
            set { this.SetValue(TopTextProperty, value); }
        }
        public static readonly DependencyProperty LeftTextProperty = DependencyProperty.Register(
            "LeftText", typeof(string), typeof(RadialMenu), new PropertyMetadata(""));
        public string LeftText
        {
            get { return (string)this.GetValue(LeftTextProperty); }
            set { this.SetValue(LeftTextProperty, value); }
        }
        public static readonly DependencyProperty RightTextProperty = DependencyProperty.Register(
            "RightText", typeof(string), typeof(RadialMenu), new PropertyMetadata(""));
        public string RightText
        {
            get { return (string)this.GetValue(RightTextProperty); }
            set { this.SetValue(RightTextProperty, value); }
        }
        public static readonly DependencyProperty BottomTextProperty = DependencyProperty.Register(
            "BottomText", typeof(string), typeof(RadialMenu), new PropertyMetadata(""));
        public string BottomText
        {
            get { return (string)this.GetValue(BottomTextProperty); }
            set { this.SetValue(BottomTextProperty, value); }

        }
        #endregion

        DropShadowEffect shadowEffect = new DropShadowEffect
        {
            Color = new Color { A = 255, R = 0, G = 0, B = 0 },
            Direction = 0,
            ShadowDepth = 0,
            Opacity = .4,
            BlurRadius = 20
        };

        public delegate void ClickHandler(object sender, EventArgs e);
        public event ClickHandler leftClick;
        public event ClickHandler rightClick;
        public event ClickHandler topClick;
        public event ClickHandler bottomClick;

        public void startHovering(Image control, String direction)
        {
            radialAnimate.CompleteChanged += new EventHandler(radialAnimate_CompleteChanged);
            radialAnimate.expand(control, direction);
        }

        public void stopHovering(String direction)
        {
            radialAnimate.stop(direction);
        }

        void radialAnimate_CompleteChanged(object sender, EventArgs e)
        {
            Global.canGesture = true;
            switch (lastHovering)
            {
                case ("Left"):
                    if (leftClick != null)
                        leftClick(this, e);
                    break;
                case ("Top"):
                    if (topClick != null)
                        topClick(this, e);
                    break;
                case ("Right"):
                    if (rightClick != null)
                        rightClick(this, e);
                    break;
                case ("Bottom"):
                    if (bottomClick != null)
                        bottomClick(this, e);
                    break;
            }
        }

        public void setZindex(String active)
        {
            Panel.SetZIndex(RadialHighBottom, 96);
            Panel.SetZIndex(RadialHighRight, 96);
            Panel.SetZIndex(RadialHighLeft, 96);
            Panel.SetZIndex(RadialHighTop, 96);
            Panel.SetZIndex(RadialLeft, 100);
            Panel.SetZIndex(RadialRight, 100);
            Panel.SetZIndex(RadialTop, 100);
            Panel.SetZIndex(RadialBottom, 100);
            switch (active)
            {
                case ("Right"):
                    Panel.SetZIndex(RadialHighRight, 99);
                    Panel.SetZIndex(RadialRight, 98);
                    break;
                case ("Left"):
                    Panel.SetZIndex(RadialHighLeft, 99);
                    Panel.SetZIndex(RadialLeft, 98);
                    break;
                case ("Top"):
                    Panel.SetZIndex(RadialHighTop, 99);
                    Panel.SetZIndex(RadialTop, 98);
                    break;
                case ("Bottom"):
                    Panel.SetZIndex(RadialHighBottom, 99);
                    Panel.SetZIndex(RadialBottom, 98);
                    break;
            }
        }

        public void SetRadialPosition(double x, double y)
        {
            radialX = x;
            radialY = y;
        }

        public String getLastHovering()
        {
            return lastHovering;
        }

        public void setCursorPosition(HandCursor hand, Joint joint)
        {
            Global.canGesture = false;
            Joint scaledJoint = joint.ScaleTo((int)Global.windowWidth, (int)Global.windowHeight, (float)0.3, (float)0.4);
            if ((scaledJoint.Position.X > radialX + 100 || scaledJoint.Position.X < radialX - 100) &&
                (Math.Abs(scaledJoint.Position.Y - radialY) < Math.Abs(scaledJoint.Position.X - radialX)))
            {
                cursorX = (double)Math.Max(radialX - ActualWidth / 2 + hand.ActualWidth / 2,
                    Math.Min(scaledJoint.Position.X, radialX + ActualWidth / 2 - hand.ActualWidth / 2));
                cursorY = radialY;
                if (cursorX > radialX && (lastHovering != "Right"))
                {
                    stopHovering(lastHovering);
                    lastHovering = "Right";
                    setZindex(lastHovering);
                    startHovering(RadialHighRight, lastHovering);
                }
                else if (cursorX < radialX && (lastHovering != "Left"))
                {
                    stopHovering(lastHovering);
                    lastHovering = "Left";
                    setZindex(lastHovering);
                    startHovering(RadialHighLeft, lastHovering);
                }

            }
            else if ((scaledJoint.Position.Y < radialY - 100 || scaledJoint.Position.Y > radialY + 100) &&
                     (Math.Abs(scaledJoint.Position.Y - radialY) > Math.Abs(scaledJoint.Position.X - radialX)))
            {
                cursorX = radialX;
                cursorY = (double)Math.Max(radialY - ActualHeight / 2 + hand.ActualHeight / 2,
                    Math.Min(scaledJoint.Position.Y, radialY + ActualHeight / 2 - hand.ActualHeight / 2));
                if (cursorY > radialY && (lastHovering != "Bottom"))
                {
                    stopHovering(lastHovering);
                    lastHovering = "Bottom";
                    setZindex(lastHovering);
                    startHovering(RadialHighBottom, lastHovering);
                }
                else if (cursorY < radialY && (lastHovering != "Top"))
                {
                    stopHovering(lastHovering);
                    lastHovering = "Top";
                    setZindex(lastHovering);
                    startHovering(RadialHighTop, lastHovering);
                }

            }
            else
            {
                cursorX = radialX;
                cursorY = radialY;
                stopHovering(lastHovering);
            }
            hand.setCursor(cursorX, cursorY);
        }
    }
}
