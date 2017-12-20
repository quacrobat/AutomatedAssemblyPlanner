﻿using Assembly_Planner.GraphSynth.BaseClasses;
using GPprocess;
using GraphSynth.Representation;
using StarMathLib;
using System;
using System.Collections.Generic;
using System.Linq;
using TVGL;
namespace Assembly_Planner
{
    /// <summary>
    /// Class EvaluationForBinaryTree - this is a stub for evaluating a particular install step
    /// </summary>
    public class EvaluationForBinaryTree
    {
        internal static Dictionary<string, TVGLConvexHull> ConvexHullsForParts =
            new Dictionary<string, TVGLConvexHull>();

        internal static Dictionary<Component, HashSet<Component>> AdjacentParts =
            new Dictionary<Component, HashSet<Component>>();

        internal EvaluationForBinaryTree(Dictionary<string, List<TessellatedSolid>> subAssems)
        {
            ConvexHullsForParts = NonadjacentBlockingWithPartitioning.CombinedCVHForMultipleGeometries;
        }

        /// <summary>
        /// Evaluates the subassemly.
        /// </summary>
        /// <param name="subassemblyNodes">The subassembly nodes - all the nodes in the combined install action.</param>
        /// <param name="optNodes">The subset of nodes that represent one of the two parts in the install step.</param>
        /// <param name="sub">The sub is the class that is then tied into the treequence.</param>
        /// <returns>System.Double.</returns>
        public double EvaluateSub(designGraph graph, HashSet<Component> subassemblyNodes, List<Component> optNodes,
            HashSet<int> validDirs, out SubAssembly sub)
        {
            var rest = subassemblyNodes.Where(n => !optNodes.Contains(n)).ToList();
            sub = Update(optNodes, rest);
            var connectingArcs =
                graph.arcs.Where(a => a is Connection)
                    .Cast<Connection>()
                    .Where(a => ((optNodes.Contains(a.To) && rest.Contains(a.From))
                                 || (optNodes.Contains(a.From) && rest.Contains(a.To))))
                    .ToList();
            var install = new[] { rest, optNodes };
            if (EitherRefOrMovHasSeperatedSubassemblies(install, subassemblyNodes))
                return -1;
            var insertionDirections = InsertionDirectionsFromRemovalDirections(sub, optNodes, validDirs);
            var minDis = double.PositiveInfinity;
            var distanceToFree = 0.0;
            var bestInstallDirection = new[] { 0.0, 0.0, 0.0 };
            foreach (var dir in insertionDirections)
            {
                double cvhOverlapDist;
                var dis = DetermineDistanceToSeparateHull(sub, dir, out cvhOverlapDist) / Program.MeshMagnifier;
                if (dis >= minDis) continue;
                minDis = dis;
                distanceToFree = cvhOverlapDist / Program.MeshMagnifier * 1000;
                bestInstallDirection = dir;
            }
            sub.Install.InstallDirection = bestInstallDirection;
            sub.Install.InstallDistance = minDis;

            var moivingnames = sub.Install.Moving.PartNames;
            var refnames = sub.Install.Reference.PartNames;
            var movingnodes = subassemblyNodes.Where(n => moivingnames.Contains(n.name)).ToList();
            var refnodes = subassemblyNodes.Where(n => refnames.Contains(n.name)).ToList();
            sub.Secure.Fasteners = ConnectingArcsFastener(connectingArcs, subassemblyNodes, optNodes, rest);
            /////time 
            sub.Install.Time = 0;
            if (distanceToFree == 0)
            {
                distanceToFree = 0.3;
            }
            EvaluateSubFirstThreeTime(sub, refnodes, movingnodes, distanceToFree);
         //   EvaluateSubFirstThreeTime(sub, refnodes, movingnodes, distanceToFree, out sub.Install.Time, out sub.Install.TimeSD, out sub.Secure.Time, out sub.Secure.TimeSD);

            
            if (sub.Install.Time < 0)
                sub.Install.Time = 0.5;
            
            if (sub.MoveRoate.Time < 0)
                sub.MoveRoate.Time = 0.5;
          
            //if (DetermineWeight)
            //{
            //    LeapSearch.InitialTimes.Add(sub.Install.Time+sub.MoveRoate.Time);
            //}
            ///////stability

            LeapSearch.stabilityscore = EvaluateSubStable(graph, refnodes, movingnodes, sub);
            //if (DetermineWeight)
            //{
            //    LeapSearch.InitialStableScores.Add(stabilityscore);
            //    return 1;
            //}
            //if (sub.Secure.Fasteners.Count != 0)
            //{
            //    foreach (var f in sub.Secure.Fasteners)
            //    {
            //        sub.Secure.Time += CalculateAssemblyTimeAndSD.GetSecureTime(f);
            //    }
            //}
            return 1;
        }

