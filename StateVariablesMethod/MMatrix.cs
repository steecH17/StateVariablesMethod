namespace StateVariablesMethod;

public class MMatrix
{
    private double[,] matrix;
    private DirectedGraph graph;
    public int Rows { get; }
    public int Cols { get; }

    public MMatrix(int rows, int cols)
    {
        Rows = rows;
        Cols = cols;
        matrix = new double[rows, cols];
    }

    public double this[int row, int col]
    {
        get => matrix[row, col];
        set => matrix[row, col] = value;
    }

    public void BuildMMatrix(DirectedGraph directedGraph)
    {
        graph = directedGraph;
        int chordCount = graph.Chords.Count;
        int treeCount = graph.Tree.Count;


        if (chordCount == 0 || treeCount == 0)
        {
            Console.WriteLine("Нет хорд или дерево пустое - M-матрица не может быть построена");
            return;
        }

        // Для каждой хорды строим фундаментальный контур
        for (int i = 0; i < chordCount; i++)
        {
            var chord = graph.Chords[i];

            Console.WriteLine($"\n=== Контур для хорды {chord.Name} ({chord.Node1}→{chord.Node2}) ===");

            // Строим фундаментальный контур для мультиграфа
            var loopInfo = BuildFundamentalLoopForMultiGraph(chord);

            // Заполняем строку M-матрицы
            for (int j = 0; j < treeCount; j++)
            {
                var treeComponent = graph.Tree[j];

                if (loopInfo.ComponentsInLoop.Contains(treeComponent))
                {
                    matrix[i, j] = DetermineSignForComponentInLoop(treeComponent, loopInfo);
                    Console.WriteLine($"  {treeComponent.Name} ({treeComponent.Node1}→{treeComponent.Node2}): {matrix[i, j]}");
                }
                else
                {
                    matrix[i, j] = 0;
                }
            }
        }
    }

    private LoopInfo BuildFundamentalLoopForMultiGraph(CircuitComponent chord)
    {
        var loopInfo = new LoopInfo(chord);

        // В мультиграфе ищем УНИКАЛЬНЫЙ путь через дерево между узлами хорды
        var pathThroughTree = FindUniquePathInTree(chord.Node1, chord.Node2);

        loopInfo.ComponentsInLoop.Add(chord);
        loopInfo.ComponentsInLoop.AddRange(pathThroughTree);

        // Восстанавливаем последовательность узлов с учетом ориентации
        loopInfo.NodeSequence = ReconstructLoopSequence(chord, pathThroughTree);

        Console.WriteLine($"  Путь через дерево: {string.Join(" → ", pathThroughTree.Select(c => $"{c.Name}({c.Node1}→{c.Node2})"))}");
        Console.WriteLine($"  Полная последовательность узлов: {string.Join("→", loopInfo.NodeSequence)}");

        return loopInfo;
    }

    private List<CircuitComponent> FindUniquePathInTree(int startNode, int endNode)
    {
        // BFS для нахождения пути в мультиграфе-дереве
        var queue = new Queue<PathState>();
        var visited = new HashSet<int>();

        queue.Enqueue(new PathState(startNode, new List<CircuitComponent>()));
        visited.Add(startNode);

        while (queue.Count > 0)
        {
            var currentState = queue.Dequeue();

            if (currentState.Node == endNode)
            {
                return currentState.Path;
            }

            // Находим ВСЕ компоненты дерева, подключенные к текущему узлу
            foreach (var component in graph.Tree)
            {
                if (currentState.Path.Contains(component)) continue;

                int nextNode = -1;
                if (component.Node1 == currentState.Node && !visited.Contains(component.Node2))
                {
                    nextNode = component.Node2;
                }
                else if (component.Node2 == currentState.Node && !visited.Contains(component.Node1))
                {
                    nextNode = component.Node1;
                }

                if (nextNode != -1)
                {
                    visited.Add(nextNode);
                    var newPath = new List<CircuitComponent>(currentState.Path) { component };
                    queue.Enqueue(new PathState(nextNode, newPath));
                }
            }
        }

        return new List<CircuitComponent>();
    }

    private List<int> ReconstructLoopSequence(CircuitComponent chord, List<CircuitComponent> treePath)
    {
        var sequence = new List<int> { chord.Node1 };
        int currentNode = chord.Node1;

        // Проходим по пути через дерево
        foreach (var component in treePath)
        {
            if (component.Node1 == currentNode)
            {
                sequence.Add(component.Node2);
                currentNode = component.Node2;
            }
            else if (component.Node2 == currentNode)
            {
                sequence.Add(component.Node1);
                currentNode = component.Node1;
            }
        }

        // Замыкаем контур через хорду (должны прийти в chord.Node2)
        if (sequence.Last() != chord.Node2)
        {
            sequence.Add(chord.Node2);
        }

        return sequence;
    }

    private double DetermineSignForComponentInLoop(CircuitComponent component, LoopInfo loopInfo)
    {
        if (component == loopInfo.Chord)
            return 1.0;

        // Простой подход: анализируем как компоненты подключены друг к другу
        int commonNode = FindCommonNode(component, loopInfo.Chord);

        if (commonNode == -1) return 0;

        // Логика: если компоненты "встречаются" в общем узле -> +1, если "расходятся" -> -1
        bool chordEntersCommon = (loopInfo.Chord.Node2 == commonNode);
        bool treeExitsCommon = (component.Node1 == commonNode);

        bool chordExitsCommon = (loopInfo.Chord.Node1 == commonNode);
        bool treeEntersCommon = (component.Node2 == commonNode);

        if ((chordEntersCommon && treeExitsCommon) || (chordExitsCommon && treeEntersCommon))
        {
            // "Встречаются" в узле -> одно направление
            Console.WriteLine($"    {component.Name} и {loopInfo.Chord.Name} ВСТРЕЧАЮТСЯ в узле {commonNode} → +1");
            return 1.0;
        }
        else
        {
            // "Расходятся" из узла -> разные направления  
            Console.WriteLine($"    {component.Name} и {loopInfo.Chord.Name} РАСХОДЯТСЯ из узла {commonNode} → -1");
            return -1.0;
        }
    }

    private int FindCommonNode(CircuitComponent comp1, CircuitComponent comp2)
    {
        if (comp1.Node1 == comp2.Node1 || comp1.Node1 == comp2.Node2)
            return comp1.Node1;
        if (comp1.Node2 == comp2.Node1 || comp1.Node2 == comp2.Node2)
            return comp1.Node2;
        return -1;
    }

    // Вспомогательные классы
    private class LoopInfo
    {
        public CircuitComponent Chord { get; }
        public List<CircuitComponent> ComponentsInLoop { get; }
        public List<int> NodeSequence { get; set; }

        public LoopInfo(CircuitComponent chord)
        {
            Chord = chord;
            ComponentsInLoop = new List<CircuitComponent>();
        }
    }

    private class PathState
    {
        public int Node { get; }
        public List<CircuitComponent> Path { get; }

        public PathState(int node, List<CircuitComponent> path)
        {
            Node = node;
            Path = path;
        }
    }

    public void Print()
    {
        if (Rows == 0 || Cols == 0)
        {
            Console.WriteLine("M-матрица пуста");
            return;
        }

        Console.WriteLine("M-матрица:");
        for (int i = 0; i < Rows; i++)
        {
            for (int j = 0; j < Cols; j++)
            {
                Console.Write($"{matrix[i, j],8:F3} ");
            }
            Console.WriteLine();
        }
    }
}