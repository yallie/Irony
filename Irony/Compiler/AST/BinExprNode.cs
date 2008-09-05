﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Irony.Runtime;

namespace Irony.Compiler.AST {
  public class BinExprNode : AstNode {
    public AstNode Left;
    public string Op;
    public AstNode Right;
    CallDispatcher _dispatcher;


    public BinExprNode(NodeArgs args, AstNode left, string op, AstNode right) : base(args) {
      ChildNodes.Clear();
      Left = left;
      AddChild("Arg", Left);
      Op = op; 
      Right = right;
      AddChild("Arg", Right);
      //Flags |= AstNodeFlags.TypeBasedDispatch;
    }
    public BinExprNode(NodeArgs args) 
      : this(args, args.ChildNodes[0], args.ChildNodes[1].GetContent(), args.ChildNodes[2]) {  }

    public override void OnCodeAnalysis(CodeAnalysisArgs args) {
      switch (args.Phase) {
        case CodeAnalysisPhase.Binding:
          _dispatcher = args.Context.Runtime.GetDispatcher(Op);
          if (_dispatcher == null)
            args.Context.ReportError(this.Location, "Operator " + Op + " not defined.");
          break;
      }//switch
      base.OnCodeAnalysis(args);
    }

    protected override void DoEvaluate(EvaluationContext context) {
      Left.Evaluate(context);
      object arg1 = context.CurrentResult;
      Right.Evaluate(context);
      context.Arg2 = context.CurrentResult;
      context.Arg1 = arg1; 
      _dispatcher.Evaluate(context);
    }

    public override string ToString() {
      return Op + " (operator)";
    }
  }//class
}//namespace