using SatSolver.Core.Solving.Clauses;

namespace SatSolver.Core.Solving.Cdcl.Analysis;

/// <summary>Conflict-analysis result.</summary>
internal sealed record ConflictAnalysisResult(
    LearnedClause AssertingClause,
    IReadOnlyList<LearnedClause> AdditionalClauses);
