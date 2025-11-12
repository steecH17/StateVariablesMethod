namespace StateVariablesMethod;

public class CircuitSimulator
{
    private List<CircuitComponent> components;
    private DirectedGraph graph;
    private MMatrix mMatrix;
    private StateSpaceSystem system;
    private CircuitAnalyzer analyzer;

    public CircuitSimulator(List<CircuitComponent> circuitComponents)
    {
        components = circuitComponents;
        graph = new DirectedGraph();
        analyzer = new CircuitAnalyzer(components, graph, mMatrix);
    }

    public SimulationResult Simulate(double simulationTime, double timeStep)
    {
        //Построение ориентированного графа
        Console.WriteLine("Шаг 1: Построение ориентированного графа...");
        graph.BuildGraph(components);

        Console.WriteLine("Дерево:");
        foreach (var component in graph.Tree)
        {
            Console.WriteLine($"  {component.Name} ({component.Type})");
        }

        Console.WriteLine("Хорды:");
        foreach (var component in graph.Chords)
        {
            Console.WriteLine($"  {component.Name} ({component.Type})");
        }

        Console.WriteLine("\nШаг 2: Построение M-матрицы...");
        BuildMMatrix();
        if (mMatrix != null)
            mMatrix.Print();

        //Формирование уравнений состояния
        Console.WriteLine("\nШаг 3: Формирование уравнений состояния...");
        BuildStateSpaceSystem();

        //Решение уравнений
        Console.WriteLine("\nШаг 4: Решение уравнений методом Эйлера...");
        return SolveEquations(simulationTime, timeStep);
    }

    private void BuildStateSpaceSystem()
    {
        var stateVariables = analyzer.GetStateVariables();
        var inputVariables = analyzer.GetInputVariables();

        int stateCount = stateVariables.Count;
        int inputCount = inputVariables.Count;
        int outputCount = stateCount + 2; // Все состояния + дополнительные выходы

        system = new StateSpaceSystem(stateCount, inputCount, outputCount);

        if (stateCount == 0)
        {
            Console.WriteLine("В схеме нет переменных состояния (конденсаторов или индуктивностей)");
            return;
        }

        //формирование матриц
        BuildMatricesAutomatically(stateVariables, inputVariables);

        // Начальные условия
        SetInitialConditions(stateVariables);

        Console.WriteLine($"Переменные состояния: {string.Join(", ", stateVariables)}");
        Console.WriteLine($"Входные переменные: {string.Join(", ", inputVariables)}");
    }

    private void BuildMatricesAutomatically(List<string> stateVars, List<string> inputVars)
    {
        // уравнения состояния через анализатор
        var stateEquations = analyzer.BuildStateEquations();

        // A
        for (int i = 0; i < stateVars.Count; i++)
        {
            var equation = stateEquations[i];

            for (int j = 0; j < stateVars.Count; j++)
            {
                string stateVar = stateVars[j];
                if (equation.Coefficients.ContainsKey(stateVar))
                {
                    system.A[i, j] = equation.Coefficients[stateVar];
                }
                else
                {
                    system.A[i, j] = 0;
                }
            }
        }

        //B (входные воздействия)
        // Упрощенная реализация - предполагаем, что первый источник влияет на первую переменную состояния
        for (int i = 0; i < stateVars.Count; i++)
        {
            for (int j = 0; j < inputVars.Count; j++)
            {
                var input = components.First(c => c.Name == inputVars[j]);
                system.B[i, j] = CalculateInputCoefficient(input, stateVars[i]);
            }
        }

        //C (выходные величины)
        BuildOutputMatrix(stateVars, inputVars);

        //D (прямое влияние входов на выходы)
        BuildDirectMatrix(stateVars, inputVars);

        system.PrintSystem();
    }

    private double CalculateInputCoefficient(CircuitComponent input, string stateVar)
    {

        if (input.Type == ComponentType.CurrentSource)
        {
            if (stateVar.StartsWith("U_")) // Напряжение на конденсаторе
            {
                var capacitor = components.First(c => c.Name == stateVar.Substring(2));
                return 1.0 / capacitor.Value;
            }
        }
        else if (input.Type == ComponentType.VoltageSource)
        {
            if (stateVar.StartsWith("I_")) // Ток через индуктивность
            {
                var inductor = components.First(c => c.Name == stateVar.Substring(2));
                return 1.0 / inductor.Value;
            }
        }

        return 0;
    }

