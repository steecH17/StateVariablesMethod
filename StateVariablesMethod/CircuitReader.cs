using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace StateVariablesMethod
{
    public class CircuitReader
    {
        private string inputDirectory;

        public CircuitReader(string inputDirectory = "InputData")
        {
            this.inputDirectory = inputDirectory;

            if (!Directory.Exists(inputDirectory))
            {
                Directory.CreateDirectory(inputDirectory);
            }
        }

        public List<CircuitComponent> ReadCircuitFromTxt(string fileName)
        {
            string filePath = Path.Combine(inputDirectory, fileName);

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Файл схемы не найден: {filePath}");
            }

            var components = new List<CircuitComponent>();
            int lineNumber = 0;

            try
            {
                var lines = File.ReadAllLines(filePath);

                foreach (var line in lines)
                {
                    lineNumber++;
                    var trimmedLine = line.Trim();

                    if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("//"))
                        continue;

                    var component = ParseComponentLine(trimmedLine, lineNumber);
                    if (component != null)
                    {
                        components.Add(component);
                    }
                }

                Console.WriteLine($"Успешно загружено {components.Count} компонентов из файла: {fileName}");
                return components;
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка чтения файла {fileName} на строке {lineNumber}: {ex.Message}");
            }
        }

        private CircuitComponent ParseComponentLine(string line, int lineNumber)
        {
            // Формат: Name Type Value Node1 Node2

            var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length < 5)
            {
                Console.WriteLine($"Предупреждение: Неверный формат на строке {lineNumber}: '{line}'");
                return null;
            }

            try
            {
                string name = parts[0];
                ComponentType type = ParseComponentType(parts[1]);
                double value = ParseValue(parts[2]);
                int node1 = int.Parse(parts[3]);
                int node2 = int.Parse(parts[4]);

                return new CircuitComponent(name, type, value, node1, node2);
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка парсинга строки {lineNumber}: '{line}'. {ex.Message}");
            }
        }

        private ComponentType ParseComponentType(string typeStr)
        {
            return typeStr.ToLower() switch
            {
                "r" or "resistor" => ComponentType.Resistor,
                "c" or "capacitor" => ComponentType.Capacitor,
                "l" or "inductor" => ComponentType.Inductor,
                "v" or "voltagesource" or "vs" => ComponentType.VoltageSource,
                "i" or "currentsource" or "cs" or "j" => ComponentType.CurrentSource,
                _ => throw new ArgumentException($"Неизвестный тип компонента: {typeStr}")
            };
        }

        private double ParseValue(string valueStr)
        {
            valueStr = valueStr.Replace(',', '.');
            
            if (double.TryParse(valueStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double result))
            {
                return result;
            }
            
            throw new FormatException($"Неверный формат числа: '{valueStr}'");
        }

    }
}