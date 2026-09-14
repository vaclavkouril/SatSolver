#!/usr/bin/env python3
"""Read CDCL results and save one metric or all metrics as a plot."""

import argparse
import os
from pathlib import Path


METRICS = {
    "cpu": "CPU time",
    "decisions": "Decisions",
    "propagations": "Unit propagations",
    "checks": "Propagation clause checks",
    "conflicts": "Conflicts",
    "backjumps": "Backjumps",
    "restarts": "Restarts",
    "learned": "Learned clauses",
    "deleted": "Deleted learned clauses",
    "length": "Mean learned-clause length",
    "lbd": "Mean learned-clause LBD",
}


def read_results(path: Path) -> dict[str, list[float]]:
    results = {metric: [] for metric in METRICS}
    for line in path.read_text(encoding="utf-8-sig").splitlines():
        label, _, value = line.strip().partition(":")
        for metric, name in METRICS.items():
            if label == name:
                number = value.strip().removesuffix(" ms").replace(",", ".")
                results[metric].append(float(number))
    return results


def save_plot(reports, labels, metrics, output: Path) -> None:
    os.environ.setdefault("MPLCONFIGDIR", str(Path(__file__).resolve().parent / ".matplotlib"))
    import matplotlib
    matplotlib.use("Agg")
    import matplotlib.pyplot as plt

    columns = 3 if len(metrics) > 1 else 1
    rows = (len(metrics) + columns - 1) // columns
    figure, axes = plt.subplots(
        rows, columns, figsize=(7 * columns, 4 * rows), squeeze=False, layout="constrained"
    )
    for axis, metric in zip(axes.flat, metrics):
        for index, report in enumerate(reports):
            values = report[metric]
            axis.plot(range(1, len(values) + 1), values, label=labels[index])
        title = METRICS[metric] + (" (ms)" if metric == "cpu" else "")
        axis.set_title(title)
        axis.set_ylabel(title)
        axis.set_xlabel("Instance (file order)")
        axis.set_ylim(bottom=0)
        axis.grid(alpha=0.25)
        axis.legend()
    for axis in list(axes.flat)[len(metrics):]:
        axis.set_visible(False)
    output.parent.mkdir(parents=True, exist_ok=True)
    figure.savefig(output, dpi=180)
    plt.close(figure)


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("files", type=Path, nargs="+", help="CDCL result files")
    parser.add_argument("--metric", choices=("all", *METRICS), default="all")
    parser.add_argument("--labels", nargs="+", help="One legend label per input file")
    parser.add_argument("-o", "--output", type=Path, default=Path("cdcl-comparison.png"))
    args = parser.parse_args()

    reports = [read_results(path) for path in args.files]
    labels = args.labels or [path.stem for path in args.files]
    metrics = list(METRICS) if args.metric == "all" else [args.metric]
    save_plot(reports, labels, metrics, args.output)
    print(f"Saved {args.output}")


if __name__ == "__main__":
    main()
