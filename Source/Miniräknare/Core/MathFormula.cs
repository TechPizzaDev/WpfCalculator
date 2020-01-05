using System;

namespace Miniräknare
{
    public class MathFormula
    {
        public MathFormula(MathFormulaData data) : this(data.Inputs, data.Outputs)
        {
        }
        
        public MathFormula(string[] input, string[] output)
        {
            Console.WriteLine("HEH");
        }
    }
}
