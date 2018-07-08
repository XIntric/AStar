using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace XIntric.AStar
{



    public class Traversal<TState,TCost,TDistance>
    {
        public Traversal(
            IProblem<TState, TCost, TDistance> problem,
            IScenario<TState, TDistance> scenario,
            TState initstate
            )
        {
            Problem = problem;
            Scenario = scenario;
            var initstateestimateddistance = scenario.GetDistance(initstate);
            OpenNodes = new NodeRepository<TState, TCost, TDistance>(
                new Node<TState, TCost, TDistance>(null, initstate, Problem.InitCost, initstateestimateddistance, Problem.GetTotalCost(Problem.InitCost, initstateestimateddistance)),
                Problem.CostComparer);
        }

        IProblem<TState, TCost, TDistance> Problem;
        IScenario<TState, TDistance> Scenario;

        public Task<INode<TState,TCost,TDistance>> Result => ResultSetter.Task;


        CancellationTokenSource Cancelled = new CancellationTokenSource();
        CancellationTokenSource GoalFound = new CancellationTokenSource();
        CancellationTokenSource QueueDepleted = new CancellationTokenSource();
        

        public event Action<INode<TState,TCost,TDistance>> Diag_NodeTraversing;
        public event Action<INode<TState,TCost,TDistance>> Diag_NodeTraversed;




        public async Task<INode<TState,TCost,TDistance>> StepAsync()
        {
            try
            {
                //System.Console.WriteLine($"{Thread.CurrentThread.ManagedThreadId}: Step started.");


                return await OpenNodes.PerformWorkerOperationAsync(
                    async node =>
                {
                    Diag_NodeTraversing?.Invoke(node);
                    if (Problem.DistanceComparer.Compare(node.EstimatedDistance,Scenario.AcceptedDistance) <= 0) //Found a goal node
                    {
                        lock (ResultSetter)
                        {
                            System.Console.WriteLine($"{Thread.CurrentThread.ManagedThreadId}: Found goal node!.");
                            ResultSetter.SetResult(node);
                            return node;
                        }
                    }


                    var newnodes = (await Problem.ExpandNode(node)).ToList();

                    foreach(var newnode in newnodes)
                    {
                        OpenNodes.Add(newnode);
                    }

                    Diag_NodeTraversed?.Invoke(node);
                    return node;
                });

            }
            catch(NodeRepository<TState,TCost,TDistance>.QueueExhaustedException)
            {
                if (Result.IsCompleted) return Result.Result;
                if (Result.IsFaulted) throw Result.Exception;
                throw new MissingSolutionException();
            }
            catch(Exception e)
            {
                ResultSetter.TrySetException(e);
                throw;
            }

        }

        NodeRepository<TState,TCost,TDistance> OpenNodes;
        TaskCompletionSource<INode<TState,TCost,TDistance>> ResultSetter = new TaskCompletionSource<INode<TState,TCost,TDistance>>();

        public class MissingSolutionException : Exception
        {
            public MissingSolutionException() : base("Unable to find path to goal.") { }
        }

        public class InternalErrorException : AggregateException
        {
            public InternalErrorException(INode<TState,TCost,TDistance> currentnode, Exception subexception)
                : base("An internal exception was detected",subexception)
            {
                CurrentNode = currentnode;
            }

            public INode<TState,TCost,TDistance> CurrentNode { get; }

        }


    }
}
