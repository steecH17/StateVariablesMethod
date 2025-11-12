namespace StateVariablesMethod;
public class CircuitAnalyzer
{
    private List<CircuitComponent> components;
    private DirectedGraph graph;
    private MMatrix mMatrix;

    public CircuitAnalyzer(List<CircuitComponent> components, DirectedGraph graph, MMatrix mMatrix)
    {
        this.components = components;
        this.graph = graph;
        this.mMatrix = mMatrix;
    }

    public List<string> GetStateVariables()
    {
        var stateVars = new List<string>();

        // Переменные состояния: напряжения на конденсаторах и токи через индуктивности
        foreach (var capacitor in components.Where(c => c.Type == ComponentType.Capacitor))
        {
            stateVars.Add($"U_{capacitor.Name}");
        }

        foreach (var inductor in components.Where(c => c.Type == ComponentType.Inductor))
        {
            stateVars.Add($"I_{inductor.Name}");
        }

        return stateVars;
    }

    public List<string> GetInputVariables()
    {
        var inputs = new List<string>();

        // Входные переменные: источники напряжения и тока
        foreach (var source in components.Where(c =>
            c.Type == ComponentType.VoltageSource || c.Type == ComponentType.CurrentSource))
        {
            inputs.Add(source.Name);
        }

        return inputs;
    }

    public Dictionary<string, CircuitComponent> GetComponentDictionary()
    {
        return components.ToDictionary(c => c.Name, c => c);
    }

    public List<StateEquation> BuildStateEquations()
    {
        var equations = new List<StateEquation>();
        var compDict = GetComponentDictionary();

        // C * dU_C/dt = i_C
        foreach (var capacitor in components.Where(c => c.Type == ComponentType.Capacitor))
        {
            var equation = new StateEquation($"dU_{capacitor.Name}/dt");

            //ток через конденсатор через уравнения Кирхгофа
            var capacitorCurrent = FindComponentCurrent(capacitor, compDict);
            equation.Coefficients.Add($"U_{capacitor.Name}", -1.0 / (capacitor.Value * 1000));
            equation.Constant = capacitorCurrent / capacitor.Value;

            equations.Add(equation);
        }

        // L * dI_L/dt = U_L
        foreach (var inductor in components.Where(c => c.Type == ComponentType.Inductor))
        {
            var equation = new StateEquation($"dI_{inductor.Name}/dt");

            //напряжение на индуктивности через уравнения Кирхгофа
            var inductorVoltage = FindComponentVoltage(inductor, compDict);
            equation.Coefficients.Add($"I_{inductor.Name}", -inductor.Value / 1000);
            equation.Constant = inductorVoltage / inductor.Value;

            equations.Add(equation);
        }

        return equations;
    }

    private double FindComponentCurrent(CircuitComponent component, Dictionary<string, CircuitComponent> compDict)
    {
        if (component.Type == ComponentType.Capacitor)
        {
            var resistors = components.Where(c => c.Type == ComponentType.Resistor).ToList();
            var sources = components.Where(c => c.Type == ComponentType.CurrentSource).ToList();

            if (resistors.Count > 0 && sources.Count > 0)
            {
                return sources.First().Value / (resistors.Count + 1);
            }
        }

        return 0.1; 
    }

    private double FindComponentVoltage(CircuitComponent component, Dictionary<string, CircuitComponent> compDict)
    {
        if (component.Type == ComponentType.Inductor)
        {
            var resistors = components.Where(c => c.Type == ComponentType.Resistor).ToList();
            var sources = components.Where(c => c.Type == ComponentType.VoltageSource).ToList();

            if (resistors.Count > 0 && sources.Count > 0)
            {
                return sources.First().Value / (resistors.Count + 1);
            }
        }

        return 1.0; 
    }
}