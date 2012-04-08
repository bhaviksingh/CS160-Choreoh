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

namespace WaveformStuff
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Canvas wfCanvas;
        private bool select;
        private Rectangle selectRect;
        public MainWindow()
        {
            InitializeComponent();
            wfCanvas = new Canvas();
            select = false;
            selectRect = null;
        }
        /**
         * Takes in the width of a waveform image, the length of a song in seconds, a properly formatted string for an image, and the Canvas this all needs to be drawn in.
         * Will return the generated waveform canvas with the image and timestamps
         * 
         **/
        public Canvas initializeWaveform(int width, int seconds, string source, Canvas topCanvas)
        {
            Canvas waveformCanvas = new Canvas //generates canvas to be returned
            {
                Height = 160,
                Width = width
            };
            topCanvas.Children.Add(waveformCanvas); //properly adds and positions waveformCanvas within topCanvas
            Canvas.SetLeft(waveformCanvas, 0);
            Canvas.SetTop(waveformCanvas, 400);
            Image waveformImage = new Image         //generates image object within waveformCanvas
            {
                Height = waveformCanvas.Height,
                Width = waveformCanvas.Width,
                Stretch = Stretch.Fill
            };
            waveformCanvas.Children.Add(waveformImage);  //adds and positions waveformImage within waveformCanvas
            Canvas.SetLeft(waveformImage, 8);
            Canvas.SetTop(waveformImage, 0);
            waveformImage.Source = new BitmapImage(new Uri(source,UriKind.RelativeOrAbsolute)); //adds source to waveformImage
            double timeInc = 30 * ((double) width) / ((double) seconds);
            for (int i = 0; (i * timeInc) < width; i++)                    //generates timestamps every 30 seconds and lines every 15 seconds
            {
                TextBlock nextTimestamp = new TextBlock();
                nextTimestamp.Foreground = Brushes.Black;
                waveformCanvas.Children.Add(nextTimestamp);
                Canvas.SetLeft(nextTimestamp, i * timeInc);
                nextTimestamp.Text = numToTime(i * 30);
                Rectangle timeTick = new Rectangle();
                timeTick.Height = waveformCanvas.Height-15;
                timeTick.Width = 1;
                timeTick.Stroke = Brushes.Black;
                waveformCanvas.Children.Add(timeTick);
                Canvas.SetLeft(timeTick, i * timeInc + 8);
                Canvas.SetTop(timeTick, 15);
                Rectangle timeTick2 = new Rectangle();
                timeTick2.Height = waveformCanvas.Height - 25;
                timeTick2.Width = 1;
                timeTick2.Stroke = Brushes.Black;
                waveformCanvas.Children.Add(timeTick2);
                Canvas.SetLeft(timeTick2, i * timeInc + 8 + (timeInc/2));
                Canvas.SetTop(timeTick2, 20);
            }

            return waveformCanvas;
        }

        public Rectangle selectSegment(int width, int seconds, int start, int length, Canvas waveformCanvas, Rectangle selection)
        {
            double timeInc = ((double)width) / ((double)seconds);
            if (selection == null)
            {
                selection = new Rectangle
                {
                    Height = waveformCanvas.Height,
                    Width = timeInc,
                    Visibility = Visibility.Hidden,
                    Opacity = .2,
                    Fill = Brushes.Orange,
                    Stroke = Brushes.Transparent
                };
                waveformCanvas.Children.Add(selection);
            }
            Canvas.SetLeft(selection, start * timeInc + 8);
            Canvas.SetTop(selection, 0);
            selection.Width = length * timeInc;
            selection.Visibility = Visibility.Visible;
            return selection;
        }

        public Rectangle deselectSegment(int width, int seconds, int start, int length, Canvas waveformCanvas, Rectangle selection)
        {
            double timeInc = ((double)width) / ((double)seconds);
            if (selection == null)
            {
                selection = new Rectangle
                {
                    Height = waveformCanvas.Height,
                    Width = timeInc,
                    Visibility = Visibility.Hidden,
                    Opacity = .5,
                    Fill = Brushes.Orange,
                    Stroke = Brushes.Transparent
                };
                waveformCanvas.Children.Add(selection);
            }
            selection.Visibility = Visibility.Hidden;
            return selection;
        }

        private string numToTime(int num) // helper function to convert a time in seconds to a 'min:sec' format
        {
            int min = 0;
            int sec = num;
            while (sec > 30)
            {
                sec -= 60;
                min++;
            }

            if (sec == 0)
            {
                return "" + min + ":00";
            }
            return "" + min + ":30";
        }

        /**
         * Will incrementally shift given canvas the given amount of pixels.
         * Uses waveformOnEdge to check to see if canvas is on edge of screen, does not move waveform if it is.
         **/
        public void shiftCanvas(int pixels, Canvas canvas)
        {
            int inc = 1;
            if (pixels < 0)
            {
                inc *= -1;
                pixels *= -1;
            }
            for (int i = 0; i < pixels; i++)
            {
                if (waveformOnEdge(canvas) == 0 || ((waveformOnEdge(canvas) == -1) && inc < 0) || ((waveformOnEdge(canvas) == 1) && inc > 0))
                {
                    //textBlock1.Text = "" + waveformOnEdge(canvas);
                    Canvas.SetLeft(canvas, Canvas.GetLeft(canvas) + inc);
                    
                }
                else
                {
                    return;
                }
            }
        }

        /**
         * Checks to see if waveform is on edge of screen (currently hardcoded to 1024).
         * 
         **/
        public int waveformOnEdge(Canvas waveformCanvas)
        {
            textBlock1.Text = "" + (Canvas.GetLeft(waveformCanvas) + waveformCanvas.Width);
            if(Canvas.GetLeft(waveformCanvas) == 0) return -1;
            if(Canvas.GetLeft(waveformCanvas)+waveformCanvas.Width == 1024) return 1;
            return  0;
        }

        //testing button functions follow

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            wfCanvas = initializeWaveform(1800, 259, "pack://application:,,,/WaveformStuff;component/beatit-waveform.png",windowCanvas);
        }

        private void button2_Click(object sender, RoutedEventArgs e)
        {
            shiftCanvas(Convert.ToInt32(pixelShift.Text), wfCanvas);
        }

        private void selectButton_Click(object sender, RoutedEventArgs e)
        {
            if (select)
            {
                selectRect = deselectSegment(1800, 259, Convert.ToInt32(segStart.Text), Convert.ToInt32(segLength.Text), wfCanvas, selectRect);
            }
            else
            {
                selectRect = selectSegment(1800, 259, Convert.ToInt32(segStart.Text), Convert.ToInt32(segLength.Text), wfCanvas, selectRect);
            }
            select = !select;
        }

        

    }
}
