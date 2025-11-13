using System;
using System.Collections.Generic;
using System.Linq;

namespace StateVariablesMethod
{
    public class StateSpaceBuilder
    {
        private List<CircuitComponent> components;
        private DirectedGraph graph;
        private MMatrix mMatrix;

        public StateSpaceBuilder(List<CircuitComponent> components, DirectedGraph graph, MMatrix mMatrix)
        {
            this.components = components;
            this.graph = graph;
            this.mMatrix = mMatrix;
        }

        public StateSpaceSystem BuildStateSpaceSystem()
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

            var system = new StateSpaceSystem(stateCount, inputCount, outputCount);

            // 4. Строим уравнения состояния из уравнений Кирхгофа
            BuildStateEquationsFromKirchhoff(system, stateVariables, inputVariables);

            // 5. Устанавливаем начальные условия
            SetInitialConditions(system, stateVariables);

            Console.WriteLine($"Построена система: {stateCount} состояний, {inputCount} входов, {outputCount} выходов");

            return system;
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

        private void BuildStateEquationsFromKirchhoff(StateSpaceSystem system, List<CircuitComponent> stateVariables, List<CircuitComponent> inputVariables)
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
                    BuildCapacitorEquation(system, stateVar, i, currentEquations, stateVariables, inputVariables);
                }
                else if (stateVar.Type == ComponentType.Inductor)
                {
                    // Для индуктивности: L·di_L/dt = u_L  
                    BuildInductorEquation(system, stateVar, i, voltageEquations, stateVariables, inputVariables);
                }
            }

            // Строим уравнения выходов
            BuildOutputEquations(system, stateVariables, inputVariables);
        }

        // Все остальные методы переносим сюда с небольшими изменениями...
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

        private void BuildCapacitorEquation(StateSpaceSystem system, CircuitComponent capacitor, int stateIndex,
    List<CurrentEquation> currentEquations, List<CircuitComponent> stateVariables, List<CircuitComponent> inputVariables)
        {
            Console.WriteLine($"\nУравнение для конденсатора {capacitor.Name}:");

            var currentEq = currentEquations.FirstOrDefault(e => e.Component == capacitor);
            if (currentEq != null)
            {
                Console.WriteLine($"  Исходное: C·du_{capacitor.Name}/dt = {currentEq.Equation.Replace($"+I_{capacitor.Name} = ", "")}");

                // Сначала собираем все коэффициенты без применения 1/C
                double[] aCoefficients = new double[stateVariables.Count];
                double[] bCoefficients = new double[inputVariables.Count];

                foreach (var term in currentEq.CurrentTerms)
                {
                    double coefficient = GetCurrentCoefficient(term, stateVariables, inputVariables);

                    if (stateVariables.Contains(term.Component))
                    {
                        int termIndex = stateVariables.IndexOf(term.Component);
                        aCoefficients[termIndex] += coefficient;
                        Console.WriteLine($"    Коэффициент для {GetVariableName(term.Component)}: {coefficient}");
                    }
                    else if (inputVariables.Contains(term.Component))
                    {
                        int inputIndex = inputVariables.IndexOf(term.Component);
                        bCoefficients[inputIndex] += coefficient;
                        Console.WriteLine($"    Коэффициент для {term.Component.Name}: {coefficient}");
                    }
                }

                // ПРАВИЛЬНО применяем коэффициент 1/C ко всей строке
                double C = capacitor.Value;
                for (int j = 0; j < stateVariables.Count; j++)
                {
                    system.A[stateIndex, j] = aCoefficients[j] / C;
                    if (system.A[stateIndex, j] != 0)
                        Console.WriteLine($"    A[{stateIndex},{j}] = {system.A[stateIndex, j]:F6}  // du_C/dt ← {system.A[stateIndex, j]:F6}·{GetVariableName(stateVariables[j])}");
                }

                for (int j = 0; j < inputVariables.Count; j++)
                {
                    system.B[stateIndex, j] = bCoefficients[j] / C;
                    if (system.B[stateIndex, j] != 0)
                        Console.WriteLine($"    B[{stateIndex},{j}] = {system.B[stateIndex, j]:F6}  // du_C/dt ← {system.B[stateIndex, j]:F6}·{inputVariables[j].Name}");
                }
            }
        }

        private void BuildInductorEquation(StateSpaceSystem system, CircuitComponent inductor, int stateIndex,
            List<VoltageEquation> voltageEquations, List<CircuitComponent> stateVariables, List<CircuitComponent> inputVariables)
        {
            Console.WriteLine($"\nУравнение для индуктивности {inductor.Name}:");

            var voltageEq = voltageEquations.FirstOrDefault(e => e.Chord == inductor);
            if (voltageEq != null)
            {
                Console.WriteLine($"  Исходное: L·di_{inductor.Name}/dt = {voltageEq.Equation.Replace($"+U_{inductor.Name} = ", "")}");

                // Сначала собираем все коэффициенты без применения 1/L
                double[] aCoefficients = new double[stateVariables.Count];
                double[] bCoefficients = new double[inputVariables.Count];

                foreach (var term in voltageEq.VoltageTerms)
                {
                    double coefficient = GetVoltageCoefficient(term.Component, term.Sign, stateVariables, inputVariables);

                    if (stateVariables.Contains(term.Component))
                    {
                        int termIndex = stateVariables.IndexOf(term.Component);
                        aCoefficients[termIndex] += coefficient;
                        Console.WriteLine($"    Коэффициент для {GetVariableName(term.Component)}: {coefficient}");
                    }
                    else if (inputVariables.Contains(term.Component))
                    {
                        int inputIndex = inputVariables.IndexOf(term.Component);
                        bCoefficients[inputIndex] += coefficient;
                        Console.WriteLine($"    Коэффициент для {term.Component.Name}: {coefficient}");
                    }
                }

                // Применяем коэффициент 1/L ко всей строке
                double L = inductor.Value;
                for (int j = 0; j < stateVariables.Count; j++)
                {
                    system.A[stateIndex, j] = aCoefficients[j] / L;
                    if (system.A[stateIndex, j] != 0)
                        Console.WriteLine($"    A[{stateIndex},{j}] = {system.A[stateIndex, j]:F6}  // di_L/dt ← {system.A[stateIndex, j]:F6}·{GetVariableName(stateVariables[j])}");
                }

                for (int j = 0; j < inputVariables.Count; j++)
                {
                    system.B[stateIndex, j] = bCoefficients[j] / L;
                    if (system.B[stateIndex, j] != 0)
                        Console.WriteLine($"    B[{stateIndex},{j}] = {system.B[stateIndex, j]:F6}  // di_L/dt ← {system.B[stateIndex, j]:F6}·{inputVariables[j].Name}");
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
                // Для резистора в хордах: ток через резистор выражается через напряжение
                var voltageVar = FindVoltageVariableForComponent(component, stateVariables);
                if (voltageVar != null)
                {
                    // i_R = u_R / R
                    return sign / component.Value;
                }
                else
                {
                    // Если резистор подключен к индуктивности
                    var currentVar = FindCurrentVariableForComponent(component, stateVariables);
                    if (currentVar != null)
                    {
                        return sign; // i_R = i_L (последовательное соединение)
                    }
                }
            }
            else if (component.Type == ComponentType.CurrentSource)
            {
                return sign;
            }
            else if (component.Type == ComponentType.Inductor)
            {
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

        private void BuildOutputEquations(StateSpaceSystem system, List<CircuitComponent> stateVariables, List<CircuitComponent> inputVariables)
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
            return comp1.Node1 == comp2.Node1 || comp1.Node1 == comp2.Node2 ||
                   comp1.Node2 == comp2.Node1 || comp1.Node2 == comp2.Node2;
        }

        private void SetInitialConditions(StateSpaceSystem system, List<CircuitComponent> stateVariables)
        {
            for (int i = 0; i < stateVariables.Count; i++)
            {
                var stateVar = stateVariables[i];

                if (stateVar.Type == ComponentType.Capacitor)
                {
                    // Для LC-цепи задаем начальное напряжение для возбуждения колебаний
                    var hasInductor = components.Any(c => c.Type == ComponentType.Inductor);
                    var hasSource = components.Any(c => c.Type == ComponentType.CurrentSource || c.Type == ComponentType.VoltageSource);

                    if (hasInductor && !hasSource)
                    {
                        system.InitialConditions[i] = 1.0; // Начальное напряжение 1В для LC-генерации
                    }
                    else
                    {
                        system.InitialConditions[i] = 0;
                    }
                }
                else if (stateVar.Type == ComponentType.Inductor)
                {
                    system.InitialConditions[i] = 0; // Начальный ток = 0
                }
                else
                {
                    system.InitialConditions[i] = 0;
                }

                Console.WriteLine($"  Начальное условие {GetVariableName(stateVar)} = {system.InitialConditions[i]}");
            }
        }
    }
}