/// Dedicated to the public domain by Christopher Diggins
/// http://creativecommons.org/licenses/publicdomain/

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.ComponentModel;
using System.Text.RegularExpressions;

namespace Cat
{
    public class Executor
    {
        public TextReader input = Console.In;
        public TextWriter output = Console.Out;
        private Dictionary<string, Function> dictionary = new Dictionary<string, Function>();
        List<Object> stack = new List<Object>();

        public Executor()
        {
            RegisterType(typeof(MetaCommands));
            RegisterType(typeof(Primitives));
        }
        
        public Object Peek()
        {
            return stack[stack.Count - 1];
        }

        public void Push(Object o)
        {
            stack.Add(o);
        }

        public Object Pop()
        {
            Object o = Peek();
            stack.RemoveAt(stack.Count - 1);
            return o;
        }

        public Object PeekBelow(int n)
        {
            return stack[stack.Count - 1 - n];
        }

        public int Count()
        {
            return stack.Count;
        }

        public Object[] GetStackAsArray()
        {
            return stack.ToArray();
        }

        public void Dup()
        {
            if (Peek() is CatList)
                Push((Peek() as CatList).Clone());
            else
                Push(Peek());
        }

        public void Swap()
        {
            Object tmp = stack[stack.Count - 2];
            stack[stack.Count - 2] = stack[stack.Count - 1];
            stack[stack.Count - 1] = tmp;
        }

        public bool IsEmpty()
        {
            return Count() == 0;
        }

        public void Clear()
        {
            stack.Clear();
        }

        public string StackToString()
        {
            if (IsEmpty()) return "_empty_";
            string s = "";
            int nMax = 5;
            if (Count() > nMax) s = "...";
            if (Count() < nMax) nMax = Count();
            for (int i = nMax - 1; i >= 0; --i)
            {
                Object o = PeekBelow(i);
                s += Output.ObjectToString(o) + " ";
            }
            return s;
        }

        public T TypedPop<T>()
        {
            T result = TypedPeek<T>();
            Pop();
            return result;
        }

        public T TypedPeek<T>()
        {
            Object o = Peek();

            if (!(o is T))
                throw new Exception("Expected type " + typeof(T).Name + " but instead found " + o.GetType().Name);

            return (T)o;
        }

        public void PushInt(int n)
        {
            Push(n);
        }

        public void PushBool(bool x)
        {
            Push(x);
        }

        public void PushString(string x)
        {
            Push(x);
        }

        public void PushFxn(Function x)
        {
            Push(x);
        }

        public int PopInt()
        {
            return TypedPop<int>();
        }

        public bool PopBool()
        {
            return TypedPop<bool>();
        }

        public QuotedFunction PopFunction()
        {
            return TypedPop<QuotedFunction>();
        }

        public String PopString()
        {
            return TypedPop<String>();
        }

        public Function PeekProgram()
        {
            return TypedPeek<Function>();
        }

        public String PeekString()
        {
            return TypedPeek<String>();
        }

        public int PeekInt()
        {
            return TypedPeek<int>();
        }

        public bool PeekBool()
        {
            return TypedPeek<bool>();
        }

        public void AddInt(int n)
        {
            PushInt(PopInt() + n);
        }

        public void SubInt(int n)
        {
            PushInt(PopInt() + n);
        }

        public void IncInt()
        {
            PushInt(PopInt() + 1);
        }

        public void DecInt()
        {
            PushInt(PopInt() - 1);
        }

        public void LtInt(int n)
        {
            PushBool(PopInt() < n);
        }

        public void Import()
        {
            LoadModule(PopString());
        }

        public void LoadModule(string s)
        {
            bool b1 = Config.gbVerboseInference;
            bool b2 = Config.gbShowInferredType;
            Config.gbVerboseInference = Config.gbVerboseInferenceOnLoad;
            Config.gbShowInferredType = false;
            try
            {
                Execute(Util.FileToString(s));
            }
            catch (Exception e)
            {
                Output.WriteLine("Failed to load \"" + s + "\" with message: " + e.Message);
            }
            Config.gbVerboseInference = b1;
            Config.gbShowInferredType = b2;
        }

        public void Execute(string s)
        {
            try
            {
                List<CatAstNode> nodes = CatParser.Parse(s + "\n");
                Execute(nodes);
            }
            catch (Exception e)
            {
                Output.WriteLine("error: " + e.Message);
            }
        }

        public void OutputStack()
        {
            Output.WriteLine("stack: " + StackToString());
        }

