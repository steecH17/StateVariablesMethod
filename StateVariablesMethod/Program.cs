// Пример использования
namespace StateVariablesMethod;

class Program
{
    static void Main(string[] args)
    {

        Console.WriteLine("Выберите схему для моделирования:");
        Console.WriteLine("1. RC-цепь");
        Console.WriteLine("2. RL-цепь");
        Console.WriteLine("3. Сложная RLC-цепь");
        Console.WriteLine("4. Считать схему из .txt файла");
        Console.WriteLine("5. Схема из задания");

        var choice = Console.ReadLine();

        List<CircuitComponent> components;

        CircuitReader reader = new CircuitReader();

        switch (choice)
        {
            case "1":
                components = TestsCircuit.CreateRCSeriesCircuit();
                break;
            case "2":
                components = TestsCircuit.CreateRLCircuit();
                break;
            case "3":
                components = TestsCircuit.CreateComplexCircuit();
                break;
            case "4":
                components = reader.ReadCircuitFromTxt("testRLC.txt");
                break;
            case "5":
            default:
                components = new List<CircuitComponent>
                    {
                        new CircuitComponent("R1", ComponentType.Resistor, 1000, 1, 2),
                        new CircuitComponent("R2", ComponentType.Resistor, 2000, 2, 0),
                        new CircuitComponent("C", ComponentType.Capacitor, 1e-6, 2, 0),
                        new CircuitComponent("L", ComponentType.Inductor, 0.1, 1, 0),
                        new CircuitComponent("J", ComponentType.CurrentSource, 0.001, 2, 0)
                    };
                break;
        }

        Console.WriteLine($"\nМоделирование схемы с {components.Count} компонентами:");
        foreach (var comp in components)
        {
            Console.WriteLine($"  {comp.Name}: {comp.Type} = {comp.Value}");
        }

        var simulator = new CircuitSimulator(components);
        var result = simulator.Simulate(0.02,  0.00002);

        // Создание графиков
        GraphPlotter plotter = new GraphPlotter();
        plotter.PlotCircuitCurrents("plots", result);

        Console.WriteLine("\nНажмите любую клавишу для выхода...");
        Console.ReadKey();
    }
}