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

namespace tester
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        KinectSensor kinect;
        public static double canvasSize;
        private LinkedList<Skeleton> moves;
        private double menuX = 0;
        private double menuY = 0;
        public MainWindow()
        {
            InitializeComponent();
        }
        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {  
            Global.windowWidth = mainCanvas.ActualWidth;
            Global.windowHeight = mainCanvas.ActualHeight;
        }
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            canvasSize = mainCanvas.ActualWidth;
            Global.windowWidth = mainCanvas.ActualWidth;
            Global.windowHeight = mainCanvas.ActualHeight;
            if (KinectSensor.KinectSensors.Count() == 0)
            {
                this.Title = "No Kinect Connected";
            }
            else
            {
                kinect = KinectSensor.KinectSensors[0];
                kinect.SkeletonStream.Enable();
                kinect.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);

                kinect.SkeletonFrameReady += new EventHandler<SkeletonFrameReadyEventArgs>(kinect_SkeletonFrameReady);
                //kinect.ColorFrameReady += new EventHandler<ColorImageFrameReadyEventArgs>(kinect_ColorFrameReady);
                Global.canGestureTimer.Elapsed += new System.Timers.ElapsedEventHandler(Global.canGestureTimer_Elapsed);
                moves = new LinkedList<Skeleton>();
                kinect.Start();
                hand.GestureEvent += new System.ComponentModel.PropertyChangedEventHandler(hand_GestureEvent);
            }
        }

        void hand_GestureEvent(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            gestureText.Text = e.PropertyName;
            String gesture = e.PropertyName;
            switch (gesture)
            {
                case "Pushed":
                    
                    break;
                default:
                    break;
            }

        }
        void kinect_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            /* **This is how to add content to a hoverButton **
            Label newLabel = new Label();
            newLabel.Content = "New Element";
            btn1.hoverCanvas.Children.Add(newLabel);
            Canvas.SetLeft(newLabel, 0);
            Canvas.SetTop(newLabel, 0);
            */
            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame != null)
                {
                    Skeleton[] skeletonData = new Skeleton[skeletonFrame.SkeletonArrayLength];
                    skeletonFrame.CopySkeletonDataTo(skeletonData);
                    Skeleton skeleton = (from s in skeletonData where s.TrackingState == SkeletonTrackingState.Tracked select s).FirstOrDefault();
                    if (skeleton == null)
                    {
                        return;
                    }
                    moves.AddLast(skeleton);
                    if (moves.Count > 9)
                    {
                        moves.RemoveFirst();
                    }
                    Joint handJoint = skeleton.Joints[JointType.HandRight];
                    hand.checkGestures(moves);
                    buttonUpdater(handJoint);
                    //temporary to clear gestureText
                    if (Global.canGesture)
                        gestureText.Text = "";
                }
            }
        }
        private void buttonUpdater(Joint handJoint)
        {
            hand.SetPosition(handJoint);
            btn1.Check(hand);
        }

        private void bottom_Click(object sender, EventArgs e)
        {
            RadialMenu menu = (RadialMenu)sender;
            String direction = menu.getLastHovering();
            radialMenu.Visibility = Visibility.Hidden;
            hand.menuOpened = false;
        }
        private void btn1_Clicked(object sender, EventArgs e)
        {
            Point handPosition = hand.TransformToAncestor(mainCanvas).Transform(new Point(0, 0));
            menuY = handPosition.Y;
            menuY = menuY + hand.ActualHeight / 2;
            menuX = handPosition.X;
            menuX = menuX + hand.ActualWidth / 2;
            Canvas.SetLeft(radialMenu, menuX - radialMenu.ActualWidth / 2);
            Canvas.SetTop(radialMenu, menuY - radialMenu.ActualHeight / 2);
            hand.menuOpened = true;
            hand.SetRadialMenu(handPosition.X, handPosition.Y, radialMenu);
            radialMenu.Visibility = Visibility.Visible;
        }
    }
}
