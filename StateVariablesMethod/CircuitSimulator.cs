namespace StateVariablesMethod;

public class CircuitSimulator
{
    private List<CircuitComponent> components;
    private DirectedGraph graph;
    private MMatrix mMatrix;
    private StateSpaceSystem system;
      private List<CircuitComponent> cachedInputVariables = null;

    public CircuitSimulator(List<CircuitComponent> circuitComponents)
    {
        components = circuitComponents;
        graph = new DirectedGraph();
    }

    public SimulationResult Simulate(double simulationTime = -1, double timeStep = -1)
    {
        Console.WriteLine("Шаг 1: Построение ориентированного графа...");
        graph.BuildGraph(components);
        graph.PrintGraphInfo();

        Console.WriteLine("\nШаг 2: Построение M-матрицы...");
        mMatrix = new MMatrix(graph.Chords.Count, graph.Tree.Count);
        mMatrix.BuildMMatrix(graph);

        if (mMatrix != null)
            mMatrix.Print();

        PrintKirchhoffEquations();
        PrintKCLEquations();
        PrintComponentEquations();

        Console.WriteLine("\nШаг 3: Анализ цепи...");
        var topologyAnalyzer = new TopologyAnalyzer(components, graph, mMatrix);
        var circuitType = topologyAnalyzer.AnalyzeCircuitType();
        Console.WriteLine($"Тип цепи: {circuitType}");

        if (simulationTime <= 0 || timeStep <= 0)
        {
            var recommendedParams = topologyAnalyzer.GetSimulationParameters();
            simulationTime = recommendedParams.simulationTime;
            timeStep = recommendedParams.timeStep;
            Console.WriteLine($"Автоматические параметры: время={simulationTime:F6}с, шаг={timeStep:F6}с");
        }

        Console.WriteLine("\nШаг 4: Формирование уравнений состояния...");
        var stateSpaceBuilder = new StateSpaceBuilder(components, graph, mMatrix);
        system = stateSpaceBuilder.BuildStateSpaceSystem();

        Console.WriteLine("\nШаг 5: Решение уравнений методом Эйлера...");
        return SolveEquations(simulationTime, timeStep);
    }

    private SimulationResult SolveEquations(double simulationTime, double timeStep)
    {
        cachedInputVariables = components.Where(c =>
            c.Type == ComponentType.CurrentSource ||
            c.Type == ComponentType.VoltageSource).ToList();

        timeStep = CalculateStableTimeStep(timeStep);
        int steps = (int)(simulationTime / timeStep);

        var result = new SimulationResult(steps + 1, system.A.Rows, system.C.Rows);

        Console.WriteLine($"Решение уравнений: время={simulationTime:F4}с, шаг={timeStep:F6}с, шагов={steps}");

        try
        {
            result.Time[0] = 0;
            for (int i = 0; i < system.A.Rows; i++)
            {
                result.States[0, i] = system.InitialConditions[i];
            }
            CalculateOutputs(result, 0);

            int progressInterval = Math.Max(1, steps / 50);

            double maxStateChange = 0;

            for (int n = 0; n < steps; n++)
            {
                result.Time[n + 1] = result.Time[n] + timeStep;

                // X(n+1) = X(n) + h*(A*X(n) + B*V)
                for (int i = 0; i < system.A.Rows; i++)
                {
                    double derivative = 0;

                    // A * X
                    for (int j = 0; j < system.A.Cols; j++)
                    {
                        derivative += system.A[i, j] * result.States[n, j];
                    }

                    // + B * V
                    for (int j = 0; j < system.B.Cols; j++)
                    {
                        derivative += system.B[i, j] * GetInputValue(j);
                    }

                    double newState = result.States[n, i] + timeStep * derivative;

                    result.States[n + 1, i] = newState;

                    double change = Math.Abs(derivative * timeStep);
                    if (change > maxStateChange) maxStateChange = change;
                }

                CalculateOutputs(result, n + 1);
            }

            PrintResults(result);
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nОшибка при решении: {ex.Message}");
            return result;
        }
        finally
        {
            cachedInputVariables = null;
        }
    }