        private double EvaluateSubStable(designGraph graph, List<Component> optNodes, List<Component> rest,
            SubAssembly sub)
        {
            var subassembly = new List<Component>();
            var othersubassembly = new List<Component>();
            double bothsubstatiblit = 0;
            double finalscore = 0;
            for (int i = 0; i < 2; i++) //check moving and reference
            {
                if (i == 0)
                {
                    subassembly = optNodes;
                    othersubassembly = rest;
                }
                else
                {
                    subassembly = rest;
                    othersubassembly = optNodes;
                }
                var overallstatiblity = 0.0;
                var ss = EvaluationForBinaryTree.CreateCombinedConvexHull2(subassembly);
                var reductedfaces = AssemblyEvaluator.MergingFaces(ss.Faces.ToList());
                var maxsigleDOF = 0.0;
                var minsigleSB = double.PositiveInfinity;
                // double totaldof = 0;
                // double totalSBscore = 0;
                var totaldof = new List<double>();
                var totalSBscore = new List<double>();
                foreach (var refnode in subassembly)
                {
                    bool anyscoreinarc = false;
                    double signlescore;
                    bool isanyfastener = false;
                    double stability = 0;
                    var ministablility = double.PositiveInfinity;
                    var alldof = new double[12];
                    var othernodes = subassembly.Where(n => !n.name.Equals(refnode.name)).ToList();
                    var refarcs =
                        graph.arcs.Where(a => a is Connection).Cast<Connection>()
                            .Where(
                                a =>
                                    (a.XmlFrom.Equals(refnode.name) && othernodes.Exists(on => on.name.Equals(a.XmlTo)))
                                    ||
                                    ((a.XmlTo.Equals(refnode.name) && othernodes.Exists(on => on.name.Equals(a.XmlFrom)))
                                        )).ToList();
                    var refarcsnames = new HashSet<string>();

                    if (subassembly.Count == 1)
                    {
                        totaldof.Add(3.5);
                        totalSBscore.Add(9.8);
                        ; // TBD
                        maxsigleDOF = 3.5;
                        minsigleSB = 9.8;
                        Program.DegreeOfFreedoms.Add(3.5);
                        Program.StablbiblityScores.Add(9.8);
                        continue;
                    }
                    foreach (Connection a in refarcs.Where(a => a is Connection))
                    {
                        refarcsnames.Add(a.name);
                    }
                    //debug
                    var refnodename = refnode.name;
                    var othernames = new List<string>();
                    //debug
                    foreach (var on in othernodes)
                    {
                        if (!on.Equals(refnodename))
                            othernames.Add(on.name);
                    }
                    var firstkey = new HashSet<string>();
                    if (graph.nodes.Where(n => n is Component).Cast<Component>().First(n => n.name.Equals(refnode.name))
                        .SingleStabilityAndDOF.Keys.Any(
                            k =>
                                (k.Intersect(refarcsnames).Count().Equals(k.Count) &&
                                 k.Intersect(refarcsnames).Count().Equals(refarcsnames.Count))))
                    {
                        firstkey = graph.nodes.Where(n => n is Component)
                            .Cast<Component>()
                            .First(n => n.name.Equals(refnode.name))
                            .SingleStabilityAndDOF.Keys.First(
                                k =>
                                    (k.Intersect(refarcsnames).Count().Equals(k.Count) &&
                                     k.Intersect(refarcsnames).Count().Equals(refarcsnames.Count)));
                        anyscoreinarc = true;
                    }
                    var val = new List<double>();

                    if (anyscoreinarc == true)
                    {
                        val =
                            graph.nodes.Where(n => n is Component)
                                .Cast<Component>()
                                .First(n => n.name.Equals(refnode.name))
                                .SingleStabilityAndDOF[firstkey];
                        ministablility = val[0];
                        for (int j = 1; j < 13; j++)
                        {
                            alldof[j - 1] = val[j];
                        }
                    }
                    foreach (Connection carc in refarcs.Where(a => a is Connection))
                    {
                        if (carc.Fasteners.Count != 0)
                        {
                            isanyfastener = true;
                            break;
                        }
                    }
                    if (refnode.name.StartsWith("PumpAssembly.21"))
                    {
                        var s = 123;
                    }

                    if (isanyfastener == true && anyscoreinarc == false)
                    {
                        alldof = new double[12];
                    }
                    else if (isanyfastener == false && anyscoreinarc == false)
                    {
                        alldof = Stabilityfunctions.GetDOF(refnode, refarcs);
                    }
                    /////////////anyscoreinarc == false
                    var linearDOF = new double[6];
                    var rotateDOF = new double[6];
                    var sumlinearDOF = new double();
                    var sumrotateDOF = new double();
                    for (int k = 0; k < 6; k++)
                    {
                        linearDOF[k] = alldof[k];
                        rotateDOF[k] = alldof[6 + k];
                        sumlinearDOF += alldof[k];
                        sumrotateDOF += alldof[6 + k];
                    }
                    //debug wait nima to fix bug
                    if (linearDOF.Sum() == 3)
                    {
                        alldof = Stabilityfunctions.GetDOF(refnode, refarcs);
                    }

                    var sumdof = alldof.SumAllElements();
                    Program.DegreeOfFreedoms.Add(sumdof);

                    totaldof.Add(sumdof);
                    if (sumdof > maxsigleDOF)
                    {
                        maxsigleDOF = sumdof;
                    }


                    /////check stability
                    var mindir = new double[3];


                    if ((sumlinearDOF <= 1 && sumrotateDOF <= 1) || isanyfastener == true)
                    //need new if only 1 DOF or 1 roitate DOF
                    {
                        minsigleSB = 9.8; //50??
                        ministablility = 9.8;
                        Program.StablbiblityScores.Add(9.8);
                    }
                    else if (anyscoreinarc == false)
                    {
                        var removalindex = Stabilityfunctions.GetSubPartRemovealDirectionIndexs(refnode, refarcs);
                        var Gdirection = new double[3];
                        ///perpendicular to the ground. TBD
                        foreach (var indxe in removalindex) ///siglepare stability
                        {
                            //Gdirection = Gdirection.add(DisassemblyDirections.Directions[indxe]);

                            Gdirection = Gdirection.add(DisassemblyDirections.Directions[indxe].multiply(-1));
                        }

                        var selected = new double[3];
                        // stability = RotateBeforeTreeSearch.Getstabilityscore(refnode, refarcs, reduceface.Normal, out mindir);

                        stability = Stabilityfunctions.Getstabilityscore(refnode, refarcs, Gdirection.normalize(),
                            out mindir,
                            out selected);
                        if (stability > 9.8)
                        {
                            stability = 9.8;
                        }
                        if (stability < ministablility)
                        {
                            ministablility = stability;
                        }
                        if (stability == 0) //at least one part can tip without any acceleration
                            sub.InternalStabilityInfo.needfixture = true;
                        //minimum acceleration to tip a part

                        var stabilityanddof = new List<double>();
                        stabilityanddof.Add(ministablility);
                        Program.StablbiblityScores.Add(ministablility);
                        for (int j = 0; j < 12; j++)
                        {
                            stabilityanddof.Add(alldof[j]);
                        }
                        graph.nodes.Where(n => n is Component).Cast<Component>().First(n => n.name.Equals(refnode.name))
                            .SingleStabilityAndDOF.Add(refarcsnames, stabilityanddof);
                    }

                    if (100000000000000 < ministablility)
                    {
                        var bug2 = 1;
                    }
                    if (ministablility < minsigleSB)
                    {
                        minsigleSB = ministablility;
                    }

                    totalSBscore.Add(ministablility);
                }
                //old stable score
                //finalscore += totaldof.Sum() / subassembly.Count + maxsigleDOF - (minsigleSB + totalSBscore.Sum() / subassembly.Count)+GetSD(totaldof)+GetSD(totalSBscore);
                finalscore += (totaldof.Sum() / subassembly.Count + maxsigleDOF) / 12 -
                              ((minsigleSB + totalSBscore.Sum() / subassembly.Count)) / 19.6;
                var score = (totaldof.Sum() / subassembly.Count + maxsigleDOF) / 12 -
                            ((minsigleSB + totalSBscore.Sum() / subassembly.Count)) / 19.6;
                //the less the better
                if (finalscore < -99999)
                {
                    var sdf = 1;
                }
                if (i == 1)
                {
                    finalscore = (finalscore + 2) / 4;
                    UpdateInternalStabilityInfo(sub, totaldof, maxsigleDOF, totalSBscore, minsigleSB);
                    sub.InternalStabilityInfo.Totalsecore = finalscore;
                }
                var subnames = new List<string>();
                var othersubnames = new List<string>();
                var allname = new List<List<string>>();
                foreach (var sn in subassembly)
                {
                    subnames.Add(sn.name);
                }
                foreach (var osn in othersubassembly)
                {
                    othersubnames.Add(osn.name);
                }
                allname.Add(subnames);
                allname.Add(othersubnames);
            }
            return finalscore; //return score between 0-1;
        }

