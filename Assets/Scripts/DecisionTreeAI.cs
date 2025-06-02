using System;
using System.Collections.Generic;

namespace DecisionTreeAI
{
    // Enumeração segura para status dos nós
    public enum NodeStatus
    {
        Failure,
        Success,
    }

    // Classe base abstrata para todos os nós
    public abstract class Node
    {
        public IReadOnlyList<Node> Children => _children.AsReadOnly();
        private protected readonly List<Node> _children = new();

        // História de execução para debug
        public NodeStatus LastStatus { get; private protected set; } = NodeStatus.Failure;
        public int LastChildIndex { get; private protected set; } = -1;

        public virtual Node AddChild(Node node)
        {
            _children.Add(node ?? throw new ArgumentNullException(nameof(node)));
            return this;
        }

        public abstract NodeStatus Execute(float input);
    }

    // Nó de controle com sistema de decisão seguro
    public sealed class ControlNode : Node
    {
        private readonly Func<float, int> _decisionFunction;
        public Func<float, int> DecisionFunction => _decisionFunction;

        public ControlNode(Func<float, int> decisionFunction)
        {
            _decisionFunction =
                decisionFunction ?? throw new ArgumentNullException(nameof(decisionFunction));
        }

        public override NodeStatus Execute(float input)
        {
            try
            {
                int childIndex = _decisionFunction(input);

                // Validação rigorosa do índice
                if (childIndex < 0 || childIndex >= _children.Count)
                {
                    throw new InvalidNodeIndexException(childIndex, _children.Count);
                }

                LastChildIndex = childIndex;
                NodeStatus result = _children[childIndex].Execute(input);
                LastStatus = result;
                return result;
            }
            catch (Exception ex)
            {
                DecisionTreeLogger.LogError(ex);
                LastStatus = NodeStatus.Failure;
                return NodeStatus.Failure;
            }
        }
    }

    // Nó de execução com validação de retorno
    public sealed class ExecutionNode : Node
    {
        private readonly Func<float, NodeStatus> _action;
        public Func<float, NodeStatus> Action => _action;

        public ExecutionNode(Func<float, NodeStatus> action)
        {
            _action = action ?? throw new ArgumentNullException(nameof(action));
        }

        public override NodeStatus Execute(float input)
        {
            try
            {
                NodeStatus result = _action(input);
                LastStatus = result;
                return result;
            }
            catch (Exception ex)
            {
                DecisionTreeLogger.LogError(ex);
                LastStatus = NodeStatus.Failure;
                return NodeStatus.Failure;
            }
        }
    }

    // Exceção customizada para erros de índice
    public sealed class InvalidNodeIndexException : Exception
    {
        public int InvalidIndex { get; }
        public int ValidRange { get; }

        public InvalidNodeIndexException(int index, int maxIndex)
            : base($"Índice inválido: {index}. Intervalo permitido: 0-{maxIndex - 1}")
        {
            InvalidIndex = index;
            ValidRange = maxIndex;
        }
    }

    // Logger customizado para a árvore
    public static class DecisionTreeLogger
    {
        public static void LogError(Exception ex)
        {
            // Implementação de logging (Unity Debug, arquivo, etc.)
            UnityEngine.Debug.LogError($"[DecisionTree] Error: {ex.Message}");
        }
    }
}
