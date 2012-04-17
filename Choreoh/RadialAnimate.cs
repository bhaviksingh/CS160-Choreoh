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


namespace Choreoh
{
    class RadialAnimate : DoubleAnimationUsingKeyFrames
    {
        private DoubleAnimationUsingKeyFrames growImageX;
        private DoubleAnimationUsingKeyFrames growImageY;
        private DoubleAnimationUsingKeyFrames killImageX;
        private DoubleAnimationUsingKeyFrames killImageY;
        private double currentVal = 1.0;
        public event EventHandler CompleteChanged;
        TranslateTransform rightTrans = new TranslateTransform();
        TranslateTransform topTrans = new TranslateTransform();
        TranslateTransform leftTrans = new TranslateTransform();
        TranslateTransform bottomTrans = new TranslateTransform();
        private int expandTo = 128;

        public RadialAnimate()
        {
            growImageX = new DoubleAnimationUsingKeyFrames();
            growImageY = new DoubleAnimationUsingKeyFrames();
            killImageX = new DoubleAnimationUsingKeyFrames();
            killImageY = new DoubleAnimationUsingKeyFrames();
            topTrans.Changed += new EventHandler(topTrans_Changed);
            rightTrans.Changed += new EventHandler(rightTrans_Changed);
            leftTrans.Changed += new EventHandler(leftTrans_Changed);
            bottomTrans.Changed += new EventHandler(bottomTrans_Changed);
        }

        public void expand(Image control, String direction)
        {
            DoubleAnimationUsingKeyFrames dax = new DoubleAnimationUsingKeyFrames();
            DoubleAnimationUsingKeyFrames day = new DoubleAnimationUsingKeyFrames();
            TransformGroup tg = new TransformGroup();
            switch (direction)
            {
                case ("Left"):
                    dax.KeyFrames.Add(new LinearDoubleKeyFrame(-expandTo, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(2))));
                    day.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(2))));
                    control.RenderTransform = leftTrans;
                    leftTrans.BeginAnimation(TranslateTransform.XProperty, dax);
                    break;
                case ("Top"):
                    dax.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(2))));
                    day.KeyFrames.Add(new LinearDoubleKeyFrame(-expandTo, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(2))));
                    control.RenderTransform = topTrans;
                    topTrans.BeginAnimation(TranslateTransform.YProperty, day);
                    break;
                case ("Right"):
                    dax.KeyFrames.Add(new LinearDoubleKeyFrame(expandTo, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(2))));
                    day.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(2))));
                    control.RenderTransform = rightTrans;
                    rightTrans.BeginAnimation(TranslateTransform.XProperty, dax);
                    break;
                case ("Bottom"):
                    dax.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(2))));
                    day.KeyFrames.Add(new LinearDoubleKeyFrame(expandTo, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(2))));
                    control.RenderTransform = bottomTrans;
                    bottomTrans.BeginAnimation(TranslateTransform.YProperty, day);
                    break;
            }
            //tg.Children.Add(ttx);
            //tg.Children.Add(tty);



        }

        public void stop(String direction)
        {
            switch (direction)
            {
                case ("Top"):
                    topTrans.BeginAnimation(TranslateTransform.YProperty, null);
                    break;
                case ("Right"):
                    rightTrans.BeginAnimation(TranslateTransform.XProperty, null);
                    break;
                case ("Left"):
                    leftTrans.BeginAnimation(TranslateTransform.XProperty, null);
                    break;
                case ("Bottom"):
                    bottomTrans.BeginAnimation(TranslateTransform.YProperty, null);
                    break;
                default:
                    break;
            }
        }




        //Update current transform value so if doesn't fully expand it won't look jerky <--yum
        private void rightTrans_Changed(object sender, EventArgs e)
        {
            TranslateTransform transformer = (TranslateTransform)sender;
            this.Val = transformer.X;
        }
        private void topTrans_Changed(object sender, EventArgs e)
        {
            TranslateTransform transformer = (TranslateTransform)sender;
            this.Val = transformer.Y;
        }
        private void leftTrans_Changed(object sender, EventArgs e)
        {
            TranslateTransform transformer = (TranslateTransform)sender;
            this.Val = transformer.X;
        }
        private void bottomTrans_Changed(object sender, EventArgs e)
        {
            TranslateTransform transformer = (TranslateTransform)sender;
            this.Val = transformer.Y;
        }

        public double Val
        {
            get { return currentVal; }
            set
            {
                if (currentVal != value)
                {
                    currentVal = value;
                    if (currentVal == expandTo || currentVal == -expandTo)
                    {
                        OnComplete();
                    }
                }
            }
        }

        protected virtual void OnComplete()
        {
            if (CompleteChanged != null)
                CompleteChanged(this, EventArgs.Empty);
        }


    }
}
