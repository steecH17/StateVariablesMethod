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

        public double GetVoltageCoefficient(CircuitComponent component, double sign,
            List<CircuitComponent> stateVariables, List<CircuitComponent> inputVariables)
        {
            if (component.Type == ComponentType.Resistor)
            {
                return AnalyzeResistorVoltage(component, sign, stateVariables);
            }
            else if (component.Type == ComponentType.VoltageSource)
            {
                return sign;
            }
            else if (component.Type == ComponentType.Capacitor)
            {
                return sign;
            }

            return 0;
        }

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
                return sign;
            }
            else if (component.Type == ComponentType.Inductor)
            {
                return sign;
            }

            return 0;
        }

        private double AnalyzeResistorVoltage(CircuitComponent resistor, double sign,
            List<CircuitComponent> stateVariables)
        {

            bool isInTree = graph.Tree.Contains(resistor);

            if (isInTree)
            {
                return AnalyzeTreeResistorVoltage(resistor, sign, stateVariables);
            }
            else
            {
                return AnalyzeChordResistorVoltage(resistor, sign, stateVariables);
            }
        }

        private double AnalyzeTreeResistorVoltage(CircuitComponent resistor, double sign,
            List<CircuitComponent> stateVariables)
        {
            Console.WriteLine($"    Анализ напряжения на R{resistor.Name} в дереве:");

            var relatedChords = FindRelatedChordComponents(resistor);

            foreach (var chord in relatedChords)
            {
                if (chord.Type == ComponentType.Inductor && stateVariables.Contains(chord))
                {
                    if (AreComponentsInSeries(resistor, chord))
                    {
                        Console.WriteLine($"      R{resistor.Name} последовательно с L{chord.Name} -> u_R = i_L * R");
                        return sign * resistor.Value;
                    }
                }

                if (chord.Type == ComponentType.CurrentSource)
                {
                    Console.WriteLine($"      R{resistor.Name} с источником тока {chord.Name} -> u_R = J * R");
                    return sign * resistor.Value;
                }
            }

            return FindResistorCurrentThroughKirchhoff(resistor, sign, stateVariables);
        }

        private double AnalyzeChordResistorVoltage(CircuitComponent resistor, double sign,
            List<CircuitComponent> stateVariables)
        {
            Console.WriteLine($"    Анализ напряжения на R{resistor.Name} в хордах:");

            var relatedTree = FindRelatedTreeComponents(resistor);

            foreach (var treeComp in relatedTree)
            {
                if (treeComp.Type == ComponentType.Capacitor && stateVariables.Contains(treeComp))
                {
                    if (AreComponentsInParallel(resistor, treeComp))
                    {
                        Console.WriteLine($"      R{resistor.Name} параллельно C{treeComp.Name} -> u_R = u_C");
                        return sign; // u_R = u_C
                    }
                }

                if (treeComp.Type == ComponentType.VoltageSource)
                {
                    Console.WriteLine($"      R{resistor.Name} с источником напряжения {treeComp.Name} -> u_R = V");
                    return sign; // u_R = V
                }
            }

            return 0;
        }

        private double AnalyzeResistorCurrent(CircuitComponent resistor, double sign,
            List<CircuitComponent> stateVariables)
        {
            Console.WriteLine($"  Анализ тока через R{resistor.Name}:");

            bool isInTree = graph.Tree.Contains(resistor);

            if (isInTree)
            {
                return AnalyzeTreeResistorCurrent(resistor, sign, stateVariables);
            }
            else
            {
                return AnalyzeChordResistorCurrent(resistor, sign, stateVariables);
            }
        }

        private double AnalyzeTreeResistorCurrent(CircuitComponent resistor, double sign,
            List<CircuitComponent> stateVariables)
        {
            Console.WriteLine($"    Анализ тока через R{resistor.Name} в дереве:");

            var relatedChords = FindRelatedChordComponents(resistor);

            foreach (var chord in relatedChords)
            {
                if (chord.Type == ComponentType.Inductor && stateVariables.Contains(chord))
                {
                    if (AreComponentsInSeries(resistor, chord))
                    {
                        Console.WriteLine($"      R{resistor.Name} последовательно с L{chord.Name} -> i_R = i_L");
                        return sign;
                    }
                }

                if (chord.Type == ComponentType.CurrentSource)
                {
                    Console.WriteLine($"      R{resistor.Name} с источником тока {chord.Name} -> i_R = J");
                    return sign;
                }
            }

            return FindResistorCurrentThroughKirchhoff(resistor, sign, stateVariables);
        }

        private double AnalyzeChordResistorCurrent(CircuitComponent resistor, double sign,
            List<CircuitComponent> stateVariables)
        {
            Console.WriteLine($"    Анализ тока через R{resistor.Name} в хордах:");

            var relatedTree = FindRelatedTreeComponents(resistor);

            foreach (var treeComp in relatedTree)
            {
                if (treeComp.Type == ComponentType.Capacitor && stateVariables.Contains(treeComp))
                {
                    if (AreComponentsInParallel(resistor, treeComp))
                    {
                        Console.WriteLine($"      R{resistor.Name} параллельно C{treeComp.Name} -> i_R = u_C / R");
                        return sign / resistor.Value;
                    }
                }
            }

            return 0;
        }

        private double FindResistorCurrentThroughKirchhoff(CircuitComponent resistor, double sign,
            List<CircuitComponent> stateVariables)
        {
            Console.WriteLine($"      KCL анализ для R{resistor.Name}:");

            var componentsAtNode1 = FindComponentsAtNodes(resistor.Node1, resistor.Node1);
            var componentsAtNode2 = FindComponentsAtNodes(resistor.Node2, resistor.Node2);

            var allComponentsAtNodes = componentsAtNode1.Union(componentsAtNode2).ToList();

            foreach (var comp in allComponentsAtNodes)
            {
                if (comp == resistor) continue;

                if (comp.Type == ComponentType.Inductor && stateVariables.Contains(comp))
                {
                    Console.WriteLine($"        R{resistor.Name} и L{comp.Name} в одном узле -> i_R = i_L");
                    return sign;
                }

                if (comp.Type == ComponentType.Capacitor && stateVariables.Contains(comp))
                {
                    Console.WriteLine($"        R{resistor.Name} и C{comp.Name} в одном узле -> i_R = u_C / R");
                    return sign / resistor.Value;
                }

                if (comp.Type == ComponentType.CurrentSource)
                {
                    Console.WriteLine($"        R{resistor.Name} и источник тока {comp.Name} в одном узле -> i_R = J");
                    return sign;
                }
            }

            Console.WriteLine($"        Не удалось определить ток через R{resistor.Name}");
            return 0;
        }

        private List<CircuitComponent> FindRelatedChordComponents(CircuitComponent treeComponent)
        {
            var related = new List<CircuitComponent>();

            int treeIndex = graph.Tree.IndexOf(treeComponent);
            if (treeIndex >= 0)
            {
                for (int i = 0; i < graph.Chords.Count; i++)
                {
                    if (mMatrix[i, treeIndex] != 0)
                    {
                        related.Add(graph.Chords[i]);
                    }
                }
            }

            return related;
        }

        private List<CircuitComponent> FindRelatedTreeComponents(CircuitComponent chordComponent)
        {
            var related = new List<CircuitComponent>();

            int chordIndex = graph.Chords.IndexOf(chordComponent);
            if (chordIndex >= 0)
            {
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

        private List<CircuitComponent> FindComponentsAtNodes(int node1, int node2)
        {
            return components.Where(c =>
                (c.Node1 == node1 || c.Node1 == node2 ||
                 c.Node2 == node1 || c.Node2 == node2)).ToList();
        }

        // Анализирует, соединены ли два компонента последовательно
        private bool AreComponentsInSeries(CircuitComponent comp1, CircuitComponent comp2)
        {
            var commonNodes = new List<int>();

            if (comp1.Node1 == comp2.Node1 || comp1.Node1 == comp2.Node2)
                commonNodes.Add(comp1.Node1);
            if (comp1.Node2 == comp2.Node1 || comp1.Node2 == comp2.Node2)
                commonNodes.Add(comp1.Node2);

            foreach (var node in commonNodes)
            {
                var componentsAtNode = FindComponentsAtNodes(node, node);

                if (componentsAtNode.Count == 2)
                    return true;
            }

            return false;
        }

        // Анализирует, соединены ли два компонента параллельно
        private bool AreComponentsInParallel(CircuitComponent comp1, CircuitComponent comp2)
        {
            return (comp1.Node1 == comp2.Node1 && comp1.Node2 == comp2.Node2) ||
                   (comp1.Node1 == comp2.Node2 && comp1.Node2 == comp2.Node1);
        }

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
}
