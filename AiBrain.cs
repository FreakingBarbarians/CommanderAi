using System;
using System.Collections.Generic;
using System.Threading;

namespace CommanderAi2 {
     public class AiBrain {
        private Stack<AiState> state_stack = new Stack<AiState>();
        private AiInterface iface;
        private Dictionary<string, AiState> interrupts = new Dictionary<string, AiState>();
        private AiState current_state = null;
        public int orderResult = 0;
        public Mutex BrainMutex;
        public AiState currentState { get => current_state; }

        // make an even higher level thing. Like select for fd's but in the Ai Core.
        // It will check what AiActors have state changes, and process those.
        // Then if it doesn't have a state change it just throws it out onto a blocked list.

        /// <summary>
        /// Sets the interrupts for this actor.
        /// </summary>
        public Dictionary<string, AiState> Interrupts { set => interrupts = value; }
        
        public AiBrain(AiInterface iface, AiState state)
        {
            this.iface = iface;
            this.current_state = null;
            BrainMutex = new Mutex();
            state.RegisterToBrain(this);
            state_stack.Push(state);
        }

        public void Interrupt(string code)
        {
        }
        
        public virtual void Process()
        {
            if (current_state != null) {
                if (orderResult != (int)DefaultOrderResults.NONE) {
                    string prev_node_id = current_state.currentNode.node_id;
                    int previous_result = orderResult;
                    orderResult = (int)DefaultOrderResults.NONE;
                    if (current_state.IsAtEndNode()) {
                        current_state = null;
                        if (state_stack.Count > 0)
                        {
                            current_state = state_stack.Pop(); // Resume?
                            current_state.Resume();
                            // Console.WriteLine("{0} -> {1} : {2}", prev_node_id, current_state.currentNode.node_id, ((DefaultOrderResults)previous_result).ToString());
                        }
                        else {
                            // Console.WriteLine("{0} -> {1} : {2}", prev_node_id, "END", ((DefaultOrderResults)previous_result).ToString());
                        }
                    } else {
                        current_state.AdvanceState(previous_result);
                        current_state.Process();
                        // Console.WriteLine("{0} -> {1} : {2}", prev_node_id, current_state.currentNode.node_id, ((DefaultOrderResults)previous_result).ToString());
                    }
                }
            } else {
                if (state_stack.Count > 0) {
                    current_state = state_stack.Pop();
                    current_state.Process();
                    // Console.WriteLine("{0} -> {1} : {2}", "NONE", current_state.currentNode.node_id, "EPSILON");
                }
            }
        }

        public void OrderCallback(int result) {
            BrainMutex.WaitOne();
            this.orderResult = result;
            BrainMutex.ReleaseMutex();
        }
    }
}