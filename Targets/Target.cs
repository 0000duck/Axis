﻿using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

using Axis.Tools;
using Axis.Core;

namespace Axis.Targets
{
    public class Target
    {
        public Point3d Position { get; set; }
        public Plane Plane { get; set; }
        public Quaternion Quaternion { get; set; }
        public List<double> JointAngles { get; set; }
        public Speed Speed { get; }
        public Zone Zone { get; }
        public Tool Tool { get; }
        public CSystem CSystem { get; set; }

        public double ExtRot { get; set; }
        public double ExtLin { get; set; }

        public string WorkObject { get; }

        public string StrABB { get; }
        public string StrKUKA { get; }

        public MotionType Method { get; }
        public string StrMethod { get; }     

        public static Target Default { get; }

        public Target(Plane target, MotionType method, Speed speed, Zone zone, Tool tool, CSystem wobj, double extRot, double extLin, bool robot)
        {
            // Adjust plane to comply with robot programming convetions.
            Quaternion realQuat = Util.QuaternionFromPlane(target);

            // Set publicly accessible property values based on the manufacturer data.
            this.Plane = target;
            this.Quaternion = realQuat;
            this.ExtRot = extRot;
            this.ExtLin = extLin;
            this.WorkObject = wobj.Name;
            this.Method = method;
            this.Tool = tool;

            // Offset with the CSystem to get the right program code.
            Transform xForm = Transform.PlaneToPlane(wobj.CSPlane, Plane.WorldXY);

            Plane plane = new Plane(target);
            plane.Transform(xForm);

            Quaternion quat = Util.QuaternionFromPlane(plane);

            Point3d position = plane.Origin;
            double posX = Math.Round(position.X, 3);
            double posY = Math.Round(position.Y, 3);
            double posZ = Math.Round(position.Z, 3);

            this.Position = position;

            // Declare some strings to store the target information in a manufacturer specific manner.
            string movement = null;
            string strABB = null;
            string strKUKA = null;
            string strZone = zone.Name;
            string strSpeed = null;

            string toolName = String.Empty;
            if (tool.Name != "DefaultTool")
            {
                toolName = tool.Name;
            }

            // Work object
            string workObject = @"\Wobj:=" + wobj.Name;
            this.CSystem = wobj;

            if (robot == false) // ABB Targets
            {
                string ABBposition = posX.ToString() + ", " + posY.ToString() + ", " + posZ.ToString(); ;

                double A = quat.A, B = quat.B, C = quat.C, D = quat.D;
                double w = Math.Round(A, 6);
                double x = Math.Round(B, 6);
                double y = Math.Round(C, 6);
                double z = Math.Round(D, 6);

                string strQuat = w.ToString() + ", " + x.ToString() + ", " + y.ToString() + ", " + z.ToString();

                if (method == MotionType.Linear) // Linear movement method.
                {
                    movement = "MoveL";
                    this.StrMethod = "Linear";
                }
                else if (method == MotionType.Joint) // Joint movement method.
                {
                    movement = "MoveJ";
                    this.StrMethod = "Joint";
                }
                
                if (speed.Time > 0)
                {
                    /*
                    Time
                    Data type: num
                    This argument is used to specify the total time in seconds during which the robot moves. It is
                    then substituted for the corresponding speed data.
                    */
                    if (speed != null)
                    {
                        strSpeed = speed.Name + @"\T:=" + speed.Time.ToString();
                    }
                    else
                    {
                        // If we dont have speed data to begin with, then replace it with a default value.
                        strSpeed = @"v200\T:=" + speed.Time.ToString();
                    }
                }
                else
                {
                    if (speed.Name != null)
                    {
                        strSpeed = speed.Name;
                    }                
                }

                // External axis values
                string lin = "9E9"; string rot = "9E9";
                if (extRot != Util.ExAxisTol || extLin != Util.ExAxisTol) // If the external axis value is present... (otherwise 0.00001 is passed as a default value).
                {
                    if (extLin != Util.ExAxisTol)
                    {
                        lin = Math.Round(extLin, 4).ToString();
                    }
                    if (extRot != Util.ExAxisTol)
                    {
                        rot = Math.Round(extRot, 2).ToString(); // Get the external axis value per target and round it to two decimal places.
                    }
                    strABB = movement + @" [[" + ABBposition + "],[" + strQuat + "]," + " cData, " + "[" + rot + ", " + lin + ", 9E9, 9E9, 9E9, 9E9]" + "], " + strSpeed + ", " + strZone + ", " + tool.Name + " " + workObject + ";";
                }
                else { strABB = movement + @" [[" + ABBposition + "],[" + strQuat + "]," + " cData, eAxis], " + strSpeed + ", " + strZone + ", " + tool.Name + " " + workObject + ";"; }

            }

            else // KUKA Targets
            {
                string KUKAposition = "X " + posX.ToString() + ", Y " + posY.ToString() + ", Z " + posZ.ToString();

                List<double> eulers = Util.QuaternionToEuler(quat);

                double E1 = eulers[0] * 180 / Math.PI;
                E1 = Math.Round(E1, 3);
                double E2 = eulers[1] * 180 / Math.PI;
                E2 = Math.Round(E2, 3);
                double E3 = eulers[2] * 180 / Math.PI;
                E3 = Math.Round(E3, 3);

                string strEuler = "A " + E3.ToString() + ", B " + E2.ToString() + ", C " + E1.ToString();
                string strExtAxis = "E1 0, E2 0, E3 0, E4 0"; // Default values for the external axis.
                string approx = ""; // Declare an empty approximation method value.

                if (method == MotionType.Linear) // Linear movement method.
                {
                    movement = "LIN";
                    approx = "C_VEL";
                    this.StrMethod = "Linear";
                }
                else if (method == MotionType.Joint) // Joint movement method.
                {
                    movement = "PTP";
                    approx = "C_PTP";
                    this.StrMethod = "Joint";
                }

                // Compile the KUKA robot target string.
                strKUKA = movement + " {E6POS: " + KUKAposition + ", " + strEuler + ", " + strExtAxis + "} " + approx;
            }

            this.StrABB = strABB;
            this.StrKUKA = strKUKA;
        }

