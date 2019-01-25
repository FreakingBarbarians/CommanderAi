using System;
using System.Runtime;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Collections.Generic;
using System.IO;

namespace CommanderAi2{

    public class AiStateTemplate {
        // id's of the end nodes
        public HashSet<string> endNodes = new HashSet<string>();
        // id's and type of the node
        public Dictionary<string, AiNode> nodes = new Dictionary<string, AiNode>();
        // transitions for each node
        public Dictionary<string, Dictionary<int, string>> transitions = new Dictionary<string, Dictionary<int, string>>();
    }

    public class AiState {
        
        private AiNode current_node;
        public Dictionary<string, AiNode> nodes;
        public readonly AiStateTemplate template;
        public AiNode currentNode { get => current_node; }

        public AiBrain containingBrain;

        public AiState(AiStateTemplate template, string start_node) {
            this.template = template;

            nodes = new Dictionary<string, AiNode>();
            foreach (string key in template.nodes.Keys) {
                AiNode node = (AiNode) template.nodes[key].Clone();
                nodes.Add(key, node);
            }

            foreach (AiNode ai_node in nodes.Values) {
                ai_node.RegisterTo(this);
            }

            current_node = nodes[start_node];
        }

        public bool IsAtEndNode() {
            return template.endNodes.Contains(current_node.node_id);
        }

        public void Resume() {
            // idk what to do with this yet
            return;
        }

        public void Process() {
            current_node.PreMain();
            current_node.Main();
            current_node.PostMain();
        }

        public void RegisterToBrain(AiBrain brain) {
            this.containingBrain = brain;
        }

        public AiNode AdvanceState(int orderResult) {
            string id;
            if((id = current_node.GetTransition(orderResult)) != null) {
                if(nodes.ContainsKey(id)) {
                    current_node = nodes[id];
                    return current_node;
                }
            }
            return null;
        }
    }
}