    private double CalculateStableTimeStep(double initialTimeStep)
    {
        double maxEigenvalue = EstimateMaxEigenvalue();
        double stableTimeStep = 1.0 / Math.Abs(maxEigenvalue); 

        if (maxEigenvalue == 0) return initialTimeStep;

        double recommendedStep = Math.Min(initialTimeStep, stableTimeStep * 0.1);

        Console.WriteLine($"Макс. собственное число: {maxEigenvalue:F2}, устойчивый шаг: {stableTimeStep:F6}, рекомендовано: {recommendedStep:F6}");

        return recommendedStep;
    }

    private double EstimateMaxEigenvalue()
    {
        double maxEigenvalue = 0;

        for (int i = 0; i < system.A.Rows; i++)
        {
            double rowSum = 0;
            for (int j = 0; j < system.A.Cols; j++)
            {
                rowSum += Math.Abs(system.A[i, j]);
            }
            maxEigenvalue = Math.Max(maxEigenvalue, rowSum);
        }

        return maxEigenvalue;
    }

    private void CalculateOutputs(SimulationResult result, int step)
    {
        double[] inputValues = new double[system.D.Cols];
        for (int j = 0; j < system.D.Cols; j++)
        {
            inputValues[j] = GetInputValue(j);
        }

        for (int i = 0; i < system.C.Rows; i++)
        {
            result.Outputs[step, i] = 0;

            // C * X
            for (int j = 0; j < system.C.Cols; j++)
            {
                result.Outputs[step, i] += system.C[i, j] * result.States[step, j];
            }

            // + D * V
            for (int j = 0; j < system.D.Cols; j++)
            {
                result.Outputs[step, i] += system.D[i, j] * inputValues[j];
            }
        }
    }

    private double GetInputValue(int inputIndex)
    {
        if (cachedInputVariables == null)
        {
            cachedInputVariables = components.Where(c =>
                c.Type == ComponentType.CurrentSource ||
                c.Type == ComponentType.VoltageSource).ToList();
        }

        if (inputIndex < cachedInputVariables.Count)
        {
            var inputComponent = cachedInputVariables[inputIndex];
            return inputComponent.Value;
        }
        return 0;
    }

    private void PrintKirchhoffEquations()
    {
        Console.WriteLine("Уравнения KVL (по контурам):");

        for (int i = 0; i < graph.Chords.Count; i++)
        {
            var chord = graph.Chords[i];
            Console.Write($"Контур {i + 1} ({chord.Name}): ");

            string leftSide = $"+U_{chord.Name}";

            string rightSide = "0";
            bool firstTerm = true;

            for (int j = 0; j < graph.Tree.Count; j++)
            {
                if (mMatrix[i, j] != 0)
                {
                    string sign = mMatrix[i, j] > 0 ? "-" : "+";
                    string term = $"{sign}U_{graph.Tree[j].Name}";

                    if (firstTerm)
                    {
                        rightSide = term;
                        firstTerm = false;
                    }
                    else
                    {
                        rightSide += " " + term;
                    }
                }
            }

            Console.WriteLine($"{leftSide} = {rightSide}");
        }
    }

    private void PrintKCLEquations()
    {
        Console.WriteLine("\nУравнения KCL (по сечениям):");

        for (int j = 0; j < graph.Tree.Count; j++)
        {
            var treeComponent = graph.Tree[j];
            Console.Write($"Сечение ({treeComponent.Name}): +I_{treeComponent.Name} = ");

            List<string> terms = new List<string>();

            for (int i = 0; i < graph.Chords.Count; i++)
            {
                if (mMatrix[i, j] != 0)
                {
                    string sign = mMatrix[i, j] > 0 ? "+" : "-"; 
                    terms.Add($"{sign}I_{graph.Chords[i].Name}");
                }
            }

            Console.WriteLine(string.Join(" ", terms));
        }
    }