        public static double GetSD(IEnumerable<double> values)
        {
            double ret = 0;
            if (values.Count() > 0)
            {
                //Compute the Average      
                double avg = values.Average();
                //Perform the Sum of (value-avg)_2_2      
                double sum = values.Sum(d => Math.Pow(d - avg, 2));
                //Put it all together      
                ret = Math.Sqrt((sum) / (values.Count() - 1));
            }
            return ret;
        }

        public static void EvaluateSubFirstThreeTime(SubAssembly sub, List<Component> refnodes, List<Component> movingnodes,
            double olpdis)
        {
            var inputunitscaler = Program.MeshMagnifier / 1000;//magnifier translate unit to cm
            var movingsolids = new List<TessellatedSolid>();
            var referencesolids = new List<TessellatedSolid>();
            var movingvertex = new List<Vertex>();

            var counter = 10e6;
            ///////
            ///moving
            ///////
            foreach (var n in movingnodes)
            {
                movingsolids.Add(Program.Solids[n.name][0]); //cheange bbbbb class
            }
            foreach (var v in sub.Install.Moving.CVXHull.Vertices)
            {
                movingvertex.Add(v);
            }
            var movingOBB = OBB.BuildUsingPoints(movingvertex);

            //  var movingOBB = OBB.BuildUsingPoints(movingvertex);
            var lengths = new double[]
                {
                    StarMath.norm1(movingOBB.CornerVertices[0].Position.subtract(movingOBB.CornerVertices[1].Position)),
                    StarMath.norm1(movingOBB.CornerVertices[2].Position.subtract(movingOBB.CornerVertices[1].Position)),
                    StarMath.norm1(movingOBB.CornerVertices[0].Position.subtract(movingOBB.CornerVertices[4].Position)),
                };
            var lmax = lengths.Max() / inputunitscaler;//mm
            var lmin = lengths.Min() / inputunitscaler;//mm
            double lmid = lengths.Average() / inputunitscaler * 3 - lmax - lmin;
            var movingweight = Math.Log(sub.Install.Moving.Mass / inputunitscaler / Math.Pow(inputunitscaler, 3)); //in g ???????????
            var OBBvolue = Math.Log(movingOBB.Volume / 1000 / Math.Pow(inputunitscaler, 3));//cm^3
            var OBBmaxsurface = Math.Log(lmax * lmid / 1000);
            var OBBminsurface = Math.Log(lmin * lmid / 1000);
            /////
            /// distance TBD
            var movingdistance = 50;//cm
            var movingdis = Math.Log(movingdistance);
            var movinginput = new double[5] { movingweight, OBBvolue, OBBmaxsurface, OBBminsurface, movingdis };
            sub.MoveRoateModelInputs = OnlineGPupdating.RowtoMatrix(movinginput);
            //    Log ( weight, OOBvol OBB, maxf OBB, minf moving, distance  )all in mmgs unit system 
            double movingtime = 0.0;
            double movingSD = 0.0;
            CalculateAssemblyTimeAndSD.GetTimeAndSD(sub, movinginput, "moving", out movingtime, out movingSD);

            // Program.allmtime.Add(movingtime);
            ///////
            ///install
            ///////
            var allolpfs = BlockingDetermination.OverlappingSurfaces.FindAll(
                s =>
                    (movingnodes.Any(n => n.name.Equals(s.Solid1.Name))) &&
                    (refnodes.Any(n => n.name.Equals(s.Solid2.Name)))
                    ||
                    (movingnodes.Any(n => n.name.Equals(s.Solid2.Name))) &&
                    (refnodes.Any(n => n.name.Equals(s.Solid1.Name)))
                );
            double olpfeatures = 0.0;
            for (int i = 0; i < allolpfs.Count; i++)
            {
                olpfeatures += allolpfs[i].Overlappings.Count; ////no alignment features for now;
            }
            if (olpfeatures == 0)
            {
                olpfeatures = 0.3;
            }
            var logOverlappingDist = Math.Log(olpdis / inputunitscaler);
            if (double.IsInfinity(logOverlappingDist)) logOverlappingDist = 1e-6;
            var installinput = new double[6] { movingweight, OBBvolue, OBBmaxsurface, OBBminsurface, logOverlappingDist, Math.Log(olpfeatures) };
            sub.InstallModelInputs = OnlineGPupdating.RowtoMatrix(installinput);
            double installtime = 0;
            double instalSD = 0;
            CalculateAssemblyTimeAndSD.GetTimeAndSD(sub, installinput, "install", out installtime, out instalSD);
            //Program.allitime.Add(installtime);
            ///////
            ///secure
            ///////
            var securetime = 0.0;
            var securesd = 0.0;
            if (sub.Secure.Fasteners != null)
            {
                foreach (var f in sub.Secure.Fasteners)
                {
                    double eachseucretime = 0;
                    double eachsecureSD = 0;
                    var secureinput = new double[6];
                    if (f.SecureModelInputs == null)
                    {
                        if (f.Diameter == 0) f.Diameter = 1;
                        if (f.OverallLength == 0) f.OverallLength = 1;
                        var OBBmax = (f.Diameter / inputunitscaler) * (f.OverallLength / inputunitscaler);
                        var OBBmin = (f.Diameter / inputunitscaler) * (f.Diameter / inputunitscaler);
                        var numberofthreads = f.NumberOfThreads;
                        if (numberofthreads == 0)
                        {
                            numberofthreads = 10;
                        }
                        var insertdistance = f.EngagedLength / inputunitscaler * 0.5;
                        if (insertdistance == 0)
                        {
                            insertdistance = numberofthreads * 0.25;
                        }
                        var nut = 1;
                        var tool = 2;
                        if (f.Nuts != null)
                        {
                            nut = 2;
                        }
                        secureinput = new double[6] { Math.Log(OBBmax / (double)1000), Math.Log(OBBmin / (double)1000), Math.Log(numberofthreads), Math.Log(insertdistance), nut, tool };
                        f.SecureModelInputs = OnlineGPupdating.RowtoMatrix(secureinput);
                    }
                    else
                    {
                        secureinput = OnlineGPupdating.MatrixtoRow(f.SecureModelInputs);
                    }
                    CalculateAssemblyTimeAndSD.GetTimeAndSD(sub, secureinput, "s", out eachseucretime, out eachsecureSD);
                    securetime += eachseucretime;
                    securesd += eachsecureSD;
                    f.Time = eachseucretime;
                    f.TimeSD = eachsecureSD;
                    if (eachseucretime > 30)
                    {
                        var sssss = 1;
                    }
                    if (securetime != 0)
                        Program.gpsecuretime.Add(eachseucretime);
                }
            }
            if (installtime < 0)
                installtime = 0.5;
            if (movingtime < 0)
                movingtime = 0.5;
            sub.MoveRoate.Time = movingtime;
            sub.MoveRoate.TimeSD = movingSD;
            var totalSD = instalSD + movingSD;
            sub.Install.Time = installtime;
            sub.Install.TimeSD = totalSD;
            sub.Secure.Time = securetime;
            sub.Secure.TimeSD = securesd;
            var totaltime = installtime + movingtime;



            Program.gpinstalltime.Add(installtime);
            Program.gpmovingtime.Add(movingtime);
        }


