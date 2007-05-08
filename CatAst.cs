/// Public domain code by Christopher Diggins
/// http://www.cat-language.com

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Globalization;
using Peg;

namespace Cat
{
    /// <summary>
    /// A AstNode is used as a base class for a typed abstract syntax tree for Cat programs.
    /// CatAstNodes are created from a Peg.Ast. Apart from being typed, the big difference
    /// is the a AstNode can be modified. This makes rewriting algorithms much easier. 
    /// </summary>
    public abstract class CatAstNode
    {
        string msText;
        string msLabel;
        string msComment;

        public CatAstNode(PegAstNode node)
        {
            if (node.GetNumChildren() == 0)
                msText = node.ToString();
            else
                msText = "";
            msLabel = node.GetLabel();
        }

        public CatAstNode(string sLabel, string sText)
        {
            msLabel = sLabel;
            msText = sText;
        }

        public void SetComment(string s)
        {
            msComment = s;
        }

        public static CatAstNode Create(PegAstNode node)
        {
            switch (node.GetLabel())
            {
                case "program":
                    return new AstProgram(node);
                case "def":
                    return new AstDefNode(node);
                case "name":
                    return new AstNameNode(node);
                case "param":
                    return new AstParamNode(node);
                case "quote":
                    return new AstQuoteNode(node);
                case "char":
                    return new AstCharNode(node);
                case "string":
                    return new AstStringNode(node);
                case "float":
                    return new AstFloatNode(node);
                case "int":
                    return new AstIntNode(node);
                case "bin":
                    return new AstBinNode(node);
                case "hex":
                    return new AstHexNode(node);
                case "stack":
                    return new AstStackNode(node);
                case "type_fxn":
                    return new AstFxnTypeNode(node);
                case "type_var":
                    return new AstTypeVarNode(node);
                case "type_name":
                    return new AstSimpleTypeNode(node);
                case "stack_var":
                    return new AstStackVarNode(node);
                case "macro":
                    return new AstMacroNode(node);
                case "macro_pattern":
                    return new AstMacroPattern(node);
                case "macro_type_var":
                    return new AstMacroTypeVar(node);
                case "macro_stack_var":
                    return new AstMacroStackVar(node);
                case "macro_name":
                    return new AstMacroName(node);
                default:
                    throw new Exception("unrecognized node type in AST tree: " + node.GetLabel());
            }
        }   

        public void CheckLabel(string s)
        {
            if (msLabel != s)
            {
                throw new Exception("Expected '" + s + "' node, but instead found '" + msLabel + "' node");
            }
        }

        public void CheckIsLeaf(PegAstNode node)
        {
            CheckChildCount(node, 0);
        }

        public void CheckChildCount(PegAstNode node, int n)
        {
            if (node.GetNumChildren() != n)
            {
                throw new Exception("expected " + n.ToString() + " children, instead found " + node.GetNumChildren().ToString());
            }
        }

        public string GetLabel()
        {
            return msLabel;
        }

        public override string ToString()
        {
            return msText;
        }

        public void SetText(string sText)
        {
            msText = sText;
        }

        public string GetComment()
        {
            return msComment;
        }

        public bool HasComment()
        {
            return msComment != null;
        }

        public string IndentedString(int nIndent, string s)
        {
            if (nIndent > 0)
                return new String('\t', nIndent) + s;
            else
                return s;
        }

        public virtual void Output(TextWriter writer, int nIndent)
        {
            writer.Write(ToString());
        }
    }

    public class AstProgram : CatAstNode
    {
        public List<AstDefNode> Defs = new List<AstDefNode>();

        public AstProgram(PegAstNode node) : base(node)
        {
            CheckLabel("ast");
            foreach (PegAstNode child in node.GetChildren())
                Defs.Add(new AstDefNode(child));
        }

        public override void Output(TextWriter writer, int nIndent)
        {
            foreach (AstDefNode d in Defs)
                d.Output(writer, nIndent);
        }
    }

    public class AstExprNode : CatAstNode
    {
        public AstExprNode(PegAstNode node) : base(node) { }
        public AstExprNode(string sLabel, string sText) : base(sLabel, sText) { }

        public override void Output(TextWriter writer, int nIndent)
        {
            string sLine = ToString();
            if (HasComment())
                sLine += " // " + GetComment();
            writer.WriteLine(IndentedString(nIndent, sLine));
        }
    }

    public class AstDefNode : CatAstNode
    {
        public string mName;
        public AstFxnTypeNode mType;
        public List<AstParamNode> mParams = new List<AstParamNode>();
        public List<AstExprNode> mTerms = new List<AstExprNode>();

