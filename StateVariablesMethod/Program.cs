namespace StateVariablesMethod;

class Program
{
    static void Main(string[] args)
    {
        List<CircuitComponent> components;

        CircuitReader reader = new CircuitReader();

        Console.WriteLine("Поместите схему в формате .txt в папку InputData");
        Console.WriteLine("Напишите название файла в формате fileName.txt : ");
        var fileName = Console.ReadLine();

        components = reader.ReadCircuitFromTxt(fileName);

        Console.WriteLine($"\nМоделирование схемы с {components.Count} компонентами:");
        foreach (var comp in components)
        {
            Console.WriteLine($"  {comp.Name}: {comp.Type} = {comp.Value}");
        }

        var simulator = new CircuitSimulator(components);
        var result = simulator.Simulate();

        GraphPlotter plotter = new GraphPlotter();
        plotter.PlotCircuitCurrents("plots", result);

        Console.WriteLine("\nНажмите любую клавишу для выхода...");
        Console.ReadKey();
    }
} 