// TopologyAnalyzer.cs
using System;
using System.Collections.Generic;
using System.Linq;

namespace StateVariablesMethod
{
    public class TopologyAnalyzer
    {
        private List<CircuitComponent> components;
        private DirectedGraph graph;
        private MMatrix mMatrix;

        public TopologyAnalyzer(List<CircuitComponent> components, DirectedGraph graph, MMatrix mMatrix)
        {
            this.components = components;
            this.graph = graph;
            this.mMatrix = mMatrix;
        }

        // Основной метод для определения коэффициента напряжения
        public double GetVoltageCoefficient(CircuitComponent component, double sign, 
            List<CircuitComponent> stateVariables, List<CircuitComponent> inputVariables)
        {
            if (component.Type == ComponentType.Resistor)
            {
                return AnalyzeResistorVoltage(component, sign, stateVariables);
            }
            else if (component.Type == ComponentType.VoltageSource)
            {
                return sign; // Напряжение источника - входное воздействие
            }
            else if (component.Type == ComponentType.Capacitor)
            {
                return sign; // Напряжение на конденсаторе - переменная состояния
            }

            return 0;
        }

        // Основной метод для определения коэффициента тока
        public double GetCurrentCoefficient((CircuitComponent Component, bool IsPositive) term,
            List<CircuitComponent> stateVariables, List<CircuitComponent> inputVariables)
        {
            var component = term.Component;
            double sign = term.IsPositive ? 1.0 : -1.0;

            if (component.Type == ComponentType.Resistor)
            {
                return AnalyzeResistorCurrent(component, sign, stateVariables);
            }
            else if (component.Type == ComponentType.CurrentSource)
            {
                return sign; // Ток источника - входное воздействие
            }
            else if (component.Type == ComponentType.Inductor)
            {
                return sign; // Ток через индуктивность - переменная состояния
            }

            return 0;
        }

        // Анализ резистора в контуре (для напряжений)
        private double AnalyzeResistorVoltage(CircuitComponent resistor, double sign, 
            List<CircuitComponent> stateVariables)
        {
            // Находим связанные компоненты через M-матрицу
            var relatedComponents = FindRelatedComponents(resistor);

            // Приоритет 1: резистор последовательно с индуктивностью (u_R = i_L * R)
            var seriesInductor = relatedComponents.FirstOrDefault(c => 
                c.Type == ComponentType.Inductor && stateVariables.Contains(c));
            if (seriesInductor != null)
            {
                Console.WriteLine($"  Топология: R{resistor.Name} последовательно с L{seriesInductor.Name} -> u_R = i_L * R");
                return sign * resistor.Value;
            }

            // Приоритет 2: резистор параллельно конденсатору (u_R = u_C)
            var parallelCapacitor = relatedComponents.FirstOrDefault(c => 
                c.Type == ComponentType.Capacitor && stateVariables.Contains(c));
            if (parallelCapacitor != null)
            {
                Console.WriteLine($"  Топология: R{resistor.Name} параллельно C{parallelCapacitor.Name} -> u_R = u_C");
                return sign; // u_R = u_C
            }

            // Приоритет 3: резистор подключен к тому же узлу что и конденсатор
            var capacitorAtSameNode = FindComponentAtSameNodes(resistor, ComponentType.Capacitor, stateVariables);
            if (capacitorAtSameNode != null)
            {
                Console.WriteLine($"  Топология: R{resistor.Name} подключен к тому же узлу что и C{capacitorAtSameNode.Name} -> u_R = u_C");
                return sign;
            }

            Console.WriteLine($"  ПРЕДУПРЕЖДЕНИЕ: Не удалось определить топологию для R{resistor.Name}");
            return 0;
        }

        // Анализ резистора в сечении (для токов)
        private double AnalyzeResistorCurrent(CircuitComponent resistor, double sign, 
            List<CircuitComponent> stateVariables)
        {
            var relatedComponents = FindRelatedComponents(resistor);

            // Приоритет 1: резистор параллельно конденсатору (i_R = u_C / R)
            var parallelCapacitor = relatedComponents.FirstOrDefault(c => 
                c.Type == ComponentType.Capacitor && stateVariables.Contains(c));
            if (parallelCapacitor != null)
            {
                Console.WriteLine($"  Топология: R{resistor.Name} параллельно C{parallelCapacitor.Name} -> i_R = u_C / R");
                return sign / resistor.Value;
            }

            // Приоритет 2: резистор последовательно с индуктивностью (i_R = i_L)
            var seriesInductor = relatedComponents.FirstOrDefault(c => 
                c.Type == ComponentType.Inductor && stateVariables.Contains(c));
            if (seriesInductor != null)
            {
                Console.WriteLine($"  Топология: R{resistor.Name} последовательно с L{seriesInductor.Name} -> i_R = i_L");
                return sign;
            }

            // Приоритет 3: резистор подключен к тому же узлу что и индуктивность
            var inductorAtSameNode = FindComponentAtSameNodes(resistor, ComponentType.Inductor, stateVariables);
            if (inductorAtSameNode != null)
            {
                Console.WriteLine($"  Топология: R{resistor.Name} подключен к тому же узлу что и L{inductorAtSameNode.Name} -> i_R = i_L");
                return sign;
            }

            Console.WriteLine($"  ПРЕДУПРЕЖДЕНИЕ: Не удалось определить топологию для R{resistor.Name}");
            return 0;
        }

