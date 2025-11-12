namespace StateVariablesMethod;
public class SimulationResult
{
    public double[] Time { get; set; }
    public double[,] States { get; set; }
    public double[,] Outputs { get; set; }

    public SimulationResult(int steps, int stateVars, int outputs)
    {
        Time = new double[steps + 1];
        States = new double[steps + 1, stateVars];
        Outputs = new double[steps + 1, outputs];
    }
}