﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Threading.Tasks;
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
        public static Dictionary<string, List<TessellatedSolid>> solids = new Dictionary<string, List<TessellatedSolid>>();
        private static void Main(string[] args)
        {

            var graphExists = false;
            var inputDir =
                 " C:/WeifengDOC/Desktop/sim3";
            //"../../../Test/Cube";
            // "../../../Test/PumpWExtention";
            //"../../../Test/FastenerTest/new/test";
            //"../../../Test/Double";
            //"../../../Test/test7";
            //"../../../Test/Mc Cormik/STL2";
            //"../../../Test/Truck -TXT-1/STL";
            //"../../../Test/FoodPackagingMachine/FPMSTL2";
            //"../../../../GearAndFastener Detection/TrainingData/not-screw/Gear";
            //  "../../../Test/test8";
            var s = Stopwatch.StartNew();
            s.Start();
            solids = GetSTLs(inputDir);
            designGraph assemblyGraph;
            List<int> globalDirPool = new List<int>();
            if (graphExists)
            {
                var fileName = "../../../Test/PremadeGraphs/FPM.gxml";
                assemblyGraph = (designGraph)GraphSaving.OpenSavedGraph(fileName)[0];
                globalDirPool = GraphSaving.RetrieveGlobalDirsFromExistingGraph(assemblyGraph);
                var Directions = IcosahedronPro.DirectionGeneration();
                DisassemblyDirections.Directions = new List<double[]>(Directions);
            }
            else
            {
                assemblyGraph = new designGraph();
                //var globalDirPool = DisassemblyDirections.Run(assemblyGraph, solids);
                globalDirPool = DisassemblyDirectionsWithFastener.Run(assemblyGraph, solids, false);
                //Updates.AddPartsProperties(inputDir, assemblyGraph);
                //NonadjacentBlockingDeterminationPro.Run(assemblyGraph, solids, globalDirPool);
                NonadjacentBlockingWithPartitioning.Run(assemblyGraph, solids, globalDirPool);
                //NonadjacentBlockingDetermination.Run(assemblyGraph, solids, globalDirPool);
                //GraphSaving.SaveTheGraph(assemblyGraph);
            }
            //var solutions = RecursiveOptimizedSearch.Run(assemblyGraph, solids, globalDirPool);
            Stabilityfunctions.GenerateReactionForceInfo(assemblyGraph);
            var solutions = LeapBeta.Run(assemblyGraph, solids, globalDirPool, 1);
            //var solutions = OrderedDFS.Run(inputData, globalDirPool,solids); // the output is the assembly sequence
            //var solutions = BeamSearch.Run(inputData, globalDirPool);

            //var reorientation = OptimalOrientation.Run(solutions);
            //WorkerAllocation.Run(solutions, reorientation);
            s.Stop();
            Console.WriteLine();
            Console.WriteLine("TOTAL TIME:" + "     " + s.Elapsed);
            Console.ReadLine();
        }

        private static Dictionary<string, List<TessellatedSolid>> GetSTLs(string InputDir)
        {
            Console.WriteLine("Loading STLs ....");
            var parts = new List<TessellatedSolid>();
            var di = new DirectoryInfo(InputDir);
            var fis = di.EnumerateFiles("*.STL");
            // Parallel.ForEach(fis, fileInfo =>
            foreach (var fileInfo in fis)
            {
                var ts = IO.Open(fileInfo.Open(FileMode.Open), fileInfo.Name);
                //ts.Name = ts.Name.Remove(0, 1);
                //lock (parts) 
                parts.Add(ts[0]);
            }
            //);
            Console.WriteLine("All the files are loaded successfully");
            Console.WriteLine("    * Number of tessellated solids:   " + parts.Count);
            Console.WriteLine("    * Total Number of Triangles:   " + parts.Sum(s => s.Faces.Count()));
            return parts.ToDictionary(tessellatedSolid => tessellatedSolid.Name, tessellatedSolid => new List<TessellatedSolid> { tessellatedSolid });
        }
    }
}