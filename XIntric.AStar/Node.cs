using System;
using System.Collections.Generic;
using System.Text;

namespace XIntric.AStar
{
    public class Node<TState,TDistance>
    {
        public Node(Node<TState, TDistance> parent, TState state, TDistance accumulatedcost, TDistance estimateddistance, SumCost<TDistance> sumcost)
        {
            SumCost = sumcost;

            Parent = parent;
            State = state;
            AccumulatedCost = accumulatedcost;
            EstimatedDistance = estimateddistance;

            TotalCost = sumcost(accumulatedcost,estimateddistance);
        }

        public Node<TState, TDistance> Parent { get; }
        public TDistance AccumulatedCost { get; }
        public TDistance EstimatedDistance { get; }
        public TDistance TotalCost { get; }
        public TState State { get; }
        SumCost<TDistance> SumCost;

        public Node<TState, TDistance> CreateChild(TState token, TDistance stepcost, TDistance estimateddistance)
            => new Node<TState, TDistance>(this, token, SumCost(AccumulatedCost,stepcost), estimateddistance, SumCost);


        public override bool Equals(object obj) => (obj as Node<TState, TDistance>)?.State?.Equals(State) ?? false;
        public override int GetHashCode() => State.GetHashCode();



        //public class Comparer : IComparer<Node<TState, TDistance>>
        //{
        //    public Comparer(IDistanceDefinitions<TDistance> def)
        //    {
        //        DistanceDefinitions = def;
        //    }

        //    IDistanceDefinitions<TDistance> DistanceDefinitions;

        //    public int Compare(Node<TState, TDistance> x, Node<TState, TDistance> y)
        //        => DistanceDefinitions.Compare(x.TotalCost, y.TotalCost);
        //}


    }
}
