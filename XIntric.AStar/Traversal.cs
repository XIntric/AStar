using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace XIntric.AStar
{



    public class Traversal<TState,TCost>
    {
        public Traversal(
            IProblem<TState, TCost> problem,
            IScenario<TState, TCost> scenario,
            TState initstate
            )
        {
            Problem = problem;
            Scenario = scenario;
            var initstateestimateddistance = scenario.GetDistance(initstate);
            OpenNodes = new NodeRepository<TState, TCost>(
                new Node.Primitive<TState, TCost>(null, initstate, Problem.InitCost, initstateestimateddistance, Problem.Accumulate(Problem.InitCost, initstateestimateddistance)),
                Problem.Comparer);
        }


        public Task<INode<TState,TCost>> Result => ResultSetter.Task;
        public event Action<INode<TState,TCost>> Diag_NodeTraversing;
        public event Action<INode<TState,TCost>> Diag_NodeTraversed;

        public CancellationToken WorkToken => WorkTokenSource.Token;
        CancellationTokenSource WorkTokenSource = new CancellationTokenSource();




        public async Task<INode<TState,TCost>> StepAsync()
        {
            if (Result.IsCompleted) return Result.Result;
            try
            {
                //System.Console.WriteLine($"{Thread.CurrentThread.ManagedThreadId}: Step started.");


                return await OpenNodes.PerformWorkerOperationAsync(
                    async node =>
                {
                    Diag_NodeTraversing?.Invoke(node);
                    if (Problem.Comparer.Compare(node.EstimatedDistance,Scenario.AcceptedDistance) <= 0) //Found a goal node
                    {
                        lock (ResultSetter)
                        {
                            ResultSetter.SetResult(node);
                            WorkTokenSource.Cancel();
                            return node;
                        }
                    }


                    var newnodes = (await Problem.ExpandNode(new NodeExpansionImplementation(this,node))).ToList();

                    foreach(var newnode in newnodes)
                    {
                        OpenNodes.Add(newnode);
                    }

                    Diag_NodeTraversed?.Invoke(node);
                    return node;
                });

            }
            catch(NodeRepository<TState,TCost>.QueueExhaustedException)
            {
                if (Result.IsCompleted) return Result.Result;
                if (Result.IsFaulted) throw Result.Exception;
                var ex = new MissingSolutionException();
                ResultSetter.TrySetException(ex);
                throw ex;
            }
            catch(Exception e)
            {
                ResultSetter.TrySetException(e);
                throw;
            }

        }

        NodeRepository<TState,TCost> OpenNodes;
        TaskCompletionSource<INode<TState,TCost>> ResultSetter = new TaskCompletionSource<INode<TState,TCost>>();

        public class MissingSolutionException : Exception
        {
            public MissingSolutionException() : base("Unable to find path to goal.") { }
        }

        public class InternalErrorException : AggregateException
        {
            public InternalErrorException(INode<TState,TCost> currentnode, Exception subexception)
                : base("An internal exception was detected",subexception)
            {
                CurrentNode = currentnode;
            }

            public INode<TState,TCost> CurrentNode { get; }

        }


        public interface INodeExpansion
        {
            INode<TState,TCost> Node { get; }
            INode<TState, TCost> CreateChild(TState childstate, TCost stepcost);
        }

        class NodeExpansionImplementation : INodeExpansion
        {
            public NodeExpansionImplementation(Traversal<TState,TCost> traversal, INode<TState,TCost> node)
            {
                Traversal = traversal;
                Node = node;
            }

            Traversal<TState, TCost> Traversal;
            public INode<TState, TCost> Node { get; }

            public INode<TState, TCost> CreateChild(TState childstate, TCost stepcost)
            {
                var accumulatedcost = Traversal.Problem.Accumulate(Node.AccumulatedCost, stepcost);
                var distance = Traversal.Scenario.GetDistance(childstate);
                var totalcost = Traversal.Problem.Accumulate(accumulatedcost, distance);
                return new Node.Primitive<TState, TCost>(Node, childstate, accumulatedcost, distance, totalcost);
            }
        }
        IProblem<TState, TCost> Problem;
        IScenario<TState, TCost> Scenario;

    }
}
