# Task 2 statistics

## Build and run the solver

```sh
dotnet build ../Dpll/Dpll.csproj -c Release
dotnet ../Dpll/bin/Release/net10.0/dpll.dll --propagation watched toy_5.sat
dotnet ../Dpll/bin/Release/net10.0/dpll.dll --propagation adjacency toy_5.sat
```

## Run a dataset with both methods

Change `dataset-folder` to select another dataset.

```bash
for method in watched adjacency; do
  find dataset-folder -type f -name '*.cnf' -print0 | sort -z |
    while IFS= read -r -d '' instance; do
      printf '\n===== %s =====\n' "$instance"
      dotnet ../Dpll/bin/Release/net10.0/dpll.dll --propagation "$method" "$instance"
    done > "$method-results.txt"
done
```

## Set up Python

```sh
python3 -m venv .venv
.venv/bin/python -m pip install -r requirements.txt
```

## Plot results

One file:

```sh
.venv/bin/python plot_results.py watched-results.txt -o plots/watched.png
```

Compare files with the same instances in the same order:

```sh
.venv/bin/python plot_results.py \
  watched-results.txt adjacency-results.txt \
  --labels watched adjacency -o plots/comparison.png
```

Plot one metric instead of the default four:

```sh
.venv/bin/python plot_results.py watched-results.txt adjacency-results.txt \
  --labels watched adjacency --metric cpu -o plots/cpu.png
```

Available metrics: `cpu`, `decisions`, `propagations`, `checks`.
Plots are saved to a file (PNG, SVG or PDF).

## Plot CDCL results

All 11 metrics by default, or one using `--metric`:

```sh
.venv/bin/python plot_cdcl.py cdcl-results.txt -o plots/cdcl.png
.venv/bin/python plot_cdcl.py cdcl-results.txt --metric conflicts -o plots/conflicts.png
```

Metrics: `cpu`, `decisions`, `propagations`, `checks`, `conflicts`, `backjumps`,
`restarts`, `learned`, `deleted`, `length`, `lbd`.
Multiple files and `--labels` work the same way as above.

## Compare CPU time (DPLL and CDCL)

```sh
.venv/bin/python plot_cpu.py dpll-results.txt cdcl-results.txt \
  --labels DPLL CDCL -o plots/cpu-comparison.png
```
