﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml.Serialization;
using AssemblyEvaluation;
using Assembly_Planner;
using Assembly_Planner.GraphSynth.BaseClasses;
using GraphSynth.Representation;
using TVGL;
using TVGL.IOFunctions;
using TVGL;

namespace Assembly_Planner
{
    internal class Program
    {

        public static List<double> DegreeOfFreedoms = new List<double>();
        public static List<double> StablbiblityScores = new List<double>();
        public static Dictionary<string, List<TessellatedSolid>> Solids = new Dictionary<string, List<TessellatedSolid>>();
        public static Dictionary<string, List<TessellatedSolid>> SolidsNoFastener = new Dictionary<string, List<TessellatedSolid>>();
        public static Dictionary<string, double> SolidsMass = new Dictionary<string, double>();
        public static designGraph AssemblyGraph;
        public const double MeshMagnifier = 1;
        public static double[] PointInMagicBox = { 0, 0, 0.0 };

        public static List<int> globalDirPool = new List<int>();

        public static void Main(string[] args)
        {
            Program1.main(args);     
        }
       
    }

}

