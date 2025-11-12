using System;
using System.Collections.Generic;
using System.Linq;

namespace StateVariablesMethod
{
    public class DataSeries
    {
        public string Name { get; set; }
        public double[] XValues { get; set; }
        public double[] YValues { get; set; }

        public DataSeries(string name, double[] xValues, double[] yValues)
        {
            if (xValues.Length != yValues.Length)
                throw new ArgumentException("XValues and YValues must have the same length");

            Name = name;
            XValues = xValues;
            YValues = yValues;
        }
    }
}