        public Target(List<double> axisVals, Speed speed, Zone zone, double extRot, double extLin, bool robot)
        {
            string strABB = null;
            string strZone = zone.Name;
            string jTarg = "[" + axisVals[0].ToString() + ", " + axisVals[1].ToString() + ", " + axisVals[2].ToString() + ", " + axisVals[3].ToString() + ", " + axisVals[4].ToString() + ", " + axisVals[5].ToString() + "]";

            this.JointAngles = axisVals;

            string strSpeed = null;

            if (speed.Time > 0)
            {
                /*
                Time
                Data type: num
                This argument is used to specify the total time in seconds during which the robot moves. It is
                then substituted for the corresponding speed data.
                */
                if (speed != null)
                {
                    strSpeed = speed.Name + @"\T:=" + speed.Time.ToString();
                }
                else
                {
                    // If we dont have speed data to begin with, then replace it with a default value.
                    strSpeed = @"v200\T:=" + speed.Time.ToString();
                }
            }
            else
            {
                if (speed.Name != null)
                {
                    strSpeed = speed.Name;
                }
            }

            // External axis values
            string lin = "9E9"; string rot = "9E9";
            if (extRot != Util.ExAxisTol || extLin != Util.ExAxisTol) // If the external axis value is present... (otherwise 0.00001 is passed as a default value).
            {
                if (extLin != Util.ExAxisTol)
                {
                    lin = Math.Round(extLin, 4).ToString();
                }
                if (extRot != Util.ExAxisTol)
                {
                    rot = Math.Round(extRot, 2).ToString(); // Get the external axis value per target and round it to two decimal places.
                }

                strABB = @"MoveAbsJ [" + jTarg + ", [" + rot + ", " + lin + ", 9E9, 9E9, 9E9, 9E9]" + "], " + strSpeed + ", " + strZone + ", tool0;";
            }
            else { strABB = @"MoveAbsJ [" + jTarg + ", [9E9, 9E9, 9E9, 9E9, 9E9, 9E9]" + "], " + strSpeed + ", " + strZone + ", tool0;"; }

            // Set publicly accessible property values based on the data.
            this.ExtRot = extRot;
            this.ExtLin = extLin;
            this.StrABB = strABB;
            this.Method = MotionType.AbsoluteJoint;

            // CTool() instead of tool0; *********
            this.StrMethod = "Absolute Joint";
        }

        public override string ToString() => (Method != null) ? $"Target ({StrMethod})" : $"Target ({Position})";
    }

    public class Speed
    {
        public string Name { get; set; }
        public double TranslationSpeed { get; set; }
        public double RotationSpeed { get; set; }
        public double Time { get; set; } = 0;

        public static Speed Default { get; }

        static Speed()
        {
            Default = new Speed(100, 30, "DefaultSpeed");
        }

        public Speed(double tcpSpeed = 100, double rotSpeed = 30, string name = null, double time = 0.0)
        {
            this.Name = name;
            this.TranslationSpeed = tcpSpeed;
            this.RotationSpeed = rotSpeed;
            this.Time = time;
        }

        public override string ToString() => (Name != null) ? $"Speed ({Name})" : $"Speed ({TranslationSpeed:0.0} mm/s)";
    }

    public class Zone
    {
        public string Name { get; set; }
        public double PathRadius { get; set; }
        public double PathOrient { get; set; }
        public double PathExternal { get; set; }
        public double Orientation { get; set; }
        public double LinearExternal { get; set; }
        public double RotaryExternal { get; set; }
        public bool StopPoint { get; set; }

        public static Zone Default { get; }

        static Zone()
        {
            Default = new Zone(false, 5, 25, 25, 15, 35, 5, "DefaultZone");
        }

        public Zone(bool stop, double pathRadius, double pathOrient, double pathExternal, double orientation, double linExternal, double rotExternal, string name)
        {
            this.Name = name;
            this.PathRadius = pathRadius;
            this.PathOrient = pathOrient;
            this.PathExternal = pathExternal;
            this.Orientation = orientation;
            this.LinearExternal = linExternal;
            this.RotaryExternal = rotExternal;
            this.StopPoint = stop;
        }

        public override string ToString() => (Name != null) ? $"Zone ({Name})" : $"Zone ({PathRadius:0.0} mm)";
    }

    public class CSystem
    {
        public string Name { get; set; }
        public Plane CSPlane { get; set; }

        public static CSystem Default { get; set; }

        public CSystem(string name, Plane csPlane)
        {
            this.Name = name;
            this.CSPlane = csPlane;
        }

        static CSystem()
        {
            Default = new CSystem("Default", Plane.WorldXY);
        }
    }

    public class ExternalTarget
    {
        // MoveExtJ [\Conc] ToJointPos [\ID] [\UseEOffs] Speed [\T] Zone [\Inpos]

        public bool Concurrent { get; set; }
        public double Speed { get; set; }
        public double Time { get; set; }
        public Zone Zone { get; set; }
    }

    public enum MotionType
    {
        Linear = 0,
        Joint = 1,
        AbsoluteJoint = 2,
        NoMovement = 3
    }
}