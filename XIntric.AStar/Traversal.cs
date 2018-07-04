using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace XIntric.AStar
{

    public delegate Task<IEnumerable<Node<TState,TDistance>>> ExpandNodeFunc<TState,TDistance>(Node<TState,TDistance> node);
    public delegate TDistance SumCost<TDistance>(TDistance accumulatedcost, TDistance estimateddistance);

    public class Traversal<TState,TDistance>
    {
        public Traversal(
            ExpandNodeFunc<TState,TDistance> expandnode, 
            SumCost<TDistance> sumcost,
            Func<TDistance,TDistance,int> distancecomparer,
            TState initstate,
            TDistance initcost,
            TDistance initestimateddistance,
            TDistance goaldistance
            )
        {
            ExpandNode = expandnode;
            SumCost = sumcost;
            GoalDistance = goaldistance;
            Comparer = new MyComparer(distancecomparer);
            OpenNodes = new NodeRepository<TState,TDistance>(new Node<TState,TDistance>(null,initstate,initcost,initestimateddistance,sumcost), Comparer);
        }

        ExpandNodeFunc<TState, TDistance> ExpandNode;
        SumCost<TDistance> SumCost;
        TDistance GoalDistance;
        IComparer<TDistance> Comparer;

        public Task<Node<TState,TDistance>> Result => ResultSetter.Task;


        CancellationTokenSource Cancelled = new CancellationTokenSource();
        CancellationTokenSource GoalFound = new CancellationTokenSource();
        CancellationTokenSource QueueDepleted = new CancellationTokenSource();
        

        public event Action<Node<TState,TDistance>> Diag_NodeTraversing;
        public event Action<Node<TState,TDistance>> Diag_NodeTraversed;




        public async Task<Node<TState,TDistance>> StepAsync()
        {
            try
            {
                //System.Console.WriteLine($"{Thread.CurrentThread.ManagedThreadId}: Step started.");


                return await OpenNodes.PerformWorkerOperationAsync(
                    async node =>
                {
                    Diag_NodeTraversing?.Invoke(node);
                    if (Comparer.Compare(node.EstimatedDistance,GoalDistance) <= 0) //Found a goal node
                    {
                        lock (ResultSetter)
                        {
                            System.Console.WriteLine($"{Thread.CurrentThread.ManagedThreadId}: Found goal node!.");
                            ResultSetter.SetResult(node);
                            return node;
                        }
                    }


                    var newnodes = (await ExpandNode(node)).ToList();

                    foreach(var newnode in newnodes)
                    {
                        OpenNodes.Add(newnode);
                    }

                    Diag_NodeTraversed?.Invoke(node);
                    return node;
                });

            }
            catch(NodeRepository<TState,TDistance>.QueueExhaustedException)
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

        NodeRepository<TState,TDistance> OpenNodes;
        TaskCompletionSource<Node<TState,TDistance>> ResultSetter = new TaskCompletionSource<Node<TState,TDistance>>();

        public class MissingSolutionException : Exception
        {
            public MissingSolutionException() : base("Unable to find path to goal.") { }
        }

        public class InternalErrorException : AggregateException
        {
            public InternalErrorException(Node<TState,TDistance> currentnode, Exception subexception)
                : base("An internal exception was detected",subexception)
            {
                CurrentNode = currentnode;
            }

            public Node<TState,TDistance> CurrentNode { get; }

        }

        public class MyComparer : IComparer<TDistance>
        {
            public MyComparer(Func<TDistance,TDistance,int> cfunc) { CompareFunc = cfunc; }
            Func<TDistance, TDistance, int> CompareFunc;
            public int Compare(TDistance x, TDistance y) => CompareFunc(x, y);
        }

    }
}
