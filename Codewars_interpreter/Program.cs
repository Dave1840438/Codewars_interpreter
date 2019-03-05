using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace Codewars_interpreter
{
    public abstract class ASTNode
    {
        public abstract double? Evaluate();
    }

    class Variable : ASTNode
    {
        private enum VarType { Constant, Variable }

        public Variable(List<string> tokens)
        {
            Debug.Assert(tokens.Count == 1);

            if (double.TryParse(tokens[0], out m_value))
            {
                m_type = VarType.Constant;
            }
            else
            {
                m_type = VarType.Variable;
            }
        }

        public override double? Evaluate()
        {
            return m_type == VarType.Constant ? m_value : (double?)null;
        }


        private VarType m_type;
        private double m_value;
    }

    class BinOp : ASTNode
    {
        public enum OpType { Add = 0, Mul, Pow, Division, Modulo, Sub };

        public BinOp(List<string> leftOperand, string opToken, List<string> rightOperand)
        {
            m_opType = (OpType)"+*^/%-".IndexOf(opToken);
            m_leftOperand = Interpreter.Parse(leftOperand);
            m_rightOperand = Interpreter.Parse(rightOperand);
        }


        public override double? Evaluate()
        {
            return m_leftOperand.Evaluate() + m_rightOperand.Evaluate();
        }

        private OpType m_opType;
        private ASTNode m_leftOperand;
        private ASTNode m_rightOperand;
    }

    public class Interpreter
    {
        public double? input(string input)
        {
            List<string> tokens = tokenize(input);
            return Parse(tokens).Evaluate();
        }

        static public ASTNode Parse(List<string> tokens)
        {
            int index = tokens.IndexOf("+");


            if (tokens.Count == 1)
            {
                return new Variable(tokens.GetRange(0, 1));
            }

            return new BinOp(tokens.GetRange(0, index), tokens[index],
                tokens.GetRange(index + 1, tokens.Count - (index + 1)));
        }

        private List<string> tokenize(string input)
        {
            //input = input + ")";
            List<string> tokens = new List<string>();
            Regex rgxMain = new Regex("=>|[-+*/%=\\(\\)]|[A-Za-z_][A-Za-z0-9_]*|[0-9]*(\\.?[0-9]+)");
            MatchCollection matches = rgxMain.Matches(input);
            foreach (Match m in matches) tokens.Add(m.Groups[0].Value);
            return tokens;
        }
    }



    class Program
    {
        static void Main(string[] args)
        {
            Interpreter interpret = new Interpreter();

            string input = "";

            while (input != "exit")
            {
                input = Console.ReadLine();

                double? result = interpret.input(input);

                if (result.HasValue)
                {
                    Console.WriteLine(result.Value);
                }
                else
                {
                    Console.WriteLine("No result!");
                }

            }

        }
    }
}
