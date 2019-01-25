using CommanderAi2;
using System;
using System.Threading;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;

namespace UnitTests
{

    public class Accumulator : AiNode {
        public int val = 0;

        public Accumulator(string node_id, Dictionary<int, string> transitions) : 
            base(node_id, transitions) {
        }

        public override void Main()
        {
            val++;

            containing_state.containingBrain.OrderCallback((int)DefaultOrderResults.SUCCESS);
        }

        public override object Clone()
        {
            Accumulator ac = new Accumulator(node_id, transitions);
            ac.val = this.val;
            return ac;
        }

    }

    public class AccumulatorChecker : AiNode {
        public enum CHECKER_MODE {
            EQUALS,
            NOT_EQUAL,
            GREATER,
            GREATER_EQUALS,
            LESS,
            LESS_EQUALS
        }

        public int val;
        public CHECKER_MODE mode;
        public string target;

        public AccumulatorChecker(string node_id, Dictionary<int, string> transitions, string accumulator_id, int val, CHECKER_MODE mode) : base(node_id, transitions) {
            this.target = accumulator_id;
            this.mode = mode;
            this.val = val;
        }

        public override void Main()
        {
            int result = (int) DefaultOrderResults.NONE;
            if (!containing_state.nodes.ContainsKey(target)) {
                Console.WriteLine("No state target for Checker");
                return;
            }

            Accumulator acc = (Accumulator)containing_state.nodes[target];

            switch (mode) {
                case CHECKER_MODE.EQUALS:
                    result = (int) (acc.val == val ? DefaultOrderResults.SUCCESS : DefaultOrderResults.FAILURE);
                    break;
                case CHECKER_MODE.NOT_EQUAL:
                    result = (int)(acc.val != val ? DefaultOrderResults.SUCCESS : DefaultOrderResults.FAILURE);
                    break;
                case CHECKER_MODE.GREATER:
                    result = (int)(acc.val > val ? DefaultOrderResults.SUCCESS : DefaultOrderResults.FAILURE);
                    break;
                case CHECKER_MODE.GREATER_EQUALS:
                    result = (int)(acc.val >= val ? DefaultOrderResults.SUCCESS : DefaultOrderResults.FAILURE);
                    break;
                case CHECKER_MODE.LESS:
                    result = (int)(acc.val < val ? DefaultOrderResults.SUCCESS : DefaultOrderResults.FAILURE);
                    break;
                case CHECKER_MODE.LESS_EQUALS:
                    result = (int)(acc.val <= val ? DefaultOrderResults.SUCCESS : DefaultOrderResults.FAILURE);
                    break;
                default:
                    break;
            }
            containing_state.containingBrain.OrderCallback(result);

        }

        public override object Clone()
        {
            return new AccumulatorChecker(node_id, transitions, target, val, mode);
        }

    }

    public class DummyInterface : AiInterface
    {
        public void ClearOrder()
        {
            throw new NotImplementedException();
        }

        public AiBrain GetActor()
        {
            throw new NotImplementedException();
        }

        public void SetActor(AiBrain brain)
        {
            throw new NotImplementedException();
        }

        public void SetOrder()
        {
            throw new NotImplementedException();
        }
    }

    [TestClass]
    public class SanityStateChecks
    {
        private static AiState state_1, state_2;
        private static AiStateTemplate template;

        [ClassInitialize]
        public static void CreateDiagrams(TestContext ctx) {
            template = new AiStateTemplate();
            template.endNodes.Add("0");
            template.transitions["0"] = new Dictionary<int, string>();
            template.nodes.Add("0", new Accumulator("0", template.transitions["0"]));
            state_1 = new AiState(template, "0");
            state_2 = new AiState(template, "0");
        }

        [TestCleanup]
        public void Cleanup() {
            
        }

