using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace DecisionTreeAI
{
    public abstract class Node
    {
        public int history = -2;
        public Func<float, int> Action = null;

        public abstract NodeStatus Execute(float input); // 0 - 1

        public Node(Func<float, int> action = null) => Action = action;
    }

    public enum NodeStatus
    {
        Failure,
        Success,
    }

    public class ControlNode : Node
    {
        protected List<Node> children = new List<Node>();

        private Func<float, int> decisionFunction; // Função que escolhe o filho baseado no input

        public Func<float, int> GetDecisionFunction() => decisionFunction;

        public ControlNode(Func<float, int> df)
        {
            decisionFunction = df;
            Action = df;
        }

        public void AddChild(Node node) => children.Add(node);

        public override NodeStatus Execute(float input)
        {
            if (children.Count == 0)
                return NodeStatus.Failure;

            int selectedIndex = decisionFunction(input); // Escolhe qual filho executar
            if (selectedIndex < 0 || selectedIndex >= children.Count)
                return NodeStatus.Failure;

            history = selectedIndex;
            return children[selectedIndex].Execute(input);
        }
    }

    public class ExecutionNode : Node
    {
        public ExecutionNode(Func<float, int> action = null)
            : base(action) { }

        public override NodeStatus Execute(float input)
        {
            int result = Action(input);
            history = result;
            Debug.WriteLine("ExecutionNode(Execute): " + result);
            return (NodeStatus)result; // Converte o retorno da action para NodeStatus
        }
    }

    public class NodeInput
    {
        public float Value { get; }

        public NodeInput(float value) => Value = value;
    }
}
