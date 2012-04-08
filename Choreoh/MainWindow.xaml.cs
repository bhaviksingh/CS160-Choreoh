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
using Microsoft.Speech.AudioFormat;
using Microsoft.Speech.Recognition;
using System.Windows.Threading;
using System.Threading;
using System.IO;


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
        private KinectSensor sensor;
        private DanceSegment segmentToRecordTo;
        private DanceRoutine routine;
        private Waveform waveform;

        public MainWindow()
        {
            InitializeComponent();
        }

        /* This is how to create a dance segment button
         * Want to do it programmatically so need a linked list of
         * dance segment buttons so when click event takes place
         * iterate throw the linkedlist calling listElement.check(handCursor);
            HoverButton hb = new HoverButton();
            hb.LeftImage = "Images/RadialHighLeft.png";
            hb.RightImage = "Images/RadialHighRight.png";
            hb.dotDot.Visibility = Visibility.Visible;
            hb.Height = 100;
            hb.Width = 200;
            Canvas.SetTop(hb, 100);
            Canvas.SetLeft(hb, 0);
            mainCanvas.Children.Add(hb);
            hb.Click += new HoverButton.ClickHandler(button_Clicked);
            */


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
            Canvas wfcanvas = new Canvas();
            wfcanvas.Width = 1800;
            wfcanvas.Height = 160;
            Canvas.SetTop(wfcanvas, 0);
            Canvas.SetLeft(wfcanvas, 0);
            waveButton.hoverCanvas.Children.Add(wfcanvas);
            waveButton.enableExpandAnimation = false;
            waveform = new Waveform(1800, 259, wfcanvas);
        }

        private void waveform_Clicked(object sender, EventArgs e)
        {
            if (sender.ToString() == "Choreoh.HoverButton")
            {
                Debug.WriteLine("Waveform Button clicked");
                HoverButton waveButton = (HoverButton)sender;

                Point handPosition = hand.TransformToAncestor(mainCanvas).Transform(new Point(0, 0));
                menuY = handPosition.Y;
                menuY = menuY + hand.ActualHeight / 2 - timelineRadialMenu.getDiameter()/2;
                menuX = handPosition.X;
                menuX = menuX + hand.ActualWidth / 2 - timelineRadialMenu.getDiameter() / 2;
                Canvas.SetLeft(timelineRadialMenu, menuX);
                Canvas.SetTop(timelineRadialMenu, menuY);

                hand.menuOpened = true;
                hand.SetRadialMenu(handPosition.X, handPosition.Y, timelineRadialMenu);

                timelineRadialMenu.Visibility = Visibility.Visible;
            }
            else
            {
                debug.Text = "I have made a huge mistake";
            }
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
            waveButton.Check(hand);
        }

       

        private void bottom_Click(object sender, EventArgs e)
        {
            RadialMenu menu = (RadialMenu)sender;
            String direction = menu.getLastHovering();
            menu.Visibility = Visibility.Collapsed;
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
                    Canvas.SetLeft(radialMenu, menuX - radialMenu.getDiameter() / 2);
                    Canvas.SetTop(radialMenu, menuY - radialMenu.getDiameter() / 2);
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
            // newSensor.AllFramesReady += new EventHandler<AllFramesReadyEventArgs>(tempRecord);

            newSensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
            newSensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);
            newSensor.SkeletonStream.Enable();
            this.speechRecognizer = this.CreateSpeechRecognizer();
            try
            {
                newSensor.Start();
            }
            catch (System.IO.IOException)
            {
                kinectSensorChooser1.AppConflictOccurred();
            }

            
            if (this.speechRecognizer != null && sensor != null)
            {
                // NOTE: Need to wait 4 seconds for device to be ready to stream audio right after initialization
                this.readyTimer = new DispatcherTimer();
                this.readyTimer.Tick += this.ReadyTimerTick;
                this.readyTimer.Interval = new TimeSpan(0, 0, 4);
                this.readyTimer.Start();
            }

        }

        private void ReadyTimerTick(object sender, EventArgs e)
        {
            startAudio();
            this.readyTimer.Stop();
            this.readyTimer = null;
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

                    if (this.speechRecognizer != null && sensor != null)
                    {
                        sensor.AudioSource.Stop();
                        sensor.Stop();
                        this.speechRecognizer.RecognizeAsyncCancel();
                        this.speechRecognizer.RecognizeAsyncStop();
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
            recordingCanvas.Visibility = Visibility.Collapsed;
        }

        public void StartRecording(DanceSegment s)
        {
            Debug.WriteLine("Setting recording canvas's segment to destination segment");
            segmentToRecordTo = s;
            //pre_recording = false;

            sensor.AllFramesReady += newSensor_AllFramesReady_Record;
        }

        public void StopRecording()
        {
            segmentToRecordTo = null;
            Debug.WriteLine("Set recording canvas's segment to null");
            //post_recording = true;

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

        #region audio config and control

        private DispatcherTimer readyTimer;
        private SpeechRecognitionEngine speechRecognizer;
        private String wordsForGrammar;
        private String[] wordsArray;
        private String comment;

        private bool pre_recording = false;
        private bool post_recording = false;
        private bool annotating = true;

        private void startAudio()
        {
            var audioSource = this.sensor.AudioSource;
            audioSource.BeamAngleMode = BeamAngleMode.Adaptive;

            // This should be off by default, but just to be explicit, this MUST be set to false.
            audioSource.AutomaticGainControlEnabled = false;

            var kinectStream = audioSource.Start();
            this.speechRecognizer.SetInputToAudioStream(
                kinectStream, new SpeechAudioFormatInfo(EncodingFormat.Pcm, 16000, 16, 1, 32000, 2, null));
            // Keep recognizing speech until window closes
            this.speechRecognizer.RecognizeAsync(RecognizeMode.Multiple);
        }

         #region Speech recognizer setup

        private static RecognizerInfo GetKinectRecognizer()
        {
            Func<RecognizerInfo, bool> matchingFunc = r =>
            {
                string value;
                r.AdditionalInfo.TryGetValue("Kinect", out value);
                return "True".Equals(value, StringComparison.InvariantCultureIgnoreCase) && "en-US".Equals(r.Culture.Name, StringComparison.InvariantCultureIgnoreCase);
            };
            return SpeechRecognitionEngine.InstalledRecognizers().Where(matchingFunc).FirstOrDefault();
        }

        //takes vocabulary of words from text file, puts it into a string array
        private void LoadWords()
        {
            var path = System.IO.Path.GetFullPath("english_words.txt");
            if (File.Exists(path))
            {
                wordsForGrammar = File.ReadAllText(path);
            }
            else
            {
                wordsForGrammar = "panda";
            }
            wordsArray = wordsForGrammar.Split('\n');

            for (int i = 0; i < wordsArray.Length; i++)
            {
                wordsArray[i] = wordsArray[i].Trim();
            }

        }

        private SpeechRecognitionEngine CreateSpeechRecognizer()
        {
            #region Initialization
            RecognizerInfo ri = GetKinectRecognizer();
            if (ri == null)
            {
                MessageBox.Show(
                    @"There was a problem initializing Speech Recognition.
                    Ensure you have the Microsoft Speech SDK installed.",
                    "Failed to load Speech SDK",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                this.Close();
                return null;
            }

            SpeechRecognitionEngine sre;
            try
            {
                sre = new SpeechRecognitionEngine(ri.Id);
            }
            catch
            {
                MessageBox.Show(
                    @"There was a problem initializing Speech Recognition.
                    Ensure you have the Microsoft Speech SDK installed and configured.",
                    "Failed to load Speech SDK",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                this.Close();
                return null;
            }
            #endregion

            #region Build grammar

            //takes vocabulary of words from text file, puts it into a string array
            LoadWords();

            var wordChoices = new Choices(wordsArray);

            var preRecordingChoices = new Choices(new string[] { "start" });
            var gb_preR = new GrammarBuilder { Culture = ri.Culture };

            var postRecordingChoices = new Choices(new string[] { "keep", "cancel", "redo", "play" });
            var gb_postR = new GrammarBuilder { Culture = ri.Culture };

            var gb_1 = new GrammarBuilder { Culture = ri.Culture };
            gb_1.Append(wordChoices);

            /*
            var gb_2 = new GrammarBuilder { Culture = ri.Culture };
            gb_2.Append(wordChoices);

            var gb_3 = new GrammarBuilder { Culture = ri.Culture };
            gb_3.Append(wordChoices);

            var gb_4 = new GrammarBuilder { Culture = ri.Culture };
            gb_4.Append(wordChoices);
            */

            var gb = new GrammarBuilder { Culture = ri.Culture };

            gb.Append(gb_preR, 0, 1);
            gb.Append(gb_postR, 0, 1);

            //gb.Append(new SemanticResultKey("Words0", wordChoices));
            gb.Append(gb_1, 0, 1);
            //gb.Append(gb_2, 0, 1);
            //gb.Append(gb_3, 0, 1);
            //gb.Append(gb_4, 0, 1);



            // Create the actual Grammar instance, and then load it into the speech recognizer.
            var g = new Grammar(gb);


            sre.LoadGrammar(g);

            #endregion

            #region Hook up events
            sre.SpeechRecognized += new EventHandler<SpeechRecognizedEventArgs>(sre_SpeechRecognized);
            sre.SpeechRecognitionRejected += new EventHandler<SpeechRecognitionRejectedEventArgs>(sre_SpeechRecognitionRejected);
            /*
            sre.SpeechHypothesized += this.SreSpeechHypothesized;
            sre.SpeechRecognitionRejected += this.SreSpeechRecognitionRejected;
            */
            #endregion

            return sre;
        }
         #endregion

        #region Speech recognition events

        void sre_SpeechRecognitionRejected(object sender, SpeechRecognitionRejectedEventArgs e)
        {
            this.RejectSpeech(e.Result);
        }

        void sre_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {

            var alternates = e.Result.Alternates;
            String alternates_string = "";
            String[] alternates_string_array = new String[alternates.Count];

            foreach (RecognizedPhrase i in alternates)
            {
                int j = 0;
                alternates_string = alternates_string + " " + i.Text.ToString();
                alternates_string_array[j] = alternates_string + " " + i.Text.ToString();
                j++;
            }

            if (annotating)
            {
                if (e.Result.Confidence < 0.5)
                {
                    this.RejectSpeech(e.Result);

                    return;
                }
                else
                {
                    //alternates_label.Content = alternates_string;
                    this.RecognizeSpeech(e.Result);
                    return;
                }
            }
            else if (pre_recording)
            {
                switch (e.Result.Text.ToString().ToUpperInvariant())
                {
                    case "START":
                        //start_label.Visibility = Visibility.Visible;
                        return;
                    default:
                        return;
                }
            }
            else if (post_recording)
            {

                switch (e.Result.Text.ToString().ToUpperInvariant())
                {
                    case "KEEP":
                        //keep_label.Visibility = Visibility.Visible;
                        return;
                    case "CANCEL":
                        //cancel_label.Visibility = Visibility.Visible;
                        return;
                    case "REDO":
                        //redo_label.Visibility = Visibility.Visible;
                        return;
                    case "PLAY":
                        //play_label.Visibility = Visibility.Visible;
                        return;
                    default:
                        return;
                }

            }
            else
            {
                return;
            }

        }

   
        private void RecognizeSpeech(RecognitionResult result)
        {
            string status = "Recognzied: " + (result == null ? string.Empty : result.Text + " " + result.Confidence);
            this.ReportStatus(status);
            string newText = result.Text.ToString();
            this.UpdateText(newText);
            this.comment = this.comment + result.Text.ToString();
        }

        private void RejectSpeech(RecognitionResult result)
        {
            string status = "Rejected: " + (result == null ? string.Empty : result.Text + " " + result.Confidence);
            this.ReportStatus(status);
        }

        String getComment()
        {
            if (this.comment != null)
            {
                return this.comment;
            }
            return "";
        }

        #endregion

        #region UI update functions
        private void ReportStatus(string status)
        {
            //Dispatcher.BeginInvoke(new Action(() => { statusLabel.Content = status; }), DispatcherPriority.Normal);
        }
        private void UpdateText(string newText)
        {
            Dispatcher.BeginInvoke(new Action(() => { debug.Text = debug.Text + " " + newText; }), DispatcherPriority.Normal);
        }
        #endregion
        #endregion
        #region timeline radial menu clicks
        private void timelineRadialMenu_leftClick(object sender, EventArgs e)
        {
            Debug.WriteLine("orange");
            RadialMenu menu = (RadialMenu)sender;
            Debug.WriteLine(menu.ToString());
        }

        private void timelineRadialMenu_rightClick(object sender, EventArgs e)
        {

        }

        private void timelineRadialMenu_topClick(object sender, EventArgs e)
        {

        }
        #endregion
    }
}