        [TestMethod]
        public void TestMethod1()
        {
            AiCore.InitCore();
            Thread thread = new Thread(AiCore.Run);
            thread.Start();
            DummyInterface dummy = new DummyInterface();
            AiBrain b1 = new AiBrain(dummy, state_1);
            AiBrain b2 = new AiBrain(dummy, state_2);

            AiCore.AddBrain(b1);
            AiCore.AddBrain(b2);

            Thread.Sleep(1000);
            AiCore.Shutdown();

            Assert.AreEqual(1, ((Accumulator)state_1.nodes["0"]).val);
            Assert.AreEqual(1, ((Accumulator)state_2.nodes["0"]).val);
            thread.Join();
        }
    }

    [TestClass]
    public class BasicStateChecks {
        private static AiState state_1, state_2;
        private static AiStateTemplate template;

        [ClassInitialize]
        public static void CreateDiagrams(TestContext ctx) {
            template = new AiStateTemplate();
            Dictionary<int, string> zero_transitions = new Dictionary<int, string>();
            zero_transitions.Add((int)DefaultOrderResults.SUCCESS, "1");
            template.nodes["0"] = new Accumulator("0", zero_transitions);
            Dictionary<int, string> one_transitions = new Dictionary<int, string>();
            one_transitions.Add((int)DefaultOrderResults.SUCCESS, "1");
            one_transitions.Add((int)DefaultOrderResults.FAILURE, "0");
            template.nodes["1"] = new AccumulatorChecker("1", one_transitions, "0", 10, AccumulatorChecker.CHECKER_MODE.EQUALS);

            state_1 = new AiState(template, "0");
            state_2 = new AiState(template, "0");
        }

        [TestMethod]
        public void TestMethod1() {
            AiCore.InitCore();
            Thread thread = new Thread(AiCore.Run);
            thread.Start();

            DummyInterface dummy = new DummyInterface();

            AiBrain b1 = new AiBrain(dummy, state_1);
            AiBrain b2 = new AiBrain(dummy, state_2);

            AiCore.AddBrain(b1);
            AiCore.AddBrain(b2);

            Thread.Sleep(1000);
            AiCore.Shutdown();

            Assert.AreEqual(10, ((Accumulator)state_1.nodes["0"]).val);
            Assert.AreEqual(10, ((Accumulator)state_2.nodes["0"]).val);
            thread.Join();
        }

        int StressTestSize = 100;
        [TestMethod]
        public void StressTest() {
            AiBrain[] b = new AiBrain[StressTestSize];
            AiState[] s = new AiState[StressTestSize];

            DummyInterface dummy = new DummyInterface();

            AiCore.InitCore();
            Thread thread = new Thread(AiCore.Run);
            thread.Start();

            for (int i = 0; i < StressTestSize; i++) {
                s[i] = new AiState(template, "0");
                b[i] = new AiBrain(dummy, s[i]);
                AiCore.AddBrain(b[i]);
            }

            Thread.Sleep(1000);
            AiCore.Shutdown();

            for (int i = 0; i < StressTestSize; i++) {
                Assert.AreEqual(10, ((Accumulator)s[i].nodes["0"]).val);
            }

            thread.Join();
        }

        int RemoveTestSize = 100;
        float parity = 0.5f;
        [TestMethod]
        public void RemoveTest() {
            AiBrain[] b = new AiBrain[RemoveTestSize];
            AiState[] s = new AiState[RemoveTestSize];

            DummyInterface dummy = new DummyInterface();

            AiCore.InitCore();
            Thread thread = new Thread(AiCore.Run);
            thread.Start();

            for (int i = 0; i < RemoveTestSize; i++)
            {
                s[i] = new AiState(template, "0");
                b[i] = new AiBrain(dummy, s[i]);
                AiCore.AddBrain(b[i]);
                if (i > RemoveTestSize * parity) {
                    AiCore.RemoveBrain(b[i]);
                }
            }

            Thread.Sleep(1000);

            AiCore.Shutdown();

            for (int i = 0; i < RemoveTestSize; i++)
            {
                if (i > RemoveTestSize * parity)
                {
                    Assert.IsFalse(AiCore.HasBrain(b[i]));
                }
                else {
                    Assert.IsTrue(AiCore.HasBrain(b[i]));
                }
            }

            thread.Join();
        }
    }
}
