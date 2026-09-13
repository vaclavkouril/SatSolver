#!/usr/bin/env python3
"""Save one metric or all four DPLL metrics as a comparison plot."""

import argparse
import math
import os
import re
from dataclasses import dataclass, field
from pathlib import Path


METRICS = {
    "cpu": "CPU time",
    "decisions": "Decisions",
    "propagations": "Unit propagations",
    "checks": "Propagation clause checks",
}
HEADER = re.compile(r"^=====\s+(.+?)\s+=====$")
SINGLE_INSTANCE = "<single instance>"


@dataclass
class Result:
    status: str | None = None
    values: dict[str, float] = field(default_factory=dict)


def read_results(path: Path) -> dict[str, Result]:
    results = {}
    current = None
    for line_number, line in enumerate(path.read_text(encoding="utf-8-sig").splitlines(), 1):
        line = line.strip()
        if not line:
            continue
        try:
            header = HEADER.fullmatch(line)
            if header:
                name = Path(header[1].replace("\\", "/")).as_posix()
                if name in results:
                    raise ValueError(f"duplicate instance {name}")
                current = Result()
                results[name] = current
                continue
            if current is None:
                current = Result()
                results[SINGLE_INSTANCE] = current
            if line in ("SAT", "UNSAT", "UNKNOWN", "TIMEOUT"):
                if current.status is not None:
                    raise ValueError("repeated status; missing instance header?")
                current.status = line
                continue
            if line == "Statistics:" or line == "v" or line.startswith("v "):
                continue

            label, separator, value = line.partition(":")
            if not separator or label not in METRICS.values():
                raise ValueError(f"unrecognised report line: {line}")
            metric = next(key for key in METRICS if METRICS[key] == label)
            value = value.strip()
            if metric == "cpu":
                if not value.endswith(" ms"):
                    raise ValueError("expected CPU time in ms")
                value = value[:-3].strip().replace(",", ".")
            number = float(value) if metric == "cpu" else int(value)
            if not math.isfinite(number) or number < 0 or metric in current.values:
                raise ValueError(f"invalid or duplicate {label}")
            current.values[metric] = number
        except ValueError as error:
            raise ValueError(f"{path}:{line_number}: {error}") from error
    if not results:
        raise ValueError(f"{path}: empty report")
    return results


def validate_results(reports, metrics) -> list[str]:
    names = sorted(reports[0])
    if len(reports) > 1 and any(SINGLE_INSTANCE in report for report in reports):
        raise ValueError("Comparing files requires ===== instance-path ===== headers")
    for report in reports[1:]:
        if report.keys() != reports[0].keys():
            raise ValueError("Input files contain different instances")
    for name in names:
        statuses = {report[name].status for report in reports}
        if len(statuses) != 1:
            raise ValueError(f"{name}: solver statuses disagree")
        if not statuses.issubset({"SAT", "UNSAT"}):
            raise ValueError(f"{name}: incomplete/unsolved instance")
        for index, report in enumerate(reports, 1):
            missing = set(metrics) - report[name].values.keys()
            if missing:
                raise ValueError(f"File {index}, {name}: missing metrics {', '.join(sorted(missing))}")
    return names


def save_plot(reports, labels, names, metrics, output: Path) -> None:
    os.environ.setdefault("MPLCONFIGDIR", str(Path(__file__).resolve().parent / ".matplotlib"))
    import matplotlib
    matplotlib.use("Agg")
    import matplotlib.pyplot as plt

    size = 2 if len(metrics) == 4 else 1
    figure, axes = plt.subplots(size, size, figsize=(7 * size, 4 * size),
                                squeeze=False, layout="constrained")
    positions = range(1, len(names) + 1)
    for axis, metric in zip(axes.flat, metrics):
        for label, report in zip(labels, reports):
            values = [report[name].values[metric] for name in names]
            axis.plot(positions, values, label=label, linewidth=0.8,
                      marker="." if len(names) <= 50 else None)
        title = METRICS[metric] + (" (ms)" if metric == "cpu" else "")
        axis.set_title(title)
        axis.set_ylabel(title)
        axis.set_xlabel("Instance (ordered by path)")
        axis.set_ylim(bottom=0)
        axis.grid(alpha=0.25)
        axis.legend()
        if len(names) <= 15:
            axis.set_xticks(positions, [Path(name).name for name in names],
                            rotation=45, ha="right")
    output.parent.mkdir(parents=True, exist_ok=True)
    figure.savefig(output, dpi=180)
    plt.close(figure)


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("files", type=Path, nargs="+", help="DPLL result files")
    parser.add_argument("--metric", choices=("all", *METRICS), default="all",
                        help="One metric or all four (default)")
    parser.add_argument("--labels", nargs="+", help="One legend label per input file")
    parser.add_argument("-o", "--output", type=Path, default=Path("comparison.png"),
                        help="Output file: PNG, SVG or PDF (default: comparison.png)")
    args = parser.parse_args()
    labels = args.labels if args.labels is not None else [path.stem for path in args.files]
    if len(labels) != len(args.files) or len(set(labels)) != len(labels):
        parser.error("Provide one unique label per file using --labels")
    if args.output.resolve() in {path.resolve() for path in args.files}:
        parser.error("Output must not overwrite an input file")
    if args.output.suffix.lower() not in (".png", ".svg", ".pdf"):
        parser.error("Output must end in .png, .svg or .pdf")

    metrics = list(METRICS) if args.metric == "all" else [args.metric]
    try:
        reports = [read_results(path) for path in args.files]
        names = validate_results(reports, metrics)
        save_plot(reports, labels, names, metrics, args.output)
        print(f"Saved {args.output}")
    except (OSError, ValueError) as error:
        parser.error(str(error))
    except ImportError as error:
        parser.error(f"{error}. Install dependencies: python -m pip install -r requirements.txt")


if __name__ == "__main__":
    main()
