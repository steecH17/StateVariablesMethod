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
                ComponentType.Inductor
            };

       
        var currentSources = remainingComponents.Where(c => c.Type == ComponentType.CurrentSource).ToList();
        Chords.AddRange(currentSources);
        remainingComponents.RemoveAll(c => currentSources.Contains(c));

        foreach (var type in priorityOrder)
        {
            var componentsOfType = remainingComponents.Where(c => c.Type == type).ToList();

            foreach (var component in componentsOfType)
            {
                if (!WouldCreateCycle(Tree, component))
                {
                    Tree.Add(component);
                    remainingComponents.Remove(component);
                }
            }
        }

        Chords.AddRange(remainingComponents);
    }

    private bool WouldCreateCycle(List<CircuitComponent> tree, CircuitComponent newComponent)
    {
        return tree.Any(t => SharesNodes(t, newComponent));
    }

    private bool SharesNodes(CircuitComponent comp1, CircuitComponent comp2)
    {
        return (comp1.Node1 == comp2.Node1 && comp1.Node2 == comp2.Node2) ||
               (comp1.Node1 == comp2.Node2 && comp1.Node2 == comp2.Node1);
    }
}