        private void UpdateInternalStabilityInfo(SubAssembly sub, List<double> totaldof, double maxsigleDOF,
            List<double> totalSBscore, double minsigleSB)
        {
            sub.InternalStabilityInfo.OverAllDOF = totaldof.Sum();
            sub.InternalStabilityInfo.MaxSingleDOF = maxsigleDOF;
            sub.InternalStabilityInfo.OverAllTippingScore = totalSBscore.Sum();
            sub.InternalStabilityInfo.MinSingleTippingScore = minsigleSB;
        }
        public static bool IsSameImputs(double[] newinputs, double[,] historicalinput)
        {
            var oldinputs = OnlineGPupdating.MatrixtoRow(historicalinput);
            bool sameinput = true;
            for (int i = 0; i < newinputs.Length; i++)
            {
                var testnewdata = newinputs[i] + 10e-15;// incase is zero
                var testolddata = oldinputs[i] + 10e-15;
                if (Math.Abs((testnewdata - testolddata) / testolddata) < 0.05)
                {
                    continue;
                }
                else
                {
                    sameinput = false;
                    break;
                }
            }
            return sameinput;
        }
        private HashSet<Double[]> InsertionDirectionsFromRemovalDirections(SubAssembly sub, List<Component> optNodes,
            HashSet<int> validDirs)
        {
            // Note: The hashset of validDirs are the removal directions to remove optNodes from the rest.
            //       If the optNodes is the reference, then the installDirections are the same as validDirs
            //       If the optNodes is the moving, then the InstallDirection is the opposite of the validDirs

            var dirs = new HashSet<Double[]>();
            if (sub.Install.Reference.PartNames.All(optNodes.Select(n => n.name).ToList().Contains) &&
                optNodes.Select(n => n.name).ToList().All(sub.Install.Reference.PartNames.Contains))
            {
                foreach (var dir in validDirs)
                    dirs.Add(DisassemblyDirections.Directions[dir]);
                return dirs;
            }
            foreach (var dir in validDirs)
                dirs.Add(DisassemblyDirections.Directions[dir].multiply(-1.0));
            return dirs;
        }

