using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Kinect;

namespace Choreoh
{
    public class ButtonAnimate: DoubleAnimationUsingKeyFrames
    {
        private DoubleAnimationUsingKeyFrames growImageX;
        private DoubleAnimationUsingKeyFrames growImageY;
        private DoubleAnimationUsingKeyFrames killImageX;
        private DoubleAnimationUsingKeyFrames killImageY;
        private double currentVal = 1.0;
        public event EventHandler CompleteChanged;
        private ScaleTransform RectXForm = new ScaleTransform();
        private ScaleTransform RectYForm = new ScaleTransform();
        private double ratioToGrow = 1.05;
        private int expandMilli = 300;
        private int contractMilli = 200;

        public ButtonAnimate()
        {
            growImageX = new DoubleAnimationUsingKeyFrames();
            growImageY = new DoubleAnimationUsingKeyFrames();
            killImageX = new DoubleAnimationUsingKeyFrames();
            killImageY = new DoubleAnimationUsingKeyFrames();
            RectXForm.Changed += new EventHandler(RectXForm_Changed);
        }

        public void expand(UserControl border)
        {
            //Initialization
            HoverButton button = (HoverButton)border;
            SolidColorBrush scb = new SolidColorBrush();
            scb.Color = Color.FromArgb(50, 255, 255, 0);
            button.HoverOverlay = scb;
            growImageX.KeyFrames.Add(new LinearDoubleKeyFrame(1, KeyTime.FromPercent(0)));
            growImageX.KeyFrames.Add(new LinearDoubleKeyFrame(ratioToGrow, KeyTime.FromPercent(1)));
            growImageX.Duration = new Duration(TimeSpan.FromMilliseconds(expandMilli));

            border.RenderTransform = RectXForm;
            growImageY.KeyFrames.Add(new LinearDoubleKeyFrame(1, KeyTime.FromPercent(0)));
            growImageY.KeyFrames.Add(new LinearDoubleKeyFrame(ratioToGrow, KeyTime.FromPercent(1)));
            growImageY.Duration = new Duration(TimeSpan.FromMilliseconds(expandMilli));

            border.RenderTransform = RectXForm;


            //Execution
            RectXForm.BeginAnimation(ScaleTransform.ScaleXProperty, growImageX);
            RectXForm.BeginAnimation(ScaleTransform.ScaleYProperty, growImageY);

        }


        //Update current transform value so if doesn't fully expand it won't look jerky <--yum
        private void RectXForm_Changed(object sender, EventArgs e)
        {
            ScaleTransform transformer = (ScaleTransform)sender;
            this.Val = transformer.ScaleX;
        }

        public void contract(UserControl border)
        {
            //Initialization
            HoverButton button = (HoverButton)border;
            SolidColorBrush scb = new SolidColorBrush();
            scb.Color = Color.FromArgb(0, 0, 0, 0);
            button.HoverOverlay = scb;
            killImageX.KeyFrames.Add(new LinearDoubleKeyFrame(1, KeyTime.FromPercent(.5)));
            killImageX.KeyFrames.Add(new LinearDoubleKeyFrame(currentVal, KeyTime.FromPercent(0)));
            killImageX.Duration = new Duration(TimeSpan.FromMilliseconds(contractMilli));
            ScaleTransform RectXForm = new ScaleTransform();
            border.RenderTransform = RectXForm;
            killImageY.KeyFrames.Add(new LinearDoubleKeyFrame(1, KeyTime.FromPercent(.5)));
            killImageY.KeyFrames.Add(new LinearDoubleKeyFrame(currentVal, KeyTime.FromPercent(0)));
            killImageY.Duration = new Duration(TimeSpan.FromMilliseconds(contractMilli));
            ScaleTransform RectYForm = new ScaleTransform();
            border.RenderTransform = RectXForm;
            //Execution
            RectXForm.BeginAnimation(ScaleTransform.ScaleXProperty, killImageX);
            RectXForm.BeginAnimation(ScaleTransform.ScaleYProperty, killImageY);
        }

        public double Val
        {
            get { return currentVal; }
            set
            {
                currentVal = value;
                if (currentVal == ratioToGrow)
                {
                    OnComplete();
                }
            }
        }

        protected virtual void OnComplete()
        {
            if (CompleteChanged != null && HoverButton.canClick)
                CompleteChanged(this, EventArgs.Empty);
        }


    }
}
