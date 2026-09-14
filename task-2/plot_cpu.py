#!/usr/bin/env python3
"""Read DPLL or CDCL results and save a CPU time comparison plot."""

import argparse
import os
from pathlib import Path


def read_cpu_times(path: Path) -> list[float]:
    times = []
    for line in path.read_text(encoding="utf-8-sig").splitlines():
        label, _, value = line.strip().partition(":")
        if label == "CPU time":
            number = value.strip().removesuffix(" ms").replace(",", ".")
            times.append(float(number))
    return times


def save_plot(reports, labels, output: Path) -> None:
    os.environ.setdefault("MPLCONFIGDIR", str(Path(__file__).resolve().parent / ".matplotlib"))
    import matplotlib
    matplotlib.use("Agg")
    import matplotlib.pyplot as plt

    figure, axis = plt.subplots(figsize=(10, 5), layout="constrained")
    for index, times in enumerate(reports):
        axis.plot(range(1, len(times) + 1), times, label=labels[index])
    axis.set_title("CPU time comparison")
    axis.set_ylabel("CPU time (ms)")
    axis.set_xlabel("Instance (file order)")
    axis.set_ylim(bottom=0)
    axis.grid(alpha=0.25)
    axis.legend()
    output.parent.mkdir(parents=True, exist_ok=True)
    figure.savefig(output, dpi=180)
    plt.close(figure)


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("files", type=Path, nargs="+", help="DPLL or CDCL result files")
    parser.add_argument("--labels", nargs="+", help="One legend label per input file")
    parser.add_argument("-o", "--output", type=Path, default=Path("cpu-comparison.png"))
    args = parser.parse_args()

    reports = [read_cpu_times(path) for path in args.files]
    labels = args.labels or [path.stem for path in args.files]
    save_plot(reports, labels, args.output)
    print(f"Saved {args.output}")


if __name__ == "__main__":
    main()