        public Function LiteralToFunction(AstLiteral literal)
        {
            switch (literal.GetLabel())
            {
                case AstLabel.Int:
                    {
                        AstInt tmp = literal as AstInt;
                        return new PushInt(tmp.GetValue());
                    }
                case AstLabel.Bin:
                    {
                        AstBin tmp = literal as AstBin;
                        return new PushInt(tmp.GetValue());
                    }
                case AstLabel.Char:
                    {
                        AstChar tmp = literal as AstChar;
                        return new PushValue<char>(tmp.GetValue());
                    }
                case AstLabel.String:
                    {
                        AstString tmp = literal as AstString;
                        return new PushValue<string>(tmp.GetValue());
                    }
                case AstLabel.Float:
                    {
                        AstFloat tmp = literal as AstFloat;
                        return new PushValue<double>(tmp.GetValue());
                    }
                case AstLabel.Hex:
                    {
                        AstHex tmp = literal as AstHex;
                        return new PushInt(tmp.GetValue());
                    }
                case AstLabel.Quote:
                    {
                        AstQuote tmp = literal as AstQuote;
                        return new PushFunction(NodesToFxns(tmp.GetTerms()));
                    }
                case AstLabel.Lambda:
                    {
                        AstLambda tmp = literal as AstLambda;
                        return new PushFunction(NodesToFxns(tmp.GetTerms()));
                    }
                default:
                    throw new Exception("unhandled literal " + literal.ToString());
            }
        }
        
        public void Execute(List<Function> fxns)
        {
            for (int i = 0; i < fxns.Count; ++i)
            {
                Function f = fxns[i];
                // Is tail call? 
                if (i == fxns.Count - 1 && !(f is PrimitiveFunction))
                {
                    fxns = f.GetSubFxns();
                }
                else
                {
                    f.Eval(this);
                }
            }
        }

        public List<Function> NodesToFxns(List<CatAstNode> nodes)
        {
            List<Function> result = new List<Function>(); 
            for (int i = 0; i < nodes.Count; ++i)
            {
                CatAstNode node = nodes[i];
                if (node.GetLabel().Equals(AstLabel.Name))
                {
                    string s = node.ToString();
                    Function f = ThrowingLookup(s);
                    result.Add(f);
                }
                else if (node is AstLiteral)
                {
                    result.Add(LiteralToFunction(node as AstLiteral));
                }
                else if (node is AstDef)
                {
                    result.Add(MakeFunction(node as AstDef));
                }
                else if (node is AstMacro)
                {
                    Macros.AddMacro(node as AstMacro);
                }
                else
                {
                    throw new Exception("unable to convert node to function: " + node.ToString());
                }
            }
            return result;
        }
        
        public void Execute(List<CatAstNode> nodes)
        {
            Execute(NodesToFxns(nodes));
        }

        public void ClearTo(int n)
        {
            while (Count() > n)
                Pop();
        }

        public CatList GetStackAsList()
        {
            return new CatList(stack);
        }

        #region dictionary management
        public void RegisterType(Type t)
        {
            foreach (Type memberType in t.GetNestedTypes())
            {
                // Is is it a function object
                if (typeof(Function).IsAssignableFrom(memberType))
                {
                    ConstructorInfo ci = memberType.GetConstructor(new Type[] { });
                    Object o = ci.Invoke(null);
                    if (!(o is Function))
                        throw new Exception("Expected only function objects in " + t.ToString());
                    Function f = o as Function;
                    AddFunction(f);
                }
                else
                {
                    RegisterType(memberType);
                }
            }
            foreach (MemberInfo mi in t.GetMembers())
            {
                if (mi is MethodInfo)
                {
                    MethodInfo meth = mi as MethodInfo;
                    if (meth.IsStatic)
                    {
                        AddMethod(null, meth);
                    }
                }
            }
        }

        /// <summary>
        /// Creates an ObjectBoundMethod for each public function in the object
        /// </summary>
        /// <param name="o"></param>
        public void RegisterObject(Object o)
        {
            foreach (MemberInfo mi in o.GetType().GetMembers())
            {
                if (mi is MethodInfo)
                {
                    MethodInfo meth = mi as MethodInfo;
                    AddMethod(o, meth);
                }
            }
        }

        public Function Lookup(string s)
        {
            if (s.Length < 1)
                throw new Exception("trying to lookup a function with no name");

            if (dictionary.ContainsKey(s))
                return dictionary[s];

            return null;
        }

        public Function ThrowingLookup(string s)
        {
            Function f = Lookup(s);
            if (f == null)
                throw new Exception("could not find function " + s);
            return f;
        }

        /// <summary>
        /// Methods allow overloading of function definitions.
        /// </summary>
        public void AddMethod(Object o, MethodInfo mi)
        {
            // Does not add public methods. Simplifies using this 
            // Function in a loop
            if (!mi.IsPublic)
                return;                

            if (mi.IsStatic)
                o = null;

            Method f = new Method(o, mi);
            AddFunction(f);
        }

        public Dictionary<String, Function>.ValueCollection GetAllFunctions()
        {
            return dictionary.Values;
        }

        public Function MakeFunction(AstDef def) 
        {
            List<Function> fxns = NodesToFxns(def.mTerms);
            Function ret = new DefinedFunction(def.mName, fxns);
            return AddFunction(ret);
        }

        public Function AddFunction(Function f)
        {
            string s = f.GetName();
            if (dictionary.ContainsKey(s))
            {
                if (!Config.gbAllowRedefines)
                    throw new Exception("can not overload functions " + s);
                dictionary[s] = f;
            }
            else
            {
                dictionary.Add(s, f);
            }
            return f;
        }

        #endregion
    }
}