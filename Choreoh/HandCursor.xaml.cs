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
using System.ComponentModel;

namespace Choreoh
{
    /// <summary>
    /// Interaction logic for HandCursor.xaml
    /// </summary>
    public partial class HandCursor : UserControl
    {
        public double cursorX = 0;
        public double cursorY = 0;
        public double radialX = 0;
        public double radialY = 0;
        private static String gesture = "";
        public bool menuOpened = false;
        public RadialMenu radialMenu;
        
        public HandCursor()
        {
            InitializeComponent();
        }
        public void SetPosition(Joint joint)
        {
            if (!menuOpened)
            {
                if (Global.initPos)
                {
                    
                    //in Global check if z moving forward by some amount then increase 0.3 and 0.4 so hand moves less
                    Joint scaledJoint = joint.ScaleTo((int)Global.windowWidth, (int)Global.windowHeight, (float)0.25, (float)0.25);
                    
                    cursorX = (double)Math.Max(0, Math.Min(scaledJoint.Position.X, Global.windowWidth - hand.Width));
                    cursorY = (double)Math.Max(0, Math.Min(scaledJoint.Position.Y, Global.windowHeight - hand.Height));
                    Canvas.SetLeft(this, cursorX);
                    Canvas.SetTop(this, cursorY);
                }
                else
                {

                    Canvas.SetLeft(this, Global.windowWidth - hand.Width);
                    Canvas.SetTop(this, Global.windowHeight - hand.Height);
                }
            }
            else
            {
                radialMenu.setCursorPosition(this, joint);
                Canvas.SetLeft(this, cursorX);
                Canvas.SetTop(this, cursorY);
            }
        }

        public void SetRadialMenu(double x, double y, RadialMenu radialMenu)
        {
            this.radialMenu = radialMenu;
            radialMenu.SetRadialPosition(x, y);
        }

        public void setCursor(double x, double y)
        {
            cursorX = x;
            cursorY = y;
        }

        #region GestureEvent
        public event PropertyChangedEventHandler GestureEvent;

        private void NotifyPropertyChanged(String info)
        {
            if (GestureEvent != null)
            {
                GestureEvent(this, new PropertyChangedEventArgs(info));
            }
        }
        public String Gestured
        {
            get { return gesture; }
            set
            {
                if (value != gesture)
                {
                    gesture = value;
                    NotifyPropertyChanged(value);
                    gesture = "";
                }
            }
        }

        public void checkGestures(LinkedList<Skeleton> moves)
        {
            Gestured = Global.checkMoves(moves);
        }
        #endregion
    }
}

