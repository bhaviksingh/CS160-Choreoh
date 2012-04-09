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

namespace Choreoh
{
    public class Waveform
    {
        private Canvas waveformCanvas;
        private Image waveformImage;
        private Rectangle selectRect;
        private double timeInc;
        private Image startSlider;
        private Image playSlider;
        private Image stopSlider;
        private double offset;
        /**
         * Takes in the width of a waveform image, the length of a song in seconds, a properly formatted string for an image, and the Canvas this all needs to be drawn in.
         * 
         * 
         **/
        public Waveform(int width, int seconds, Canvas topCanvas)
        {
            waveformCanvas = new Canvas //generates canvas to be returned
            {
                Height = 160,
                Width = width,
            };
            topCanvas.Children.Add(waveformCanvas); //properly adds and positions waveformCanvas within topCanvas
            Canvas.SetLeft(waveformCanvas, 0);
            Canvas.SetTop(waveformCanvas, 0);
            waveformImage = new Image         //generates image object within waveformCanvas
            {
                Height = waveformCanvas.Height,
                Width = waveformCanvas.Width,
                Stretch = Stretch.Fill
            };
            waveformCanvas.Children.Add(waveformImage);  //adds and positions waveformImage within waveformCanvas
            Canvas.SetLeft(waveformImage, 8);
            Canvas.SetTop(waveformImage, 0);
            waveformImage.Source = new BitmapImage(new Uri(@"pack://application:,,,/Choreoh;component/img/waveform/beatit-waveform.png", UriKind.RelativeOrAbsolute)); //adds source to waveformImage
            timeInc = ((double)width) / ((double)seconds);
            for (int i = 0; (i * timeInc) < width; i++)                    //generates timestamps every 30 seconds and lines every 15 seconds
            {
                TextBlock nextTimestamp = new TextBlock();
                nextTimestamp.Foreground = Brushes.Black;
                waveformCanvas.Children.Add(nextTimestamp);
                Canvas.SetLeft(nextTimestamp, i * timeInc * 30);
                nextTimestamp.Text = numToTime(i * 30);
                Rectangle timeTick = new Rectangle();
                timeTick.Height = waveformCanvas.Height - 15;
                timeTick.Width = 1;
                timeTick.Stroke = Brushes.Black;
                waveformCanvas.Children.Add(timeTick);
                Canvas.SetLeft(timeTick, 30 * i * timeInc + 8);
                Canvas.SetTop(timeTick, 15);
                Rectangle timeTick2 = new Rectangle();
                timeTick2.Height = waveformCanvas.Height - 25;
                timeTick2.Width = 1;
                timeTick2.Stroke = Brushes.Black;
                waveformCanvas.Children.Add(timeTick2);
                Canvas.SetLeft(timeTick2, 30 * i * timeInc + 8 + (30 * timeInc / 2));
                Canvas.SetTop(timeTick2, 20);
            }

            selectRect = new Rectangle
            {
                Height = waveformCanvas.Height,
                Width = timeInc,
                Visibility = Visibility.Hidden,
                Opacity = .2,
                Fill = Brushes.Orange,
                Stroke = Brushes.Transparent
            };
            waveformCanvas.Children.Add(selectRect);
            startSlider = new Image
            {
                Height = 190,
                Width = 54,
                Stretch = Stretch.Fill,
                Source = new BitmapImage(new Uri(@"pack://application:,,,/Choreoh;component/img/waveform/startslider.png", UriKind.RelativeOrAbsolute)),
                Visibility = Visibility.Hidden
            };
            waveformCanvas.Children.Add(startSlider);
            Canvas.SetLeft(startSlider, 0);
            Canvas.SetTop(startSlider, -20);
            stopSlider = new Image
            {
                Height = 190,
                Width = 54,
                Stretch = Stretch.Fill,
                Source = new BitmapImage(new Uri(@"pack://application:,,,/Choreoh;component/img/waveform/stopslider.png", UriKind.RelativeOrAbsolute)),
                Visibility = Visibility.Hidden
            };
            waveformCanvas.Children.Add(stopSlider);
            Canvas.SetLeft(stopSlider, 0);
            Canvas.SetTop(stopSlider, -20);
            offset = 0.0;
            playSlider = new Image
            {
                Height = 190,
                Width = 54,
                Stretch = Stretch.Fill,
                Source = new BitmapImage(new Uri(@"pack://application:,,,/Choreoh;component/img/waveform/playslider.png", UriKind.RelativeOrAbsolute)),
                Visibility = Visibility.Hidden
            };
            waveformCanvas.Children.Add(playSlider);
            Canvas.SetLeft(playSlider, 0);
            Canvas.SetTop(playSlider, -20);
        }

        public void selectStart(double start)
        {
            Canvas.SetLeft(startSlider, start * timeInc - 22);
            startSlider.Visibility = Visibility.Visible;
        }

        public void selectEnd(double end)
        {
            Canvas.SetLeft(stopSlider, end * timeInc - 22);
            stopSlider.Visibility = Visibility.Visible;
            double start = Canvas.GetLeft(startSlider);
            selectSegment(start + 30, Math.Abs((end * timeInc - 22) - start));
        }

        public void selectSegment(double start, double length)
        {
            Canvas.SetLeft(selectRect, start);
            Canvas.SetTop(selectRect, 0);
            selectRect.Width = length;
            selectRect.Visibility = Visibility.Visible;
        }
        
        public void startPlay()
        {
            Canvas.SetLeft(playSlider, Canvas.GetLeft(startSlider));
            playSlider.Visibility = Visibility.Visible;
        }

        public void movePlay()
        {
            if (Canvas.GetLeft(playSlider) >= Canvas.GetLeft(stopSlider))
            {
                endPlay();
                return;
            }
            Canvas.SetLeft(playSlider, Canvas.GetLeft(playSlider) + 1);
        }

        public void endPlay()
        {
            playSlider.Visibility = Visibility.Hidden;
        }

        public void deselectSegment()
        {
            selectRect.Visibility = Visibility.Hidden;
            startSlider.Visibility = Visibility.Hidden;
            stopSlider.Visibility = Visibility.Hidden;
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
                    offset -= inc;
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

            if (Canvas.GetLeft(waveformCanvas) == 0) return -1;
            if (Canvas.GetLeft(waveformCanvas) + waveformCanvas.Width == 1024) return 1;
            return 0;
        }

        public Canvas getWaveform()
        {
            return waveformCanvas;
        }

        public double getOffset()
        {
            return offset;
        }

        public double getPixelsPerSecond()
        {
            return timeInc;
        }

    }
}