        private List<Fastener> ConnectingArcsFastener(List<Connection> connectingArcs,
            HashSet<Component> subassemblyNodes, List<Component> optNodes, List<Component> rest)
        {
            // if all of the parts exist in the partsLockedByFastener, are in the subassemblyNodes, add the fastenrs to subAssembly,
            // otherwise it's removed before

            if (!AdjacentParts.Any())
            {
                foreach (Component comp in Program.AssemblyGraph.nodes)
                {
                    var adjacents = new HashSet<Component>();
                    foreach (Connection compArc in comp.arcs.Where(a => a is Connection))
                    {
                        if (compArc.From.name == comp.name)
                            adjacents.Add((Component)compArc.To);
                        else adjacents.Add((Component)compArc.From);
                    }
                    AdjacentParts.Add(comp, adjacents);
                }
            }
            var fasteners = new List<Fastener>();
            foreach (var arc in connectingArcs)
            {
                foreach (var fas in arc.Fasteners.Where(a => !fasteners.Contains(a)))
                {
                    if (fas.PartsLockedByFastener.All(ind => subassemblyNodes.Contains(Program.AssemblyGraph.nodes[ind])))
                    {
                        if (fasteners.Contains(fas)) continue;
                        fasteners.Add(fas);
                    }
                }
            }

            // now see if there is any pin:
            var partsWithPins = subassemblyNodes.Where(n => n.Pins.Any()).ToList();
            if (!partsWithPins.Any())
                return fasteners;
            foreach (var part in partsWithPins)
            {
                var howManyInOption = AdjacentParts[part].Count(optNodes.Contains);
                if (AdjacentParts[part].All(subassemblyNodes.Contains) && howManyInOption > 0 &&
                    howManyInOption <= AdjacentParts[part].Count)
                {
                    fasteners.AddRange(part.Pins.Where(p => !fasteners.Contains(p)));
                }
                else
                {
                    var howManyInOption2 = AdjacentParts[part].Count(rest.Contains);
                    if (AdjacentParts[part].All(subassemblyNodes.Contains) && howManyInOption2 > 0 &&
                        howManyInOption2 <= AdjacentParts[part].Count)
                    {
                        fasteners.AddRange(part.Pins.Where(p => !fasteners.Contains(p)));
                    }
                }
            }
            return fasteners;
        }

