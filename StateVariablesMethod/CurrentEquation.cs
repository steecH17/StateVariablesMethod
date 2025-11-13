namespace StateVariablesMethod;
// Вместо KCL Equation  
public class CurrentEquation  // или УзловоеУравнение
{
    public CircuitComponent Component { get; set; }
    public string Equation { get; set; }
    public List<(CircuitComponent Component, bool IsPositive)> CurrentTerms { get; set; } = new List<(CircuitComponent, bool)>();
}