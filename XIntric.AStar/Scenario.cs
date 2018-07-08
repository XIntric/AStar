using System;
using System.Collections.Generic;
using System.Text;

namespace XIntric.AStar
{
    public interface IScenario<TState,TCost>
    {
        TCost GetDistance(TState state);
        TCost AcceptedDistance { get; }
    }

    public static class Scenario
    {
        public static IScenario<TState, TCost> Create<TState, TCost>(
            Func<TState, TCost> getdistance,
            TCost accepteddistance)
            => new Primitive<TState, TCost>() { AcceptedDistance = accepteddistance, GetDistance = getdistance };

        public class Primitive<TState, TCost> : IScenario<TState, TCost>
        {
            public TCost AcceptedDistance { get; set; }
            public Func<TState,TCost> GetDistance { get; set; }

            TCost IScenario<TState, TCost>.GetDistance(TState state) => GetDistance(state);
        }


    }
}