        public AstDefNode(PegAstNode node) : base(node)
        {
            CheckLabel("def");

            if (node.GetNumChildren() == 0)
                throw new Exception("invalid function definition node");

            AstNameNode name = new AstNameNode(node.GetChild(0));
            mName = name.ToString();

            int n = 1;

            // Look to see if a type is defined
            if ((node.GetNumChildren() >= 2) && (node.GetChild(1).GetLabel() == "type_fxn"))
            {
                mType = new AstFxnTypeNode(node.GetChild(1));
                ++n;
            }

            while (n < node.GetNumChildren())
            {
                PegAstNode child = node.GetChild(n);
                
                if (child.GetLabel() != "param")
                    break;

                mParams.Add(new AstParamNode(child));
                n++;
            }

            while (n < node.GetNumChildren())
            {
                PegAstNode child = node.GetChild(n);
                CatAstNode expr = Create(child);

                if (!(expr is AstExprNode))
                    throw new Exception("expected expression node");
                
                mTerms.Add(expr as AstExprNode);
                n++;
            }
        }

        public override void Output(TextWriter writer, int nIndent)
        {
            if (HasComment())
                writer.WriteLine(IndentedString(nIndent, " // " + GetComment()));
            string s = "define " + mName;
            if (mType != null)
            {
                s += " : " + mType.ToString();
            }
            if (mParams.Count > 0)
            {
                s += " // ( ";
                foreach (AstParamNode p in mParams)
                    s += p.ToString() + " ";
                s += ")";
            }
            writer.WriteLine(IndentedString(nIndent, s));            
            writer.WriteLine(IndentedString(nIndent, "{"));
            foreach (AstExprNode x in mTerms)
                x.Output(writer, nIndent + 1);
            writer.WriteLine(IndentedString(nIndent, "}"));
        }
    }

    public class AstNameNode : AstExprNode
    {
        public AstNameNode(PegAstNode node) : base(node)
        {
            CheckLabel("name");
            CheckIsLeaf(node);
        }        
        
        public AstNameNode(string sOp, string sComment)
            : base("name", sOp)
        {
            SetComment(sComment);
        }
    }

    public class AstParamNode : CatAstNode
    {
        public AstParamNode(PegAstNode node) : base(node)
        {
            CheckLabel("param");
            CheckIsLeaf(node);
        }

        public override void Output(TextWriter writer, int nIndent)
        {
            throw new Exception("The method or operation is not implemented.");
        }
    }

    public class AstQuoteNode : AstExprNode
    {
        public List<AstExprNode> Terms = new List<AstExprNode>();

        public AstQuoteNode(PegAstNode node) : base(node)
        {
            CheckLabel("quote");
            foreach (PegAstNode child in node.GetChildren())
            {
                CatAstNode tmp = CatAstNode.Create(child);
                if (!(tmp is AstExprNode))
                    throw new Exception("invalid child node " + child.ToString() + ", expected an expression node");
                Terms.Add(tmp as AstExprNode);
            }
        }

        public AstQuoteNode(AstExprNode expr)
            : base("quote", "")
        {
            Terms.Add(expr);
        }

        public override string ToString()
        {
            string result = "[ ";
            foreach (AstExprNode x in Terms)
                result += x.ToString() + " ";
            result += "]";
            return result;
        }

        public override void Output(TextWriter writer, int nIndent)
        {
            if (HasComment())
                writer.WriteLine(IndentedString(nIndent, " // " + GetComment()));
            writer.WriteLine(IndentedString(nIndent, "["));
            foreach (AstExprNode x in Terms)
                x.Output(writer, nIndent + 1);
            writer.WriteLine(IndentedString(nIndent, "]"));
        }
    }

    public class AstIntNode : AstExprNode
    {
        public AstIntNode(PegAstNode node) : base(node)
        {
            CheckLabel("int");
            CheckIsLeaf(node);
        }

        public AstIntNode(int n)
            : base("int", n.ToString())
        { }

        public int GetValue()
        {
            return int.Parse(ToString());
        }
    }

    public class AstBinNode : AstExprNode
    {
        public AstBinNode(PegAstNode node)
            : base(node)
        {
            CheckLabel("bin");
            CheckIsLeaf(node);
        }

        public int GetValue()
        {
            string s = ToString();
            int n = 0;
            int place = 1;
            for (int i = s.Length; i > 0; --i)
            {
                if (s[i - 1] == '1')
                {
                    n += place;
                }
                else
                {
                    if (s[i - 1] != '0')
                        throw new Exception("Invalid binary number");
                }
                place *= 2;
            }
            return n;
        }
    }

    public class AstHexNode : AstExprNode
    {
        public AstHexNode(PegAstNode node)
            : base(node)
        {
            CheckLabel("hex");
            CheckIsLeaf(node);
        }

        public int GetValue()
        {
            return int.Parse(ToString(), NumberStyles.AllowHexSpecifier);
        }
    }

    public class AstCharNode : AstExprNode
    {
        public AstCharNode(PegAstNode node) : base(node)
        {
            CheckLabel("char");
            CheckIsLeaf(node);
        }

