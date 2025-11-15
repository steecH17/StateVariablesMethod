namespace StateVariablesMethod;

// KVL Equation
public class VoltageEquation
{
    public CircuitComponent Chord { get; set; }
    public string Equation { get; set; }
    public List<(CircuitComponent Component, double Sign)> VoltageTerms { get; set; } = new List<(CircuitComponent, double)>();
}