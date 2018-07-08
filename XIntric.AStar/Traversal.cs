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

        IProblem<TState, TCost> Problem;
        IScenario<TState, TCost> Scenario;

        public Task<INode<TState,TCost>> Result => ResultSetter.Task;


        CancellationTokenSource Cancelled = new CancellationTokenSource();
        CancellationTokenSource GoalFound = new CancellationTokenSource();
        CancellationTokenSource QueueDepleted = new CancellationTokenSource();
        

        public event Action<INode<TState,TCost>> Diag_NodeTraversing;
        public event Action<INode<TState,TCost>> Diag_NodeTraversed;




        public async Task<INode<TState,TCost>> StepAsync()
        {
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
            catch(NodeRepository<TState,TCost>.QueueExhaustedException)
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


    }
}
