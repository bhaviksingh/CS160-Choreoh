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
using Microsoft.Samples.Kinect.WpfViewers;
using Coding4Fun.Kinect.Wpf;
using System.Diagnostics;

namespace Choreoh
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        bool closing = false;
        Skeleton[] allSkeletons = new Skeleton[6];
        LinkedList<Skeleton> moves;
        private double menuX = 0;
        private double menuY = 0;
        private int i = 0;
        private KinectSensor sensor;
        private DanceSegment segmentToRecordTo;
        private DanceRoutine routine;

        public MainWindow()
        {
            InitializeComponent();
        }

        /*
         * delete these testing vars when done
         * */
        DanceSegment newSegment;
        bool isRecording = false;
        int framesLeft = 30 * 3;

        //Load window
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

            kinectSensorChooser1.KinectSensorChanged += new DependencyPropertyChangedEventHandler(kinectSensorChooser1_KinectSensorChanged);
            this.Cursor = Cursors.None;      
            moves = new LinkedList<Skeleton>();
            Global.canGestureTimer.Elapsed += new System.Timers.ElapsedEventHandler(Global.canGestureTimer_Elapsed);
            Global.windowWidth = mainCanvas.ActualWidth;
            Global.windowHeight = mainCanvas.ActualHeight;

            if (DanceRoutine.saveAlreadyExists("fakeSong.wav"))
            {
                routine = DanceRoutine.load("fakeSong.wav");
            } else  {
                routine = new DanceRoutine("fakeSong.wav");
            }
            routine.deleteDanceSegmentAt(0);
            showRecordingCanvas();

            newSegment = routine.addDanceSegment(0);
        }

        void newSensor_AllFramesReady(object sender, AllFramesReadyEventArgs e)
        {

            if (closing)
            {
                return;
            }
            //Get a skeleton
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
            backButton.Check(hand);

            radialCreator.Check(hand);
            //HoverButton hb = new HoverButton();
            //hb.Text = "SDjf";
            //Panel.SetZIndex(hb, 1020);
            //SolidColorBrush sb = new SolidColorBrush();
            //sb.Color = Color.FromArgb(255, 255, 0, 0);
            //hb.BackgroundColor = sb;
            //hb.Width = 500;
            //hb.Height = 500;
            //hb.Click += new HoverButton.ClickHandler(button_Clicked);
            //Canvas.SetLeft(hb, 60);
            //Canvas.SetTop(hb, 30);

            //mainCanvas.Children.Add(hb);
           
        }

       

        private void bottom_Click(object sender, EventArgs e)
        {
            RadialMenu menu = (RadialMenu)sender;
            String direction = menu.getLastHovering();
            radialMenu.Visibility = Visibility.Hidden;
            hand.menuOpened = false;
        }
        
        private void button_Clicked(object sender, EventArgs e)
        {

            debug.Text += "bclick";
            if (sender.ToString() == "Choreoh.HoverButton")
            {
                
                HoverButton temp = (HoverButton)sender;
                debug.Text += "hover button" + temp.Name;
                if (temp.Name == "backButton")
                {
                    debug.Text = "go back homeeeee";
                }
                else
                {
                    debug.Text = debug.Text + "radial menu";
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
            else
            {
                debug.Text = "I have made a huge mistake";
            }
        }
        #region helper functions
        Skeleton GetFirstSkeleton(AllFramesReadyEventArgs e)
        {
            using (SkeletonFrame skeletonFrameData = e.OpenSkeletonFrame())
            {
                if (skeletonFrameData == null)
                {
                    return null;
                }


                skeletonFrameData.CopySkeletonDataTo(allSkeletons);

                //get the first tracked skeleton
                Skeleton first = (from s in allSkeletons
                                  where s.TrackingState == SkeletonTrackingState.Tracked
                                  select s).FirstOrDefault();

                return first;

            }
        }

        //Set up kinect
        void kinectSensorChooser1_KinectSensorChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            KinectSensor oldSensor = (KinectSensor)e.OldValue;

            StopKinect(oldSensor);

            KinectSensor newSensor = (KinectSensor)e.NewValue;
            sensor = newSensor;
            if (newSensor == null)
            {
                return;
            }

            var parameters = new TransformSmoothParameters
            {
                Smoothing = 0.75f,
                Correction = 0.0f,
                Prediction = 0.0f,
                JitterRadius = 0.05f,
                MaxDeviationRadius = 0.04f
            };

            newSensor.SkeletonStream.Enable(parameters);
            newSensor.AllFramesReady += new EventHandler<AllFramesReadyEventArgs>(newSensor_AllFramesReady);
            // DELETE THIS TEMPRECORD
            newSensor.AllFramesReady += new EventHandler<AllFramesReadyEventArgs>(tempRecord);

            newSensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
            newSensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);
            newSensor.SkeletonStream.Enable();
            try
            {
                newSensor.Start();
            }
            catch (System.IO.IOException)
            {
                kinectSensorChooser1.AppConflictOccurred();
            }

        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            closing = true;
            StopKinect(kinectSensorChooser1.Kinect);
        }
        private void StopKinect(KinectSensor sensor)
        {
            if (sensor != null)
            {
                if (sensor.IsRunning)
                {
                    //stop sensor 
                    sensor.Stop();

                    //stop audio if not null
                    if (sensor.AudioSource != null)
                    {
                        sensor.AudioSource.Stop();
                    }


                }
            }
        }
        #endregion

        private void hand_GestureEvent(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            debug.Text = debug.Text + "gesture " + Global.lastGesture;
        }


        private void showRecordingCanvas()
        {
            recordingCanvas.Visibility = Visibility.Visible;
        }

        private void hideRecordingCanvas()
        {
            recordingCanvas.Visibility = Visibility.Hidden;
        }

        public void StartRecording(DanceSegment s)
        {
            Debug.WriteLine("Setting recording canvas's segment to destination segment");
            segmentToRecordTo = s;

            sensor.AllFramesReady += newSensor_AllFramesReady_Record;
        }

        public void StopRecording()
        {
            segmentToRecordTo = null;
            Debug.WriteLine("Set recording canvas's segment to null");

            sensor.AllFramesReady -= newSensor_AllFramesReady_Record;
        }

        //this event fires when Color/Depth/Skeleton are synchronized
        void newSensor_AllFramesReady_Record(object sender, AllFramesReadyEventArgs e)
        {
            if (closing)
            {
                return;
            }

            //Get a skeleton
            Skeleton first = GetFirstSkeleton(e);
            BitmapSource bitmap = GetBitmap();
            if (first == null)
            {
                return;
            }

            Debug.WriteLine("Recording frame");
            if (segmentToRecordTo != null)
            {
                segmentToRecordTo.updateImages(bitmap);
                segmentToRecordTo.updateSkeletons(first);
            }
            else
            {
                Debug.WriteLine("Trying to record to empty segment");
            }
        }

        BitmapSource GetBitmap()
        {
            RenderTargetBitmap renderBitmap = new RenderTargetBitmap((int)recordingCanvas.ActualWidth, (int)recordingCanvas.ActualHeight, 96d, 96d, PixelFormats.Pbgra32);
            renderBitmap.Render(recordingColorViewer);
            return renderBitmap;
        }



        void tempRecord(object sender, AllFramesReadyEventArgs e)
        {
            if (!isRecording && framesLeft > 0)
            {
                isRecording = true;
                StartRecording(newSegment);
            }

            framesLeft--;

            if (framesLeft == 0)
            {
                StopRecording();
            }
        }       
    }
}