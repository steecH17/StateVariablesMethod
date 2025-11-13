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

    public SimulationResult Simulate(double simulationTime = 100.0, double timeStep = 0.001)
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
        graph.PrintGraphInfo();

        Console.WriteLine("\nШаг 2: Построение M-матрицы...");
        mMatrix = new MMatrix(graph.Chords.Count, graph.Tree.Count);
        mMatrix.BuildMMatrix(graph);

        if (mMatrix != null)
            mMatrix.Print();

        PrintKirchhoffEquations();
        // Добавляем вывод уравнений KCL
        Console.WriteLine("\nУравнения KCL (по сечениям):");
        PrintKCLEquations();

        // Добавляем вывод уравнений с компонентными соотношениями
        Console.WriteLine("\nУравнения с компонентными соотношениями:");
        PrintComponentEquations();

        // //Формирование уравнений состояния
        // Console.WriteLine("\nШаг 3: Формирование уравнений состояния...");
        // BuildStateSpaceSystem();
        // Шаг 4: Формирование уравнений состояния (используем новый класс)
        Console.WriteLine("\nШаг 4: Формирование уравнений состояния...");
        var stateSpaceBuilder = new StateSpaceBuilder(components, graph, mMatrix);
        system = stateSpaceBuilder.BuildStateSpaceSystem();

        //Решение уравнений
        Console.WriteLine("\nШаг 4: Решение уравнений методом Эйлера...");
        return SolveEquations(simulationTime, timeStep);
    }


    // Основной метод построения системы состояний
    private void BuildStateSpaceSystem()
    {
        Console.WriteLine("\nПостроение системы уравнений состояния...");

        // 1. Определяем переменные состояния
        var stateVariables = IdentifyStateVariables();
        int stateCount = stateVariables.Count;

        // 2. Определяем входные воздействия  
        var inputVariables = IdentifyInputVariables();
        int inputCount = inputVariables.Count;

        // 3. Определяем выходные величины
        int outputCount = 2; // i₂ и i₃

        system = new StateSpaceSystem(stateCount, inputCount, outputCount);

        // 4. Строим уравнения состояния из уравнений Кирхгофа
        BuildStateEquationsFromKirchhoff(stateVariables, inputVariables);

        // 5. Устанавливаем начальные условия
        SetInitialConditions(stateVariables);

        Console.WriteLine($"Построена система: {stateCount} состояний, {inputCount} входов, {outputCount} выходов");
    }

    private List<CircuitComponent> IdentifyStateVariables()
    {
        var stateVars = new List<CircuitComponent>();

        // Переменные состояния: напряжения на конденсаторах и токи через индуктивности
        stateVars.AddRange(components.Where(c => c.Type == ComponentType.Capacitor));
        stateVars.AddRange(components.Where(c => c.Type == ComponentType.Inductor));

        Console.WriteLine($"Переменные состояния: {string.Join(", ", stateVars.Select(v => $"{GetVariableName(v)}"))}");
        return stateVars;
    }

    private List<CircuitComponent> IdentifyInputVariables()
    {
        var inputs = new List<CircuitComponent>();

        // Входные воздействия: источники
        inputs.AddRange(components.Where(c =>
            c.Type == ComponentType.CurrentSource ||
            c.Type == ComponentType.VoltageSource));

        Console.WriteLine($"Входные переменные: {string.Join(", ", inputs.Select(v => v.Name))}");
        return inputs;
    }

    private void BuildStateEquationsFromKirchhoff(List<CircuitComponent> stateVariables, List<CircuitComponent> inputVariables)
    {
        // Получаем уравнения Кирхгофа
        var voltageEquations = GetVoltageEquations();
        var currentEquations = GetCurrentEquations();

        Console.WriteLine("\nПреобразование уравнений Кирхгофа в уравнения состояния:");

        // Для каждой переменной состояния строим дифференциальное уравнение
        for (int i = 0; i < stateVariables.Count; i++)
        {
            var stateVar = stateVariables[i];

            if (stateVar.Type == ComponentType.Capacitor)
            {
                // Для конденсатора: C·du_C/dt = i_C
                BuildCapacitorEquation(stateVar, i, currentEquations, stateVariables, inputVariables);
            }
            else if (stateVar.Type == ComponentType.Inductor)
            {
                // Для индуктивности: L·di_L/dt = u_L  
                BuildInductorEquation(stateVar, i, voltageEquations, stateVariables, inputVariables);
            }
        }

        // Строим уравнения выходов
        BuildOutputEquations(stateVariables, inputVariables);
    }

    // Методы для получения уравнений Кирхгофа
    private List<VoltageEquation> GetVoltageEquations()
    {
        var equations = new List<VoltageEquation>();

        for (int i = 0; i < graph.Chords.Count; i++)
        {
            var chord = graph.Chords[i];
            var equation = new VoltageEquation { Chord = chord };

            string eqString = $"+U_{chord.Name} = ";
            bool firstTerm = true;

            for (int j = 0; j < graph.Tree.Count; j++)
            {
                if (mMatrix[i, j] != 0)
                {
                    var treeComponent = graph.Tree[j];
                    double sign = mMatrix[i, j];
                    equation.VoltageTerms.Add((treeComponent, sign));

                    string signStr = sign > 0 ? "+" : "-";
                    string term = $"{signStr}U_{treeComponent.Name}";

                    if (firstTerm)
                    {
                        eqString += term;
                        firstTerm = false;
                    }
                    else
                    {
                        eqString += " " + term;
                    }
                }
            }

            equation.Equation = eqString;
            equations.Add(equation);
        }

        return equations;
    }

    private List<CurrentEquation> GetCurrentEquations()
    {
        var equations = new List<CurrentEquation>();

        // Упрощенная реализация - для каждого компонента дерева
        foreach (var treeComponent in graph.Tree)
        {
            if (treeComponent.Type == ComponentType.Capacitor || treeComponent.Type == ComponentType.Resistor)
            {
                var equation = new CurrentEquation { Component = treeComponent };

                // Находим связанные хорды через M-матрицу
                string eqString = $"+I_{treeComponent.Name} = ";
                bool firstTerm = true;

                for (int i = 0; i < graph.Chords.Count; i++)
                {
                    if (mMatrix[i, graph.Tree.IndexOf(treeComponent)] != 0)
                    {
                        var chord = graph.Chords[i];
                        double sign = mMatrix[i, graph.Tree.IndexOf(treeComponent)];
                        bool isPositive = sign < 0; // Инверсия для токов

                        equation.CurrentTerms.Add((chord, isPositive));

                        string signStr = isPositive ? "+" : "-";
                        string term = $"{signStr}I_{chord.Name}";

                        if (firstTerm)
                        {
                            eqString += term;
                            firstTerm = false;
                        }
                        else
                        {
                            eqString += " " + term;
                        }
                    }
                }

                equation.Equation = eqString;
                equations.Add(equation);
            }
        }

        return equations;
    }

    private void BuildCapacitorEquation(CircuitComponent capacitor, int stateIndex,
        List<CurrentEquation> currentEquations, List<CircuitComponent> stateVariables, List<CircuitComponent> inputVariables)
    {
        Console.WriteLine($"\nУравнение для конденсатора {capacitor.Name}:");

        // Находим уравнение токов для этого конденсатора
        var currentEq = currentEquations.FirstOrDefault(e => e.Component == capacitor);
        if (currentEq != null)
        {
            Console.WriteLine($"  Исходное: C·du_{capacitor.Name}/dt = {currentEq.Equation.Replace($"+I_{capacitor.Name} = ", "")}");

            // Преобразуем: du_C/dt = (1/C) · (сумма токов)
            foreach (var term in currentEq.CurrentTerms)
            {
                double coefficient = GetCurrentCoefficient(term, stateVariables, inputVariables);

                // Добавляем в матрицу A или B
                AddCoefficientToStateEquation(capacitor, stateIndex, term.Component, coefficient, stateVariables, inputVariables);
            }

            // Учитываем коэффициент 1/C для всех членов
            double C = capacitor.Value;
            for (int j = 0; j < stateVariables.Count; j++)
            {
                system.A[stateIndex, j] /= C;
            }
            for (int j = 0; j < inputVariables.Count; j++)
            {
                system.B[stateIndex, j] /= C;
            }
        }
    }

    private void BuildInductorEquation(CircuitComponent inductor, int stateIndex,
        List<VoltageEquation> voltageEquations, List<CircuitComponent> stateVariables, List<CircuitComponent> inputVariables)
    {
        Console.WriteLine($"\nУравнение для индуктивности {inductor.Name}:");

        // Находим уравнение напряжений для этой индуктивности
        var voltageEq = voltageEquations.FirstOrDefault(e => e.Chord == inductor);
        if (voltageEq != null)
        {
            Console.WriteLine($"  Исходное: L·di_{inductor.Name}/dt = {voltageEq.Equation.Replace($"+U_{inductor.Name} = ", "")}");

            // Преобразуем: di_L/dt = (1/L) · (сумма напряжений)
            foreach (var term in voltageEq.VoltageTerms)
            {
                double coefficient = GetVoltageCoefficient(term.Component, term.Sign, stateVariables, inputVariables);

                // Добавляем в матрицу A или B
                AddCoefficientToStateEquation(inductor, stateIndex, term.Component, coefficient, stateVariables, inputVariables);
            }

            // Учитываем коэффициент 1/L для всех членов
            double L = inductor.Value;
            for (int j = 0; j < stateVariables.Count; j++)
            {
                system.A[stateIndex, j] /= L;
            }
            for (int j = 0; j < inputVariables.Count; j++)
            {
                system.B[stateIndex, j] /= L;
            }
        }
    }

    private double GetCurrentCoefficient((CircuitComponent Component, bool IsPositive) term,
        List<CircuitComponent> stateVariables, List<CircuitComponent> inputVariables)
    {
        var component = term.Component;
        double sign = term.IsPositive ? 1.0 : -1.0;

        if (component.Type == ComponentType.Resistor)
        {
            // Ток через резистор: i_R = u_R / R
            var voltageVar = FindVoltageVariableForComponent(component, stateVariables);
            if (voltageVar != null)
            {
                return sign / component.Value;
            }
        }
        else if (component.Type == ComponentType.CurrentSource)
        {
            // Ток источника - входное воздействие
            return sign;
        }
        else if (component.Type == ComponentType.Inductor)
        {
            // Ток через индуктивность - переменная состояния
            return sign;
        }

        return 0;
    }

    private double GetVoltageCoefficient(CircuitComponent component, double sign,
        List<CircuitComponent> stateVariables, List<CircuitComponent> inputVariables)
    {
        if (component.Type == ComponentType.Resistor)
        {
            // Напряжение на резисторе: u_R = i_R · R
            var currentVar = FindCurrentVariableForComponent(component, stateVariables);
            if (currentVar != null)
            {
                return sign * component.Value;
            }
        }
        else if (component.Type == ComponentType.VoltageSource)
        {
            // Напряжение источника - входное воздействие
            return sign;
        }
        else if (component.Type == ComponentType.Capacitor)
        {
            // Напряжение на конденсаторе - переменная состояния
            return sign;
        }

        return 0;
    }

    private void AddCoefficientToStateEquation(CircuitComponent stateVar, int stateIndex,
        CircuitComponent termComponent, double coefficient, List<CircuitComponent> stateVariables, List<CircuitComponent> inputVariables)
    {
        // Определяем, куда добавлять коэффициент: в A (переменные состояния) или B (входы)

        if (stateVariables.Contains(termComponent))
        {
            // Переменная состояния
            int termIndex = stateVariables.IndexOf(termComponent);
            system.A[stateIndex, termIndex] += coefficient;
            Console.WriteLine($"    A[{stateIndex},{termIndex}] += {coefficient:F6}  // {GetVariableName(stateVar)} ← {coefficient:F6}·{GetVariableName(termComponent)}");
        }
        else if (inputVariables.Contains(termComponent))
        {
            // Входное воздействие
            int inputIndex = inputVariables.IndexOf(termComponent);
            system.B[stateIndex, inputIndex] += coefficient;
            Console.WriteLine($"    B[{stateIndex},{inputIndex}] += {coefficient:F6}  // {GetVariableName(stateVar)} ← {coefficient:F6}·{termComponent.Name}");
        }
    }

    private void BuildOutputEquations(List<CircuitComponent> stateVariables, List<CircuitComponent> inputVariables)
    {
        Console.WriteLine("\nПостроение уравнений выходов:");

        // Выход 1: i₂ (ток через R2)
        var r2 = components.FirstOrDefault(c => c.Name == "R2");
        if (r2 != null)
        {
            // i₂ = u_R2 / R2
            var voltageVar = FindVoltageVariableForComponent(r2, stateVariables);
            if (voltageVar != null)
            {
                int varIndex = stateVariables.IndexOf(voltageVar);
                system.C[0, varIndex] = 1.0 / r2.Value;
                Console.WriteLine($"  i₂ = {system.C[0, varIndex]:F6}·{GetVariableName(voltageVar)}");
            }
        }

        // Выход 2: i₃ (ток через источник J)
        var jSource = inputVariables.FirstOrDefault(c => c.Type == ComponentType.CurrentSource);
        if (jSource != null)
        {
            // Упрощенная логика: i₃ = J - i₂ - i_L
            var r2VoltageVar = FindVoltageVariableForComponent(r2, stateVariables);
            var inductor = stateVariables.FirstOrDefault(v => v.Type == ComponentType.Inductor);

            if (r2VoltageVar != null && inductor != null)
            {
                int uC_Index = stateVariables.IndexOf(r2VoltageVar);
                int iL_Index = stateVariables.IndexOf(inductor);
                int jIndex = inputVariables.IndexOf(jSource);

                system.C[1, uC_Index] = -1.0 / r2.Value;
                system.C[1, iL_Index] = -1;
                system.D[1, jIndex] = 1;

                Console.WriteLine($"  i₃ = {system.C[1, uC_Index]:F6}·u_C + {system.C[1, iL_Index]:F1}·i_L + {system.D[1, jIndex]:F0}·J");
            }
        }
    }

    // Вспомогательные методы
    private string GetVariableName(CircuitComponent component)
    {
        return component.Type switch
        {
            ComponentType.Capacitor => $"u_{component.Name}",
            ComponentType.Inductor => $"i_{component.Name}",
            _ => component.Name
        };
    }

    private CircuitComponent FindVoltageVariableForComponent(CircuitComponent component, List<CircuitComponent> stateVariables)
    {
        // Находим конденсатор на тех же узлах
        return stateVariables.FirstOrDefault(sv =>
            sv.Type == ComponentType.Capacitor &&
            AreComponentsConnected(sv, component));
    }

    private CircuitComponent FindCurrentVariableForComponent(CircuitComponent component, List<CircuitComponent> stateVariables)
    {
        // Находим индуктивность на тех же узлах
        return stateVariables.FirstOrDefault(sv =>
            sv.Type == ComponentType.Inductor &&
            AreComponentsConnected(sv, component));
    }

    private bool AreComponentsConnected(CircuitComponent comp1, CircuitComponent comp2)
    {
        // Компоненты связаны, если имеют общий узел
        return comp1.Node1 == comp2.Node1 || comp1.Node1 == comp2.Node2 ||
               comp1.Node2 == comp2.Node1 || comp1.Node2 == comp2.Node2;
    }

    private void SetInitialConditions(List<CircuitComponent> stateVariables)
    {
        for (int i = 0; i < stateVariables.Count; i++)
        {
            system.InitialConditions[i] = 0; // Начальные условия = 0
        }
    }


    // private void BuildStateSpaceSystem()
    // {
    //     var stateVariables = analyzer.GetStateVariables();
    //     var inputVariables = analyzer.GetInputVariables();

    //     int stateCount = stateVariables.Count;
    //     int inputCount = inputVariables.Count;
    //     int outputCount = stateCount + 2; // Все состояния + дополнительные выходы

    //     system = new StateSpaceSystem(stateCount, inputCount, outputCount);

    //     if (stateCount == 0)
    //     {
    //         Console.WriteLine("В схеме нет переменных состояния (конденсаторов или индуктивностей)");
    //         return;
    //     }

    //     //формирование матриц
    //     BuildMatricesAutomatically(stateVariables, inputVariables);

    //     // Начальные условия
    //     SetInitialConditions(stateVariables);

    //     Console.WriteLine($"Переменные состояния: {string.Join(", ", stateVariables)}");
    //     Console.WriteLine($"Входные переменные: {string.Join(", ", inputVariables)}");
    // }

    // private void BuildMatricesAutomatically(List<string> stateVars, List<string> inputVars)
    // {
    //     // уравнения состояния через анализатор
    //     var stateEquations = analyzer.BuildStateEquations();

    //     // A
    //     for (int i = 0; i < stateVars.Count; i++)
    //     {
    //         var equation = stateEquations[i];

    //         for (int j = 0; j < stateVars.Count; j++)
    //         {
    //             string stateVar = stateVars[j];
    //             if (equation.Coefficients.ContainsKey(stateVar))
    //             {
    //                 system.A[i, j] = equation.Coefficients[stateVar];
    //             }
    //             else
    //             {
    //                 system.A[i, j] = 0;
    //             }
    //         }
    //     }

    //     //B (входные воздействия)
    //     // Упрощенная реализация - предполагаем, что первый источник влияет на первую переменную состояния
    //     for (int i = 0; i < stateVars.Count; i++)
    //     {
    //         for (int j = 0; j < inputVars.Count; j++)
    //         {
    //             var input = components.First(c => c.Name == inputVars[j]);
    //             system.B[i, j] = CalculateInputCoefficient(input, stateVars[i]);
    //         }
    //     }

    //     //C (выходные величины)
    //     BuildOutputMatrix(stateVars, inputVars);

    //     //D (прямое влияние входов на выходы)
    //     BuildDirectMatrix(stateVars, inputVars);

    //     system.PrintSystem();
    // }

    // private double CalculateInputCoefficient(CircuitComponent input, string stateVar)
    // {

    //     if (input.Type == ComponentType.CurrentSource)
    //     {
    //         if (stateVar.StartsWith("U_")) // Напряжение на конденсаторе
    //         {
    //             var capacitor = components.First(c => c.Name == stateVar.Substring(2));
    //             return 1.0 / capacitor.Value;
    //         }
    //     }
    //     else if (input.Type == ComponentType.VoltageSource)
    //     {
    //         if (stateVar.StartsWith("I_")) // Ток через индуктивность
    //         {
    //             var inductor = components.First(c => c.Name == stateVar.Substring(2));
    //             return 1.0 / inductor.Value;
    //         }
    //     }

    //     return 0;
    // }

    // private void BuildOutputMatrix(List<string> stateVars, List<string> inputVars)
    // {
    //     // Выходные величины: все переменные состояния + дополнительные токи/напряжения

    //     // Первые stateCount выходов - сами переменные состояния
    //     for (int i = 0; i < stateVars.Count; i++)
    //     {
    //         system.C[i, i] = 1; // Единичная матрица для состояний
    //     }

    //     // Дополнительные выходы (токи через резисторы и т.д.)
    //     int outputIndex = stateVars.Count;
    //     foreach (var resistor in components.Where(c => c.Type == ComponentType.Resistor))
    //     {
    //         if (outputIndex < system.C.Rows)
    //         {
    //             // Ток через резистор = напряжение / сопротивление
    //             // Находим, какое состояние соответствует напряжению на этом резисторе
    //             for (int j = 0; j < stateVars.Count; j++)
    //             {
    //                 if (stateVars[j].StartsWith("U_"))
    //                 {
    //                     var capName = stateVars[j].Substring(2);
    //                     system.C[outputIndex, j] = 1.0 / resistor.Value;
    //                     break;
    //                 }
    //             }
    //             outputIndex++;
    //         }
    //     }
    // }

    // private void BuildDirectMatrix(List<string> stateVars, List<string> inputVars)
    // {
    //     // Матрица D обычно разреженная - большинство входов не влияют напрямую на выходы

    //     // Для источников тока, которые напрямую влияют на некоторые токи
    //     for (int i = stateVars.Count; i < system.C.Rows; i++)
    //     {
    //         for (int j = 0; j < inputVars.Count; j++)
    //         {
    //             var input = components.First(c => c.Name == inputVars[j]);
    //             if (input.Type == ComponentType.CurrentSource)
    //             {
    //                 // Источник тока может напрямую влиять на токи в ветвях
    //                 system.D[i, j] = 1.0;
    //             }
    //             else
    //             {
    //                 system.D[i, j] = 0.0;
    //             }
    //         }
    //     }
    // }

    // private void SetInitialConditions(List<string> stateVars)
    // {
    //     for (int i = 0; i < stateVars.Count; i++)
    //     {
    //         if (stateVars[i].StartsWith("U_"))
    //         {
    //             // Начальное напряжение на конденсаторе
    //             system.InitialConditions[i] = CalculateInitialVoltage(stateVars[i]);
    //         }
    //         else if (stateVars[i].StartsWith("I_"))
    //         {
    //             // Начальный ток через индуктивность
    //             system.InitialConditions[i] = CalculateInitialCurrent(stateVars[i]);
    //         }
    //     }
    // }

    // private double CalculateInitialVoltage(string stateVar)
    // {
    //     var capName = stateVar.Substring(2);
    //     var capacitor = components.First(c => c.Name == capName);

    //     var voltageSources = components.Where(c => c.Type == ComponentType.VoltageSource);
    //     if (voltageSources.Any())
    //     {
    //         return voltageSources.First().Value * 0.5;
    //     }

    //     return 0;
    // }

    // private double CalculateInitialCurrent(string stateVar)
    // {
    //     var indName = stateVar.Substring(2);
    //     var inductor = components.First(c => c.Name == indName);

    //     var currentSources = components.Where(c => c.Type == ComponentType.CurrentSource);
    //     if (currentSources.Any())
    //     {
    //         return currentSources.First().Value * 0.5;
    //     }

    //     return 0;
    // }

    private SimulationResult SolveEquations(double simulationTime, double timeStep)
    {
        // Предварительно кэшируем входные переменные
        cachedInputVariables = components.Where(c =>
            c.Type == ComponentType.CurrentSource ||
            c.Type == ComponentType.VoltageSource).ToList();

        // Автоматический подбор шага для устойчивости
        timeStep = CalculateStableTimeStep(timeStep);
        int steps = (int)(simulationTime / timeStep);

        var result = new SimulationResult(steps + 1, system.A.Rows, system.C.Rows);

        Console.WriteLine($"Решение уравнений: время={simulationTime:F4}с, шаг={timeStep:F6}с, шагов={steps}");

        try
        {
            // Начальные условия
            result.Time[0] = 0;
            for (int i = 0; i < system.A.Rows; i++)
            {
                result.States[0, i] = system.InitialConditions[i];
            }
            CalculateOutputs(result, 0);

            // Прогресс-бар
            Console.Write("Прогресс: [");
            int progressInterval = Math.Max(1, steps / 50);

            double maxStateChange = 0;

            // Метод Эйлера с мониторингом устойчивости
            for (int n = 0; n < steps; n++)
            {
                result.Time[n + 1] = result.Time[n] + timeStep;

                // Расчет новых состояний: X(n+1) = X(n) + h*(A*X(n) + B*V)
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

                    // Проверка на численную устойчивость
                    if (double.IsNaN(newState) || double.IsInfinity(newState))
                    {
                        throw new Exception($"Численная неустойчивость на шаге {n}, состояние {i}");
                    }

                    result.States[n + 1, i] = newState;

                    // Отслеживаем максимальное изменение
                    double change = Math.Abs(derivative * timeStep);
                    if (change > maxStateChange) maxStateChange = change;
                }

                // Расчет выходных величин
                CalculateOutputs(result, n + 1);

                // Отображение прогресса
                if (n % progressInterval == 0)
                {
                    Console.Write("=");
                }

                // Автоматическое уменьшение шага при быстром изменении
                if (maxStateChange > 10.0) // Порог для уменьшения шага
                {
                    timeStep *= 0.5;
                    Console.WriteLine($"\nУменьшен шаг до: {timeStep:F6}с");
                }
            }

            Console.WriteLine($"] Завершено (макс. изменение: {maxStateChange:F6})");
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
        // Оценка максимального собственного числа для устойчивости метода Эйлера
        double maxEigenvalue = EstimateMaxEigenvalue();
        double stableTimeStep = 1.0 / Math.Abs(maxEigenvalue); // Условие устойчивости

        if (maxEigenvalue == 0) return initialTimeStep;

        double recommendedStep = Math.Min(initialTimeStep, stableTimeStep * 0.1);

        Console.WriteLine($"Макс. собственное число: {maxEigenvalue:F2}, устойчивый шаг: {stableTimeStep:F6}, рекомендовано: {recommendedStep:F6}");

        return recommendedStep;
    }

    private double EstimateMaxEigenvalue()
    {
        // Простая оценка максимального собственного числа через норму матрицы
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
        // Предварительно получаем значения входов
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
        // Кэшируем входные переменные, чтобы не вычислять каждый раз
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

    private List<CircuitComponent> cachedInputVariables = null;

    private void PrintKirchhoffEquations()
    {
        Console.WriteLine("Уравнения KVL (по контурам):");

        for (int i = 0; i < graph.Chords.Count; i++)
        {
            var chord = graph.Chords[i];
            Console.Write($"Контур {i + 1} ({chord.Name}): ");

            // ЛЕВАЯ часть: напряжение на хорде
            string leftSide = $"+U_{chord.Name}";

            // ПРАВАЯ часть: сумма напряжений на компонентах дерева в контуре
            // Знак берется ИЗ M-матрицы, но интерпретируется как коэффициент при напряжении
            string rightSide = "0";
            bool firstTerm = true;

            for (int j = 0; j < graph.Tree.Count; j++)
            {
                if (mMatrix[i, j] != 0)
                {
                    // ЗНАК берется непосредственно из M-матрицы
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

        Console.WriteLine("\nУравнения KCL (по сечениям):");
        PrintKCLEquations();
    }

    private void PrintKCLEquations()
    {
        // Уравнения KCL получаются из M-матрицы по СТОЛБЦАМ
        // Для каждого компонента дерева: сумма токов с учетом знаков из M-матрицы

        for (int j = 0; j < graph.Tree.Count; j++)
        {
            var treeComponent = graph.Tree[j];
            Console.Write($"Сечение ({treeComponent.Name}): +I_{treeComponent.Name} = ");

            List<string> terms = new List<string>();

            // Анализируем j-й столбец M-матрицы
            for (int i = 0; i < graph.Chords.Count; i++)
            {
                if (mMatrix[i, j] != 0)
                {
                    // ЗНАК берется непосредственно из M-матрицы, но ИНВЕРТИРУЕТСЯ для токов
                    string sign = mMatrix[i, j] > 0 ? "+" : "-"; // ИНВЕРСИЯ ЗНАКОВ
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

        // Проверяем, что у нас есть достаточно данных
        if (result.Time == null || result.Time.Length == 0)
        {
            Console.WriteLine("Нет данных для отображения");
            return;
        }

        // Безопасное определение количества точек для вывода
        int totalSteps = result.Time.Length;
        int displaySteps = Math.Min(11, totalSteps); // Максимум 11 точек

        // Определяем шаг для равномерного вывода
        int stepSize = Math.Max(1, totalSteps / (displaySteps - 1));

        for (int displayIndex = 0; displayIndex < displaySteps; displayIndex++)
        {
            int dataIndex = displayIndex * stepSize;

            // Проверяем, не вышли ли за границы массива
            if (dataIndex >= totalSteps)
                break;

            // Безопасный доступ к данным
            double time = result.Time[dataIndex];
            double uC = (result.States.GetLength(1) > 0) ? result.States[dataIndex, 0] : 0;
            double iL = (result.States.GetLength(1) > 1) ? result.States[dataIndex, 1] : 0;
            double i2 = (result.Outputs.GetLength(1) > 0) ? result.Outputs[dataIndex, 0] : 0;
            double i3 = (result.Outputs.GetLength(1) > 1) ? result.Outputs[dataIndex, 1] : 0;

            Console.WriteLine($"{time:F6}\t{uC:F6}\t{iL:F6}\t{i2:F6}\t{i3:F6}");
        }
    }
}