# SatSolver

Implementation of my own SAT solver for NAIL094 - Decision procedures and verification.

## Layout

- `SatSolver.Core/Solving/Contracts` - public solver and heuristic contracts.
- `SatSolver.Core/Solving/Search` - assignment trail, levels, reasons and statistics.
- `SatSolver.Core/Solving/Propagation` - adjacency-list and lazy watched-literal propagation.
- `SatSolver.Core/Solving/Dpll` - chronological DPLL search.
- `SatSolver.Core/Solving/Cdcl` - conflict analysis, learning, restarts and clause deletion.
- `SatSolver.IO` - SMT-LIB and DIMACS readers/writers.
- `Formula2Cnf` - translates a description of a formula in NNF into a DIMACS CNF formula using Tseitin encoding
- `Dpll` - command-line solver.
- `Cdcl` - command-line CDCL solver.

## Run

```sh
dotnet test SatSolver.slnx
dotnet run --project Dpll -- task-1/toy_5.sat
dotnet run --project Cdcl -- task-1/toy_5.sat
```

Run `dpll --help` for DPLL propagation options and `cdcl --help` for CDCL configuration.
