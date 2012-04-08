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
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;


namespace Choreoh
{
    /// <summary>
    /// Interaction logic for HoverButton.xaml
    /// </summary>
    public partial class HoverButton : UserControl
    {
        #region Fields

        //animation related
        private bool isHovering = false;
        private ButtonAnimate expandAnimation;
        public static bool canClick = true;
        private static System.Timers.Timer canClickTimer = new System.Timers.Timer()
        {
            Interval = 2000,
            Enabled = true,
            AutoReset = false
        };



        #endregion

        #region Properties

        public static readonly DependencyProperty BackgroundColorProperty = DependencyProperty.Register(
            "BackgroundColor", typeof(Brush), typeof(HoverButton), new PropertyMetadata(Brushes.Red));
        public Brush BackgroundColor
        {
            get { return (Brush)this.GetValue(BackgroundColorProperty); }
            set { this.SetValue(BackgroundColorProperty, value); }
        }

        public static readonly DependencyProperty HoverOverlayProperty = DependencyProperty.Register(
            "HoverOverlay", typeof(Brush), typeof(HoverButton), new PropertyMetadata(Brushes.Transparent));
        public Brush HoverOverlay
        {
            get { return (Brush)this.GetValue(HoverOverlayProperty); }
            set { this.SetValue(HoverOverlayProperty, value); }
        }

        public static readonly DependencyProperty HoverColorProperty = DependencyProperty.Register(
            "HoverColor", typeof(Brush), typeof(HoverButton), new PropertyMetadata(Brushes.White));
        public Brush HoverColor
        {
            get { return (Brush)this.GetValue(HoverColorProperty); }
            set { this.SetValue(HoverColorProperty, value); }
        }

        public static readonly DependencyProperty TextColorProperty = DependencyProperty.Register(
           "TextColor", typeof(Brush), typeof(HoverButton), new PropertyMetadata(Brushes.White));
        public Brush TextColor
        {
            get { return (Brush)this.GetValue(TextColorProperty); }
            set { this.SetValue(TextColorProperty, value); }
        }

        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
            "Text", typeof(string), typeof(HoverButton), new PropertyMetadata(""));
        public string Text
        {
            get { return (string)this.GetValue(TextProperty); }
            set { this.SetValue(TextProperty, value); }
        }

        public static readonly DependencyProperty TextSizeProperty = DependencyProperty.Register(
            "TextSize", typeof(int), typeof(HoverButton), new PropertyMetadata((int)36));
        public int TextSize
        {
            get { return (int)this.GetValue(TextSizeProperty); }
            set { this.SetValue(TextSizeProperty, value); }
        }

        public static readonly DependencyProperty HorizontalTextAlignmentProperty = DependencyProperty.Register(
            "HorizontalTextAlignment", typeof(string), typeof(HoverButton), new PropertyMetadata("Center"));
        public string HorizontalTextAlignment
        {
            get { return (string)this.GetValue(HorizontalAlignmentProperty); }
            set { this.SetValue(HorizontalAlignmentProperty, value); }
        }

        public static readonly DependencyProperty VerticalTextAlignmentProperty = DependencyProperty.Register(
            "VerticalTextAlignment", typeof(string), typeof(HoverButton), new PropertyMetadata("Center"));
        public string VerticalTextAlignment
        {
            get { return (string)this.GetValue(VerticalAlignmentProperty); }
            set { this.SetValue(VerticalAlignmentProperty, value); }
        }

        public static readonly DependencyProperty ImageProperty = DependencyProperty.Register(
           "Image", typeof(string), typeof(HoverButton), new PropertyMetadata(""));
        public string Image
        {
            get { return (string)this.GetValue(ImageProperty); }
            set { this.SetValue(ImageProperty, value); }
        }

        public static readonly DependencyProperty CornerRadiusProperty = DependencyProperty.Register(
            "CornerRadius", typeof(double), typeof(HoverButton), new PropertyMetadata((double)0));
        public double CornerRadius
        {
            get { return (double)this.GetValue(CornerRadiusProperty); }
            set { this.SetValue(CornerRadiusProperty, value); }
        }

        #endregion

        #region Button Effects
        DropShadowEffect shadowEffect = new DropShadowEffect
        {
            Color = new Color { A = 255, R = 5, G = 5, B = 5 },
            Direction = 0,
            ShadowDepth = 0,
            Opacity = .7,
            BlurRadius = 15
        };
        #endregion

        #region Events

        public delegate void ClickHandler(object sender, EventArgs e);
        public event ClickHandler Click;

        #endregion

        #region Animation and Event HelperMethods

        private void StartHovering()
        {
            if (!isHovering && canClick)
            {
                isHovering = true;
                this.Effect = shadowEffect;
                expandAnimation.CompleteChanged += new EventHandler(expandAnimation_Completed);
                expandAnimation.expand(this);
            }
        }

        void hoverTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            /*
            if (canClick)
            {
                canClick = false;

                canClickTimer.Start();
                if (Click != null)
                    Click(this, e);
            }
            */
            return;

        }

        private void StopHovering()
        {
            if (isHovering)
            {
                isHovering = false;
                this.Effect = null;
                expandAnimation.CompleteChanged -= new EventHandler(expandAnimation_Completed);
                expandAnimation.contract(this);

            }
        }


        public bool Check(FrameworkElement cursor)
        {
            if (IsCursorInButton(cursor))
            {
                if (Global.pushed && canClick)
                {
                    Global.pushed = false;
                    canClick = false;
                    canClickTimer.Start();
                    if (Click != null)
                        Click(this, null);
                }
                this.StartHovering();
                return true;
            }
            else
            {
                this.StopHovering();
                return false;
            }
        }

        private bool IsCursorInButton(FrameworkElement cursor)
        {
            try
            {
                //Cursor midpoint location
                Point cursorTopLeft = cursor.PointToScreen(new Point());
                double cursorCenterX = cursorTopLeft.X + (cursor.ActualWidth / 2);
                double cursorCenterY = cursorTopLeft.Y + (cursor.ActualHeight / 2);

                //Button location
                Point buttonTopLeft = this.PointToScreen(new Point());
                double buttonLeft = buttonTopLeft.X;
                double buttonRight = buttonLeft + this.Width;
                double buttonTop = buttonTopLeft.Y;
                double buttonBottom = buttonTop + this.ActualHeight;

                if (cursorCenterX < buttonLeft || cursorCenterX > buttonRight)
                    return false;

                if (cursorCenterY < buttonTop || cursorCenterY > buttonBottom)
                    return false;

                return true;
            }
            catch
            {
                return false;
            }
        }



        public HoverButton()
        {
            InitializeComponent();
            this.DataContext = this;
            this.Effect = null;
            
            expandAnimation = new ButtonAnimate();
            canClickTimer.Elapsed += new System.Timers.ElapsedEventHandler(canClickTimer_Elapsed);

        }

        void expandAnimation_Completed(object sender, EventArgs e)
        {
            /*
            if (canClick)
            {
                canClick = false;
                canClickTimer.Elapsed += new System.Timers.ElapsedEventHandler(canClickTimer_Elapsed);
                canClickTimer.Start();
                if (Click != null)
                    Click(this, e);
            }
            */
            return;
        }

        void canClickTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            canClick = true;
        }
        #endregion
    }
}