        /// <summary>
        /// returns the subassembly class given the two lists of components
        /// </summary>
        /// <param name="opt">The opt.</param>
        /// <param name="rest">The rest.</param>
        /// <returns>SubAssembly.</returns>
        public SubAssembly Update(List<Component> opt, List<Component> rest)
        {
            //todo: change List to HashSet
            Part refAssembly, movingAssembly;
            var movingNodes = opt;
            var newSubAsmNodes = rest;
            if (movingNodes.Count == 1)
            {
                var nodeName = movingNodes[0].name;
                movingAssembly = new SubAssembly(new HashSet<Component>(movingNodes), ConvexHullsForParts[nodeName],
                    movingNodes[0].Mass,
                    movingNodes[0].Volume, new Vertex(movingNodes[0].CenterOfMass));
                //movingAssembly = new Part(nodeName, movingNodes[0].Volume, movingNodes[0].Volume,
                //    ConvexHullsForParts[nodeName], new Vertex(movingNodes[0].CenterOfMass));
            }
            else
            {
                var combinedCVXHullM = CreateCombinedConvexHull2(movingNodes);
                var VolumeM = GetSubassemblyVolume(movingNodes);
                var MassM = GetSubassemblyMass(movingNodes);
                var centerOfMass = GetSubassemblyCenterOfMass(movingNodes);
                movingAssembly = new SubAssembly(new HashSet<Component>(movingNodes), combinedCVXHullM, MassM, VolumeM,
                    centerOfMass);
            }

            var referenceHyperArcnodes = new List<Component>();
            referenceHyperArcnodes = (List<Component>)newSubAsmNodes.Where(a => !movingNodes.Contains(a)).ToList();
            if (referenceHyperArcnodes.Count == 1)
            {
                var nodeName = referenceHyperArcnodes[0].name;
                refAssembly = new SubAssembly(new HashSet<Component>(referenceHyperArcnodes),
                    ConvexHullsForParts[nodeName],
                    referenceHyperArcnodes[0].Mass, referenceHyperArcnodes[0].Volume,
                    new Vertex(referenceHyperArcnodes[0].CenterOfMass));
                //refAssembly = new Part(nodeName, referenceHyperArcnodes[0].Mass, referenceHyperArcnodes[0].Volume,
                //    ConvexHullsForParts[nodeName],
                //   new Vertex(referenceHyperArcnodes[0].CenterOfMass));
            }
            else
            {
                var combinedCVXHullR = CreateCombinedConvexHull2(referenceHyperArcnodes);
                var VolumeR = GetSubassemblyVolume(referenceHyperArcnodes);
                var MassR = GetSubassemblyMass(referenceHyperArcnodes);
                var centerOfMass = GetSubassemblyCenterOfMass(referenceHyperArcnodes);
                refAssembly = new SubAssembly(new HashSet<Component>(referenceHyperArcnodes), combinedCVXHullR, MassR,
                    VolumeR, centerOfMass);
            }
            var combinedCvxHull = CreateCombinedConvexHull(refAssembly.CVXHull, movingAssembly.CVXHull);
            List<PolygonalFace> movingFacesInCombined;
            List<PolygonalFace> refFacesInCombined;
            var InstallCharacter = shouldReferenceAndMovingBeSwitched(refAssembly, movingAssembly, combinedCvxHull,
                out refFacesInCombined, out movingFacesInCombined);
            if ((int)InstallCharacter < 0)
            {
                var tempASM = refAssembly;
                refAssembly = movingAssembly;
                movingAssembly = tempASM;
                refFacesInCombined = movingFacesInCombined; // no need to use temp here, as the movingFaces in the 
                // combined convex hull are not needed.
                InstallCharacter = (InstallCharacterType)(-((int)InstallCharacter));
            }
            var newSubassembly = new SubAssembly(refAssembly, movingAssembly, combinedCvxHull, InstallCharacter,
                refFacesInCombined);
            newSubassembly.CenterOfMass = CombinedCenterOfMass(newSubassembly);
            return newSubassembly;
        }

