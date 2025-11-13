using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ScottPlot;

namespace StateVariablesMethod
{
    public class GraphPlotter
    {
        private int width;
        private int height;

        public GraphPlotter(int width = 800, int height = 600)
        {
            this.width = width;
            this.height = height;
        }

        public void PlotAndSave(string filePath, string title, List<DataSeries> dataSeries,
                      string xLabel = "Time", string yLabel = "Value")
        {
            var plot = new Plot();
            plot.Title(title);
            plot.XLabel(xLabel);
            plot.YLabel(yLabel);

            for (int i = 0; i < dataSeries.Count; i++)
            {
                var series = dataSeries[i];
                if (series.XValues.Length > 0 && series.YValues.Length > 0)
                {
                    // Используем только маркеры без линий
                    var sp = plot.Add.ScatterPoints(series.XValues, series.YValues);
                    sp.LegendText = series.Name;
                    sp.MarkerSize = 5;
                    sp.MarkerShape = MarkerShape.FilledCircle;
                    sp.Color = GetColor(i);
                }
            }

            plot.ShowLegend();
            plot.SavePng(filePath, width, height);
        }

        public void PlotCircuitCurrents(string directory, SimulationResult result)
        {
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

            var time = result.Time;

            // Проверяем размерности
            Console.WriteLine($"Размерности данных: Time={time?.Length}, Outputs={result.Outputs?.GetLength(1)}, States={result.States?.GetLength(1)}");

            // График i2 (индекс 0 в Outputs)
            var i2Values = new double[time.Length];
            for (int i = 0; i < time.Length; i++)
            {
                i2Values[i] = result.Outputs[i, 0]; // i2 это первый выход (индекс 0)
            }

            var i2Series = new DataSeries("i2 (ток через R2)", time, i2Values);

            string plotName = $"i2_current_scottplot_{timestamp}.png";
            PlotAndSave(
                Path.Combine(directory, plotName),
                "График тока i2 через резистор R2",
                new List<DataSeries> { i2Series },
                "Время (s)", "Ток i2 (A)"
            );

            // График i3 (индекс 1 в Outputs)
            var i3Values = new double[time.Length];
            for (int i = 0; i < time.Length; i++)
            {
                i3Values[i] = result.Outputs[i, 1]; // i3 это второй выход (индекс 1)
            }

            var i3Series = new DataSeries("i3 (ток через источник)", time, i3Values);

            plotName = $"i3_current_scottplot_{timestamp}.png";
            PlotAndSave(
                Path.Combine(directory, plotName),
                "График тока i3 через источник тока",
                new List<DataSeries> { i3Series },
                "Время (s)", "Ток i3 (A)"
            );

            // Совмещенный график i2 и i3
            plotName = $"both_current_scottplot_{timestamp}.png";
            PlotAndSave(
                Path.Combine(directory, plotName),
                "Графики токов i2 и i3",
                new List<DataSeries> { i2Series, i3Series },
                "Время (s)", "Ток (A)"
            );

            // Графики переменных состояния (проверяем, что есть состояния)
            if (result.States.GetLength(1) >= 2)
            {
                var uCValues = new double[time.Length];
                var iLValues = new double[time.Length];
                for (int i = 0; i < time.Length; i++)
                {
                    uCValues[i] = result.States[i, 0]; // u_C - первое состояние
                    iLValues[i] = result.States[i, 1]; // i_L - второе состояние
                }

                var uCSeries = new DataSeries("U_C (напряжение на конденсаторе)", time, uCValues);
                var iLSeries = new DataSeries("I_L (ток через индуктивность)", time, iLValues);

                plotName = $"states_current_scottplot_{timestamp}.png";
                PlotAndSave(
                    Path.Combine(directory, plotName),
                    "Переменные состояния схемы",
                    new List<DataSeries> { uCSeries, iLSeries },
                    "Время (s)", "Значение"
                );
            }
            else
            {
                Console.WriteLine("ВНИМАНИЕ: Недостаточно данных о состояниях для построения графиков");
            }

            Console.WriteLine($"Графики ScottPlot сохранены в папке: {directory}");
        }

        private Color GetColor(int index)
        {
            var colors = new[]
            {
                ScottPlot.Colors.Red,
                ScottPlot.Colors.Blue,
                ScottPlot.Colors.Green,
                ScottPlot.Colors.Orange,
                ScottPlot.Colors.Purple,
                ScottPlot.Colors.Brown,
                ScottPlot.Colors.Magenta,
                ScottPlot.Colors.Cyan
            };

            return colors[index % colors.Length];
        }
    }
}