        public char GetValue()
        {
            return char.Parse(ToString());
        }
    }

    public class AstStringNode : AstExprNode
    {
        public AstStringNode(PegAstNode node) : base(node)
        {
            CheckLabel("string");
            CheckIsLeaf(node);
        }

        public string GetValue()
        {            
            string s = ToString();
            // strip quotes
            return s.Substring(1, s.Length - 2);
        }
    }

    public class AstFloatNode : AstExprNode
    {
        public AstFloatNode(PegAstNode node) : base(node)
        {
            CheckLabel("float");
            CheckIsLeaf(node);
        }

        public double GetValue()
        {
            return double.Parse(ToString());
        }
    }

    public class AstTypeNode : CatAstNode
    {
        public AstTypeNode(PegAstNode node)
            : base(node)
        {
        }
    }

    public class AstStackNode : CatAstNode
    {
        public List<AstTypeNode> mTypes = new List<AstTypeNode>();

        public AstStackNode(PegAstNode node)
            : base(node)
        {
            CheckLabel("stack");
            foreach (PegAstNode child in node.GetChildren())
            {
                CatAstNode tmp = Create(child);
                if (!(tmp is AstTypeNode))
                    throw new Exception("stack AST node should only have type AST nodes as children");
                mTypes.Add(tmp as AstTypeNode);
            }
        }

        public override string ToString()
        {
            string result = "";
            foreach (AstTypeNode x in mTypes)
                result += x.ToString() + " ";
            return result;
        }
    }

    public class AstTypeVarNode : AstTypeNode
    {
        public AstTypeVarNode(PegAstNode node)
            : base(node)
        {
            CheckLabel("type_var");
            CheckIsLeaf(node);
        }
    }

    public class AstSimpleTypeNode : AstTypeNode
    {
        public AstSimpleTypeNode(PegAstNode node)
            : base(node)
        {
            CheckLabel("type_name");
            CheckIsLeaf(node);
        }
    }

    public class AstStackVarNode : AstTypeNode
    {
        public AstStackVarNode(PegAstNode node)
            : base(node)
        {
            CheckLabel("stack_var");
            CheckIsLeaf(node);
        }
    }

    public class AstFxnTypeNode : AstTypeNode
    {
        public AstStackNode mProd;
        public AstStackNode mCons;

        public AstFxnTypeNode(PegAstNode node)
            : base(node)
        {
            CheckLabel("type_fxn");
            CheckChildCount(node, 2);
            mCons = new AstStackNode(node.GetChild(0));
            mProd = new AstStackNode(node.GetChild(1));
        }

        public override string ToString()
        {
            return "( " + mCons.ToString() + "-> " + mProd.ToString() + ")";
        }
    }

    public class AstMacroNode : CatAstNode
    {
        public AstMacroPattern mSrc;
        public AstMacroPattern mDest;

        public AstMacroNode(PegAstNode node)
            : base(node)
        {
            CheckChildCount(node, 2);
            CheckLabel("macro");
            mSrc = new AstMacroPattern(node.GetChild(0));
            mDest = new AstMacroPattern(node.GetChild(1));
        }

        public override string ToString()
        {
            return "{" + mSrc.ToString() + "} => {" + mDest.ToString() + "}";
        }
    }

    public class AstMacroPattern : CatAstNode
    {
        public List<AstMacroToken> mPattern = new List<AstMacroToken>();

        public AstMacroPattern(PegAstNode node)
            : base(node)
        {
            CheckLabel("macro_pattern");
            foreach (PegAstNode child in node.GetChildren())
            {
                AstMacroToken tmp = CatAstNode.Create(child) as AstMacroToken;
                if (tmp == null)
                    throw new Exception("invalid grammar: only macro terms can be children of an ast macro mPattern");
                mPattern.Add(tmp);
            }
        }

        public override string ToString()
        {
            string ret = "";
            foreach (AstMacroToken t in mPattern)
                ret += " " + t.ToString();
            ret = ret.Substring(1);
            return ret;
        }
    }

    public class AstMacroToken : CatAstNode
    {
        public AstMacroToken(PegAstNode node)
            : base(node)
        {
            CheckChildCount(node, 0);
        }
    }

    public class AstMacroTypeVar : AstMacroToken
    {
        public AstMacroTypeVar(PegAstNode node)
            : base(node)
        {
            CheckLabel("macro_type_var");
        }
    }

    public class AstMacroStackVar : AstMacroToken
    {
        public AstMacroStackVar(PegAstNode node)
            : base(node)
        {
            CheckLabel("macro_stack_var");
        }
    }

    public class AstMacroName : AstMacroToken
    {
        public AstMacroName(PegAstNode node)
            : base(node)
        {
            CheckLabel("macro_name");
        }
    }
}