        // Находит компоненты, связанные через M-матрицу
        private List<CircuitComponent> FindRelatedComponents(CircuitComponent component)
        {
            var related = new List<CircuitComponent>();

            // Проверяем, находится ли компонент в дереве
            int treeIndex = graph.Tree.IndexOf(component);
            if (treeIndex >= 0)
            {
                // Компонент в дереве - ищем связанные хорды
                for (int i = 0; i < graph.Chords.Count; i++)
                {
                    if (mMatrix[i, treeIndex] != 0)
                    {
                        related.Add(graph.Chords[i]);
                    }
                }
                return related;
            }

            // Проверяем, находится ли компонент в хордах
            int chordIndex = graph.Chords.IndexOf(component);
            if (chordIndex >= 0)
            {
                // Компонент в хордах - ищем связанное дерево
                for (int j = 0; j < graph.Tree.Count; j++)
                {
                    if (mMatrix[chordIndex, j] != 0)
                    {
                        related.Add(graph.Tree[j]);
                    }
                }
            }

            return related;
        }

        // Находит компонент указанного типа, подключенный к тем же узлам
        private CircuitComponent FindComponentAtSameNodes(CircuitComponent reference, 
            ComponentType targetType, List<CircuitComponent> stateVariables)
        {
            return stateVariables.FirstOrDefault(comp => 
                comp.Type == targetType && 
                (comp.Node1 == reference.Node1 || comp.Node1 == reference.Node2 ||
                 comp.Node2 == reference.Node1 || comp.Node2 == reference.Node2));
        }

        // Анализ типа цепи для автоматической настройки
        public CircuitType AnalyzeCircuitType()
        {
            bool hasR = components.Any(c => c.Type == ComponentType.Resistor);
            bool hasL = components.Any(c => c.Type == ComponentType.Inductor);
            bool hasC = components.Any(c => c.Type == ComponentType.Capacitor);

            if (hasR && hasL && hasC) return CircuitType.RLC;
            if (hasR && hasC) return CircuitType.RC;
            if (hasR && hasL) return CircuitType.RL;
            if (hasL && hasC) return CircuitType.LC;
            if (hasC) return CircuitType.C;
            if (hasL) return CircuitType.L;
            return CircuitType.Resistive;
        }

        // Получение рекомендаций по параметрам моделирования
        public (double simulationTime, double timeStep) GetSimulationParameters()
        {
            var circuitType = AnalyzeCircuitType();
            
            switch (circuitType)
            {
                case CircuitType.RLC:
                    var capacitor = components.First(c => c.Type == ComponentType.Capacitor);
                    var inductor = components.First(c => c.Type == ComponentType.Inductor);
                    double naturalPeriod = 2 * Math.PI * Math.Sqrt(inductor.Value * capacitor.Value);
                    return (naturalPeriod * 5, naturalPeriod / 100);
                
                case CircuitType.LC:
                    capacitor = components.First(c => c.Type == ComponentType.Capacitor);
                    inductor = components.First(c => c.Type == ComponentType.Inductor);
                    naturalPeriod = 2 * Math.PI * Math.Sqrt(inductor.Value * capacitor.Value);
                    return (naturalPeriod * 3, naturalPeriod / 200);
                
                case CircuitType.RC:
                    capacitor = components.First(c => c.Type == ComponentType.Capacitor);
                    var resistors = components.Where(c => c.Type == ComponentType.Resistor);
                    double minResistance = resistors.Min(r => r.Value);
                    double timeConstant = capacitor.Value * minResistance;
                    return (timeConstant * 5, timeConstant / 100);
                
                case CircuitType.RL:
                    inductor = components.First(c => c.Type == ComponentType.Inductor);
                    resistors = components.Where(c => c.Type == ComponentType.Resistor);
                    minResistance = resistors.Min(r => r.Value);
                    timeConstant = inductor.Value / minResistance;
                    return (timeConstant * 5, timeConstant / 100);
                
                default:
                    return (0.01, 0.0001);
            }
        }
    }

    public enum CircuitType
    {
        RLC,
        RC, 
        RL,
        LC,
        C,
        L,
        Resistive
    }
}