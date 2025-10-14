#!/usr/bin/env python3
"""
Aggregate BenchmarkDotNet results for Neo VM opcode benchmarks.

This script scans the BenchmarkDotNet artifacts directory, collects data from
all `*-report.csv` files, and produces consolidated CSV/JSON summaries that can
be fed into the gas-pricing derivation pipeline.
"""

from __future__ import annotations

import argparse
import csv
import json
import sys
from dataclasses import dataclass, asdict
from pathlib import Path
from typing import Iterable, List, Tuple


DEFAULT_ARTIFACT_DIR = Path("benchmarks/Neo.VM.Benchmarks/BenchmarkDotNet.Artifacts/results")
DEFAULT_CSV_OUTPUT = Path("benchmarks/results/benchmark_summary.csv")
DEFAULT_JSON_OUTPUT = Path("benchmarks/results/benchmark_summary.json")


@dataclass
class BenchmarkRow:
    benchmark: str
    method: str
    job: str
    params: str
    mean_ns: float
    error_ns: float
    stddev_ns: float
    allocated_bytes: float
    file: str


def parse_csv(file_path: Path) -> Iterable[BenchmarkRow]:
    with file_path.open(newline="", encoding="utf-8") as fh:
        reader = csv.DictReader(fh)
        for row in reader:
            try:
                mean = parse_time(row.get("Mean"))
                error = parse_time(row.get("Error"))
                stddev = parse_time(row.get("StdDev"))
                allocated = parse_size(row.get("Allocated"))
                yield BenchmarkRow(
                    benchmark=row.get("Benchmark") or row.get("Method") or "",
                    method=row.get("Method") or "",
                    job=row.get("Job") or "",
                    params=row.get("Params") or row.get("Parameters") or "",
                    mean_ns=mean,
                    error_ns=error,
                    stddev_ns=stddev,
                    allocated_bytes=allocated,
                    file=file_path.name,
                )
            except ValueError as exc:
                print(f"[WARN] Skipping row in {file_path}: {exc}", file=sys.stderr)


def ensure_parent(path: Path) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)


def write_csv(rows: List[BenchmarkRow], path: Path) -> None:
    ensure_parent(path)
    with path.open("w", newline="", encoding="utf-8") as fh:
        writer = csv.DictWriter(
            fh,
            fieldnames=[
                "benchmark",
                "method",
                "job",
                "params",
                "mean_ns",
                "error_ns",
                "stddev_ns",
                "allocated_bytes",
                "file",
            ],
        )
        writer.writeheader()
        for row in rows:
            writer.writerow(asdict(row))


def write_json(rows: List[BenchmarkRow], path: Path) -> None:
    ensure_parent(path)
    with path.open("w", encoding="utf-8") as fh:
        json.dump([asdict(row) for row in rows], fh, indent=2)


def parse_time(cell: str | None) -> float:
    if not cell:
        return 0.0
    value, unit = split_value_unit(cell)
    factor = {
        "ns": 1.0,
        "us": 1_000.0,
        "µs": 1_000.0,
        "μs": 1_000.0,
        "ms": 1_000_000.0,
        "s": 1_000_000_000.0,
    }.get(unit, 1.0)
    return value * factor


def parse_size(cell: str | None) -> float:
    if not cell:
        return 0.0
    value, unit = split_value_unit(cell)
    factor = {
        "B": 1.0,
        "KB": 1_024.0,
        "MB": 1_024.0 ** 2,
        "GB": 1_024.0 ** 3,
    }.get(unit, 1.0)
    return value * factor


def split_value_unit(cell: str) -> Tuple[float, str]:
    cleaned = cell.replace(",", "").strip()
    parts = cleaned.split()
    if len(parts) == 1:
        return float(parts[0]), ""
    value_str, unit = parts[0], parts[1]
    return float(value_str), unit


def main() -> None:
    parser = argparse.ArgumentParser(description="Aggregate BenchmarkDotNet opcode benchmark results.")
    parser.add_argument(
        "-i",
        "--input",
        type=Path,
        default=DEFAULT_ARTIFACT_DIR,
        help=f"BenchmarkDotNet results directory (default: {DEFAULT_ARTIFACT_DIR})",
    )
    parser.add_argument(
        "--csv",
        type=Path,
        default=DEFAULT_CSV_OUTPUT,
        help=f"CSV output path (default: {DEFAULT_CSV_OUTPUT})",
    )
    parser.add_argument(
        "--json",
        type=Path,
        default=DEFAULT_JSON_OUTPUT,
        help=f"JSON output path (default: {DEFAULT_JSON_OUTPUT})",
    )
    args = parser.parse_args()

    if not args.input.exists():
        print(f"[ERROR] Results directory not found: {args.input}", file=sys.stderr)
        sys.exit(1)

    rows: List[BenchmarkRow] = []
    for csv_file in sorted(args.input.glob("*-report.csv")):
        rows.extend(parse_csv(csv_file))

    if not rows:
        print(f"[WARN] No benchmark CSV files found in {args.input}", file=sys.stderr)
        sys.exit(1)

    write_csv(rows, args.csv)
    write_json(rows, args.json)

    print(f"✅ Aggregated {len(rows)} benchmark rows")
    print(f"   CSV : {args.csv}")
    print(f"   JSON: {args.json}")


if __name__ == "__main__":
    main()
