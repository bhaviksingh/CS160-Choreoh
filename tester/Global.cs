using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Kinect;
using Coding4Fun.Kinect.Wpf;
using System.ComponentModel;
using System.Windows;

namespace tester
{
    class Global
    {
        public static double windowWidth = 0;
        public static double windowHeight = 0;
        public static String lastGesture = "";
        public static bool pushed = false;
        public static bool initPos = false;
        //public static bool housed = false;
        public static bool canGesture = true;
        //public static bool swiped = false;
        public static System.Timers.Timer canGestureTimer = new System.Timers.Timer()
        {
            Interval = 2000,
            Enabled = true,
            AutoReset = false
        };//event handler added by call in MainWindow
        public static Skeleton[] allSkeletons = new Skeleton[6];

        public static void canGestureTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            canGesture = true;
        }

        public static String checkMoves(LinkedList<Skeleton> moves)
        {
            lastGesture = "";
            checkInitialize(moves);
            if (initPos)
            {
                checkPush(moves);
                checkSwipe(.05f, moves);
                checkHouse(moves);
            }
            return lastGesture;
        }

        private static void checkPush(LinkedList<Skeleton> moves)
        {
            if (!canGesture)
                return;
            Joint prevJoint = new Joint();
            Joint firstJoint = moves.First<Skeleton>().Joints[JointType.HandRight];
            Joint lastJoint = moves.Last<Skeleton>().Joints[JointType.HandRight];
            foreach (Skeleton skeleton in moves)
            {
                Joint joint = skeleton.Joints[JointType.HandRight];
                //Checking if pushing
                if (prevJoint.Position.Z == 0.0)
                {
                    prevJoint = joint;
                    continue;
                }
                if (prevJoint.Position.Z > joint.Position.Z)
                {
                    prevJoint = joint;
                    continue;
                }
                return;
            }
            if ((Math.Abs(lastJoint.Position.Z - firstJoint.Position.Z) > .25) && 
                Math.Abs(lastJoint.Position.X - firstJoint.Position.X) < .10 && 
                Math.Abs(lastJoint.Position.Y - firstJoint.Position.Y) < .10)
            {
                canGesture = false;
                lastGesture = "Pushed";
                pushed = true;
                canGestureTimer.Start();
            }
        }
        private static void checkSwipe(float tolerance, LinkedList<Skeleton> moves)
        {
            int i = 0;
            if (!canGesture)
                return;
            LinkedList<Skeleton> temp = moves;
            Joint prevJoint = new Joint();
            Joint firstJoint = moves.First<Skeleton>().Joints[JointType.HandRight];
            Joint lastJoint = moves.Last<Skeleton>().Joints[JointType.HandRight];
            foreach (Skeleton skeleton in moves)
            {
                //find better implementation, but moves too long for checking swipes so skip first few
                if (i < 3)
                {
                    i++;
                    continue;
                }
                Joint joint = skeleton.Joints[JointType.HandRight];
                if (prevJoint.Position.Z == 0.0)//not the best but checks if empty joint
                {
                    prevJoint = joint;
                    continue;
                }
                if (prevJoint.Position.X > joint.Position.X + tolerance)
                {
                    prevJoint = joint;
                    continue;
                }
            return;
            }
            if ((Math.Abs(lastJoint.Position.X - firstJoint.Position.X) > .30) && Math.Abs(lastJoint.Position.Y - firstJoint.Position.Y) < .20)
            {
                canGesture = false;
                lastGesture = "Swiped";
                canGestureTimer.Start();
            }
        }

        private static void checkInitialize(LinkedList<Skeleton> moves)
        {
            if (!canGesture)
                return;
            Joint rightHand = moves.First<Skeleton>().Joints[JointType.HandRight];
            Joint leftHand = moves.First<Skeleton>().Joints[JointType.HandLeft];
            Joint head = moves.First<Skeleton>().Joints[JointType.Head];
            if (rightHand.Position.Y > .1 + head.Position.Y && leftHand.Position.Y > .1 + head.Position.Y)
            {
                if (Math.Abs(rightHand.Position.X - leftHand.Position.X) > .5)
                {
                    initPos = !initPos;
                    canGesture = false;
                    lastGesture = "Initialized";
                    canGestureTimer.Start();
                }
            }
        }

        private static void checkHouse(LinkedList<Skeleton> moves)
        {
            if (!canGesture)
                return;
            Joint rightHand = moves.First<Skeleton>().Joints[JointType.HandRight];
            Joint leftHand = moves.First<Skeleton>().Joints[JointType.HandLeft];
            Joint head = moves.First<Skeleton>().Joints[JointType.Head];
            if (rightHand.Position.Y > head.Position.Y && leftHand.Position.Y > head.Position.Y)
            {
                if (Math.Abs(rightHand.Position.X - leftHand.Position.X) < .2)
                {
                    canGesture = false;
                    lastGesture = "House";
                    canGestureTimer.Start();
                }
            }
        }
    }
}
