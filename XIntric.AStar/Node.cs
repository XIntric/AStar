using System;
using System.Collections.Generic;
using System.Text;

namespace XIntric.AStar
{
    public interface INode<TState,TCost>
    {
        INode<TState,TCost> Parent { get; }
        TState State { get; }
        TCost AccumulatedCost { get; }
        TCost EstimatedDistance { get; }
        TCost TotalCost { get; }
    }

    public static class Node
    {
        public static INode<TState, TCost> Create<TState, TCost>(INode<TState, TCost> parent,
            TState state,
            TCost accumulatedcost,
            TCost estimateddistance,
            TCost totalcost)
            => new Primitive<TState, TCost>(parent, state, accumulatedcost, estimateddistance, totalcost);

        public class Primitive<TState, TCost> : INode<TState, TCost>
        {
            public Primitive(INode<TState, TCost> parent,
                TState state,
                TCost accumulatedcost,
                TCost estimateddistance,
                TCost totalcost)
            {
                Parent = parent;
                State = state;
                AccumulatedCost = accumulatedcost;
                EstimatedDistance = estimateddistance;
                TotalCost = totalcost;
            }

            public INode<TState, TCost> Parent { get; }
            public TState State { get; }
            public TCost AccumulatedCost { get; }
            public TCost EstimatedDistance { get; }
            public TCost TotalCost { get; }

            public override bool Equals(object obj) => (obj as INode<TState, TCost>)?.State?.Equals(State) ?? false;
            public override int GetHashCode() => State.GetHashCode();

        }

    }



    //public class OldNode<TState,TDistance>
    //{
    //    public OldNode(OldNode<TState, TDistance> parent, TState state, TDistance accumulatedcost, TDistance estimateddistance, SumCost<TDistance> sumcost)
    //    {
    //        SumCost = sumcost;

    //        Parent = parent;
    //        State = state;
    //        AccumulatedCost = accumulatedcost;
    //        EstimatedDistance = estimateddistance;

    //        TotalCost = sumcost(accumulatedcost,estimateddistance);
    //    }

    //    public OldNode<TState, TDistance> Parent { get; }
    //    public TDistance AccumulatedCost { get; }
    //    public TDistance EstimatedDistance { get; }
    //    public TDistance TotalCost { get; }
    //    public TState State { get; }
    //    SumCost<TDistance> SumCost;

    //    public OldNode<TState, TDistance> CreateChild(TState token, TDistance stepcost, TDistance estimateddistance)
    //        => new OldNode<TState, TDistance>(this, token, SumCost(AccumulatedCost,stepcost), estimateddistance, SumCost);


    //    public override bool Equals(object obj) => (obj as OldNode<TState, TDistance>)?.State?.Equals(State) ?? false;
    //    public override int GetHashCode() => State.GetHashCode();



    //    //public class Comparer : IComparer<Node<TState, TDistance>>
    //    //{
    //    //    public Comparer(IDistanceDefinitions<TDistance> def)
    //    //    {
    //    //        DistanceDefinitions = def;
    //    //    }

    //    //    IDistanceDefinitions<TDistance> DistanceDefinitions;

    //    //    public int Compare(Node<TState, TDistance> x, Node<TState, TDistance> y)
    //    //        => DistanceDefinitions.Compare(x.TotalCost, y.TotalCost);
    //    //}


    //}
}
