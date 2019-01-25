using System;
using System.Runtime;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Collections.Generic;
using System.IO;

namespace CommanderAi2 {
    public class AiNode : ICloneable {
        
        public string node_id;
        public AiState containing_state;
        protected Dictionary<int, string> transitions;

        public AiNode(string node_id, Dictionary<int,string> transitions) {
            this.node_id = node_id;
            this.transitions = transitions;
        }

        public void RegisterTo(AiState state) {
            this.containing_state = state;
        }

        public string GetTransition(int node_id) {
            if(transitions.ContainsKey(node_id)) {
                return transitions[node_id];
            }
            return null;
        }

        public virtual void ResetState() {

        }

        public virtual void PreMain() {

        }

        public virtual void Main() {

        }

        public virtual void PostMain() {

        }

        // y'all should re-implement this in your derived classes
        // And the cloned object does not auto register to the clonee's state
        public virtual object Clone()
        {
            return new AiNode(node_id, transitions);
        }
    }
}