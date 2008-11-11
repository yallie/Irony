﻿#region License
/* **********************************************************************************
 * Copyright (c) Roman Ivantsov
 * This source code is subject to terms and conditions of the MIT License
 * for Irony. A copy of the license can be found in the License.txt file
 * at the root of this distribution. 
 * By using this source code in any fashion, you are agreeing to be bound by the terms of the 
 * MIT License.
 * You must not remove this notice from this software.
 * **********************************************************************************/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Irony.Compiler {

  public class TerminalLookupTable : Dictionary<char, TerminalList> { }

  public class ScannerControlData {
    public readonly Grammar Grammar;
    public readonly TerminalLookupTable TerminalsLookup = new TerminalLookupTable(); //hash table for fast terminal lookup by input char
    public readonly TerminalList FallbackTerminals = new TerminalList(); //terminals that have no explicit prefixes
    public readonly string ScannerRecoverySymbols = "";
    public readonly char[] LineTerminators; //used for line counting

    public ScannerControlData(Grammar grammar) {
      Grammar = grammar;
      if (!Grammar.Initialized)
        Grammar.Init();
      LineTerminators = grammar.LineTerminators.ToCharArray();
      ScannerRecoverySymbols = grammar.WhitespaceChars + grammar.Delimiters;
      BuildTerminalsLookupTable();
    }

    private void BuildTerminalsLookupTable() {
      TerminalsLookup.Clear();
      FallbackTerminals.AddRange(Grammar.FallbackTerminals);
      foreach (Terminal term in Grammar.Terminals) {
        IList<string> prefixes = term.GetFirsts();
        if (prefixes == null || prefixes.Count == 0) {
          if (!FallbackTerminals.Contains(term))
            FallbackTerminals.Add(term);
          continue; //foreach term
        }
        //Go through prefixes one-by-one
        foreach (string prefix in prefixes) {
          if (string.IsNullOrEmpty(prefix)) continue;
          //Calculate hash key for the prefix
          char hashKey = prefix[0];
          if (!Grammar.CaseSensitive)
            hashKey = char.ToLower(hashKey);
          TerminalList currentList;
          if (!TerminalsLookup.TryGetValue(hashKey, out currentList)) {
            //if list does not exist yet, create it
            currentList = new TerminalList();
            TerminalsLookup[hashKey] = currentList;
          }
          //add terminal to the list
          currentList.Add(term);
        }
      }//foreach term
      //Sort all terminal lists by reverse priority, so that terminal with higher priority comes first in the list
      foreach (TerminalList list in TerminalsLookup.Values)
        if (list.Count > 1)
          list.Sort(Terminal.ByPriorityReverse);
    }//method


  }


}
