//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     ANTLR Version: 4.7.1
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

// Generated from bhl.g by ANTLR 4.7.1

// Unreachable code detected
#pragma warning disable 0162
// The variable '...' is assigned but its value is never used
#pragma warning disable 0219
// Missing XML comment for publicly visible type or member '...'
#pragma warning disable 1591
// Ambiguous reference in cref attribute
#pragma warning disable 419

using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using IToken = Antlr4.Runtime.IToken;

/// <summary>
/// This interface defines a complete generic visitor for a parse tree produced
/// by <see cref="bhlParser"/>.
/// </summary>
/// <typeparam name="Result">The return type of the visit operation.</typeparam>
[System.CodeDom.Compiler.GeneratedCode("ANTLR", "4.7.1")]
[System.CLSCompliant(false)]
public interface IbhlVisitor<Result> : IParseTreeVisitor<Result> {
	/// <summary>
	/// Visit a parse tree produced by <see cref="bhlParser.program"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitProgram([NotNull] bhlParser.ProgramContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="bhlParser.declOrImport"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitDeclOrImport([NotNull] bhlParser.DeclOrImportContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="bhlParser.mimport"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitMimport([NotNull] bhlParser.MimportContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="bhlParser.decl"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitDecl([NotNull] bhlParser.DeclContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="bhlParser.dotName"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitDotName([NotNull] bhlParser.DotNameContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="bhlParser.nsName"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitNsName([NotNull] bhlParser.NsNameContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="bhlParser.type"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitType([NotNull] bhlParser.TypeContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="bhlParser.mapType"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitMapType([NotNull] bhlParser.MapTypeContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="bhlParser.expList"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitExpList([NotNull] bhlParser.ExpListContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="bhlParser.returnVal"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitReturnVal([NotNull] bhlParser.ReturnValContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="bhlParser.name"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitName([NotNull] bhlParser.NameContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="bhlParser.lambdaCall"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitLambdaCall([NotNull] bhlParser.LambdaCallContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="bhlParser.chainExp"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitChainExp([NotNull] bhlParser.ChainExpContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>ExpTypeof</c>
	/// labeled alternative in <see cref="bhlParser.exp"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitExpTypeof([NotNull] bhlParser.ExpTypeofContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>ExpIncompleteCall</c>
	/// labeled alternative in <see cref="bhlParser.exp"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitExpIncompleteCall([NotNull] bhlParser.ExpIncompleteCallContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>ExpIs</c>
	/// labeled alternative in <see cref="bhlParser.exp"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitExpIs([NotNull] bhlParser.ExpIsContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>ExpOr</c>
	/// labeled alternative in <see cref="bhlParser.exp"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitExpOr([NotNull] bhlParser.ExpOrContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>ExpLiteralFalse</c>
	/// labeled alternative in <see cref="bhlParser.exp"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitExpLiteralFalse([NotNull] bhlParser.ExpLiteralFalseContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>ExpLiteralNum</c>
	/// labeled alternative in <see cref="bhlParser.exp"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitExpLiteralNum([NotNull] bhlParser.ExpLiteralNumContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>ExpMulDivMod</c>
	/// labeled alternative in <see cref="bhlParser.exp"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitExpMulDivMod([NotNull] bhlParser.ExpMulDivModContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>ExpAs</c>
	/// labeled alternative in <see cref="bhlParser.exp"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitExpAs([NotNull] bhlParser.ExpAsContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>ExpLiteralTrue</c>
	/// labeled alternative in <see cref="bhlParser.exp"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitExpLiteralTrue([NotNull] bhlParser.ExpLiteralTrueContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>ExpJsonObj</c>
	/// labeled alternative in <see cref="bhlParser.exp"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitExpJsonObj([NotNull] bhlParser.ExpJsonObjContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>ExpYieldCall</c>
	/// labeled alternative in <see cref="bhlParser.exp"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitExpYieldCall([NotNull] bhlParser.ExpYieldCallContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>ExpTernaryIf</c>
	/// labeled alternative in <see cref="bhlParser.exp"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitExpTernaryIf([NotNull] bhlParser.ExpTernaryIfContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>ExpLambda</c>
	/// labeled alternative in <see cref="bhlParser.exp"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitExpLambda([NotNull] bhlParser.ExpLambdaContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>ExpAnd</c>
	/// labeled alternative in <see cref="bhlParser.exp"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitExpAnd([NotNull] bhlParser.ExpAndContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>ExpChain</c>
	/// labeled alternative in <see cref="bhlParser.exp"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitExpChain([NotNull] bhlParser.ExpChainContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>ExpJsonArr</c>
	/// labeled alternative in <see cref="bhlParser.exp"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitExpJsonArr([NotNull] bhlParser.ExpJsonArrContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>ExpCompare</c>
	/// labeled alternative in <see cref="bhlParser.exp"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitExpCompare([NotNull] bhlParser.ExpCompareContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>ExpLiteralStr</c>
	/// labeled alternative in <see cref="bhlParser.exp"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitExpLiteralStr([NotNull] bhlParser.ExpLiteralStrContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>ExpUnary</c>
	/// labeled alternative in <see cref="bhlParser.exp"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitExpUnary([NotNull] bhlParser.ExpUnaryContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>ExpNew</c>
	/// labeled alternative in <see cref="bhlParser.exp"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitExpNew([NotNull] bhlParser.ExpNewContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>ExpAddSub</c>
	/// labeled alternative in <see cref="bhlParser.exp"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitExpAddSub([NotNull] bhlParser.ExpAddSubContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>ExpBitwise</c>
	/// labeled alternative in <see cref="bhlParser.exp"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitExpBitwise([NotNull] bhlParser.ExpBitwiseContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>ExpLiteralNull</c>
	/// labeled alternative in <see cref="bhlParser.exp"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitExpLiteralNull([NotNull] bhlParser.ExpLiteralNullContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>ExpTypeCast</c>
	/// labeled alternative in <see cref="bhlParser.exp"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitExpTypeCast([NotNull] bhlParser.ExpTypeCastContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>ExpIncompleteMember</c>
	/// labeled alternative in <see cref="bhlParser.exp"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitExpIncompleteMember([NotNull] bhlParser.ExpIncompleteMemberContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="bhlParser.ternaryIfExp"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitTernaryIfExp([NotNull] bhlParser.TernaryIfExpContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="bhlParser.newExp"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitNewExp([NotNull] bhlParser.NewExpContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="bhlParser.foreachExp"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitForeachExp([NotNull] bhlParser.ForeachExpContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="bhlParser.forPreIter"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitForPreIter([NotNull] bhlParser.ForPreIterContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="bhlParser.forPostIter"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitForPostIter([NotNull] bhlParser.ForPostIterContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="bhlParser.forExp"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitForExp([NotNull] bhlParser.ForExpContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="bhlParser.postOp"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitPostOp([NotNull] bhlParser.PostOpContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>StmSeparator</c>
	/// labeled alternative in <see cref="bhlParser.statement"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitStmSeparator([NotNull] bhlParser.StmSeparatorContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>StmChainExp</c>
	/// labeled alternative in <see cref="bhlParser.statement"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitStmChainExp([NotNull] bhlParser.StmChainExpContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>StmDeclOptAssign</c>
	/// labeled alternative in <see cref="bhlParser.statement"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitStmDeclOptAssign([NotNull] bhlParser.StmDeclOptAssignContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>StmIf</c>
	/// labeled alternative in <see cref="bhlParser.statement"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitStmIf([NotNull] bhlParser.StmIfContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>StmWhile</c>
	/// labeled alternative in <see cref="bhlParser.statement"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitStmWhile([NotNull] bhlParser.StmWhileContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>StmDoWhile</c>
	/// labeled alternative in <see cref="bhlParser.statement"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitStmDoWhile([NotNull] bhlParser.StmDoWhileContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>StmFor</c>
	/// labeled alternative in <see cref="bhlParser.statement"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitStmFor([NotNull] bhlParser.StmForContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>StmForeach</c>
	/// labeled alternative in <see cref="bhlParser.statement"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitStmForeach([NotNull] bhlParser.StmForeachContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>StmYield</c>
	/// labeled alternative in <see cref="bhlParser.statement"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitStmYield([NotNull] bhlParser.StmYieldContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>StmYieldCall</c>
	/// labeled alternative in <see cref="bhlParser.statement"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitStmYieldCall([NotNull] bhlParser.StmYieldCallContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>StmYieldWhile</c>
	/// labeled alternative in <see cref="bhlParser.statement"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitStmYieldWhile([NotNull] bhlParser.StmYieldWhileContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>StmBreak</c>
	/// labeled alternative in <see cref="bhlParser.statement"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitStmBreak([NotNull] bhlParser.StmBreakContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>StmContinue</c>
	/// labeled alternative in <see cref="bhlParser.statement"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitStmContinue([NotNull] bhlParser.StmContinueContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>StmReturn</c>
	/// labeled alternative in <see cref="bhlParser.statement"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitStmReturn([NotNull] bhlParser.StmReturnContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>StmParal</c>
	/// labeled alternative in <see cref="bhlParser.statement"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitStmParal([NotNull] bhlParser.StmParalContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>StmParalAll</c>
	/// labeled alternative in <see cref="bhlParser.statement"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitStmParalAll([NotNull] bhlParser.StmParalAllContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>StmDefer</c>
	/// labeled alternative in <see cref="bhlParser.statement"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitStmDefer([NotNull] bhlParser.StmDeferContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>StmBlockNested</c>
	/// labeled alternative in <see cref="bhlParser.statement"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitStmBlockNested([NotNull] bhlParser.StmBlockNestedContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="bhlParser.elseIf"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitElseIf([NotNull] bhlParser.ElseIfContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="bhlParser.else"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitElse([NotNull] bhlParser.ElseContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="bhlParser.chainExpItem"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitChainExpItem([NotNull] bhlParser.ChainExpItemContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="bhlParser.arrAccess"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitArrAccess([NotNull] bhlParser.ArrAccessContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="bhlParser.memberAccess"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitMemberAccess([NotNull] bhlParser.MemberAccessContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="bhlParser.callArgs"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitCallArgs([NotNull] bhlParser.CallArgsContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="bhlParser.callArgsList"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitCallArgsList([NotNull] bhlParser.CallArgsListContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="bhlParser.callArg"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitCallArg([NotNull] bhlParser.CallArgContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="bhlParser.block"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitBlock([NotNull] bhlParser.BlockContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="bhlParser.extensions"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitExtensions([NotNull] bhlParser.ExtensionsContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="bhlParser.nsDecl"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitNsDecl([NotNull] bhlParser.NsDeclContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="bhlParser.classDecl"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitClassDecl([NotNull] bhlParser.ClassDeclContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="bhlParser.classBlock"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitClassBlock([NotNull] bhlParser.ClassBlockContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="bhlParser.classMembers"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitClassMembers([NotNull] bhlParser.ClassMembersContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="bhlParser.fldAttribs"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitFldAttribs([NotNull] bhlParser.FldAttribsContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="bhlParser.fldDeclare"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitFldDeclare([NotNull] bhlParser.FldDeclareContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="bhlParser.classMember"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitClassMember([NotNull] bhlParser.ClassMemberContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="bhlParser.interfaceDecl"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitInterfaceDecl([NotNull] bhlParser.InterfaceDeclContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="bhlParser.interfaceBlock"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitInterfaceBlock([NotNull] bhlParser.InterfaceBlockContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="bhlParser.interfaceMembers"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitInterfaceMembers([NotNull] bhlParser.InterfaceMembersContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="bhlParser.interfaceMember"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitInterfaceMember([NotNull] bhlParser.InterfaceMemberContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="bhlParser.enumDecl"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitEnumDecl([NotNull] bhlParser.EnumDeclContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="bhlParser.enumBlock"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitEnumBlock([NotNull] bhlParser.EnumBlockContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="bhlParser.enumMember"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitEnumMember([NotNull] bhlParser.EnumMemberContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="bhlParser.virtualFlag"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitVirtualFlag([NotNull] bhlParser.VirtualFlagContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="bhlParser.overrideFlag"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitOverrideFlag([NotNull] bhlParser.OverrideFlagContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="bhlParser.staticFlag"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitStaticFlag([NotNull] bhlParser.StaticFlagContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="bhlParser.coroFlag"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitCoroFlag([NotNull] bhlParser.CoroFlagContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="bhlParser.funcAttribs"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitFuncAttribs([NotNull] bhlParser.FuncAttribsContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="bhlParser.funcDecl"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitFuncDecl([NotNull] bhlParser.FuncDeclContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="bhlParser.funcType"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitFuncType([NotNull] bhlParser.FuncTypeContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="bhlParser.funcBlock"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitFuncBlock([NotNull] bhlParser.FuncBlockContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="bhlParser.interfaceFuncDecl"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitInterfaceFuncDecl([NotNull] bhlParser.InterfaceFuncDeclContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="bhlParser.funcLambda"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitFuncLambda([NotNull] bhlParser.FuncLambdaContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="bhlParser.refType"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitRefType([NotNull] bhlParser.RefTypeContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="bhlParser.retType"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitRetType([NotNull] bhlParser.RetTypeContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="bhlParser.types"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitTypes([NotNull] bhlParser.TypesContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="bhlParser.funcParams"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitFuncParams([NotNull] bhlParser.FuncParamsContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="bhlParser.funcParamDeclare"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitFuncParamDeclare([NotNull] bhlParser.FuncParamDeclareContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="bhlParser.varDeclare"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitVarDeclare([NotNull] bhlParser.VarDeclareContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="bhlParser.varDeclareAssign"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitVarDeclareAssign([NotNull] bhlParser.VarDeclareAssignContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="bhlParser.varDeclareOptAssign"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitVarDeclareOptAssign([NotNull] bhlParser.VarDeclareOptAssignContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="bhlParser.varOrDeclare"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitVarOrDeclare([NotNull] bhlParser.VarOrDeclareContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="bhlParser.varOrDeclareAssign"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitVarOrDeclareAssign([NotNull] bhlParser.VarOrDeclareAssignContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="bhlParser.varPostOp"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitVarPostOp([NotNull] bhlParser.VarPostOpContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="bhlParser.assignExp"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitAssignExp([NotNull] bhlParser.AssignExpContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="bhlParser.operatorOr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitOperatorOr([NotNull] bhlParser.OperatorOrContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="bhlParser.operatorAnd"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitOperatorAnd([NotNull] bhlParser.OperatorAndContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="bhlParser.operatorBitwise"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitOperatorBitwise([NotNull] bhlParser.OperatorBitwiseContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="bhlParser.operatorIncDec"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitOperatorIncDec([NotNull] bhlParser.OperatorIncDecContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="bhlParser.operatorSelfOp"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitOperatorSelfOp([NotNull] bhlParser.OperatorSelfOpContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="bhlParser.operatorComparison"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitOperatorComparison([NotNull] bhlParser.OperatorComparisonContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="bhlParser.operatorAddSub"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitOperatorAddSub([NotNull] bhlParser.OperatorAddSubContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="bhlParser.operatorMulDivMod"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitOperatorMulDivMod([NotNull] bhlParser.OperatorMulDivModContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="bhlParser.operatorUnary"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitOperatorUnary([NotNull] bhlParser.OperatorUnaryContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="bhlParser.isRef"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitIsRef([NotNull] bhlParser.IsRefContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="bhlParser.number"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitNumber([NotNull] bhlParser.NumberContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="bhlParser.string"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitString([NotNull] bhlParser.StringContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="bhlParser.jsonObject"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitJsonObject([NotNull] bhlParser.JsonObjectContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="bhlParser.jsonEmptyObj"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitJsonEmptyObj([NotNull] bhlParser.JsonEmptyObjContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="bhlParser.jsonPair"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitJsonPair([NotNull] bhlParser.JsonPairContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="bhlParser.jsonArray"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitJsonArray([NotNull] bhlParser.JsonArrayContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="bhlParser.jsonEmptyArr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitJsonEmptyArr([NotNull] bhlParser.JsonEmptyArrContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="bhlParser.jsonValue"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitJsonValue([NotNull] bhlParser.JsonValueContext context);
}
