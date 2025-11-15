namespace StateVariablesMethod;

public class LoopInfo
    {
        public CircuitComponent Chord { get; }
        public List<CircuitComponent> ComponentsInLoop { get; }
        public List<int> NodeSequence { get; set; }

        public LoopInfo(CircuitComponent chord)
        {
            Chord = chord;
            ComponentsInLoop = new List<CircuitComponent>();
        }
    }