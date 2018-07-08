using System;
using System.Collections.Generic;
using System.Text;

namespace XIntric.AStar
{
    public interface IScenario<TState,TDistance>
    {
        TDistance GetDistance(TState state);
        TDistance AcceptedDistance { get; }
    }

    public static class Scenario
    {
        public static IScenario<TState, TDistance> Create<TState, TDistance>(
            Func<TState, TDistance> getdistance,
            TDistance accepteddistance)
            => new Primitive<TState, TDistance>() { AcceptedDistance = accepteddistance, GetDistance = getdistance };

        public class Primitive<TState, TDistance> : IScenario<TState, TDistance>
        {
            public TDistance AcceptedDistance { get; set; }
            public Func<TState,TDistance> GetDistance { get; set; }

            TDistance IScenario<TState, TDistance>.GetDistance(TState state) => GetDistance(state);
        }


    }
}
