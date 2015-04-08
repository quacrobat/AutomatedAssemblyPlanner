﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GraphSynth.Representation;

namespace Assembly_Planner
{
    internal class OptionGenerator
    {
        static List<hyperarc> Preceedings = new List<hyperarc>();

        internal static void GenerateOptions(designGraph assemblyGraph, hyperarc seperate,
            Dictionary<hyperarc, List<hyperarc>> blockingDic)
        {
            // This sorting is not necessary, but I think it will speed up the search. Let's see!
            blockingDic = blockingDic.OrderBy(a => a.Value.Count).ToDictionary(x => x.Key, x => x.Value);
            var localRemovableHy = new List<List<hyperarc>>();
            var trash = new List<hyperarc>();
            var visitedStatesCount = 0;
            var visitedStates = new List<hyperarc>();
            while (visitedStatesCount != blockingDic.Count)
            {
                foreach (var sccHy in blockingDic.Keys.Where(scc => !visitedStates.Contains(scc)))
                {
                    if (blockingDic[sccHy].Count == 0)
                    {
                        sccHy.localLabels.Add(DisConstants.Removable);
                        trash.Add(sccHy);
                        visitedStatesCount++;
                        visitedStates.Add(sccHy);
                        localRemovableHy.Add(new List<hyperarc> {sccHy});
                    }
                    else
                    {
                        if (blockingDic[sccHy].All(trash.Contains))
                        {
                            PreceedingFinder(sccHy, blockingDic);
                            var nodes = new List<node>();
                            foreach (var hyperarc in Preceedings)
                                nodes.AddRange(hyperarc.nodes);
                            if (nodes.Count == seperate.nodes.Count) continue;
                            assemblyGraph.addHyperArc(nodes);
                            assemblyGraph.hyperarcs[assemblyGraph.hyperarcs.Count - 1].localLabels.Add(
                                DisConstants.Removable);
                            visitedStatesCount++;
                            visitedStates.Add(sccHy);
                            localRemovableHy.Add(Preceedings);
                            Preceedings.Clear();
                        }
                    }
                }
            }
            //I am not done yet, not all of the options are generated yet!
            for (var i = 0; i < localRemovableHy.Count-1; i++)
            {
                for (var j = 0; j < localRemovableHy.Count; j++)
                {
                    if (localRemovableHy[i].All(localRemovableHy[j].Contains) ||
                        localRemovableHy[j].All(localRemovableHy[i].Contains))
                    continue;
                    var merged = new List<hyperarc>();
                    merged.AddRange(localRemovableHy[i]);
                    merged.AddRange(localRemovableHy[j]);
                    var exists = false;
                    for (var k = j + 1; k < localRemovableHy.Count; k++)
                    {
                        if (localRemovableHy[k].All(merged.Contains)&&
                            merged.All(localRemovableHy[k].Contains))
                        {
                            exists = true;
                            break;
                        }
                    }
                    if (exists) continue;
                    var nodes = new List<node>();
                    foreach (var hy in merged)
                        nodes.AddRange(hy.nodes);
                    assemblyGraph.addHyperArc(nodes);
                    assemblyGraph.hyperarcs[assemblyGraph.hyperarcs.Count - 1].localLabels.Add(
                                DisConstants.Removable);
                    localRemovableHy.Add(merged);
                }
            }
            foreach (var SCCHy in assemblyGraph.hyperarcs.Where( hyScc =>
                            hyScc.localLabels.Contains(DisConstants.SCC) &&
                            !hyScc.localLabels.Contains(DisConstants.Removable)))
                assemblyGraph.hyperarcs.Remove(SCCHy);
        }

        private static void PreceedingFinder(hyperarc sccHy, Dictionary<hyperarc, List<hyperarc>> blockingDic)
        {
            Preceedings.Add(sccHy);
            foreach (var value in blockingDic[sccHy])
            {
                PreceedingFinder(value, blockingDic);
            }
        }
    }
}
