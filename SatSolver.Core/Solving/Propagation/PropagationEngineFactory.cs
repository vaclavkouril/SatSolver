namespace SatSolver.Core.Solving.Propagation;

internal static class PropagationEngineFactory
{
    public static IPropagationEngine Create(PropagationMethod method) => method switch
    {
        PropagationMethod.AdjacencyLists => new AdjacencyListPropagator(),
        PropagationMethod.WatchedLiterals => new WatchedLiteralPropagator(),
        _ => throw new ArgumentOutOfRangeException(nameof(method))
    };
}
