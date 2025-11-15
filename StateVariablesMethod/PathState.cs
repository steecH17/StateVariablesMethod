namespace StateVariablesMethod;
 public class PathState
    {
        public int Node { get; }
        public List<CircuitComponent> Path { get; }

        public PathState(int node, List<CircuitComponent> path)
        {
            Node = node;
            Path = path;
        }
    }