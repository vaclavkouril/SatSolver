# SatSolver

Implementation of my own SAT solver for NAIL094 - Decision procedures and verification.

## Layout

- `SatSolver.Core/` - library for Clauses, actual CDCL abnd DPLL algorithm implementations, heuristics, propagation, restarts and Tseitin encoder
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

## Search data structures

- VSIDS uses a `SortedSet` of immutable activity keys. Priority updates and
  reinsertion after backtracking cost O(log N). Assigned variables are removed
  lazily when they reach the front, once per assignment interval; one decision
  can therefore also pay for removing previously propagated variables.
  Initialization and rare activity rescaling rebuild the ordering. Equal activities
  use a fixed seeded permutation, so runs are reproducible but their decision
  sequences differ from the former per-decision random tie sampling.
- Self-subsuming resolution uses a literal occurrence index, built on first use
  and updated as clauses are added. It checks only clauses containing the opposite
  literal and removes deleted entries on access. The index takes memory proportional
  to indexed literal occurrences; a very frequent literal can still be expensive.
- Clause deletion checks a maintained active learned-clause count before scanning
  the database. When due, it bulk-builds a `PriorityQueue` and extracts the worst
  k of m eligible clauses in O(m + k log m), preserving input order for ties.
  Deleting a fixed fraction still has O(m log m) complexity.
