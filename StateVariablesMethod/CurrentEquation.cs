namespace StateVariablesMethod;
// KCL Equation  
public class CurrentEquation 
{
    public CircuitComponent Component { get; set; }
    public string Equation { get; set; }
    public List<(CircuitComponent Component, bool IsPositive)> CurrentTerms { get; set; } = new List<(CircuitComponent, bool)>();
}