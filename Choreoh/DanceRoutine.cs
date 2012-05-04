using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Diagnostics;

namespace Choreoh
{
    [Serializable]
    public class DanceRoutine
    {
        // time is kept by the integer number of frames since the beginning

        public String name;
        private String saveName;
        public String saveDestinationFolder;
        public Guid guid;
        public Dictionary<int, DanceSegment> segments;
        public Dictionary<DanceSegment, String> comments;
        // private Soundtrack soundtrack;

        public DanceRoutine(String filename)
        {
            name = filename;
            saveName = getSaveDestinationName(filename);
            saveDestinationFolder = getSaveDestinationFolder(filename);
            if (saveAlreadyExists(filename))
            {
                throw new Exception("Should be loading. Save already exists.");
            }
            else
            {
                segments = new Dictionary<int, DanceSegment>();
                comments = new Dictionary<DanceSegment, String>();
                guid = Guid.NewGuid();

                Directory.CreateDirectory(saveDestinationFolder);
            }
        }

        static public String getSaveDestinationName(String songFilename)
        {
            return Path.Combine(getSaveDestinationFolder(songFilename), Path.GetFileNameWithoutExtension(songFilename) + ".dat");
        }

        static public Boolean saveAlreadyExists(String songFilename)
        {
            String saveName = getSaveDestinationName(songFilename);
            return File.Exists(saveName);
        }

        static public string getSaveDestinationFolder(String songFilename)
        {
            return @"" + Path.GetFileNameWithoutExtension(songFilename);
        }

        public Boolean save()
        {
            var formatter = new BinaryFormatter();
            try
            {
                using (var fs = new FileStream(saveName, FileMode.Create))
                {
                    formatter.Serialize(fs, this);
                }
            }
            catch (Exception e)
            {
                Debugger.Log(0, "Serialization", e.ToString());
                return false;
            }
            return true;
        }

        static public DanceRoutine load(String saveFilename)
        {
            var formatter = new BinaryFormatter();

            try
            {
                using (var fs = new FileStream(saveFilename, FileMode.Open))
                {
                    return (DanceRoutine)formatter.Deserialize(fs);
                }
            }
            catch (Exception e)
            {
                Debugger.Log(0, "Serialization", e.ToString());
            }
            return null;
        }

        public DanceSegment addDanceSegment(int startFrame)
        {
            var segment = new DanceSegment(this);
            segments.Remove(startFrame);
            segments.Add(startFrame, segment);
            return segment;
        }

        public String addComment(DanceSegment segment, String comment)
        {
            if (comments.ContainsKey(segment)) comments.Remove(segment);
            comments.Add(segment, comment);
            return comment;
        }

        public void deleteDanceSegment(DanceSegment segment)
        {
            foreach (KeyValuePair<int, DanceSegment> kvp in segments)
            {
                if (kvp.Value == segment)
                {
                    Debug.WriteLine("Deleting of segment, target segment found at frame " + kvp.Key.ToString());
                    segments.Remove(kvp.Key);
                    Debug.WriteLine("Is the segment still in the segments list? " + segments.ContainsKey(kvp.Key).ToString());
                    segment.deleteFiles();
                    return;
                }
            }
            Debug.WriteLine("Exited deleteDanceSegment without actually deleting anything");
        }

        public void deleteDanceSegmentAt(int frame)
        {
            if (segments.ContainsKey(frame))
            {
                segments.Remove(frame);
            }
        }
    }
}
