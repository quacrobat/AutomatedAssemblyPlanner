﻿using GraphSynth.Representation;
using System.Collections.Generic;
using System.Linq;

namespace AssemblyEvaluation
{
    class FeasibilityEvaluator
    {                     
        internal double EvaluateOrder(List<node> movingNodes, List<node> refNodes)
        {
            var movingNodeScores = (from n in movingNodes
                                    let i = n.localVariables.IndexOf(-8000)
                                    select n.localVariables[i + 1]);


            var refNodeScores = (from n in refNodes
                                 let i = n.localVariables.IndexOf(-8000)
                                 select n.localVariables[i + 1]);

            return movingNodeScores.Average() + refNodeScores.Average();
        }
    }
}