    private void PrintComponentEquations()
    {
        Console.WriteLine("После подстановки компонентных соотношений:");

        // Для каждого уравнения KVL заменяем напряжения на компонентные соотношения
        for (int i = 0; i < graph.Chords.Count; i++)
        {
            var chord = graph.Chords[i];

            if (chord.Type == ComponentType.Resistor)
            {
                Console.Write($"  {chord.Name}: I_{chord.Name} * {chord.Value} = ");
            }
            else if (chord.Type == ComponentType.Inductor)
            {
                Console.Write($"  {chord.Name}: {chord.Value} * dI_{chord.Name}/dt = ");
            }
            else if (chord.Type == ComponentType.CurrentSource)
            {
                Console.Write($"  {chord.Name}: U_{chord.Name} = ");
            }

            // Правая часть уравнения
            bool firstTerm = true;
            for (int j = 0; j < graph.Tree.Count; j++)
            {
                if (mMatrix[i, j] != 0)
                {
                    var treeComponent = graph.Tree[j];
                    string sign = mMatrix[i, j] > 0 ? "+" : "-";

                    if (treeComponent.Type == ComponentType.Resistor)
                    {
                        Console.Write($"{(firstTerm ? "" : " ")}{sign}I_{treeComponent.Name} * {treeComponent.Value}");
                    }
                    else if (treeComponent.Type == ComponentType.Capacitor)
                    {
                        Console.Write($"{(firstTerm ? "" : " ")}{sign}U_{treeComponent.Name}");
                    }
                    else if (treeComponent.Type == ComponentType.VoltageSource)
                    {
                        Console.Write($"{(firstTerm ? "" : " ")}{sign}{treeComponent.Value}");
                    }

                    firstTerm = false;
                }
            }
            Console.WriteLine();
        }

        // Для уравнений KCL заменяем токи на компонентные соотношения
        Console.WriteLine("\n  Уравнения KCL:");
        for (int j = 0; j < graph.Tree.Count; j++)
        {
            var treeComponent = graph.Tree[j];

            if (treeComponent.Type == ComponentType.Capacitor)
            {
                Console.Write($"    C_{treeComponent.Name}: {treeComponent.Value} * dU_{treeComponent.Name}/dt = ");
            }
            else if (treeComponent.Type == ComponentType.Resistor)
            {
                Console.Write($"    R_{treeComponent.Name}: U_{treeComponent.Name} / {treeComponent.Value} = ");
            }

            List<string> terms = new List<string>();

            // Токи из M-матрицы
            for (int i = 0; i < graph.Chords.Count; i++)
            {
                if (mMatrix[i, j] != 0)
                {
                    string sign = mMatrix[i, j] > 0 ? "+" : "-";
                    var chord = graph.Chords[i];

                    if (chord.Type == ComponentType.Resistor)
                    {
                        terms.Add($"{sign}U_{chord.Name} / {chord.Value}");
                    }
                    else if (chord.Type == ComponentType.CurrentSource)
                    {
                        terms.Add($"{sign}{chord.Value}");
                    }
                    else
                    {
                        terms.Add($"{sign}I_{chord.Name}");
                    }
                }
            }

            Console.WriteLine(string.Join(" ", terms));
        }
    }

    private void PrintResults(SimulationResult result)
    {
        Console.WriteLine("\nРезультаты моделирования:");
        Console.WriteLine("Время(s)\tU_C(V)\t\tI_L(A)\t\ti2(A)\t\ti3(A)");
        Console.WriteLine(new string('-', 80));

        if (result.Time == null || result.Time.Length == 0)
        {
            Console.WriteLine("Нет данных для отображения");
            return;
        }

        int totalSteps = result.Time.Length;
        int displaySteps = Math.Min(11, totalSteps); 

        int stepSize = Math.Max(1, totalSteps / (displaySteps - 1));

        for (int displayIndex = 0; displayIndex < displaySteps; displayIndex++)
        {
            int dataIndex = displayIndex * stepSize;

            if (dataIndex >= totalSteps)
                break;

            double time = result.Time[dataIndex];
            double uC = (result.States.GetLength(1) > 0) ? result.States[dataIndex, 0] : 0;
            double iL = (result.States.GetLength(1) > 1) ? result.States[dataIndex, 1] : 0;
            double i2 = (result.Outputs.GetLength(1) > 0) ? result.Outputs[dataIndex, 0] : 0;
            double i3 = (result.Outputs.GetLength(1) > 1) ? result.Outputs[dataIndex, 1] : 0;

            Console.WriteLine($"{time:F6}\t{uC:F6}\t{iL:F6}\t{i2:F6}\t{i3:F6}");
        }
    }
}