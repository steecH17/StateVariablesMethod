namespace StateVariablesMethod;

// Вместо KVL Equation
public class VoltageEquation  // или KonтурноеУравнение
{
    public CircuitComponent Chord { get; set; }
    public string Equation { get; set; }
    public List<(CircuitComponent Component, double Sign)> VoltageTerms { get; set; } = new List<(CircuitComponent, double)>();
}