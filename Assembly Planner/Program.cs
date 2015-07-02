﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using GraphSynth.Representation;
using TVGL.IOFunctions;
using TVGL.Tessellation;

namespace Assembly_Planner
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var inputDir =
                //"../../../Test/Cube";
                //"../../../Test/Pump Assembly";
                "../../../Test/Double";
                //"../../../Test/PumpWExtention
            var solids = GetSTLs(inputDir);//"../../../Test/PumpWExtention");
            var assemblyGraph = new designGraph();
            
            //var globalDirPool = DisassemblyDirections.Run(assemblyGraph, solids); //Input: assembly model
            var globalDirPool = DisassemblyDirectionsWithFastener.Run(assemblyGraph, solids); //Input: assembly model

            //Updates.AddPartsProperties(inputDir, assemblyGraph);
            var inputData = new ConvexHullAndBoundingBox(inputDir, assemblyGraph);
            
            //var solutions = RecursiveOptimizedSearch.Run(inputData, globalDirPool);
            var solutions = OrderedDFS.Run(inputData, globalDirPool); // the output is the assembly sequence
            //var solutions = BeamSearch.Run(inputData, globalDirPool);
           
            var reorientation = OptimalOrientation.Run(solutions);
            WorkerAllocation.Run(solutions, reorientation);
            
            Console.ReadLine();
        }

        private static List<TessellatedSolid> GetSTLs(string InputDir)
        {
            var parts = new List<TessellatedSolid>();
            var di = new DirectoryInfo(InputDir);
            var fis = di.EnumerateFiles("*.STL");
            Parallel.ForEach(fis, fileInfo =>
                //foreach (var fileInfo in fis)
            {
                var ts = IO.Open(fileInfo.Open(FileMode.Open), fileInfo.Name);
                //ts.Name = ts.Name.Remove(0, 1);
                lock (parts) parts.Add(ts[0]);
            }
                );
            return parts;
        }
    }
}