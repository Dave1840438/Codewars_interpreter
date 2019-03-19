using System;
using System.Collections.Generic;
using System.Linq;
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

        string m_varName = "";

        public Variable(List<string> tokens)
        {
            if (tokens.Count != 1)
            {
                throw new Exception("lolz");
            }

            if (double.TryParse(tokens[0], out m_value))
            {
                m_type = VarType.Constant;
            }
            else
            {
                m_type = VarType.Variable;
                m_varName = tokens[0];
            }
        }

        public override double? Evaluate()
        {
            return m_type == VarType.Constant ? m_value : Interpreter.GetVariable(m_varName);
        }

        private VarType m_type;
        private double m_value;
    }

    class AssignNode : ASTNode
    {
        string m_varName;
        double? m_value;

        public AssignNode(string varName, List<string> value)
        {
            m_varName = varName;
            m_value = Interpreter.Parse(value).Evaluate();
        }

        public override double? Evaluate()
        {
            Interpreter.SetVariable(m_varName, m_value);
            return m_value;
        }
    }

    class BinOp : ASTNode
    {
        public enum OpType { Add = 0, Mul, Division, Modulo, Sub };

        public BinOp(List<string> leftOperand, string opToken, List<string> rightOperand)
        {
            m_opType = (OpType)"+*/%-".IndexOf(opToken);
            m_leftOperand = Interpreter.Parse(leftOperand);
            m_rightOperand = Interpreter.Parse(rightOperand);
        }

        public override double? Evaluate()
        {
            double? result = 0;

            switch(m_opType)
            {
                case OpType.Add:
                    result = m_leftOperand.Evaluate() + m_rightOperand.Evaluate();
                    break;
                case OpType.Mul:
                    result = m_leftOperand.Evaluate() * m_rightOperand.Evaluate();
                    break;
                case OpType.Division:
                    result = m_leftOperand.Evaluate() / m_rightOperand.Evaluate();
                    break;
                case OpType.Modulo:
                    result = m_leftOperand.Evaluate() % m_rightOperand.Evaluate();
                    break;
                case OpType.Sub:
                    result = m_leftOperand.Evaluate() - m_rightOperand.Evaluate();
                    break;
            }


            return result;
        }

        private OpType m_opType;
        private ASTNode m_leftOperand;
        private ASTNode m_rightOperand;
    }

    public class FunctionNode : ASTNode
    {
        string m_name;
        List<string> m_body;
        List<string> m_vars;
        List<string> m_processedTokens;

        public FunctionNode(string funcName, List<string> vars, List<string> body)
        {
            m_name = funcName;
            m_body = body;
            m_vars = vars;
        }

        public int GetNbParams()
        {
            return m_vars.Count;
        }

        public ASTNode Call(List<string> parameters)
        {
            m_processedTokens = m_body.Select(x => (string)x.Clone()).ToList();

            if (parameters.Count != m_vars.Count)
            {
                throw new Exception("Parameters!!");
            }

            for (int i = 0; i < m_vars.Count; ++i)
            {
                int index;
                do
                {
                    index = m_processedTokens.FindIndex(x => x == m_vars[i]);
                    if (index != -1)
                    {
                        m_processedTokens[index] = parameters[i];
                    }
                } while (index != -1);

            }

            return Interpreter.Parse(m_processedTokens);
        }

        public override double? Evaluate()
        {
            Interpreter.SetFunction(m_name, this);
            return null;
        }
    }

    public class Interpreter
    {
        static private Dictionary<string, double?> m_variables = new Dictionary<string, double?>();
        static private Dictionary<string, FunctionNode> m_functions = new Dictionary<string, FunctionNode>();
        
        public Interpreter()
        {
            m_variables = new Dictionary<string, double?>();
            m_functions = new Dictionary<string, FunctionNode>();
        }

        public static void SetFunction(string funcName, FunctionNode node)
        {
            if (VariableExists(funcName)) throw new Exception("Func conflict!");

            m_functions[funcName] = node;
        }

        public static ASTNode CallFunction(string funcName, List<string> parameters)
        {
            return m_functions[funcName].Call(parameters);
        }

        public static bool FunctionExists(string funcName)
        {
            return m_functions.ContainsKey(funcName);
        }

        public static int GetNumberOfParameterForFunction(string funcName)
        {
            return m_functions[funcName].GetNbParams();
        }

        public static void SetVariable(string name, double? value)
        {
            if (FunctionExists(name)) throw new Exception("Var conflict!");
            m_variables[name] = value;
        }
        
        public static double? GetVariable(string name)
        {
            if (!m_variables.ContainsKey(name))
            {
                throw new Exception("Variable " + name + "is not defined.");
            }

            return m_variables[name];
        }

        public static bool VariableExists(string varName)
        {
            return m_variables.ContainsKey(varName);
        }

        private static double? realInput(string input)
        {
            List<string> tokens = tokenize(input);

            bool debug = false;

            if (debug)
            {
                return Parse(tokens).Evaluate();
            }

            double? result = null;

            try
            {
                result = Parse(tokens).Evaluate();
            }
            catch (Exception) { }

            return result;
        }

        public double? input(string input)
        {
            return realInput(input);
        }


        public static ASTNode Parse(List<string> tokens)
        {
            int funcIndex = tokens.IndexOf("=>");

            if (funcIndex != -1 && tokens.IndexOf("fn") == 0)
            {
                return new FunctionNode(tokens[1], tokens.GetRange(2, funcIndex - 2),
                tokens.GetRange(funcIndex + 1, tokens.Count - (funcIndex + 1)));
            }


            int openIndex = -1;
            int count = -1;

            bool continueParsing = true;

            while (continueParsing)
            {
                continueParsing = false;
                for (int i = 0; i < tokens.Count; ++i)
                {
                    if (tokens[i] == "(") openIndex = i;
                    if (openIndex != -1 && tokens[i] == ")")
                    {
                        count = i - openIndex;

                        //To do: catch no result
                        string newToken = Parse(tokens.GetRange(openIndex + 1, count - 1)).Evaluate().ToString();
                        tokens.RemoveRange(openIndex, count + 1);
                        tokens.Insert(openIndex, newToken);
                        continueParsing = true;
                        break;
                    }
                }
            }

            //foreach (string t in tokens) Console.Write(t + " ");
            //Console.WriteLine();

            if (tokens.IndexOf("=") == 1)
            {
                return new AssignNode(tokens[0], tokens.GetRange(2, tokens.Count-2));
            }

            if (tokens.Count == 1)
            {
                if (FunctionExists(tokens[0]))
                {
                    return CallFunction(tokens[0], new List<string>());
                }

                return new Variable(tokens.GetRange(0, 1));
            }

            int index = tokens.IndexOf(tokens.FindLast(x => "+-".Contains(x)));

            if (index != -1)
            {
                return new BinOp(tokens.GetRange(0, index), tokens[index],
                        tokens.GetRange(index + 1, tokens.Count - (index + 1)));
            }

            index = tokens.IndexOf(tokens.FindLast(x => "*/%".Contains(x)));

            if (index != -1)
            {
                return new BinOp(tokens.GetRange(0, index), tokens[index],
                        tokens.GetRange(index + 1, tokens.Count - (index + 1)));
            }

            bool continueSeaching = true;

            int functionIndex = -1;

            while (continueSeaching)
            {
                continueSeaching = false;
                for (int i = tokens.Count - 1; i >= 0; --i)
                {
                    if (FunctionExists(tokens[i]))
                    {
                        count = i - openIndex;

                        int nbOfParams = GetNumberOfParameterForFunction(tokens[i]);

                        //To do: catch no result
                        string newToken = CallFunction(tokens[i], tokens.GetRange(i + 1, nbOfParams)).Evaluate().ToString();
                        tokens.RemoveRange(i, nbOfParams + 1);
                        tokens.Insert(i, newToken);
                        continueSeaching = true;
                        break;
                    }
                }
            }

            return new Variable(tokens);
        }

        private static List<string> tokenize(string input)
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
            string input = "";

            Interpreter interpreter = new Interpreter();

            while (input != "exit")
            {
                input = Console.ReadLine();

                double? result = interpreter.input(input);

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
