namespace StateVariablesMethod;

public static class TestsCircuit
{
    public static List<CircuitComponent> CreateRCSeriesCircuit()
    {
        return new List<CircuitComponent>
            {
                new CircuitComponent("R1", ComponentType.Resistor, 1000, 1, 2),
                new CircuitComponent("C1", ComponentType.Capacitor, 1e-6, 2, 0),
                new CircuitComponent("V1", ComponentType.VoltageSource, 5, 1, 0)
            };
    }

    public static List<CircuitComponent> CreateRLCircuit()
    {
        return new List<CircuitComponent>
            {
                new CircuitComponent("R1", ComponentType.Resistor, 1000, 1, 2),
                new CircuitComponent("L1", ComponentType.Inductor, 0.1, 2, 0),
                new CircuitComponent("I1", ComponentType.CurrentSource, 0.001, 1, 0)
            };
    }

    public static List<CircuitComponent> CreateComplexCircuit()
    {
        return new List<CircuitComponent>
            {
                new CircuitComponent("R1", ComponentType.Resistor, 1000, 1, 2),
                new CircuitComponent("R2", ComponentType.Resistor, 2000, 2, 3),
                new CircuitComponent("C1", ComponentType.Capacitor, 1e-6, 3, 0),
                new CircuitComponent("L1", ComponentType.Inductor, 0.1, 2, 0),
                new CircuitComponent("V1", ComponentType.VoltageSource, 10, 1, 0),
                new CircuitComponent("I1", ComponentType.CurrentSource, 0.002, 3, 0)
            };
    }
}