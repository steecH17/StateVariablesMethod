using System.Collections.Generic;
using System.Data;
using System.Linq;
namespace StateVariablesMethod;

    public class CircuitComponent
    {
        public string Name { get; set; }
        public ComponentType Type { get; set; }
        public double Value { get; set; } // Сопротивление, емкость, индуктивность
        public int Node1 { get; set; } // Первый узел
        public int Node2 { get; set; } // Второй узел
        
        public CircuitComponent(string name, ComponentType type, double value, int node1, int node2)
        {
            Name = name;
            Type = type;
            Value = value;
            Node1 = node1;
            Node2 = node2;
        }
    }