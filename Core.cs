using System;
using System.Runtime;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Collections.Generic;
using System.IO;

/* 

FSM based ai.

// allow for some high - level augments?
// or brain level -stuff.
idk

There is a Physical world and an Ai World.

Physical world is where the objects interact, it's where the simulation takes place.
Ai World is where all the thinking and decision making happens. This results in some order
sent to the objects in the physical world.

        Order              Order Result                                
Ai World =>  Physical World     =>     Ai World (Does some thinking) => ...
*/

namespace CommanderAi2 {

    // we can deal with access later
    public struct AiCoreConfiguration {
        public int threadCount;
    }
    

    // red black tree?
    // load balancing?
    // ice cream dispending?
    // aghhh
    public class AiWorkerThread {
        public int load;
        public int NumBrains;
        public List<AiBrain> readyQueue;
        public Thread thread;
        public Mutex threadLock;
        
        public AiWorkerThread() {
            load = 0;
            NumBrains = 0;
            readyQueue = new List<AiBrain>();
            threadLock = new Mutex();
            thread = null;
        }
    }

    // Takes in worker threads as well
    // default uses 4 threads.
    // per processor ready queue?
    public static class AiCore {

        // top level stuff
        public static AiCoreConfiguration configuration;
        public static AiWorkerThread[] threads;
        private static bool configured = false;
        private static bool running = false;

        private static Mutex AddListMutex = new Mutex();
        private static Mutex RemoveListMutex = new Mutex();
        private static List<AiBrain> add_list = new List<AiBrain>();
        private static List<AiBrain> remove_list = new List<AiBrain>();

        public static Dictionary<AiBrain, int> BrainMapping = new Dictionary<AiBrain, int>();

        public static void InitCore() {
            configuration = new AiCoreConfiguration();
            configuration.threadCount = 4;
            threads = new AiWorkerThread[configuration.threadCount];
            for (int i = 0; i < configuration.threadCount; i ++) {
                threads[i] = new AiWorkerThread();
            }
            configured = true;
            AddListMutex = new Mutex();
            RemoveListMutex = new Mutex();
            add_list = new List<AiBrain>();
            remove_list = new List<AiBrain>();
            BrainMapping = new Dictionary<AiBrain, int>();
            Console.WriteLine("Initialized AI Core");
        }

        public static void InitCore(AiCoreConfiguration configuration) {
            AiCore.configuration = configuration;
            threads = new AiWorkerThread[configuration.threadCount];
            for (int i = 0; i < configuration.threadCount; i++)
            {
                threads[i] = new AiWorkerThread();
            }
            configured = true;
            AddListMutex = new Mutex();
            RemoveListMutex = new Mutex();
            add_list = new List<AiBrain>();
            remove_list = new List<AiBrain>();
            BrainMapping = new Dictionary<AiBrain, int>();
            Console.WriteLine("Initialized AI Core");
        }

        public static void Configure(AiCoreConfiguration configuration) {
            if(running) {
                return;
            }
            AiCore.configuration = configuration;
            // cannot be configured if started.
            threads = new AiWorkerThread[configuration.threadCount];
            for (int i = 0; i < configuration.threadCount; i++)
            {
                threads[i] = new AiWorkerThread();
            }
            configured = true;
        }

        public static void AddBrain(AiBrain brain) {
            AddListMutex.WaitOne();
            add_list.Add(brain);
            AddListMutex.ReleaseMutex();
        }

        private static void _AddBrain() {
            
            AddListMutex.WaitOne();
            foreach(AiBrain brain in add_list) {
                int smallest_id = 0;
                int smallest_count = int.MaxValue;
                for(int i = 0; i < configuration.threadCount; i ++) {
                    smallest_id = threads[i].readyQueue.Count < smallest_count ? i : smallest_id;
                    smallest_count = threads[i].readyQueue.Count < smallest_count ? threads[i].readyQueue.Count : smallest_count;
                }
                threads[smallest_id].threadLock.WaitOne();
                threads[smallest_id].readyQueue.Add(brain);
                threads[smallest_id].NumBrains++;
                BrainMapping.Add(brain, smallest_id);
                threads[smallest_id].threadLock.ReleaseMutex();
                Console.WriteLine("Adding Brain to {0}", smallest_id);
            }
            add_list.Clear();
            AddListMutex.ReleaseMutex();
        }

        public static void RemoveBrain(AiBrain brain)
        {
            AddListMutex.WaitOne();
            remove_list.Add(brain);
            AddListMutex.ReleaseMutex();
        }

        private static void _RemoveBrain() {
            RemoveListMutex.WaitOne();
            foreach(AiBrain brain in remove_list) {
                if(!BrainMapping.ContainsKey(brain)){
                    continue;
                }
                int thread_id = BrainMapping[brain];
                threads[thread_id].threadLock.WaitOne();
                threads[thread_id].NumBrains--;
                threads[thread_id].readyQueue.Remove(brain);
                BrainMapping.Remove(brain);
                threads[thread_id].threadLock.ReleaseMutex();
            }
            remove_list.Clear();
            RemoveListMutex.ReleaseMutex();
        }

        public static bool HasBrain(AiBrain b) {
            return BrainMapping.ContainsKey(b);
        }

        public static void Run() {
            if(!configured || running) {
                // some debug logging maybe? // if(#debug) etc.
                Console.WriteLine("Failed to Start Core");
                return;
            }

            running = true;

            int tid = 0;
            foreach(AiWorkerThread t in threads) {
                t.thread = new Thread(AiCore.ThreadWork);
                t.thread.Name = tid.ToString();
                t.thread.Start(tid++);
                Console.WriteLine("Started Thread {0}", tid);
            }
      
            while(running) {
                // do stuff

                // do any remove and add requests
                _AddBrain();
                _RemoveBrain();

                // that's it I think
            }

            foreach (AiWorkerThread t in threads) {
                t.thread.Join();
            }
            Console.WriteLine("Shutting Down");
        }

        public static void Shutdown() {
            running = false;
        }

        public static void ThreadWork(object arg) {
            int tid = (int) arg;
            AiWorkerThread my_thread = threads[tid];
            while(running) {
                my_thread.threadLock.WaitOne();
                AiBrain current_brain = null;
                if (my_thread.NumBrains > 0) {
                    current_brain = my_thread.readyQueue[0];
                    my_thread.readyQueue.RemoveAt(0);
                    my_thread.readyQueue.Add(current_brain);
                }
                my_thread.threadLock.ReleaseMutex();
                if (current_brain != null) {
                    Console.WriteLine("Processing Brain");
                    current_brain.Process();
                }
            }
            Console.WriteLine("Exiting {0}", tid);
        }
    }
}