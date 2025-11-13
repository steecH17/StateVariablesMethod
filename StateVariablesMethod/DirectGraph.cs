namespace StateVariablesMethod;

public class DirectedGraph
{
    public List<CircuitComponent> Tree { get; set; } = new List<CircuitComponent>();
    public List<CircuitComponent> Chords { get; set; } = new List<CircuitComponent>();

    public void BuildGraph(List<CircuitComponent> components)
    {
        Tree.Clear();
        Chords.Clear();

        var remainingComponents = new List<CircuitComponent>(components);

        // Приоритет для дерева: источники напряжения -> емкости -> сопротивления
        var priorityOrder = new List<ComponentType>
            {
                ComponentType.VoltageSource,
                ComponentType.Capacitor,
                ComponentType.Resistor,
                ComponentType.Inductor,
                ComponentType.CurrentSource
            };


        var currentSources = remainingComponents.Where(c => c.Type == ComponentType.CurrentSource).ToList();
        Chords.AddRange(currentSources);
        remainingComponents.RemoveAll(c => currentSources.Contains(c));

        foreach (var type in priorityOrder)
        {
            if (type == ComponentType.CurrentSource) continue;

            var componentsOfType = remainingComponents.Where(c => c.Type == type).ToList();

            foreach (var component in componentsOfType)
            {
                if (!WouldCreateCycle(component))
                {
                    Tree.Add(component);
                    remainingComponents.Remove(component);
                }
            }
        }

        Chords.AddRange(remainingComponents);

        var missingCurrentSources = components.Where(c => c.Type == ComponentType.CurrentSource && !Chords.Contains(c));
        Chords.AddRange(missingCurrentSources);
        Tree.RemoveAll(c => c.Type == ComponentType.CurrentSource);

        // сортировка хорд: сопротивления → индуктивности → источники тока
        SortChordsByPriority();
    }

    private bool WouldCreateCycle(CircuitComponent newComponent)
    {
        var visitedNodes = new HashSet<int>();
        var nodesToVisit = new Queue<int>();

        nodesToVisit.Enqueue(newComponent.Node1);

        while (nodesToVisit.Count > 0)
        {
            int currentNode = nodesToVisit.Dequeue();

            if (currentNode == newComponent.Node2)
            {
                return true;
            }

            if (visitedNodes.Contains(currentNode))
                continue;

            visitedNodes.Add(currentNode);

            // Находим все компоненты дерева, подключенные к текущему узлу
            foreach (var treeComponent in Tree)
            {
                if (treeComponent.Node1 == currentNode && !visitedNodes.Contains(treeComponent.Node2))
                {
                    nodesToVisit.Enqueue(treeComponent.Node2);
                }
                else if (treeComponent.Node2 == currentNode && !visitedNodes.Contains(treeComponent.Node1))
                {
                    nodesToVisit.Enqueue(treeComponent.Node1);
                }
            }
        }

        return false;
    }

    private void SortChordsByPriority()
    {
        // Порядок приоритета для хорд: Resistance → Inductor → CurrentSource
        var priorityOrder = new List<ComponentType>
    {
        ComponentType.Resistor,       
        ComponentType.Inductor,       
        ComponentType.CurrentSource   
    };

        var sortedChords = new List<CircuitComponent>();

        foreach (var type in priorityOrder)
        {
            var chordsOfType = Chords.Where(c => c.Type == type).ToList();
            sortedChords.AddRange(chordsOfType);
        }

        Chords = sortedChords;
    }

    public void PrintGraphInfo()
    {
        Console.WriteLine("Дерево содержит ветви:");
        foreach (var component in Tree)
        {
            Console.WriteLine($"  {component.Name} ({GetTypeSymbol(component.Type)}) {component.Node1}->{component.Node2}, Value={component.Value}");
        }

        Console.WriteLine("\nХорды:");
        foreach (var component in Chords)
        {
            Console.WriteLine($"  {component.Name} ({GetTypeSymbol(component.Type)}) {component.Node1}->{component.Node2}, Value={component.Value}");
        }

        Console.WriteLine($"Порядок столбцов (ветви дерева): {string.Join(", ", Tree.Select(c => $"{c.Name}({c.Type})"))}");
        Console.WriteLine($"Порядок строк (хорды): {string.Join(", ", Chords.Select(c => $"{c.Name}({c.Type})"))}");
    }

    private string GetTypeSymbol(ComponentType type)
    {
        return type switch
        {
            ComponentType.Resistor => "R",
            ComponentType.Capacitor => "C",
            ComponentType.Inductor => "L",
            ComponentType.VoltageSource => "V",
            ComponentType.CurrentSource => "I",
            _ => "?"
        };
    }
}