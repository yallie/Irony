using System;
using System.Collections.Generic;
using System.Text;
using Irony.Compiler;

namespace Irony.Samples.CSharp {

  //Full c# 3.0 grammar; all but 2 features are not implemented:
  //  - preprocessor directives (currently treated as comment lines)
  //  - LINQ query expressions.
  public class CSharpGrammar : Grammar {

    public CSharpGrammar() {

      #region current conflicts
/*  
      Shift-reduce conflict in state S3, reduce production: extern_alias_directives_opt -> extern_alias_directive+  on inputs: extern 
      Shift-reduce conflict in state S83, reduce production: extern_alias_directives_opt ->  on inputs: extern 
           - the cause is double-use of "extern" keyword - in "extern alias someName;" and as modifier of class members
             prefering shift is a default and correct behavior
      Shift-reduce conflict in state S541, reduce production: else_clause_opt ->  on inputs: else 
           - "dangling ELSE conflict" well described in textbooks; preferring shift is a correct behavior
*/
      #endregion

      #region Lexical structure
      StringLiteral StringLiteral = TerminalFactory.CreateCSharpString("StringLiteral");
      StringLiteral CharLiteral = TerminalFactory.CreateCSharpChar("CharLiteral");
      NumberLiteral Number = TerminalFactory.CreateCSharpNumber("Number");
      IdentifierTerminal identifier = TerminalFactory.CreateCSharpIdentifier("Identifier");

      CommentTerminal SingleLineComment = new CommentTerminal("SingleLineComment", "//", "\r", "\n", "\u2085", "\u2028", "\u2029");
      CommentTerminal DelimitedComment = new CommentTerminal("DelimitedComment", "/*", "*/");
      ExtraTerminals.Add(SingleLineComment);
      ExtraTerminals.Add(DelimitedComment);
      //Temporarily, treat preprocessor instructions like comments
      CommentTerminal ppInstruction = new CommentTerminal("ppInstruction", "#", "\n");
      ExtraTerminals.Add(ppInstruction);

      //Symbols
      SymbolTerminal colon = Symbol(":", "colon");
      SymbolTerminal semi = Symbol(";", "semi");
      NonTerminal semi_opt = new NonTerminal("semi?");
      semi_opt.Rule = Empty | semi;
      SymbolTerminal dot = Symbol(".", "dot");
      SymbolTerminal comma = Symbol(",", "comma");
      NonTerminal comma_opt = new NonTerminal("comma_opt", Empty | comma);
      NonTerminal commas_opt = new NonTerminal("commas_opt");
      commas_opt.Rule = MakeStarRule(commas_opt, null, comma);
      SymbolTerminal qmark = Symbol("?", "qmark");
      NonTerminal qmark_opt = new NonTerminal("qmark_opt", Empty | qmark);
      SymbolTerminal Lbr = Symbol("{");
      SymbolTerminal Rbr = Symbol("}");
      SymbolTerminal Lpar = Symbol("(");
      SymbolTerminal Rpar = Symbol(")");
      SymbolTerminal tgoto = Symbol("goto");
      SymbolTerminal yld = Symbol("yield");

      SymbolTerminal Lparx = Symbol("(*");


      #region operators, punctuation and delimiters 
      RegisterOperators(1, "||");
      RegisterOperators(2, "&&");
      RegisterOperators(3, "|");
      RegisterOperators(4, "^");
      RegisterOperators(5, "&");
      RegisterOperators(6, "==", "!=");
      RegisterOperators(7, "<", ">", "<=", ">=", "is", "as");
      RegisterOperators(8, "<<", ">>");
      RegisterOperators(9, "+", "-");
      RegisterOperators(10, "*", "/", "%");
      RegisterOperators(11, ".");
      // RegisterOperators(12, "++", "--");
      #region comments
      //The following makes sense, if you think about "?" in context of operator precedence. 
      // What we say here is that "?" has the lowest priority among arithm operators.
      // Therefore, the parser should prefer reduce over shift when input symbol is "?".
      // For ex., when seeing ? in expression "a + b?...", the parser will perform Reduce:
      //  (a + b)->expr
      // and not shift the "?" symbol.  
      // Same goes for ?? symbol
      #endregion
      RegisterOperators(-3, "=", "+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=");
      RegisterOperators(-2, "?");
      RegisterOperators(-1, "??");

      this.Delimiters = "{}[](),:;+-*/%&|^!~<>=";
      this.RegisterPunctuation(";", ",", "(", ")", "{", "}", "[", "]", ":");
      //Whitespace and NewLine characters
      //TODO: 
      // 1. In addition to "normal" whitespace chars, the spec mentions "any char of unicode class Z" -
      //   need to create special comment-based terminal that simply eats these category-based whitechars and produces comment token. 
      // 2. Add support for multiple line terminators to LineComment
      this.LineTerminators = "\r\n\u2085\u2028\u2029"; //CR, linefeed, nextLine, LineSeparator, paragraphSeparator
      this.WhitespaceChars = " \t\r\n\v\u2085\u2028\u2029"; //add extra line terminators
      #endregion


      #endregion

      #region NonTerminals
      //B.2.1. Basic concepts
      NonTerminal qual_name_with_targs = new NonTerminal("qual_name_with_targs");
      NonTerminal generic_dimension_specifier = new NonTerminal("generic_dimension_specifier");
      NonTerminal qual_name_segment = new NonTerminal("qual_name_segment");
      NonTerminal qual_name_segments_opt = new NonTerminal("qual_name_segments_opt");
      NonTerminal type_or_void = new NonTerminal("type_or_void");
      NonTerminal builtin_type = new NonTerminal("builtin_type");
      NonTerminal type_ref_list = new NonTerminal("type_ref_list");
      NonTerminal identifier_ext = new NonTerminal("identifier_ext");
      NonTerminal identifier_or_builtin = new NonTerminal("identifier_or_builtin");

      //B.2.2. Types
      NonTerminal type_ref = new NonTerminal("type_ref");
      NonTerminal new_type_ref = new NonTerminal("new_type_ref");
      NonTerminal type_argument_list = new NonTerminal("type_argument_list");
      NonTerminal type_argument_list_opt = new NonTerminal("type_argument_list_opt");
      NonTerminal integral_type = new NonTerminal("integral_type");

      //B.2.4. Expressions
      NonTerminal argument = new NonTerminal("argument");
      NonTerminal argument_list = new NonTerminal("argument_list");
      NonTerminal argument_list_opt = new NonTerminal("argument_list_opt");
      NonTerminal expression = new NonTerminal("expression");
      NonTerminal expression_list = new NonTerminal("expression_list");
      NonTerminal expression_opt = new NonTerminal("expression_opt");
      NonTerminal conditional_expression = new NonTerminal("conditional_expression");
      NonTerminal lambda_expression = new NonTerminal("lambda_expression");
      NonTerminal query_expression = new NonTerminal("query_expression");
      NonTerminal unary_operator = new NonTerminal("unary_operator");
      NonTerminal assignment_operator = new NonTerminal("assignment_operator");
      NonTerminal primary_expression = new NonTerminal("primary_expression");
      NonTerminal pre_incr_decr_expression = new NonTerminal("pre_incr_decr_expression");
      NonTerminal post_incr_decr_expression = new NonTerminal("post_incr_decr_expression");
      NonTerminal primary_no_array_creation_expression = new NonTerminal("primary_no_array_creation_expression");
      NonTerminal literal = new NonTerminal("literal");
      NonTerminal parenthesized_expression = new NonTerminal("parenthesized_expression");
      NonTerminal member_access = new NonTerminal("member_access");
      NonTerminal member_access_segment = new NonTerminal("member_access_segment");
      NonTerminal member_access_segments_opt = new NonTerminal("member_access_segments_opt");
      NonTerminal array_indexer = new NonTerminal("array_indexer");
      NonTerminal argument_list_par = new NonTerminal("argument_list_par");
      NonTerminal argument_list_par_opt = new NonTerminal("argument_list_par_opt");
      NonTerminal incr_or_decr = new NonTerminal("incr_or_decr");
      NonTerminal incr_or_decr_opt = new NonTerminal("incr_or_decr_opt");
      NonTerminal creation_args = new NonTerminal("creation_args");
      NonTerminal object_creation_expression = new NonTerminal("object_creation_expression");
      // delegate creation is syntactically equiv to object creation
      //NonTerminal delegate_creation_expression = new NonTerminal("delegate_creation_expression");
      NonTerminal anonymous_object_creation_expression = new NonTerminal("anonymous_object_creation_expression");
      NonTerminal typeof_expression = new NonTerminal("typeof_expression");
      NonTerminal checked_expression = new NonTerminal("checked_expression");
      NonTerminal unchecked_expression = new NonTerminal("unchecked_expression");
      NonTerminal default_value_expression = new NonTerminal("default_value_expression");
      NonTerminal anonymous_method_expression = new NonTerminal("anonymous_method_expression");

      NonTerminal elem_initializer = new NonTerminal("elem_initializer");
      NonTerminal elem_initializer_list = new NonTerminal("elem_initializer_list");
      NonTerminal elem_initializer_list_ext = new NonTerminal("elem_initializer_list_ext");
      NonTerminal initializer_value = new NonTerminal("initializer_value");

      NonTerminal anonymous_object_initializer = new NonTerminal("anonymous_object_initializer");
      NonTerminal member_declarator = new NonTerminal("member_declarator");
      NonTerminal member_declarator_list = new NonTerminal("member_declarator_list");
      NonTerminal unbound_type_name = new NonTerminal("unbound_type_name");
      NonTerminal generic_dimension_specifier_opt = new NonTerminal("generic_dimension_specifier_opt");
      NonTerminal anonymous_function_signature = new NonTerminal("anonymous_function_signature");
      NonTerminal anonymous_function_signature_opt = new NonTerminal("anonymous_function_signature_opt");
      NonTerminal anonymous_function_parameter = new NonTerminal("anonymous_function_parameter");
      NonTerminal anonymous_function_parameter_decl = new NonTerminal("anonymous_function_parameter_decl");
      NonTerminal anonymous_function_parameter_list_opt = new NonTerminal("anonymous_function_parameter_list_opt");
      NonTerminal anonymous_function_parameter_modifier_opt = new NonTerminal("anonymous_function_parameter_modifier_opt");
      NonTerminal anonymous_function_body = new NonTerminal("anonymous_function_body");
      NonTerminal lambda_function_signature = new NonTerminal("lambda_function_signature");
      NonTerminal bin_op_expression = new NonTerminal("bin_op_expression");
      NonTerminal typecast_expression = new NonTerminal("typecast_expression");
      NonTerminal bin_op = new NonTerminal("bin_op");

      //B.2.5. Statements
      NonTerminal statement = new NonTerminal("statement");
      NonTerminal statement_list = new NonTerminal("statement_list");
      NonTerminal statement_list_opt = new NonTerminal("statement_list_opt");
      NonTerminal labeled_statement = new NonTerminal("labeled_statement");
      NonTerminal declaration_statement = new NonTerminal("declaration_statement");
      NonTerminal embedded_statement = new NonTerminal("embedded_statement");
      NonTerminal selection_statement = new NonTerminal("selection_statement");
      NonTerminal iteration_statement = new NonTerminal("iteration_statement");
      NonTerminal jump_statement = new NonTerminal("jump_statement");
      NonTerminal try_statement = new NonTerminal("try_statement");
      NonTerminal checked_statement = new NonTerminal("checked_statement");
      NonTerminal unchecked_statement = new NonTerminal("unchecked_statement");
      NonTerminal lock_statement = new NonTerminal("lock_statement");
      NonTerminal using_statement = new NonTerminal("using_statement");
      NonTerminal yield_statement = new NonTerminal("yield_statement");
      NonTerminal block = new NonTerminal("block");
      NonTerminal statement_expression = new NonTerminal("statement_expression");
      NonTerminal statement_expression_list = new NonTerminal("statement_expression_list");
      NonTerminal local_variable_declaration = new NonTerminal("local_variable_declaration");
      NonTerminal local_constant_declaration = new NonTerminal("local_constant_declaration");
      NonTerminal local_variable_type = new NonTerminal("local_variable_type");
      NonTerminal local_variable_declarator = new NonTerminal("local_variable_declarator");
      NonTerminal local_variable_declarators = new NonTerminal("local_variable_declarators");
      NonTerminal local_variable_initializer = new NonTerminal("local_variable_initializer");
      NonTerminal if_statement = new NonTerminal("if_statement");
      NonTerminal switch_statement = new NonTerminal("switch_statement");
      NonTerminal else_clause_opt = new NonTerminal("else_clause_opt");
      NonTerminal switch_section = new NonTerminal("switch_section");
      NonTerminal switch_sections_opt = new NonTerminal("switch_sections_opt");
      NonTerminal switch_label = new NonTerminal("switch_label");
      NonTerminal switch_labels = new NonTerminal("switch_labels");
      NonTerminal while_statement = new NonTerminal("while_statement");
      NonTerminal do_statement = new NonTerminal("do_statement");
      NonTerminal for_statement = new NonTerminal("for_statement");
      NonTerminal foreach_statement = new NonTerminal("foreach_statement");
      NonTerminal for_initializer_opt = new NonTerminal("for_initializer_opt");
      NonTerminal for_condition_opt = new NonTerminal("for_condition_opt");
      NonTerminal for_iterator_opt = new NonTerminal("for_iterator_opt");
      NonTerminal break_statement = new NonTerminal("break_statement");
      NonTerminal continue_statement = new NonTerminal("continue_statement");
      NonTerminal goto_statement = new NonTerminal("goto_statement");
      NonTerminal return_statement = new NonTerminal("return_statement");
      NonTerminal throw_statement = new NonTerminal("throw_statement");
      NonTerminal try_clause = new NonTerminal("try_clause");
      NonTerminal try_clauses = new NonTerminal("try_clauses");

      NonTerminal catch_clause = new NonTerminal("catch_clause");
      NonTerminal finally_clause = new NonTerminal("finally_clause");
      NonTerminal catch_specifier_opt = new NonTerminal("catch_specifier_opt");
      NonTerminal identifier_opt = new NonTerminal("identifier_opt");

      NonTerminal resource_acquisition = new NonTerminal("resource_acquisition");

      //namespaces, compilation units
      NonTerminal compilation_unit = new NonTerminal("compilation_unit");
      NonTerminal extern_alias_directive = new NonTerminal("extern_alias_directive");
      NonTerminal extern_alias_directives_opt = new NonTerminal("extern_alias_directives_opt");
      NonTerminal using_directive = new NonTerminal("using_directive");
      NonTerminal using_directives = new NonTerminal("using_directives");
      NonTerminal using_directives_opt = new NonTerminal("using_directives_opt");
      NonTerminal namespace_declaration = new NonTerminal("namespace_declaration");
      NonTerminal namespace_declarations_opt = new NonTerminal("namespace_declarations_opt");
      NonTerminal qualified_identifier = new NonTerminal("qualified_identifier");
      NonTerminal namespace_body = new NonTerminal("namespace_body");
      NonTerminal namespace_member_declaration = new NonTerminal("namespace_member_declaration");
      NonTerminal namespace_member_declarations = new NonTerminal("namespace_member_declarations");
      NonTerminal using_alias_directive = new NonTerminal("using_alias_directive");
      NonTerminal using_ns_directive = new NonTerminal("using_ns_directive");
      NonTerminal type_declaration = new NonTerminal("type_declaration");
      NonTerminal class_declaration = new NonTerminal("class_declaration");
      NonTerminal delegate_declaration = new NonTerminal("delegate_declaration");
      NonTerminal qualified_alias_member = new NonTerminal("qualified_alias_member");
      NonTerminal class_body = new NonTerminal("class_body");

      //B.2.7 Classes
      Terminal partial = Symbol("partial");
      NonTerminal type_parameter_list_opt = new NonTerminal("type_parameter_list_opt");
      NonTerminal type_parameter = new NonTerminal("type_parameter");
      NonTerminal type_parameters = new NonTerminal("type_parameters");
      NonTerminal bases_opt = new NonTerminal("bases_opt");
      NonTerminal type_parameter_constraints_clause = new NonTerminal("type_parameter_constraints_clause");
      NonTerminal type_parameter_constraints_clauses_opt = new NonTerminal("type_parameter_constraints_clauses");
      NonTerminal type_parameter_constraint = new NonTerminal("type_parameter_constraint");
      NonTerminal type_parameter_constraints = new NonTerminal("type_parameter_constraints");
      NonTerminal member_declaration = new NonTerminal("member_declaration");
      NonTerminal member_declarations_opt = new NonTerminal("member_declarations_opt");
      NonTerminal constant_declaration = new NonTerminal("constant_declaration");
      NonTerminal field_declaration = new NonTerminal("field_declaration");
      NonTerminal method_declaration = new NonTerminal("method_declaration");
      NonTerminal property_declaration = new NonTerminal("property_declaration");
      NonTerminal event_declaration = new NonTerminal("event_declaration");
      NonTerminal indexer_declaration = new NonTerminal("indexer_declaration");
      NonTerminal constructor_declaration = new NonTerminal("constructor_declaration");
      NonTerminal destructor_declaration = new NonTerminal("destructor_declaration");
      NonTerminal constant_declarator = new NonTerminal("constant_declarator");
      NonTerminal constant_declarators = new NonTerminal("constant_declarators");
      NonTerminal modifier = new NonTerminal("modifier");
      NonTerminal modifiers_opt = new NonTerminal("modifiers_opt");
      NonTerminal member_header = new NonTerminal("member_header");
      NonTerminal accessor_name = new NonTerminal("accessor_name");
      NonTerminal accessor_declaration = new NonTerminal("accessor_declaration");
      NonTerminal accessor_declarations = new NonTerminal("accessor_declarations");
      NonTerminal accessor_modifier_opt = new NonTerminal("accessor_modifier_opt");
      NonTerminal event_body = new NonTerminal("event_body");
      NonTerminal event_accessor_declarations = new NonTerminal("event_accessor_declarations");
      NonTerminal add_accessor_declaration = new NonTerminal("add_accessor_declaration");
      NonTerminal remove_accessor_declaration = new NonTerminal("remove_accessor_declaration");
      NonTerminal indexer_name = new NonTerminal("indexer_name");
      NonTerminal operator_declaration = new NonTerminal("operator_declaration");
      NonTerminal conversion_operator_declaration = new NonTerminal("conversion_operator_declaration");
      NonTerminal overloadable_operator = new NonTerminal("overloadable_operator");
      NonTerminal operator_parameter = new NonTerminal("operator_parameter");
      NonTerminal operator_parameters = new NonTerminal("operator_parameters");
      NonTerminal conversion_operator_kind = new NonTerminal("conversion_operator_kind");
      NonTerminal constructor_initializer_opt = new NonTerminal("constructor_initializer_opt");
      NonTerminal constructor_base = new NonTerminal("constructor_base");
      NonTerminal variable_declarator = new NonTerminal("variable_declarator");
      NonTerminal variable_declarators = new NonTerminal("variable_declarators");
      NonTerminal method_body = new NonTerminal("method_body");
      NonTerminal formal_parameter_list = new NonTerminal("formal_parameter_list");
      NonTerminal formal_parameter_list_par = new NonTerminal("formal_parameter_list_par");
      NonTerminal fixed_parameter = new NonTerminal("fixed_parameter");
      NonTerminal fixed_parameters = new NonTerminal("fixed_parameters");
      NonTerminal parameter_modifier_opt = new NonTerminal("parameter_modifier_opt");
      NonTerminal parameter_array = new NonTerminal("parameter_array");
      
      //B.2.8 struct
      NonTerminal struct_declaration = new NonTerminal("struct_declaration");
      NonTerminal struct_body = new NonTerminal("struct_body");

      //B.2.9. Arrays
      NonTerminal rank_specifier = new NonTerminal("rank_specifier");
      NonTerminal rank_specifiers = new NonTerminal("rank_specifiers");
      NonTerminal rank_specifiers_opt = new NonTerminal("rank_specifiers_opt");
      NonTerminal dim_specifier = new NonTerminal("dim_specifier");
      NonTerminal dim_specifier_opt = new NonTerminal("dim_specifier_opt");
      NonTerminal list_initializer = new NonTerminal("array_initializer");
      NonTerminal list_initializer_opt = new NonTerminal("array_initializer_opt");


      //B.2.10 Interfaces
      NonTerminal interface_declaration = new NonTerminal("interface_declaration");
      NonTerminal interface_body = new NonTerminal("interface_body");
      NonTerminal interface_member_declaration = new NonTerminal("interface_member_declaration");
      NonTerminal interface_member_declarations = new NonTerminal("interface_member_declarations");
      NonTerminal interface_method_declaration = new NonTerminal("interface_method_declaration");
      NonTerminal interface_property_declaration = new NonTerminal("interface_property_declaration");
      NonTerminal interface_event_declaration = new NonTerminal("interface_event_declaration");
      NonTerminal interface_indexer_declaration = new NonTerminal("interface_indexer_declaration");
      NonTerminal new_opt = new NonTerminal("new_opt"); 
      NonTerminal interface_accessor = new NonTerminal("interface_get_accessor");
      NonTerminal interface_accessors = new NonTerminal("interface_accessors");

      //B.2.11 Enums
      NonTerminal enum_declaration = new NonTerminal("enum_declaration");
      NonTerminal enum_base_opt = new NonTerminal("enum_base_opt");
      NonTerminal enum_body = new NonTerminal("enum_body");
      NonTerminal enum_member_declaration = new NonTerminal("enum_member_declaration");
      NonTerminal enum_member_declarations = enum_member_declaration.Plus("enum_member_declarations", comma);

      //B.2.13 Attributes
      NonTerminal attribute_section = new NonTerminal("attribute_section");
      NonTerminal attributes_opt = new NonTerminal("attributes_opt"); 
      NonTerminal attribute_target_specifier_opt = new NonTerminal("attribute_target_specifier_opt");
      NonTerminal attribute_target = new NonTerminal("attribute_target");
      NonTerminal attribute = new NonTerminal("attribute");
      NonTerminal attribute_list = new NonTerminal("attribute_list");
      NonTerminal attribute_arguments_opt = new NonTerminal("attribute_arguments");
      NonTerminal named_argument = new NonTerminal("named_argument");
      NonTerminal attr_arg = new NonTerminal("attr_arg");
      NonTerminal attribute_arguments_par_opt = new NonTerminal("attribute_arguments_par_opt");

      
      #endregion

      // RULES


      //B.2.1. Basic concepts
      //qual_name_with_targs is an alias for namespace-name, namespace-or-type-name, type-name,

      generic_dimension_specifier.Rule = "<" + commas_opt + ">";
      qual_name_segments_opt.Rule = MakeStarRule(qual_name_segments_opt, null, qual_name_segment);
      identifier_or_builtin.Rule = identifier | builtin_type;
      identifier_ext.Rule = identifier_or_builtin | "this" | "base"; 
      qual_name_segment.Rule = dot + identifier
                              | "::" + identifier
                              | type_argument_list;
      generic_dimension_specifier.Rule = "<" + commas_opt + ">";
      qual_name_with_targs.Rule = identifier_or_builtin + qual_name_segments_opt;

      type_argument_list.Rule = "<" + type_ref_list + ">";
      type_argument_list_opt.Rule = Empty | type_argument_list; 

      //B.2.2. Types
      type_or_void.Rule = qual_name_with_targs | "void";
      builtin_type.Rule = integral_type | "bool" | "decimal" | "float" | "double" | "string" | "object";

      type_ref.Rule = type_or_void + qmark_opt + rank_specifiers_opt + generic_dimension_specifier_opt;
      type_ref_list.Rule = MakePlusRule(type_ref_list, comma, type_ref); 

      rank_specifier.Rule = "[" + comma.Star() + "]";
      rank_specifiers.Rule = MakePlusRule(rank_specifiers, null, rank_specifier);
      rank_specifiers_opt.Rule = rank_specifiers.Q();
      integral_type.Rule = Symbol("sbyte") | "byte" | "short" | "ushort" | "int" | "uint" | "long" | "ulong" | "char";

      //B.2.4. Variables
      //Quite strange in specs -
      //  variable-reference: 
      //     expression
      // Is that case it would be possible to do the following:    
      //                 GetMyStuff(out (a+b));  
      //  but MS c# rejects it   

      //B.2.4. Expressions
      argument.Rule = expression | "ref" + identifier | "out" + identifier;
      argument_list.Rule =  MakePlusRule(argument_list, comma, argument);
      argument_list_opt.Rule = Empty | argument_list;
      expression.Rule =  conditional_expression 
                    | bin_op_expression
                    | typecast_expression
                    | primary_expression;
      expression_opt.Rule = Empty | expression;
      expression_list.Rule = MakePlusRule(expression_list, comma, expression);
      unary_operator.Rule = Symbol("+") | "-" | "!" | "~" | "*";
      assignment_operator.Rule = Symbol("=") | "+=" | "-=" | "*=" | "/=" | "%=" | "&=" | "|=" | "^=" | "<<=" | ">>=";
      conditional_expression.Rule = expression + qmark + expression + colon + expression;
      bin_op_expression.Rule = expression + bin_op + expression;

      typecast_expression.Rule = parenthesized_expression + primary_expression; 
      primary_expression.Rule =
        literal
        | unary_operator + primary_expression
        | parenthesized_expression
        | member_access 
        | pre_incr_decr_expression 
        | post_incr_decr_expression 
        | object_creation_expression
        | anonymous_object_creation_expression
        | typeof_expression
        | checked_expression
        | unchecked_expression
        | default_value_expression 
        | anonymous_method_expression; 
      dim_specifier.Rule = "[" + expression_list + "]";
      dim_specifier_opt.Rule = dim_specifier.Q();
      literal.Rule = Number | StringLiteral | CharLiteral | "true" | "false" | "null";
      parenthesized_expression.Rule = Lpar + expression + Rpar;
      pre_incr_decr_expression.Rule = incr_or_decr + member_access;
      post_incr_decr_expression.Rule = member_access + incr_or_decr; 
            
      //joined invocation_expr and member_access; for member access left the most general variant
      member_access.Rule = identifier_ext + member_access_segments_opt;
      member_access_segments_opt.Rule = MakeStarRule(member_access_segments_opt, null, member_access_segment);
      member_access_segment.Rule = dot + identifier  
                                 | array_indexer
                                 | argument_list_par
                                 | type_argument_list;
      array_indexer.Rule = "[" + expression_list + "]";

      argument_list_par.Rule  = Lpar + argument_list_opt + Rpar;
      
      argument_list_par_opt.Rule = Empty | argument_list_par;

      list_initializer.Rule = Lbr + elem_initializer_list_ext + Rbr;
      list_initializer_opt.Rule = list_initializer.Q();

      elem_initializer.Rule = initializer_value | identifier + "=" + initializer_value;
      elem_initializer_list.Rule = MakePlusRule(elem_initializer_list, comma, elem_initializer); 
      elem_initializer_list_ext.Rule = Empty | elem_initializer_list + comma_opt;
      initializer_value.Rule = expression | list_initializer;

      //delegate, anon-object, object
      object_creation_expression.Rule = "new" + qual_name_with_targs + qmark_opt + creation_args + list_initializer_opt;
      creation_args.Rule = dim_specifier | rank_specifier | argument_list_par;

      anonymous_object_creation_expression.Rule = "new" + anonymous_object_initializer;
      anonymous_object_initializer.Rule = Lbr + Rbr | Lbr + member_declarator_list + comma_opt + Rbr;
      member_declarator.Rule = expression | identifier + "=" + expression;
      member_declarator_list.Rule = MakePlusRule(member_declarator_list, comma, member_declarator);
      //typeof
      typeof_expression.Rule = "typeof" + Lpar + type_ref + Rpar;
      generic_dimension_specifier_opt.Rule = Empty | "<" + commas_opt + ">";
      //checked, unchecked
      checked_expression.Rule = "checked" + parenthesized_expression;
      unchecked_expression.Rule = "unchecked" + parenthesized_expression;
      //default-value
      default_value_expression.Rule = "default" + Lpar + type_ref + Rpar;
      //note: we treat ?? as bin-operation, so null-coalesce-expr used in spec as first (condition) component is replaced with expression
      // we resolve all this expr hierarchies of binary expressions using precedence

      //anonymous method and lambda - we join explicit and implicit param definitions, making 'type' element optional
      // TODO: add after-parse check for this
      anonymous_method_expression.Rule = "delegate" + anonymous_function_signature_opt + block;
      lambda_expression.Rule = lambda_function_signature + "=>" + anonymous_function_body;
      lambda_function_signature.Rule = anonymous_function_signature | identifier; 
      anonymous_function_signature.Rule = Lpar + anonymous_function_parameter_list_opt + Rpar;
      anonymous_function_signature_opt.Rule = anonymous_function_signature.Q();
      anonymous_function_parameter_modifier_opt.Rule = Empty | "ref" | "out";
      anonymous_function_parameter.Rule = anonymous_function_parameter_modifier_opt + anonymous_function_parameter_decl;
      anonymous_function_parameter_decl.Rule = identifier | type_ref + identifier;
      anonymous_function_parameter_list_opt.Rule = MakeStarRule(anonymous_function_parameter_list_opt, comma, anonymous_function_parameter_decl);
      anonymous_function_body.Rule = expression | block;

      //we don't use grammar expressions to specify operator precedence, so we combine all these grammar elements together
      // and define just bin_op_expression. Where to put it? 
      // In spec:     non_assignment_expression.Rule = conditional_expression | lambda_expression | query_expression;
      //I think it's a mistake; there must be additional entry here for arithm expressions, so we put them here. 
      // We also have to add "is" and "as" expressions here, as we don't build entire hierarchy of elements for expressing
      // precedence (where they appear in original spec); so we put them here 
      bin_op.Rule = Symbol("<")
                  | "||" | "&&" | "|" | "^" | "&" | "==" | "!=" | ">" | "<=" | ">=" | "<<" | ">>" | "+" | "-" | "*" | "/" | "%"
                  | "=" | "+=" | "-=" | "*=" | "/=" | "%=" | "&=" | "|=" | "^=" | "<<=" | ">>="
                  | "is" | "as" | "??";
         
      //type_check_expression.Rule = expression + "is" + type_ref | expression + "as" + type_ref;

      //Queries
      query_expression.Rule = "from";

      //B.2.5. Statements
      statement.Rule = labeled_statement | declaration_statement | embedded_statement;
      statement_list.Rule = MakePlusRule(statement_list, null, statement);
      statement_list_opt.Rule = Empty | statement_list;
      //labeled_statement
      labeled_statement.Rule = identifier + colon + embedded_statement;
      //declaration_statement
      declaration_statement.Rule = local_variable_declaration + semi | local_constant_declaration + semi;
      local_variable_declaration.Rule = local_variable_type + local_variable_declarators; //!!!
      local_variable_type.Rule = member_access | "var"; // | builtin_type; //to fix the conflict, changing to member-access here
      local_variable_declarator.Rule = identifier | identifier + "=" + local_variable_initializer;
      local_variable_declarators.Rule = MakePlusRule(local_variable_declarators, comma, local_variable_declarator);
      local_variable_initializer.Rule = expression | list_initializer;
      local_constant_declaration.Rule = "const" + type_ref + constant_declarators;
      //embedded_statement
      embedded_statement.Rule = block | semi /*empty_statement*/ | statement_expression + semi | selection_statement
                               | iteration_statement | jump_statement | try_statement | checked_statement | unchecked_statement
                               | lock_statement | using_statement | yield_statement;
      block.Rule = Lbr + statement_list_opt + Rbr;
      //selection (if and switch)
      selection_statement.Rule = if_statement | switch_statement;
      if_statement.Rule = Symbol("if") + Lpar + expression + Rpar + embedded_statement + else_clause_opt;
      else_clause_opt.Rule = Empty | "else" + embedded_statement;
      switch_statement.Rule = "switch" + parenthesized_expression + Lbr + switch_sections_opt + Rbr;
      switch_section.Rule = switch_labels + statement_list;
      switch_sections_opt.Rule = MakeStarRule(switch_sections_opt, null, switch_section);
      switch_label.Rule = "case" + expression + colon | "default" + colon;
      switch_labels.Rule = MakePlusRule(switch_labels, null, switch_label);
      //iteration statements
      iteration_statement.Rule = while_statement | do_statement | for_statement | foreach_statement;
      while_statement.Rule = "while" + parenthesized_expression + embedded_statement;
      do_statement.Rule = "do" + embedded_statement + "while" + parenthesized_expression + semi;
      for_statement.Rule = "for" + Lpar + for_initializer_opt + semi + for_condition_opt + semi + for_iterator_opt + Rpar + embedded_statement;
      for_initializer_opt.Rule = Empty | local_variable_declaration | statement_expression_list;
      for_condition_opt.Rule = Empty | expression;
      for_iterator_opt.Rule = Empty | statement_expression_list;
      foreach_statement.Rule = "foreach" + Lpar + local_variable_type  + identifier + "in" + expression + Rpar + embedded_statement;
      //jump-statement
      jump_statement.Rule = break_statement | continue_statement | goto_statement | return_statement | throw_statement;
      break_statement.Rule = "break" + semi;
      continue_statement.Rule = "continue" + semi;
      goto_statement.Rule = tgoto + identifier + semi | tgoto + "case" + expression + semi | tgoto + "default" + semi;
      return_statement.Rule = "return" + expression_opt + semi;
      throw_statement.Rule = "throw" + expression_opt + semi; 
      //try-statement
      //changed to avoid conflicts; need to check correct ordering of catch/finally clause in after-parse validation
      try_statement.Rule = "try" + block + try_clauses;
      try_clause.Rule = catch_clause | finally_clause;
      try_clauses.Rule = MakePlusRule(try_clauses, null, try_clause);
      catch_clause.Rule = "catch" + catch_specifier_opt + block;
      finally_clause.Rule = "finally" + block;
      catch_specifier_opt.Rule = Empty | Lpar + qual_name_with_targs + identifier_opt + Rpar;
      identifier_opt.Rule = Empty | identifier;  
      //checked, unchecked, locked, using
      checked_statement.Rule = "checked" + block;
      unchecked_statement.Rule = "unchecked" + block;
      lock_statement.Rule = "lock" + parenthesized_expression + embedded_statement;
      using_statement.Rule = "using" + Lpar + resource_acquisition + Rpar + embedded_statement;
      resource_acquisition.Rule = local_variable_declaration | expression;
      //yield statement
      yield_statement.Rule = yld + "return" + expression + semi | yld + "break" + semi;

      //expression statement
     // expression_statement.Rule = statement_expression + semi;
      statement_expression.Rule =  object_creation_expression 
                                | member_access |  member_access + assignment_operator + expression
                                | pre_incr_decr_expression | post_incr_decr_expression
                                ;
      statement_expression_list.Rule = MakePlusRule(statement_expression_list, comma, statement_expression);
      incr_or_decr_opt.Rule = Empty | Symbol("++") | "--";
      incr_or_decr.Rule = Symbol("++") | "--";

      //B.2.6. Namespaces
      this.Root = compilation_unit;
      compilation_unit.Rule = extern_alias_directives_opt 
                            + using_directives_opt 
                            + attributes_opt + namespace_declarations_opt; 
      extern_alias_directive.Rule = Symbol("extern") + "alias" + identifier + semi;
      extern_alias_directives_opt.Rule = MakeStarRule(extern_alias_directives_opt, null, extern_alias_directive);
      namespace_declaration.Rule = "namespace" + qualified_identifier + namespace_body + semi_opt;
      namespace_declarations_opt.Rule = MakeStarRule(namespace_declarations_opt, null, namespace_declaration);
      qualified_identifier.Rule = MakePlusRule(qualified_identifier, dot, identifier);

      namespace_body.Rule = "{" + extern_alias_directives_opt + using_directives_opt + namespace_member_declarations + "}";

      using_directive.Rule = using_alias_directive | using_ns_directive;
      using_directives.Rule = MakePlusRule(using_directives, null, using_directive);
      using_directives_opt.Rule = Empty | using_directives; 

      using_alias_directive.Rule = "using" + identifier + "=" + qual_name_with_targs + semi;
      using_ns_directive.Rule = "using" + qual_name_with_targs + semi;
      namespace_member_declaration.Rule = namespace_declaration | type_declaration;
      namespace_member_declarations.Rule = MakePlusRule(namespace_member_declarations, null, namespace_member_declaration);

      type_declaration.Rule = class_declaration | struct_declaration | interface_declaration | enum_declaration | delegate_declaration;

      //B.2.7. Classes
      class_declaration.Rule = member_header + "class" + identifier + type_parameter_list_opt +
        bases_opt + type_parameter_constraints_clauses_opt + class_body;
      class_body.Rule = Lbr + member_declaration.Star() + Rbr;
      bases_opt.Rule = Empty | colon + qual_name_with_targs.Plus("type_names", comma);

      //Type parameters
      type_parameter.Rule = attributes_opt + identifier;
      type_parameters.Rule = MakePlusRule(type_parameters, comma, type_parameter);
      type_parameter_list_opt.Rule = Empty | "<" + type_parameters + ">";
      type_parameter_constraints_clause.Rule = "where" + type_parameter + colon + type_parameter_constraints;
      type_parameter_constraints.Rule = MakePlusRule(type_parameter_constraints, comma, type_parameter_constraint); 
      type_parameter_constraints_clauses_opt.Rule = MakeStarRule(type_parameter_constraints_clauses_opt, null, type_parameter_constraints_clause);
      //Note for post-processing - make sure the order is correct: new() is always last, etc. See p.503 of the spec 
      type_parameter_constraint.Rule = qual_name_with_targs | "class" | "struct" | Symbol("new") + Lpar + Rpar;

      //Class members
      //Note: we split operator-declaration into two separate operator elements: bin/unary and conversion operators
      //  to avoid possible ambiguities and conflicts
      member_declaration.Rule = constant_declaration | field_declaration | method_declaration
         | property_declaration | event_declaration | indexer_declaration
         | operator_declaration | conversion_operator_declaration
         | constructor_declaration | destructor_declaration | type_declaration;
      member_declarations_opt.Rule = MakeStarRule(member_declarations_opt, null, member_declaration);

      //Modifiers - see note #1 in Notes.txt file
      modifier.Rule = Symbol("new") | "public" | "protected" | "internal" | "private" | "static" | "virtual" | "sealed" |
        "override" | "abstract" | "readonly" | "volatile" | "partial" | "extern"; //!!!
      modifiers_opt.Rule = MakeStarRule(modifiers_opt, null, modifier);
      //Joined member header - see note #2
      member_header.Rule = attributes_opt + modifiers_opt;
      constant_declaration.Rule = member_header + "const" + type_ref + constant_declarators + semi;
      constant_declarator.Rule = identifier + "=" + expression;
      constant_declarators.Rule = MakePlusRule(constant_declarators, comma, constant_declarator);
      field_declaration.Rule = member_header + type_ref + variable_declarators + semi;
      variable_declarator.Rule = identifier | identifier + "=" + elem_initializer;
      variable_declarators.Rule = MakePlusRule(variable_declarators, comma, variable_declarator);
      //See note #3 about merging type_parameter_list into type_arguments of the preceding qual_name. 
      method_declaration.Rule = member_header + type_ref + qual_name_with_targs  // + type_parameter_list.Q() 
        + formal_parameter_list_par + type_parameter_constraints_clauses_opt + method_body;
      formal_parameter_list.Rule = fixed_parameters | fixed_parameters + comma + parameter_array | parameter_array;
      formal_parameter_list_par.Rule = Lpar + Rpar | Lpar + formal_parameter_list + Rpar;

      fixed_parameter.Rule = attributes_opt + parameter_modifier_opt + type_ref + identifier;
      fixed_parameters.Rule = MakePlusRule(fixed_parameters, comma, fixed_parameter);
      parameter_modifier_opt.Rule = Empty | "ref" | "out" | "this";
      parameter_array.Rule = attributes_opt + "params" + type_ref + /*"[" + "]" + */ identifier;
      method_body.Rule = block | semi;
      // See note #4 about member-name
      //TODO: add after-parse validation that no more than one accessor of each type is there.
      property_declaration.Rule = member_header + type_ref + qual_name_with_targs/*member-name*/ + Lbr + accessor_declarations + Rbr;
      accessor_declaration.Rule = attributes_opt + accessor_modifier_opt + accessor_name + block;
      accessor_declarations.Rule = MakePlusRule(accessor_declarations, null, accessor_declaration);
      accessor_name.Rule = Symbol("get") | "set";
      accessor_modifier_opt.Rule = Empty | "protected" | "internal" | "private" |
                           Symbol("protected") + "internal" | Symbol("internal") + "protected";

      event_declaration.Rule = member_header + "event" + type_ref + event_body;
      event_body.Rule =   variable_declarators + semi | qual_name_with_targs + Lbr + event_accessor_declarations + Rbr;
      event_accessor_declarations.Rule = add_accessor_declaration + remove_accessor_declaration |
                                         remove_accessor_declaration + add_accessor_declaration;
      add_accessor_declaration.Rule = attributes_opt + "add" + block;
      remove_accessor_declaration.Rule = attributes_opt + "remove" + block;
      
      //indexer
      indexer_declaration.Rule = member_header + type_ref + indexer_name + "[" + formal_parameter_list + "]" +  
                                     Lbr + accessor_declarations + Rbr;
      indexer_name.Rule = "this" | qual_name_with_targs + dot + "this";

      //operator
      // note: difference with specs - we separate unary/binary operators from conversion operator, 
      //   and join binary and unary operator definitions, see note #5
      operator_declaration.Rule = member_header + type_ref + "operator" + overloadable_operator + Lpar + operator_parameters + Rpar + block;
      overloadable_operator.Rule = Symbol("+") | "-" | "!" | "~" | "++" | "--" | "true" | "false" //unary operators
                                 | "*" | "/" | "%" | "&" | "|" | "^" | "<<" | ">>" | "==" | "!=" | ">" | "<" | ">=" | "<=";
      operator_parameters.Rule = operator_parameter | operator_parameter + comma + operator_parameter;
      operator_parameter.Rule = type_ref + identifier;
      conversion_operator_declaration.Rule = member_header + conversion_operator_kind + 
           "operator" + type_ref + Lpar + operator_parameter + Rpar + block;
      conversion_operator_kind.Rule = Symbol("implicit") | "explicit";

      //constructor - also covers static constructor; the only difference is the word static
      constructor_declaration.Rule = member_header + identifier + formal_parameter_list_par +
        constructor_initializer_opt + block; 
      constructor_initializer_opt.Rule = Empty | colon + constructor_base + Lpar + argument_list_opt + Rpar;
      constructor_base.Rule = Symbol("this") | "base";

      destructor_declaration.Rule = member_header + // changed from Symbol("extern").Q() 
                                     "~" + identifier + Lpar + Rpar + block;

      //B.2.8
      struct_declaration.Rule = member_header + "struct" + identifier + type_parameter_list_opt + bases_opt
        + type_parameter_constraints_clauses_opt + struct_body;
      struct_body.Rule = Lbr + member_declarations_opt + Rbr;   


      //B.2.9. Arrays

      //B.2.10 Interface
      interface_declaration.Rule = member_header + "interface" + identifier + type_parameter_list_opt + bases_opt
        + type_parameter_constraints_clauses_opt + interface_body;
      interface_body.Rule = Lbr + interface_member_declarations + Rbr;
      interface_member_declaration.Rule = interface_method_declaration | interface_property_declaration
                                        | interface_event_declaration | interface_indexer_declaration;
      interface_member_declarations.Rule = MakePlusRule(interface_member_declarations, null, interface_member_declaration);
      interface_method_declaration.Rule = attributes_opt + new_opt + type_ref + identifier + type_parameter_list_opt +
         formal_parameter_list_par + type_parameter_constraints_clauses_opt + semi;
      //NOte: changing type to type_ref to fix the conflict
      //Note: add after-parse validation that no more than one  accessor of each type is there. 
      interface_property_declaration.Rule = attributes_opt + new_opt + type_ref + identifier + Lbr + interface_accessors + Rbr;
      interface_accessor.Rule = attributes_opt + accessor_name + semi;
      interface_accessors.Rule = MakePlusRule(interface_accessors, null, interface_accessor);
      interface_event_declaration.Rule = attributes_opt + new_opt + "event" + type_ref + identifier;
      interface_indexer_declaration.Rule = attributes_opt + new_opt + type_ref + "this" + 
                                            "[" + formal_parameter_list + "]" + Lbr + interface_accessors + Rbr;
      new_opt.Rule = Empty | "new";

      //B.2.11 Enums
      enum_declaration.Rule = member_header + "enum" + identifier + enum_base_opt + Lbr + enum_body + Rbr + semi_opt;
      enum_base_opt.Rule = Empty | colon + integral_type;
      enum_body.Rule = Empty | enum_member_declarations + comma_opt;
      enum_member_declaration.Rule = attributes_opt + identifier | attributes_opt + identifier + "=" + expression;
      enum_member_declarations.Rule = MakePlusRule(enum_member_declarations, comma, enum_member_declaration);

      //B.2.12 Delegates
      delegate_declaration.Rule = member_header + "delegate" + type_ref + identifier +
        type_parameter_list_opt + formal_parameter_list_par + type_parameter_constraints_clauses_opt + semi;


      //B.2.13. Attributes

      attributes_opt.Rule = MakeStarRule(attributes_opt, null, attribute_section);
      attribute_section.Rule = "[" + attribute_target_specifier_opt + attribute_list + comma_opt + "]";
      attribute_list.Rule = MakePlusRule(attribute_list, comma, attribute);
      attribute.Rule = qual_name_with_targs + attribute_arguments_par_opt;
      
      attribute_target_specifier_opt.Rule = Empty  | attribute_target + colon;
      attribute_target.Rule = Symbol("field") | "event" | "method" | "param" | "property" | "return" | "type";
      attribute_arguments_par_opt.Rule = Empty | Lpar + attribute_arguments_opt + Rpar;
      attribute_arguments_opt.Rule = MakeStarRule(attribute_arguments_opt, comma, attr_arg);
      attr_arg.Rule = identifier + "=" + expression | expression;

    }

    private KeyList _previewTokens = new KeyList(
          ";", "{", 
          "||" , "&&" , "|" , "^" , "&" , "==" , "!=" , ">" , "<=" , ">=" , "<<" , ">>" , 
          "+" , "-" , "*" , "/" , "%", "=" , "+=" , "-=" , "*=" , "/=" , "%=" , "&=" , ",=" , 
          "^=" , "<<=" , ">>=" , "is" , "as");
    
    public override ActionRecord OnActionConflict(Parser parser, Token input, ActionRecord action) {
      if (input.Text != "<") return action;

      Token preview = parser.PreviewSymbols(_previewTokens);
      //if we see closing angle bracket before anything else, it is type arguments, so we must do shift!
      if (preview != null && preview.Text == ">") {
        if (action.ActionType == ParserActionType.Shift) return action;
        return action.CreateDerived(ParserActionType.Shift, null);
      } else {
        //otherwise, mark as operator
        return action.CreateDerived(ParserActionType.Reduce, action.Production);
      }
    }
  
  }//class
}//namespace
