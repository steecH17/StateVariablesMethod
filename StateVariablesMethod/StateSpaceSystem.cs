namespace StateVariablesMethod;
public class StateSpaceSystem
{
    public MMatrix A { get; set; }
    public MMatrix B { get; set; }
    public MMatrix C { get; set; }
    public MMatrix D { get; set; }
    public double[] InitialConditions { get; set; }

    public StateSpaceSystem(int stateVariables, int inputs, int outputs)
    {
        A = new MMatrix(stateVariables, stateVariables);
        B = new MMatrix(stateVariables, inputs);
        C = new MMatrix(outputs, stateVariables);
        D = new MMatrix(outputs, inputs);
        InitialConditions = new double[stateVariables];
    }

    public void PrintSystem()
    {
        if (A != null)
        {
            Console.WriteLine("Матрица А : ");
            A.Print();
        }

        if (B != null)
        {
            Console.WriteLine("Матрица B : ");
            B.Print();
        }

        if (C != null)
        {
            Console.WriteLine("Матрица C : ");
            C.Print();
        }

        if (D != null)
        {
            Console.WriteLine("Матрица D : ");
            D.Print();
        }
    }
}