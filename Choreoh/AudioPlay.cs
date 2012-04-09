using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Choreoh
{
    public class AudioPlay
    {

        public static void playForDuration(Canvas parent, String filename, TimeSpan startPosition, TimeSpan duration)
        {
            MediaElement songMediaElement = new MediaElement();
            parent.Children.Add(songMediaElement);
            songMediaElement.LoadedBehavior = MediaState.Manual;
            songMediaElement.Volume = 0;
            songMediaElement.Source = new Uri(@"" + filename, UriKind.RelativeOrAbsolute);
            var dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler((object sender, EventArgs e) =>
            {
                songMediaElement.Stop();
                (sender as DispatcherTimer).Stop();
            });
            dispatcherTimer.Interval = duration;
            songMediaElement.Position = startPosition;
            songMediaElement.Play();
            dispatcherTimer.Start();
        }
    }
}
