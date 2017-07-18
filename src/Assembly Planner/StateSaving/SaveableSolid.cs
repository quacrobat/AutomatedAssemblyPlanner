﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TVGL;
using System.Xml.Serialization;

namespace Assembly_Planner
{
    [Serializable]
    public class SaveableSolid
    {

        public List<string> Comments;
        public string FileName;
        public string Name;
        public string Language;
        public byte A;
        public byte B;
        public byte G;
        public byte R;
        public UnitType Units;
        public List<int[]> Faces;
        public List<double[]> Vertices;


        int[] toIndexes(Vertex[] Vertices, PolygonalFace theFace)
        {
            int[] result = new int[3];
            result[0] = Array.FindIndex(Vertices, face => face == theFace.Vertices[0] );
            result[1] = Array.FindIndex(Vertices, face => face == theFace.Vertices[1] );
            result[2] = Array.FindIndex(Vertices, face => face == theFace.Vertices[2] );
            return result;
        }

        double[] toCoords(Vertex theVertex)
        {
            double[] result = new double[3];
            result[0] = theVertex.X;
            result[1] = theVertex.Y;
            result[2] = theVertex.Z;
            return result;
        }

        public SaveableSolid(TessellatedSolid theSolid)
        {
            Comments = theSolid.Comments;
            FileName = theSolid.FileName;
            Name = theSolid.Name;
            Language = theSolid.Language;
            A = theSolid.SolidColor.A;
            B = theSolid.SolidColor.B;
            G = theSolid.SolidColor.G;
            R = theSolid.SolidColor.R;
            Units = theSolid.Units;
            Vertices = new List<double[]>();
            foreach(Vertex v in theSolid.Vertices)
            {
                Vertices.Add(toCoords(v));
            }
            Faces = new List<int[]>();
            foreach(PolygonalFace p in theSolid.Faces)
            {
                Faces.Add(toIndexes(theSolid.Vertices,p));
            }
        }

        public SaveableSolid()
        {
        
        }

        public TessellatedSolid generate()
        {
            List<Color> theColors = new List<Color>();
            theColors.Add(new Color(A,R,G,B));
            return new TessellatedSolid(Vertices, Faces, theColors, Units, Name, FileName, Comments, Language);
        }

    }
}