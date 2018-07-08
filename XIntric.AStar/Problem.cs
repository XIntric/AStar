using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace XIntric.AStar
{
    public interface IProblem<TState,TCost>
    {
        Task<IEnumerable<INode<TState, TCost>>> ExpandNode(Traversal<TState,TCost>.INodeExpansion parent);
        IComparer<TCost> Comparer { get; }
        TCost Accumulate(TCost accumulated, TCost addcost);
        TCost InitCost { get; }
    }

    public static class Problem
    {
        public static Traversal<TState, TCost> CreateTraversal<TState, TCost>(
            this IProblem<TState, TCost> problem,
            IScenario<TState, TCost> scenario,
            TState initstate
            )
            => new Traversal<TState, TCost>(problem, scenario,initstate);
    }
}
