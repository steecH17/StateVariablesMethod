namespace StateVariablesMethod;
public class StateEquation
{
    public StateEquation(string variable)
    {
        Variable = variable;
    }
    public string Variable { get; set; }
    public Dictionary<string, double> Coefficients { get; set; } = new Dictionary<string, double>();
    public double Constant { get; set; }

    
}