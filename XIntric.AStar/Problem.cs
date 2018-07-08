using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace XIntric.AStar
{
    public interface IProblem<TState,TCost,TDistance>
    {
        Task<IEnumerable<INode<TState, TCost, TDistance>>> ExpandNode(INode<TState, TCost, TDistance> parent);
        IComparer<TCost> CostComparer { get; }
        IComparer<TDistance> DistanceComparer { get; }
        TCost Accumulate(TCost accumulated, TCost addcost);
        TCost GetTotalCost(TCost accumulated, TDistance estimateddistance);
        TCost InitCost { get; }
    }

    public static class Problem
    {
        public static Traversal<TState, TCost, TDistance> CreateTraversal<TState, TCost, TDistance>(
            this IProblem<TState, TCost, TDistance> problem,
            IScenario<TState, TDistance> scenario,
            TState initstate
            )
            => new Traversal<TState, TCost, TDistance>(problem, scenario,initstate);
    }
}
