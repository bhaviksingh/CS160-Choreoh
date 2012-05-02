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
using System.Speech.AudioFormat;
using System.Speech.Recognition;
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
        DanceSegment newSegment;
        bool closing = false;
        Skeleton[] allSkeletons = new Skeleton[6];
        LinkedList<Skeleton> moves;
        private KinectSensor sensor;
        private DanceSegment segmentToRecordTo;
        private DanceRoutine routine;
        private Waveform waveform;
        private int oldButtonZIndex;
        private int oldDanceSegmentIndex;
        private int oldCommentBoxIndex;
        bool isPlaying;
        double handPointX;
        DanceSegment selectedSegment;
        string commentToSave;

        public MainWindow()
        {
            InitializeComponent();
        }

        /* This is how to create a dance segment button
         * Want to do it programmatically so need a linked list of
         * dance segment buttons so when click event takes place
         * iterate throw the linkedlist calling listElement.check(handCursor);
            HoverButton hb = new HoverButton();
            hb.leftImage = DanceSegment.getFirstFrame();
            hb.rightImage = "Images/RadialHighRight.png";
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

        bool isRecording = false;
        int framesLeft = 30 * 3;
        Point timelineMenuOpenedPosition;
        bool isSelectingEndSegment = false;
        bool isSelectingStartSegment = false;
        bool isSelectingRecordSegment = false;
        bool isSelectingPlaySegment = false;
        bool isSelectingPlaySongSelection = false;
        double startSecondsIntoWaveform;
        double endSecondsIntoWaveform;
        String songFilename = "beatit.wav";
        LinkedList<HoverButton> segmentList = new LinkedList<HoverButton>();
        LinkedList<HoverButton> buttonList = new LinkedList<HoverButton>();
        Dictionary<HoverButton, DanceSegment> buttonSegments;

        //Load window
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            homeCanvas.Visibility = Visibility.Collapsed;
            mainCanvas.Visibility = Visibility.Visible;
            blackBack.Visibility = Visibility.Collapsed;

            hideMode();
            Canvas.SetTop(playbackMode, 0);
            Canvas.SetTop(recordMode, 0);

            kinectSensorChooser1.KinectSensorChanged += new DependencyPropertyChangedEventHandler(kinectSensorChooser1_KinectSensorChanged);
            this.Cursor = Cursors.None;
            moves = new LinkedList<Skeleton>();
            Global.canGestureTimer.Elapsed += new System.Timers.ElapsedEventHandler(Global.canGestureTimer_Elapsed);
            Global.initializeTimer.Elapsed += new System.Timers.ElapsedEventHandler(Global.initializeTimer_Elapsed);
            Global.windowWidth = containerCanvas.ActualWidth;
            Global.windowHeight = containerCanvas.ActualHeight;

            if (DanceRoutine.saveAlreadyExists(songFilename))
            {
                routine = DanceRoutine.load(DanceRoutine.getSaveDestinationName(songFilename));
            }
            else
            {
                routine = new DanceRoutine(songFilename);
            }
            routine.deleteDanceSegmentAt(0);
            showRecordingCanvas();
            addButtonsToList();

            Canvas wfcanvas = new Canvas();
            wfcanvas.Width = 3600;
            wfcanvas.Height = 160;
            Canvas.SetTop(wfcanvas, 0);
            Canvas.SetLeft(wfcanvas, 0);
            waveButton.hoverCanvas.Children.Add(wfcanvas);
            waveButton.enableExpandAnimation = false;
            waveform = new Waveform(3600, 259, wfcanvas);
            renderSegments();
        }

        #region add buttons to list and update
        #region add buttons to list
        private void addButtonsToList()
        {
            addButtonToList(cancelActionButton, buttonList);
            addButtonToList(saveCommentButton, buttonList);
            addButtonToList(cancelCommentButton, buttonList);
            addButtonToList(recordSegmentButton, buttonList);
            addButtonToList(playSongSelectionButton, buttonList);
            addButtonToList(playSelectedSegmentButton, buttonList);
            addButtonToList(addCommentSegmentButton, buttonList);
            addButtonToList(deleteSegmentButton, buttonList);
            addButtonToList(cancelSegmentButton, buttonList);
            addButtonToList(backButton, buttonList);
            addButtonToList(waveButton, buttonList);
            addButtonToList(song1, buttonList);
            addButtonToList(songBeat, buttonList);
            addButtonToList(song3, buttonList);
            addButtonToList(song4, buttonList);
            addButtonToList(song5, buttonList);
        }

        private void addButtonToList(HoverButton button, LinkedList<HoverButton> list)
        {
            list.AddLast(button);
        }
        #endregion

        private void buttonUpdater(Joint handJoint)
        {
            hand.SetPosition(handJoint);

            foreach (HoverButton hb in buttonList)
            {
                if (blackBack.Visibility == Visibility.Visible)
                {
                    if (Canvas.GetZIndex(blackBack) >= Canvas.GetZIndex(hb) && Canvas.GetZIndex(blackBack) >= Canvas.GetZIndex((Canvas)hb.Parent))
                        continue;
                }
                hb.Check(hand);
            }


            foreach (HoverButton hb in segmentList)
            {
                hb.Check(hand);
            }
        }
        #endregion

        #region move canvas
        int canvasMovement = 0;
        /**
         * Moves canvas in direction indicated. Direction should be -1 or 1 for left or right respectively.
         * 
         **/
        private void moveCanvas(int direction)
        {
            var waveformTicker = new DispatcherTimer();
            waveformTicker.Tick += new EventHandler((object localsender, EventArgs locale) =>
            {
                if (canvasMovement <= 900)
                {
                    Debug.WriteLine("waveform is moving, so tick");
                    waveform.shiftCanvas(direction, timelineCanvas);
                    waveform.shiftCanvas(direction, segmentCanvas);
                    canvasMovement++;
                }
                else
                {
                    Debug.WriteLine("waveform stopped moving, so stop ticking");
                    canvasMovement = 0;
                    (localsender as DispatcherTimer).Stop();
                }
            });
        }
        #endregion

        #region waveform Click
        private void waveform_Clicked(object sender, EventArgs e)
        {
            HoverButton waveButton = (HoverButton)sender;
            Point handPosition = hand.TransformToAncestor(containerCanvas).Transform(new Point(0, 0));
            if (isSelectingStartSegment)
            {
                double handX = hand.TransformToAncestor(containerCanvas).Transform(new Point(0, 0)).X;
                handX = handX + hand.ActualWidth / 2;
                double timelineX = Canvas.GetLeft(timelineCanvas);
                double pixelsIntoWaveform = -1 * timelineX + handX;
                startSecondsIntoWaveform = (pixelsIntoWaveform - 8) / waveform.getPixelsPerSecond();
                isSelectingStartSegment = false;
                isSelectingEndSegment = true;
                makeSelectionPrompt.Visibility = Visibility.Collapsed;
                makeEndSelectionPrompt.Visibility = Visibility.Visible;
                waveform.selectStart(startSecondsIntoWaveform);
            }
            else if (isSelectingEndSegment)
            {
                double handX = hand.TransformToAncestor(containerCanvas).Transform(new Point(0, 0)).X;
                handX = handX + hand.ActualWidth / 2;
                double timelineX = Canvas.GetLeft(timelineCanvas);
                double pixelsIntoWaveform = -1 * timelineX + handX;
                endSecondsIntoWaveform = (pixelsIntoWaveform - 8) / waveform.getPixelsPerSecond();
                if (endSecondsIntoWaveform > startSecondsIntoWaveform)
                {
                    waveform.selectEnd(endSecondsIntoWaveform);
                }
                isSelectingEndSegment = false;


                if (isSelectingRecordSegment)
                {
                    Canvas.SetZIndex(timelineCanvas, oldButtonZIndex);
                    blackBack.Visibility = Visibility.Collapsed;
                    //bring up start recording dialog
                    isSelectingRecordSegment = false;
                    makeEndSelectionPrompt.Visibility = Visibility.Collapsed;
                    recordSegment();
                    cancelActionCanvas.Visibility = Visibility.Collapsed;
                }
                else if (isSelectingPlaySegment)
                {
                    Canvas.SetZIndex(timelineCanvas, oldButtonZIndex);
                    blackBack.Visibility = Visibility.Collapsed;
                    playSegment();
                    isSelectingPlaySegment = false;
                    makeEndSelectionPrompt.Visibility = Visibility.Collapsed;
                    cancelActionCanvas.Visibility = Visibility.Collapsed;
                }
                else if (isSelectingPlaySongSelection)
                {
                    Canvas.SetZIndex(timelineCanvas, oldButtonZIndex);
                    blackBack.Visibility = Visibility.Collapsed;
                    isSelectingPlaySongSelection = false;
                    makeEndSelectionPrompt.Visibility = Visibility.Collapsed;
                    cancelActionCanvas.Visibility = Visibility.Collapsed;
                    playSongSelection(startSecondsIntoWaveform, endSecondsIntoWaveform);
                }
            }
        }
        #endregion

        #region dance segment

        #region render dance segment
        private void renderSegments()
        {

            // first, remove everything from the segmentCanvas
            for (int elementIndex = segmentCanvas.Children.Count - 1; elementIndex >= 0; elementIndex--)
            {
                var child = segmentCanvas.Children[elementIndex];
                segmentCanvas.Children.Remove(child);
            }
            buttonSegments = new Dictionary<HoverButton, DanceSegment>();
            segmentList = new LinkedList<HoverButton>();
            foreach (int frame in routine.segments.Keys)
            {

                renderSegment(frame);
            }
        }

        private void renderSegment(int frame)
        {
            double pos;
            DanceSegment segment;
            if (routine.segments.TryGetValue(frame, out segment))
            {
                if (segment == null || segment.length == 0) return;
                pos = frame / 30 * waveform.getPixelsPerSecond();
                HoverButton hb = new HoverButton();
                var img = new System.Windows.Controls.Image();
                img.Width = segment.length / 30 * waveform.getPixelsPerSecond() * 1 / 3;
                img.Source = segment.getFrameSource(0);
                hb.leftImageName.Source = img.Source;
                var img2 = new System.Windows.Controls.Image();
                img2.Width = segment.length / 30 * waveform.getPixelsPerSecond() * 1 / 3;
                img2.Source = segment.getFrameSource(segment.length - 1);
                hb.rightImageName.Source = img2.Source;
                hb.dotDot.Visibility = Visibility.Visible;
                hb.Height = 160;
                hb.Width = (segment.length / 30 * waveform.getPixelsPerSecond());
                hb.BackgroundColor = Brushes.LightYellow;
                hb.BorderBrush = Brushes.DarkGray;
                hb.BorderThickness = new Thickness(2);
                segmentCanvas.Children.Add(hb);
                Canvas.SetTop(hb, 0);
                Canvas.SetLeft(hb, pos);
                hb.Click += new HoverButton.ClickHandler(segment_Clicked);
                segmentList.AddLast(hb);

                buttonSegments.Add(hb, segment);
                renderComment(segment);
            }
            
        }

        private void renderComment(DanceSegment segment)
        {
            double pos;
            String comment;
            int frame = 0;
            if (routine.comments.TryGetValue(segment, out comment))
            {
                if (comment == null) return;
                Debug.WriteLine(comment);
                foreach (KeyValuePair<int, DanceSegment> kvp in routine.segments)
                {
                    if (kvp.Value == selectedSegment)
                    {
                        frame = kvp.Key;
                        break;
                    }
                }
                pos = (frame + (segment.length/2)) / 30 * waveform.getPixelsPerSecond();
                Image cImg = new Image
                {
                    Height = 160,
                    Width = 40,
                };
                cImg.Source = new BitmapImage(new Uri(@"pack://application:,,,/Choreoh;component/img/waveform/speech_bubble.png", UriKind.RelativeOrAbsolute));
                HoverButton commentImg = new HoverButton
                {
                    Height = 50,
                    Width = 50,
                    Visibility = Visibility.Visible,
                    BackgroundColor = Brushes.LavenderBlush
                };
                commentImg.leftImageName.Source = cImg.Source;
                segmentCanvas.Children.Add(commentImg);
                Canvas.SetTop(commentImg, 0);
                Canvas.SetLeft(commentImg, pos);
                Canvas.SetZIndex(commentImg, 100);
                segmentList.AddLast(commentImg);
            }
        }
        #endregion

        #region segment Click
        private void segment_Clicked(object sender, EventArgs e)
        {
            if (sender.ToString() == "Choreoh.HoverButton")
            {
                HoverButton segmentButton = (HoverButton)sender;
                buttonSegments.TryGetValue(segmentButton, out selectedSegment);
                Point handPosition = hand.TransformToAncestor(containerCanvas).Transform(new Point(0, 0));
                handPointX = handPosition.X + hand.ActualWidth / 2;
                timelineMenuOpenedPosition = handPosition;
                blackBack.Visibility = Visibility.Visible;
                oldButtonZIndex = Canvas.GetZIndex(segmentButtonCanvas);
                Canvas.SetZIndex(segmentButtonCanvas, Canvas.GetZIndex(blackBack) + 1);
                Canvas.SetZIndex(playSelectedSegmentButton, Canvas.GetZIndex(blackBack) + 1);
                oldDanceSegmentIndex = Canvas.GetZIndex((HoverButton)sender);
                Canvas.SetZIndex(segmentCanvas, Canvas.GetZIndex(blackBack) + 1);
                onlyShowThisSegment((HoverButton)sender);
                segmentButtonCanvas.Visibility = Visibility.Visible;
            }
            else
            {
                debug.Text = "I have made a huge mistake";
            }
        }
        #endregion

        #region showing and hiding segment helpers
        private void onlyShowThisSegment(HoverButton danceSegment)
        {
            foreach (HoverButton hb in segmentList)
            {
                if (hb != danceSegment)
                    hb.Visibility = Visibility.Hidden;
            }
        }
        private void showAllSegments()
        {
            foreach (HoverButton hb in segmentList)
            {
                hb.Visibility = Visibility.Visible;
            }
        }
        private void fixSegmentIndices()
        {
            Canvas.SetZIndex(segmentButtonCanvas, oldButtonZIndex);
            segmentButtonCanvas.Visibility = Visibility.Collapsed;
            Canvas.SetZIndex(segmentCanvas, oldDanceSegmentIndex);
            showAllSegments();
            blackBack.Visibility = Visibility.Collapsed;
            segmentButtonCanvas.Visibility = Visibility.Collapsed;
        }
        #endregion

        #region play comment delete cancel segment buttons
        private void playSelectedSegmentButton_Clicked(object sender, EventArgs e)
        {
            blackBack.Visibility = Visibility.Collapsed;
            playSegment();
            fixSegmentIndices();
        }
        private void addCommentSegmentButton_Clicked(object sender, EventArgs e)
        {
            commentSegment();
            Canvas.SetZIndex(segmentButtonCanvas, oldButtonZIndex);
            segmentButtonCanvas.Visibility = Visibility.Hidden;
            oldCommentBoxIndex = Canvas.GetZIndex(commentBox);
            Canvas.SetZIndex(commentBox, Canvas.GetZIndex(blackBack) + 1);
            //fixSegmentIndices();
            Canvas.SetZIndex(commentButtonCanvas, Canvas.GetZIndex(blackBack) + 1);
            commentButtonCanvas.Visibility = Visibility.Visible;
        }
        private void deleteSegmentButton_Clicked(object sender, EventArgs e)
        {
            deleteSegment();
            fixSegmentIndices();
        }
        private void cancelSegmentButton_Clicked(object sender, EventArgs e)
        {
            cancelActionButton_Clicked(sender, e);
            waveform.selectStart(0);
            waveform.selectEnd(1);
            waveform.deselectSegment();
            fixSegmentIndices();
        }


        #endregion

        #region play record delete comment segment actions

        private void commentSegment()
        {
            double handX = timelineMenuOpenedPosition.X;
            handX = handX + hand.ActualWidth / 2;

            Point handPosition = hand.TransformToAncestor(containerCanvas).Transform(new Point(0, 0));
            handPointX = handPosition.X + hand.ActualWidth / 2;
            timelineMenuOpenedPosition = handPosition;

            commentBox.Visibility = Visibility.Visible;
            annotating = true;
            commentToSave = comment;
        }

        private void deleteSegment()
        {
            if (selectedSegment != null)
            {
                blackBack.Visibility = Visibility.Collapsed;
                routine.deleteDanceSegment(selectedSegment);
                selectedSegment = null;
                routine.save();
                renderSegments();
            }
        }

        private void recordSegment()
        {
            pre_recording = true;
            blackBack.Visibility = Visibility.Visible;
            beforeRecordCanvas.Visibility = Visibility.Visible;
            mainCanvas.MouseUp += new MouseButtonEventHandler(sre_PreSpeechRecognized_Start_Recognized);
        }

        #region playSongSelection
        private void playSongSelection(double secondsStart, double secondsEnd)
        {
            TimeSpan startTime = new TimeSpan((int)(secondsStart * 10000000));
            TimeSpan durationTime = new TimeSpan((int)(secondsEnd * 10000000)) - startTime;

            waveform.selectStart(secondsStart);
            waveform.selectEnd(secondsEnd);

            var waveformTicker = new DispatcherTimer();
            waveformTicker.Tick += new EventHandler((object localsender, EventArgs locale) =>
            {
                if (waveform.isPlaying())
                {
                    Debug.WriteLine("waveform is playing, so tick");
                    waveform.movePlay();
                }
                else
                {
                    Debug.WriteLine("waveform stopped playing, so stop ticking");
                    isPlaying = false;
                    waveform.endPlay();
                    hideMode();
                    waveform.deselectSegment();
                    (localsender as DispatcherTimer).Stop();
                }
            });
            double secondsPerPixel = 1 / waveform.getPixelsPerSecond();
            double nanoseconds = secondsPerPixel * 1000000000;
            int ticks = (int)(nanoseconds / 100);
            Debug.WriteLine("Ticks: " + ticks);
            waveformTicker.Interval = new TimeSpan(ticks);

            AudioPlay.playForDuration(mainCanvas, songFilename, startTime, durationTime);
            waveformTicker.Start();
            waveform.startPlay();
            switchModeToPlayback();
            isPlaying = true;
        }
        #endregion 

        #region playSegment
        private void playSegment()
        {
            // play the segment
            if (isPlaying)
            {
                // for the weird double-clicking issues...
                return;
            }
            //darken background so user's attention brough to video
            var videoPlayerTimer = new DispatcherTimer();
            int videoCounter = 0;

            videoPlaybackCanvas.Visibility = Visibility.Visible;
            Image img = new System.Windows.Controls.Image();

            videoPlaybackCanvas.Children.Add(img);

            Canvas.SetTop(img, 0);
            Canvas.SetLeft(img, 0);
            videoPlayerTimer.Tick += new EventHandler((object localsender, EventArgs locale) =>
            {
                if (videoCounter >= selectedSegment.length)
                {
                    videoPlaybackCanvas.Visibility = Visibility.Collapsed;
                    (localsender as DispatcherTimer).Stop();
                    return;
                }

                img.Source = selectedSegment.getFrameSource(videoCounter);


                videoCounter++;
            });
            videoPlayerTimer.Interval = new TimeSpan((int)((1.0 / 30) * (1000000000 / 100)));
            Debug.WriteLine("Videplayer Tick Interval is " + videoPlayerTimer.Interval.TotalMilliseconds + " milliseconds");


            int frameOfSegmentStart = 0;
            foreach (KeyValuePair<int, DanceSegment> kvp in routine.segments)
            {
                if (kvp.Value == selectedSegment)
                {
                    frameOfSegmentStart = kvp.Key;
                    break;
                }
            }
            int frameOfSegmentEnd = frameOfSegmentStart + selectedSegment.length;

            TimeSpan startTime = new TimeSpan(0, 0, (int)(frameOfSegmentStart / 30.0));
            TimeSpan durationTime = new TimeSpan(0, 0, (int)((frameOfSegmentEnd - frameOfSegmentStart) / 30.0));

            waveform.selectStart(frameOfSegmentStart / 30);
            waveform.selectEnd(frameOfSegmentEnd / 30);

            var waveformTicker = new DispatcherTimer();
            waveformTicker.Tick += new EventHandler((object localsender, EventArgs locale) =>
            {
                if (waveform.isPlaying())
                {
                    Debug.WriteLine("waveform is playing, so tick");
                    waveform.movePlay();
                }
                else
                {
                    Debug.WriteLine("waveform stopped playing, so stop ticking");
                    (localsender as DispatcherTimer).Stop();
                }
            });
            double secondsPerPixel = 1 / waveform.getPixelsPerSecond();
            double nanoseconds = secondsPerPixel * 1000000000;
            int ticks = (int)(nanoseconds / 100);
            Debug.WriteLine("Ticks: " + ticks);
            waveformTicker.Interval = new TimeSpan(ticks);

            var playbackTimer = new DispatcherTimer();
            playbackTimer.Tick += new EventHandler((object localsender, EventArgs locale) =>
            {
                (localsender as DispatcherTimer).Stop();
                videoPlayerTimer.Stop();
                waveformTicker.Stop();
                waveform.endPlay();
                waveform.deselectSegment();
                hideMode();
                isPlaying = false;
                videoPlaybackCanvas.Visibility = Visibility.Collapsed;
            });
            playbackTimer.Interval = durationTime;

            AudioPlay.playForDuration(mainCanvas, songFilename, startTime, durationTime);
            videoPlayerTimer.Start();
            waveformTicker.Start();
            waveform.startPlay();
            playbackTimer.Start();
            switchModeToPlayback();
            isPlaying = true;
        }
        #endregion
        #endregion
        #endregion

        #region kinect AllFramesReady
        void newSensor_AllFramesReady(object sender, AllFramesReadyEventArgs e)
        {
            if (isRecording)
            {
                return;
            }

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

                    //if (blackBack.Visibility != Visibility.Visible)
                    //{
                    hand.checkGestures(moves);
                    //}
                    buttonUpdater(handJoint);
                    if (!Global.initPosOverlay)
                    {
                        initOverlay.Visibility = Visibility.Collapsed;

                    }
                    else
                    {
                        initOverlay.Visibility = Visibility.Visible;
                    }
                    //temporary to clear gestureText

                }
            }

        }
        #endregion

        #region main page navigation
        private void back_Clicked(object sender, EventArgs e)
        {
            homeCanvas.Visibility = Visibility.Visible;
            mainCanvas.Visibility = Visibility.Collapsed;
        }

        private void RecordSegmentButton_Click(object sender, EventArgs e)
        {
            isSelectingRecordSegment = true;
            isSelectingStartSegment = true;
            blackBack.Visibility = Visibility.Visible;
            oldButtonZIndex = Canvas.GetZIndex(timelineCanvas);
            Canvas.SetZIndex(timelineCanvas, Canvas.GetZIndex(blackBack) + 1);
            makeSelectionPrompt.Visibility = Visibility.Visible;
            Canvas.SetZIndex(cancelActionCanvas, Canvas.GetZIndex(blackBack) + 1);
            cancelActionCanvas.Visibility = Visibility.Visible;
        }

        private void PlaySongSelectionButton_Click(object sender, EventArgs e)
        {
            isSelectingPlaySongSelection = true;
            isSelectingStartSegment = true;
            blackBack.Visibility = Visibility.Visible;
            oldButtonZIndex = Canvas.GetZIndex(timelineCanvas);
            Canvas.SetZIndex(timelineCanvas, Canvas.GetZIndex(blackBack) + 1);
            makeSelectionPrompt.Visibility = Visibility.Visible;
            Canvas.SetZIndex(cancelActionCanvas, Canvas.GetZIndex(blackBack) + 1);
            cancelActionCanvas.Visibility = Visibility.Visible;
        }

        #endregion

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
            this.preSpeechRecognizer = this.CreateSpeechRecognizerPreRecording();
            this.postSpeechRecognizer = this.CreateSpeechRecognizerPostRecording();

            try
            {
                newSensor.Start();
            }
            catch (System.IO.IOException)
            {
                kinectSensorChooser1.AppConflictOccurred();
            }


            if (sensor != null)
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

                    if (sensor != null)
                    {
                        sensor.AudioSource.Stop();
                        sensor.Stop();
                        this.preSpeechRecognizer.RecognizeAsyncCancel();
                        this.preSpeechRecognizer.RecognizeAsyncStop();
                        this.postSpeechRecognizer.RecognizeAsyncCancel();
                        this.postSpeechRecognizer.RecognizeAsyncStop();
                    }
                }
            }
        }
        #endregion

        private void hand_GestureEvent(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            debug.Text = debug.Text + "gesture " + Global.lastGesture;
        }

        #region recording crap
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
            isRecording = true;

            sensor.AllFramesReady += newSensor_AllFramesReady_Record;
        }

        public void StopRecording()
        {
            Debug.WriteLine("Set recording canvas's segment to null");
            //post_recording = true;
            isRecording = false;

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
            int width = (int)recordingCanvas.ActualWidth;
            int height = (int)recordingCanvas.ActualHeight;
            RenderTargetBitmap renderBitmap = new RenderTargetBitmap(width > 0 ? width : 680, height > 0 ? height : 480, 96d, 96d, PixelFormats.Pbgra32);
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
        #endregion


        #region comment buttons

        private void cancelCommentButton_Clicked(object sender, EventArgs e)
        {
            cancelActionButton_Clicked(sender, e);
            waveform.deselectSegment();
            commentButtonCanvas.Visibility = Visibility.Collapsed;
            blackBack.Visibility = Visibility.Collapsed;
            commentBox.Text = "";
            commentBox.Visibility = Visibility.Collapsed;
            showAllSegments();
        }
        private void saveCommentButton_Clicked(object sender, EventArgs e)
        {
            double handX = timelineMenuOpenedPosition.X;
            handX = handX + hand.ActualWidth / 2;

            blackBack.Visibility = Visibility.Collapsed;

            int pos = (int)((handPointX + waveform.getOffset()) / waveform.getPixelsPerSecond() * 30);
            routine.addComment(selectedSegment, commentToSave);
            commentToSave = "";
            comment = "";
            annotating = false;
            commentBox.Text = "";
            commentBox.Visibility = Visibility.Hidden;
            renderComment(selectedSegment);
            waveform.deselectSegment();
            commentButtonCanvas.Visibility = Visibility.Collapsed;
            showAllSegments();
        }

        #endregion

        #region audio config and control

        private DispatcherTimer readyTimer;

        private SpeechRecognitionEngine preSpeechRecognizer;
        private SpeechRecognitionEngine postSpeechRecognizer;
        private String comment;

        private bool pre_recording = false;
        private bool post_recording = false;
        private bool annotating = false;

        KinectAudioSource source;
        System.Speech.Recognition.RecognizerInfo ri;
        System.Speech.Recognition.SpeechRecognitionEngine recoEngine;
        System.Speech.Recognition.DictationGrammar customDictationGrammar;
        Stream s;



        private void startAudio()
        {
            var audioSource = this.sensor.AudioSource;
            audioSource.BeamAngleMode = BeamAngleMode.Adaptive;

            // This should be off by default, but just to be explicit, this MUST be set to false.
            audioSource.AutomaticGainControlEnabled = false;
            var kinectStream = audioSource.Start();

            this.preSpeechRecognizer.SetInputToAudioStream(
                kinectStream, new SpeechAudioFormatInfo(EncodingFormat.Pcm, 16000, 16, 1, 32000, 2, null));
            // Keep recognizing speech until window closes
            this.preSpeechRecognizer.RecognizeAsync(RecognizeMode.Multiple);
            this.postSpeechRecognizer.SetInputToAudioStream(
                kinectStream, new SpeechAudioFormatInfo(EncodingFormat.Pcm, 16000, 16, 1, 32000, 2, null));
            // Keep recognizing speech until window closes
            this.postSpeechRecognizer.RecognizeAsync(RecognizeMode.Multiple);

            sensor = (from sensorToCheck in KinectSensor.KinectSensors where sensorToCheck.Status == KinectStatus.Connected select sensorToCheck).FirstOrDefault();
            sensor.Start();

            source = sensor.AudioSource;
            source.EchoCancellationMode = EchoCancellationMode.None; // No AEC for this sample
            source.AutomaticGainControlEnabled = false; // Important to turn this off for speech recognition

            ri = System.Speech.Recognition.SpeechRecognitionEngine.InstalledRecognizers().FirstOrDefault();

            recoEngine = new System.Speech.Recognition.SpeechRecognitionEngine(ri.Id);

            customDictationGrammar = new System.Speech.Recognition.DictationGrammar();
            customDictationGrammar.Name = "Dictation";
            customDictationGrammar.Enabled = true;

            recoEngine.LoadGrammar(customDictationGrammar);

            recoEngine.SpeechRecognized += new EventHandler<System.Speech.Recognition.SpeechRecognizedEventArgs>(recoEngine_SpeechRecognized);

            s = source.Start();
            recoEngine.SetInputToAudioStream(s, new System.Speech.AudioFormat.SpeechAudioFormatInfo(System.Speech.AudioFormat.EncodingFormat.Pcm, 16000, 16, 1, 32000, 2, null));
            recoEngine.RecognizeAsync(System.Speech.Recognition.RecognizeMode.Multiple);
        }


        #region Speech recognizer setup
        private static RecognizerInfo GetKinectRecognizer()
        {
            var ri = System.Speech.Recognition.SpeechRecognitionEngine.InstalledRecognizers().FirstOrDefault();
            return ri;
        }


        private SpeechRecognitionEngine CreateSpeechRecognizerPreRecording()
        {
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

                var preRecordingChoices = new Choices(new string[] { "start" });

                var gb = new GrammarBuilder { Culture = ri.Culture };
                gb.Append(preRecordingChoices);

                // Create the actual Grammar instance, and then load it into the speech recognizer.
                var g = new Grammar(gb);

                sre.LoadGrammar(g);

                #endregion

                #region Hook up events
                sre.SpeechRecognized += new EventHandler<SpeechRecognizedEventArgs>(sre_PreSpeechRecognized);

                #endregion

                return sre;
            }
        }

        private SpeechRecognitionEngine CreateSpeechRecognizerPostRecording()
        {
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

                var postRecordingChoices = new Choices(new string[] { "save", "cancel", "redo", "play" });


                var gb = new GrammarBuilder { Culture = ri.Culture };
                gb.Append(postRecordingChoices);
                // Create the actual Grammar instance, and then load it into the speech recognizer.
                var g = new Grammar(gb);

                sre.LoadGrammar(g);

                #endregion

                #region Hook up events
                sre.SpeechRecognized += new EventHandler<SpeechRecognizedEventArgs>(sre_PostSpeechRecognized);

                #endregion

                return sre;
            }
        }

        #endregion

        #region Speech recognition events

        private void sre_PreSpeechRecognized_Start_Recognized(object sender, EventArgs e)
        {
            mainCanvas.MouseUp -= new MouseButtonEventHandler(sre_PreSpeechRecognized_Start_Recognized);
            int startOfSegment = 0;
            //start_label.Visibility = Visibility.Visible;
            blackBack.Visibility = Visibility.Collapsed;
            beforeRecordCanvas.Visibility = Visibility.Collapsed;
            pre_recording = false;

            showRecordingCanvas();
            switchModeToRecording();

            double duration = endSecondsIntoWaveform - startSecondsIntoWaveform;

            TimeSpan startTime = new TimeSpan(0, 0, (int)startSecondsIntoWaveform);
            TimeSpan durationTime = new TimeSpan((int)(duration * 1000000000 / 100));

            Debug.WriteLine("Start Time: " + startTime.ToString());
            Debug.WriteLine("Duration Time: " + durationTime.ToString());

            var waveformTicker = new DispatcherTimer();
            waveformTicker.Tick += new EventHandler((object localsender, EventArgs locale) =>
            {
                if (waveform.isPlaying())
                {
                    Debug.WriteLine("waveform is playing, so tick");
                    waveform.movePlay();
                }
                else
                {
                    Debug.WriteLine("waveform stopped playing, so stop ticking");
                    (localsender as DispatcherTimer).Stop();
                }
            });
            double secondsPerPixel = 1 / waveform.getPixelsPerSecond();
            double nanoseconds = secondsPerPixel * 1000000000;
            int ticks = (int)nanoseconds / 100;
            Debug.WriteLine("Ticks: " + ticks);
            waveformTicker.Interval = new TimeSpan(ticks);


            var recordingTimer = new DispatcherTimer();
            recordingTimer.Tick += new EventHandler((object localsender, EventArgs locale) =>
            {
                waveform.endPlay();
                waveformTicker.Stop();
                StopRecording();
                (localsender as DispatcherTimer).Stop();
                post_recording = true;
                blackBack.Visibility = Visibility.Visible;
                afterRecordCanvas.Visibility = Visibility.Visible;
                mainCanvas.MouseUp += new MouseButtonEventHandler(sre_PostSpeechRecognized_Save_Recognized);
                switchModeToPlayback();
                renderSegment(startOfSegment);
            });
            recordingTimer.Interval = durationTime;


            AudioPlay.playForDuration(mainCanvas, songFilename, startTime, durationTime);
            waveformTicker.Start();
            waveform.startPlay();
            startOfSegment = (int)(startTime.TotalSeconds * 30);
            DanceSegment segment = routine.addDanceSegment(startOfSegment);
            StartRecording(segment);
            recordingTimer.Start();

            return;
        }
        void sre_PreSpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            if (pre_recording)
            {
                Debug.WriteLine("Pre-recording Speech detected: " + e.Result.Text.ToString());
                switch (e.Result.Text.ToString().ToUpperInvariant())
                {
                    case "START":
                        sre_PreSpeechRecognized_Start_Recognized(sender, e);
                        return;
                    default:
                        return;
                }
            }
        }

        private void sre_PostSpeechRecognized_Save_Recognized(object sender, EventArgs e)
        {
            mainCanvas.MouseUp -= new MouseButtonEventHandler(sre_PostSpeechRecognized_Save_Recognized);
            int startOfSegment = 0;
            hideMode();
            waveform.deselectSegment();
            post_recording = false;
            blackBack.Visibility = Visibility.Collapsed;
            afterRecordCanvas.Visibility = Visibility.Collapsed;
            routine.save();
            renderSegment(startOfSegment);
            return;
        }
        void sre_PostSpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            if (post_recording)
            {
                Debug.WriteLine("Post-recording Speech detected: " + e.Result.Text.ToString());
                switch (e.Result.Text.ToString().ToUpperInvariant())
                {
                    default:
                    case "SAVE":
                        //keep_label.Visibility = Visibility.Visible;
                        sre_PostSpeechRecognized_Save_Recognized(sender, e);
                        return;
                    case "CANCEL":
                        //cancel_label.Visibility = Visibility.Visible;
                        post_recording = false;
                        blackBack.Visibility = Visibility.Collapsed;
                        afterRecordCanvas.Visibility = Visibility.Collapsed;
                        waveform.deselectSegment();
                        routine.deleteDanceSegment(segmentToRecordTo);
                        return;
                    case "REDO":
                        //redo_label.Visibility = Visibility.Visible;
                        post_recording = false;
                        blackBack.Visibility = Visibility.Collapsed;
                        afterRecordCanvas.Visibility = Visibility.Collapsed;
                        return;
                    case "PLAY":
                        //play_label.Visibility = Visibility.Visible;
                        post_recording = false;
                        blackBack.Visibility = Visibility.Collapsed;
                        afterRecordCanvas.Visibility = Visibility.Collapsed;
                        return;
                    //default:
                    //   return;
                }
            }
        }

        void recoEngine_SpeechRecognized(object sender, System.Speech.Recognition.SpeechRecognizedEventArgs e)
        {
            var alternates = e.Result.Alternates;
            String alternates_string = "";
            String[] alternates_string_array = new String[alternates.Count];
            string[] isolate_result_array;

            foreach (RecognizedPhrase i in alternates)
            {
                int j = 0;
                alternates_string = alternates_string + " " + i.Text.ToString();
                alternates_string_array[j] = alternates_string + " " + i.Text.ToString();
                j++;
            }

            string[] recognizableCommands = { "START", "SAVE", "REDO", "CANCEL", "PLAY" };

            string recognizedResult = e.Result.Text.ToString().Trim();
            string recognizedResultUpper = recognizedResult.ToUpperInvariant();
            bool found = false;

            //isolates key word from multi word recognition.
            if (recognizedResultUpper.Contains(' '))
            {
                isolate_result_array = recognizedResult.Split(' ');

                for (int i = 0; i < isolate_result_array.Length; i++)
                {
                    for (int j = 0; j < recognizableCommands.Length; j++)
                        if (isolate_result_array[i].Equals(recognizableCommands[j]))
                        {
                            recognizedResultUpper = recognizableCommands[j];
                            found = true;
                            break;
                        }
                    if (found)
                    {
                        break;
                    }
                }
            }




            //int startOfSegment = 0;
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
            if (pre_recording)
            {
                Debug.WriteLine("Pre-recording Speech detected: " + e.Result.Text.ToString());
                int startOfSegment = 0;
                switch (recognizedResultUpper)
                {
                    case "START":
                    case "STARTS":
                    case "STARTED":
                    case "TARTS":
                    case "DART":
                    case "TART":
                    case "CART":
                    case "HEART":
                    case "PART":
                    case "RESTART":
                    case "UPSTART":
                        //start_label.Visibility = Visibility.Visible;
                        blackBack.Visibility = Visibility.Collapsed;
                        beforeRecordCanvas.Visibility = Visibility.Collapsed;
                        pre_recording = false;

                        showRecordingCanvas();
                        switchModeToRecording();

                        double duration = endSecondsIntoWaveform - startSecondsIntoWaveform;

                        TimeSpan startTime = new TimeSpan(0, 0, (int)startSecondsIntoWaveform);
                        TimeSpan durationTime = new TimeSpan(0, 0, (int)duration);

                        Debug.WriteLine("Start Time: " + startTime.ToString());
                        Debug.WriteLine("Duration Time: " + durationTime.ToString());

                        var waveformTicker = new DispatcherTimer();
                        waveformTicker.Tick += new EventHandler((object localsender, EventArgs locale) =>
                        {
                            if (waveform.isPlaying())
                            {
                                Debug.WriteLine("waveform is playing, so tick");
                                waveform.movePlay();
                            }
                            else
                            {
                                Debug.WriteLine("waveform stopped playing, so stop ticking");
                                (localsender as DispatcherTimer).Stop();
                            }
                        });
                        double secondsPerPixel = 1 / waveform.getPixelsPerSecond();
                        double nanoseconds = secondsPerPixel * 1000000000;
                        int ticks = (int)nanoseconds / 100;
                        Debug.WriteLine("Ticks: " + ticks);
                        waveformTicker.Interval = new TimeSpan(ticks);


                        var recordingTimer = new DispatcherTimer();
                        recordingTimer.Tick += new EventHandler((object localsender, EventArgs locale) =>
                        {
                            waveform.endPlay();
                            waveformTicker.Stop();
                            StopRecording();
                            (localsender as DispatcherTimer).Stop();
                            post_recording = true;
                            blackBack.Visibility = Visibility.Visible;
                            afterRecordCanvas.Visibility = Visibility.Visible;
                            switchModeToPlayback();
                            renderSegment(startOfSegment);
                        });
                        recordingTimer.Interval = durationTime;


                        AudioPlay.playForDuration(mainCanvas, songFilename, startTime, durationTime);
                        waveformTicker.Start();
                        waveform.startPlay();
                        startOfSegment = (int)(startTime.TotalSeconds * 30);
                        DanceSegment segment = routine.addDanceSegment(startOfSegment);
                        StartRecording(segment);
                        recordingTimer.Start();
                        return;
                    default:
                        return;
                }
            }
            if (post_recording)
            {
                Debug.WriteLine("Post-recording Speech detected: " + e.Result.Text.ToString());
                int startOfSegment = 0;
                switch (recognizedResultUpper)
                {
                    case "SAVE":
                    case "SAVES":
                    case "SAVED":
                    case "SAVING":
                    case "SLAVE":
                    case "WAVE":
                    case "STAY":
                    case "SAFE":
                    case "SHAVE":
                    case "STATE":
                        //keep_label.Visibility = Visibility.Visible;
                        hideMode();
                        waveform.deselectSegment();
                        post_recording = false;
                        blackBack.Visibility = Visibility.Collapsed;
                        afterRecordCanvas.Visibility = Visibility.Collapsed;
                        routine.save();
                        renderSegment(startOfSegment);
                        return;

                    case "CANCEL":
                    case "CANCELED":
                    case "CANCELS":
                    case "CHANCEL":
                    case "CHANCELLOR":
                    case "CATTLE":
                    case "SCANDAL":
                    case "COUNCIL":
                    case "CANCER":
                        //cancel_label.Visibility = Visibility.Visible;
                        post_recording = false;
                        blackBack.Visibility = Visibility.Collapsed;
                        afterRecordCanvas.Visibility = Visibility.Collapsed;
                        waveform.deselectSegment();
                        routine.deleteDanceSegment(segmentToRecordTo);
                        return;

                    case "REDO":
                    case "REDOES":
                    case "REVIEW":
                    case "ACCRUE":
                    case "ASKEW":
                    case "OUTDO":
                    case "RENEW":
                    case "UNDO":
                    case "CANOE":
                    case "IMBUE":
                        //redo_label.Visibility = Visibility.Visible;
                        post_recording = false;
                        blackBack.Visibility = Visibility.Collapsed;
                        afterRecordCanvas.Visibility = Visibility.Collapsed;
                        return;

                    case "PLAY":
                    case "PLAYS":
                    case "PLAYED":
                    case "BAY":
                    case "A":
                    case "PAY":
                    case "DISPLAY":
                    case "LAY":
                    case "DELAY":
                        //play_label.Visibility = Visibility.Visible;
                        post_recording = false;
                        blackBack.Visibility = Visibility.Collapsed;
                        afterRecordCanvas.Visibility = Visibility.Collapsed;
                        return;
                    //default:
                    //   return;
                }
            }
        }

        private void RecognizeSpeech(RecognitionResult result)
        {
            //string status = "Recognzied: " + (result == null ? string.Empty : result.Text + " " + result.Confidence);
            //this.ReportStatus(status);
            string newText = result.Text.ToString();
            this.UpdateText(newText);
            this.comment = this.comment + " " + newText;
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

        #endregion

        #region homescreen
        private void songBeat_Click(object sender, EventArgs e)
        {
            homeCanvas.Visibility = Visibility.Collapsed;
            mainCanvas.Visibility = Visibility.Visible;
        }
        #endregion

        #region mode switching
        private void switchModeToRecording()
        {
            playbackMode.Visibility = Visibility.Collapsed;
            recordMode.Visibility = Visibility.Visible;
        }
        private void switchModeToPlayback()
        {

            recordMode.Visibility = Visibility.Collapsed;
            playbackMode.Visibility = Visibility.Visible;
        }

        private void hideMode()
        {
            recordMode.Visibility = Visibility.Collapsed;
            playbackMode.Visibility = Visibility.Collapsed;
        }

        #endregion

        #region UI update functions
        private void ReportStatus(string status)
        {
            //Dispatcher.BeginInvoke(new Action(() => { statusLabel.Content = status; }), DispatcherPriority.Normal);
        }
        private void UpdateText(string newText)
        {
            Dispatcher.BeginInvoke(new Action(() => { commentBox.Text = commentBox.Text + " " + newText; }), DispatcherPriority.Normal);
        }
        #endregion

        private void cancelActionButton_Clicked(object sender, EventArgs e)
        {
            blackBack.Visibility = Visibility.Collapsed;
            makeEndSelectionPrompt.Visibility = Visibility.Hidden;
            makeSelectionPrompt.Visibility = Visibility.Hidden;
            showAllSegments();
            cancelActionCanvas.Visibility = Visibility.Collapsed;
            Canvas.SetZIndex(timelineCanvas, oldButtonZIndex);

            isSelectingEndSegment = false;
            isSelectingPlaySegment = false;
            isSelectingPlaySongSelection = false;
            isSelectingRecordSegment = false;
            isSelectingStartSegment = false;
            waveform.selectStart(0);
            waveform.selectEnd(1);
            waveform.deselectSegment();
        }

    }
}
        