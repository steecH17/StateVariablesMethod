namespace StateVariablesMethod;
public class MMatrix
{
    private double[,] matrix;
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