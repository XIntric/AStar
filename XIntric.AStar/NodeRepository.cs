using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace XIntric.AStar
{
    class NodeRepository<TState,TCost,TDistance>
    {
        public NodeRepository(INode<TState,TCost,TDistance> firstitem, IComparer<TCost> comparer)
        {
            Nodes.Add(firstitem);
            Comparer = comparer;
        }


        public async Task<TRet> PerformWorkerOperationAsync<TRet>(Func<INode<TState,TCost,TDistance>, Task<TRet>> acquiredoperations)
        {


            //System.Console.WriteLine($"{Thread.CurrentThread.ManagedThreadId}: Getting node.");
            INode<TState,TCost,TDistance> myitem;

            await ConsumerLock.WaitAsync();
            bool hasconsumerlock = true;
            try
            {
                Monitor.Enter(ProducerLock);
                while (Nodes.Count <= 0)
                {
                    Monitor.Exit(ProducerLock);

                    List<Task> MonitoredTasks = new List<Task>(WorkerTasks);

                    if (MonitoredTasks.Count == 0)
                    {
                        throw new QueueExhaustedException();
                    }

                    ConsumerLock.Release();
                    hasconsumerlock = false;
                    await Task.WhenAny(MonitoredTasks);
                    await ConsumerLock.WaitAsync();
                    hasconsumerlock = true;
                    Monitor.Enter(ProducerLock);
                }

                myitem = Nodes[0];
                Nodes.RemoveAt(0);
                VisitedStates.Add(myitem.State, myitem.AccumulatedCost);
                Monitor.Exit(ProducerLock);
            }
            catch(Exception)
            {
                if (hasconsumerlock)
                {
                    ConsumerLock.Release();
                }
                throw;
            }

            //Still has consumerlock


            TaskCompletionSource<int> holder = new TaskCompletionSource<int>();
            async Task<TRet> Worker(INode<TState,TCost,TDistance> n)
            {
                await holder.Task;
                return await acquiredoperations(n);
            }

            var t = Worker(myitem);
            var cleanuptask = t.ContinueWith(async x =>
            {
                await ConsumerLock.WaitAsync();
                WorkerTasks.Remove(x);
                ConsumerLock.Release();
            });

            WorkerTasks.Add(t);
            ConsumerLock.Release();


            holder.SetResult(0); //release hold
            await cleanuptask;
            return await t;
        }



        public bool Add(INode<TState,TCost,TDistance> node)
        {
            lock (ProducerLock)
            {
                if (VisitedStates.TryGetValue(node.State,out var oldcost))
                {
                    if (Comparer.Compare(node.AccumulatedCost, oldcost) < 0)
                    {
                        VisitedStates.Remove(node.State); //re-open, found better path.
                    }
                    else return false;
                }

                for (var i = 0; i < Nodes.Count; i++)
                {
                    if (Comparer.Compare(node.TotalCost, Nodes[i].TotalCost) < 0)
                    {
                        Nodes.Insert(i++, node);
                        for (; i < Nodes.Count; i++)
                        {
                            if (Nodes[i].Equals(node))
                            {
                                Nodes.RemoveAt(i--);
                            }
                        }
                        return true;
                    }
                    if (Nodes[i].Equals(node))
                    {
                        return false; //Already present with better priority
                    }
                }
                Nodes.Add(node);
                return true;
            }
        }


        SemaphoreSlim ConsumerLock = new SemaphoreSlim(1);
        object ProducerLock => Nodes;

        async Task<INode<TState,TCost,TDistance>> GetNodeAsync()
        {

            //System.Console.WriteLine($"{Thread.CurrentThread.ManagedThreadId}: GetNodeAsync: 1.");

            await ConsumerLock.WaitAsync();
            bool hasconsumerlock = true;
            try
            {
                Monitor.Enter(ProducerLock);
                while (Nodes.Count <= 0)
                {
                    Monitor.Exit(ProducerLock);

                    List<Task> MonitoredTasks = new List<Task>(WorkerTasks);

                    if (MonitoredTasks.Count == 0)
                    {
                        throw new QueueExhaustedException();
                    }

                    ConsumerLock.Release();
                    hasconsumerlock = false;
                    await Task.WhenAny(MonitoredTasks);
                    await ConsumerLock.WaitAsync();
                    hasconsumerlock = true;
                    Monitor.Enter(ProducerLock);
                }

                var myitem = Nodes[0];
                Nodes.RemoveAt(0);
                VisitedStates.Add(myitem.State, myitem.AccumulatedCost);
                Monitor.Exit(ProducerLock);



                return myitem;

            }
            finally
            {
                if (hasconsumerlock)
                {
                    ConsumerLock.Release();
                }
            }


        }





        public class QueueExhaustedException : Exception
        {
            public QueueExhaustedException()
                : base("No more items left to acquire.") { }
        }

        IComparer<TCost> Comparer;
        List<INode<TState,TCost,TDistance>> Nodes = new List<INode<TState,TCost,TDistance>>();
        List<Task> WorkerTasks = new List<System.Threading.Tasks.Task>();
        Dictionary<TState, TCost> VisitedStates = new Dictionary<TState, TCost>();
    }

}
