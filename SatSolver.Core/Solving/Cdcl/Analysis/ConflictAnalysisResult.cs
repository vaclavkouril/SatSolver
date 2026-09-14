using SatSolver.Core.Solving.Clauses;

namespace SatSolver.Core.Solving.Cdcl.Analysis;

public sealed record ConflictAnalysisResult(
    LearnedClause AssertingClause,
    IReadOnlyList<LearnedClause> AdditionalClauses);