    private void BuildOutputMatrix(List<string> stateVars, List<string> inputVars)
    {
        // Выходные величины: все переменные состояния + дополнительные токи/напряжения

        // Первые stateCount выходов - сами переменные состояния
        for (int i = 0; i < stateVars.Count; i++)
        {
            system.C[i, i] = 1; // Единичная матрица для состояний
        }

        // Дополнительные выходы (токи через резисторы и т.д.)
        int outputIndex = stateVars.Count;
        foreach (var resistor in components.Where(c => c.Type == ComponentType.Resistor))
        {
            if (outputIndex < system.C.Rows)
            {
                // Ток через резистор = напряжение / сопротивление
                // Находим, какое состояние соответствует напряжению на этом резисторе
                for (int j = 0; j < stateVars.Count; j++)
                {
                    if (stateVars[j].StartsWith("U_"))
                    {
                        var capName = stateVars[j].Substring(2);
                        system.C[outputIndex, j] = 1.0 / resistor.Value;
                        break;
                    }
                }
                outputIndex++;
            }
        }
    }

    private void BuildDirectMatrix(List<string> stateVars, List<string> inputVars)
    {
        // Матрица D обычно разреженная - большинство входов не влияют напрямую на выходы

        // Для источников тока, которые напрямую влияют на некоторые токи
        for (int i = stateVars.Count; i < system.C.Rows; i++)
        {
            for (int j = 0; j < inputVars.Count; j++)
            {
                var input = components.First(c => c.Name == inputVars[j]);
                if (input.Type == ComponentType.CurrentSource)
                {
                    // Источник тока может напрямую влиять на токи в ветвях
                    system.D[i, j] = 1.0;
                }
                else
                {
                    system.D[i, j] = 0.0;
                }
            }
        }
    }

    private void SetInitialConditions(List<string> stateVars)
    {
        for (int i = 0; i < stateVars.Count; i++)
        {
            if (stateVars[i].StartsWith("U_"))
            {
                // Начальное напряжение на конденсаторе
                system.InitialConditions[i] = CalculateInitialVoltage(stateVars[i]);
            }
            else if (stateVars[i].StartsWith("I_"))
            {
                // Начальный ток через индуктивность
                system.InitialConditions[i] = CalculateInitialCurrent(stateVars[i]);
            }
        }
    }

    private double CalculateInitialVoltage(string stateVar)
    {
        var capName = stateVar.Substring(2);
        var capacitor = components.First(c => c.Name == capName);

        var voltageSources = components.Where(c => c.Type == ComponentType.VoltageSource);
        if (voltageSources.Any())
        {
            return voltageSources.First().Value * 0.5;
        }

        return 0;
    }

    private double CalculateInitialCurrent(string stateVar)
    {
        var indName = stateVar.Substring(2);
        var inductor = components.First(c => c.Name == indName);

        var currentSources = components.Where(c => c.Type == ComponentType.CurrentSource);
        if (currentSources.Any())
        {
            return currentSources.First().Value * 0.5;
        }

        return 0;
    }

    private void BuildMMatrix()
    {
        int chordCount = graph.Chords.Count;
        int treeCount = graph.Tree.Count;

        mMatrix = new MMatrix(chordCount, treeCount);

        if (chordCount == 0 || treeCount == 0)
        {
            Console.WriteLine("Нет хорд или дерево пустое - M-матрица не может быть построена");
            return;
        }

        for (int i = 0; i < chordCount; i++)
        {
            var chord = graph.Chords[i];
            for (int j = 0; j < treeCount; j++)
            {
                var treeComp = graph.Tree[j];

                // Проверка, образуют ли компоненты контур
                if (AreComponentsConnected(chord, treeComp))
                {
                    mMatrix[i, j] = DetermineSign(chord, treeComp);
                }
                else
                {
                    mMatrix[i, j] = 0.0;
                }
            }
        }
    }