        private Vertex CombinedCenterOfMass(SubAssembly newSubassembly)
        {
            return
                new Vertex(new[]
                {
                    (newSubassembly.Install.Moving.CenterOfMass.Position[0] +
                     newSubassembly.Install.Reference.CenterOfMass.Position[0])/2,
                    (newSubassembly.Install.Moving.CenterOfMass.Position[1] +
                     newSubassembly.Install.Reference.CenterOfMass.Position[1])/2,
                    (newSubassembly.Install.Moving.CenterOfMass.Position[2] +
                     newSubassembly.Install.Reference.CenterOfMass.Position[2])/2
                });
        }

        private TVGLConvexHull CreateCombinedConvexHull(TVGLConvexHull refCVXHull, TVGLConvexHull movingCVXHull)
        {
            var pointCloud = new List<Vertex>(refCVXHull.Vertices);
            pointCloud.AddRange(movingCVXHull.Vertices);
            return new TVGLConvexHull(pointCloud, 1e-8);
        }

        public static TVGLConvexHull CreateCombinedConvexHull2(List<Component> nodes)
        {
            var pointCloud = new List<Vertex>();
            foreach (var n in nodes)
            {
                var nodeName = n.name;
                pointCloud.AddRange(ConvexHullsForParts[nodeName].Vertices);
            }
            return new TVGLConvexHull(pointCloud, 1e-8);
        }

        public static double GetSubassemblyVolume(List<Component> nodes)
        {
            return nodes.Sum(n => n.Volume);
        }

        public static double GetSubassemblyMass(List<Component> nodes)
        {
            return nodes.Sum(n => n.Mass);
        }

        public static Vertex GetSubassemblyCenterOfMass(List<Component> nodes)
        {
            var sumMx = 0.0;
            var sumMy = 0.0;
            var sumMz = 0.0;
            var M = 0.0;
            foreach (var n in nodes)
            {
                var m = n.Mass;
                var nCOM = n.CenterOfMass;
                sumMx += nCOM[0] * m;
                sumMy += nCOM[1] * m;
                sumMz += nCOM[2] * m;
                M += m;
            }

            return new Vertex(new[] { sumMx / M, sumMy / M, sumMz / M });
        }

        private InstallCharacterType shouldReferenceAndMovingBeSwitched(Part refAssembly, Part movingAssembly,
            TVGLConvexHull combinedCVXHull,
            out List<PolygonalFace> refFacesInCombined,
            out List<PolygonalFace> movingFacesInCombined)
        {
            /* first, create a list of vertices from the reference hull that are present in the combined hull.
             * likewise, with the moving. */
            var refVertsInCombined = new List<Vertex>();
            var movingVertsInCombined = new List<Vertex>();
            foreach (var pt in combinedCVXHull.Vertices)
            {
                if (refAssembly.CVXHull.Vertices.Contains(pt)) refVertsInCombined.Add(pt);
                else
                {
                    /* this additional Contains function is unnecessary and potential time-consuming. 
                     * It was implemented for initial validiation, but it is commented out now. 
                    if (!movingAssembly.CVXHull.Points.Contains(pt)) 
                        throw new Exception("The point is in neither original part!");  */
                    movingVertsInCombined.Add(pt);
                }
            }
            /* If none of the combined vertices are from the moving, we can end this function early and save time. */
            if (movingVertsInCombined.Count == 0)
            {
                movingFacesInCombined = null;
                refFacesInCombined = new List<PolygonalFace>(combinedCVXHull.Faces);
                return InstallCharacterType.MovingIsInsideReference;
            }
            /* ...likewise for the original reference */
            if (refVertsInCombined.Count == 0)
            {
                refFacesInCombined = null;
                movingFacesInCombined = new List<PolygonalFace>(combinedCVXHull.Faces);
                return InstallCharacterType.ReferenceIsInsideMoving;
            }
            /* we could just count the number of vertices, but that would not be as accurate a prediction
             * as the area of the faces */
            refFacesInCombined = new List<PolygonalFace>();
            movingFacesInCombined = new List<PolygonalFace>();
            double refFaceArea = 0.0;
            var movingFaceArea = 0.0;
            var totalFaceArea = 0.0;
            foreach (var face in combinedCVXHull.Faces)
            {
                var faceArea = findFaceArea(face);
                totalFaceArea += faceArea;
                if (face.Vertices.All(v => refAssembly.CVXHull.Vertices.Contains(v)))
                {
                    refFacesInCombined.Add(face);
                    refFaceArea += faceArea;
                }
                else if (face.Vertices.All(v => movingAssembly.CVXHull.Vertices.Contains(v)))
                {
                    movingFacesInCombined.Add(face);
                    movingFaceArea += faceArea;
                }
            }
            /* former faces is the sum areas of faces from prior cvx hulls */
            var formerFacesArea = refFaceArea + movingFaceArea;
            /* if the former face area does not take up a significant portion of 
             * the new faces then we do not have the confidence to make the judgement
             * based on this fact. */
            if (formerFacesArea / totalFaceArea > Constants.CVXFormerFaceConfidence)
            {
                /* there are two check here: if the common area is very small, we assume the 
                 * subassembly is inside the other. If not, maybe it is more on the outside
                 * but a smaller effect on resulting convex hull. */
                if (refFaceArea / formerFacesArea < Constants.CVXOnInsideThreshold)
                    return InstallCharacterType.ReferenceIsInsideMoving;
                if (movingFaceArea / formerFacesArea < Constants.CVXOnInsideThreshold)
                    return InstallCharacterType.MovingIsInsideReference;
                if (refFaceArea / formerFacesArea < Constants.CVXOnOutsideThreshold)
                    return InstallCharacterType.ReferenceIsOnOutsideOfMoving;
                if (movingFaceArea / formerFacesArea < Constants.CVXOnOutsideThreshold)
                    return InstallCharacterType.MovingIsOnOutsideOfReference;
            }
            /* if we cannot confidently use face area then we switch to comparing
             * the magnitudes of the moment of inertia. */
            else
            {
                if (refAssembly.AvgMomentofInertia >= movingAssembly.AvgMomentofInertia)
                    return InstallCharacterType.MovingReferenceSimiliar;
                else return InstallCharacterType.ReferenceMovingSimiliarSwitch;
            }
            /* this will not be invoked, but it is left as a final result in case these heuristic cases should change. */
            return InstallCharacterType.Unknown;
        }