    private bool AreComponentsConnected(CircuitComponent comp1, CircuitComponent comp2)
{
    // Компоненты связаны, если имеют общий узел
    return comp1.Node1 == comp2.Node1 || comp1.Node1 == comp2.Node2 ||
           comp1.Node2 == comp2.Node1 || comp1.Node2 == comp2.Node2;
}

private double DetermineSign(CircuitComponent chord, CircuitComponent treeComponent)
{
    if (!AreComponentsConnected(chord, treeComponent))
        return 0;
    
    //общий узел между хордой и компонентом дерева
    int commonNode = FindCommonNode(chord, treeComponent);
    
    if (commonNode == -1) return 0;
    
    bool chordExitsCommonNode = chord.Node1 == commonNode;
    bool treeExitsCommonNode = treeComponent.Node1 == commonNode;
    
    return (chordExitsCommonNode == treeExitsCommonNode) ? -1.0 : 1.0;
}

private int FindCommonNode(CircuitComponent comp1, CircuitComponent comp2)
{
    if (comp1.Node1 == comp2.Node1 || comp1.Node1 == comp2.Node2)
        return comp1.Node1;
    if (comp1.Node2 == comp2.Node1 || comp1.Node2 == comp2.Node2)
        return comp1.Node2;
    return -1;
}

    private SimulationResult SolveEquations(double simulationTime, double timeStep)
    {
        int steps = (int)(simulationTime / timeStep);
        var result = new SimulationResult(steps, system.A.Rows, system.C.Rows);

        // Начальные условия
        result.Time[0] = 0;
        for (int i = 0; i < system.A.Rows; i++)
        {
            result.States[0, i] = system.InitialConditions[i];
        }

        // Расчет выходных величин для начального момента
        CalculateOutputs(result, 0);

        // Метод Эйлера
        for (int n = 0; n < steps; n++)
        {
            result.Time[n + 1] = result.Time[n] + timeStep;

            // Расчет новых состояний: X(n+1) = X(n) + h*(A*X(n) + B*V)
            for (int i = 0; i < system.A.Rows; i++)
            {
                double derivative = 0;
                for (int j = 0; j < system.A.Cols; j++)
                {
                    derivative += system.A[i, j] * result.States[n, j];
                }
                // Добавляем влияние источников
                for (int j = 0; j < system.B.Cols; j++)
                {
                    derivative += system.B[i, j] * GetInputValue(j); //значение входа
                }

                result.States[n + 1, i] = result.States[n, i] + timeStep * derivative;
            }

            // Расчет выходных величин
            CalculateOutputs(result, n + 1);
        }

        PrintResults(result);

        return result;
    }

    private double GetInputValue(int inputIndex)
    {
        var inputVariables = analyzer.GetInputVariables();
        if (inputIndex < inputVariables.Count)
        {
            var inputName = inputVariables[inputIndex];
            var inputComponent = components.First(c => c.Name == inputName);
            return inputComponent.Value; 
        }
        return 0;
    }

    private void CalculateOutputs(SimulationResult result, int step)
    {
        for (int i = 0; i < system.C.Rows; i++)
        {
            result.Outputs[step, i] = 0;
            for (int j = 0; j < system.C.Cols; j++)
            {
                result.Outputs[step, i] += system.C[i, j] * result.States[step, j];
            }
            //влияние входов через матрицу D
            for (int j = 0; j < system.D.Cols; j++)
            {
                result.Outputs[step, i] += system.D[i, j] * GetInputValue(j);
            }
        }
    }

    private void PrintResults(SimulationResult result)
    {
        Console.WriteLine("\nРезультаты моделирования:");
        Console.WriteLine("Время(s)\tU_C(V)\t\tI_L(A)\t\ti2(A)\t\ti3(A)");
        Console.WriteLine(new string('-', 80));

        for (int i = 0; i <= result.Time.Length - 1; i += Math.Max(1, result.Time.Length / 10))
        {
            Console.WriteLine($"{result.Time[i]:F6}\t" +
                            $"{result.States[i, 0]:F6}\t" +
                            $"{result.States[i, 1]:F6}\t" +
                            $"{result.Outputs[i, 2]:F6}\t" +
                            $"{result.Outputs[i, 3]:F6}");
        }
    }
}