        private double findFaceArea(PolygonalFace face)
        {
            var v1 = face.Vertices[1].Position.subtract(face.Vertices[0].Position);
            var v2 = face.Vertices[2].Position.subtract(face.Vertices[1].Position);

            return 0.5 * v1.crossProduct(v2).norm2();
        }

        /// <summary>
        /// if either part is really non-contiguous then return true. We do NOT want to adress
        /// these cases - they should be viewed as two separate install steps.
        /// </summary>
        /// <param name="install">The install.</param>
        /// <param name="A">a.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        internal static bool EitherRefOrMovHasSeperatedSubassemblies(List<Component>[] install,
            HashSet<Component> A)
        {
            foreach (var subAsm in install)
            {
                var stack = new Stack<Component>();
                var visited = new HashSet<Component>();
                var globalVisited = new HashSet<Component>();
                foreach (var Component in subAsm.Where(n => !globalVisited.Contains(n)))
                {
                    stack.Clear();
                    visited.Clear();
                    stack.Push(Component);
                    while (stack.Count > 0)
                    {
                        var pNode = stack.Pop();
                        visited.Add(pNode);
                        globalVisited.Add(pNode);
                        var a2 = new List<Connection>();
                        lock (pNode.arcs)
                            a2 = pNode.arcs.Where(a => a is Connection).Cast<Connection>().ToList();

                        foreach (Connection arc in a2)
                        {
                            if (!A.Contains(arc.From) || !A.Contains(arc.To) ||
                                !subAsm.Contains(arc.From) || !subAsm.Contains(arc.To)) continue;
                            var otherNode = (Component)(arc.From == pNode ? arc.To : arc.From);
                            if (visited.Contains(otherNode))
                                continue;
                            stack.Push(otherNode);
                        }
                    }
                    if (visited.Count < subAsm.Count)
                        return true;
                }
            }
            return false;
        }

        private static double DetermineDistanceToSeparateHull(SubAssembly newSubAsm, Double[] insertionDirection, out double cvhOverlapDis)
        {
            var refMaxValue =
                GeometryFunctions.FindMaxPlaneHeightInDirection(newSubAsm.Install.Reference.CVXHull.Vertices,
                    insertionDirection);
            var refMinValue =
                GeometryFunctions.FindMinPlaneHeightInDirection(newSubAsm.Install.Reference.CVXHull.Vertices,
                    insertionDirection);

            var movingMaxValue =
                GeometryFunctions.FindMaxPlaneHeightInDirection(newSubAsm.Install.Moving.CVXHull.Vertices,
                    insertionDirection);
            var movingMinValue =
                GeometryFunctions.FindMinPlaneHeightInDirection(newSubAsm.Install.Moving.CVXHull.Vertices,
                    insertionDirection);

            var distanceToFree = Math.Abs(movingMaxValue - refMinValue); //new
            //var distanceToFree = Math.Abs(refMaxValue - movingMinValue);
            if (distanceToFree < 0)
            {
                distanceToFree = 0;
                throw new Exception("How is distance to free less than zero?");
            }
            cvhOverlapDis = distanceToFree;
            return distanceToFree + (movingMaxValue - movingMinValue) / 2.0 + (refMaxValue - refMinValue) / 2.0;
        }
    }
}