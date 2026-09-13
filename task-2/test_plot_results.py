"""Regression checks for report parsing and fair comparison of instances."""

from pathlib import Path
import tempfile
import unittest

from plot_results import Result, read_results, validate_results


class ReportTests(unittest.TestCase):
    def read(self, contents):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "results.txt"
            path.write_text(contents, encoding="utf-8")
            return read_results(path)

    def test_sat_model_and_decimal_comma(self):
        report = self.read("===== ./data/a.cnf =====\nSAT\nv 1 -2 0\nStatistics:\n"
                           "CPU time: 1,250 ms\nDecisions: 2\nUnit propagations: 3\n"
                           "Propagation clause checks: 4\n")
        self.assertEqual(report["data/a.cnf"], Result("SAT", {
            "cpu": 1.25, "decisions": 2, "propagations": 3, "checks": 4}))

    def test_single_unlabelled_output(self):
        self.assertEqual(self.read("UNSAT\nCPU time: 0.000 ms\n"),
                         {"<single instance>": Result("UNSAT", {"cpu": 0.0})})

    def test_duplicate_instance_rejected(self):
        with self.assertRaisesRegex(ValueError, "duplicate instance"):
            self.read("===== ./a.cnf =====\nSAT\n===== a.cnf =====\nSAT\n")

    def test_invalid_metric_rejected(self):
        for value in ("nan", "inf", "-1", "garbage"):
            with self.subTest(value=value), self.assertRaises(ValueError):
                self.read(f"SAT\nCPU time: {value} ms\n")

    def test_reordering_keeps_series_aligned(self):
        first = {"x/b": Result("SAT", {"cpu": 1}), "x/a": Result("SAT", {"cpu": 2})}
        second = {"x/a": Result("SAT", {"cpu": 0}), "x/b": Result("SAT", {"cpu": 9})}
        self.assertEqual(validate_results([first, second], ["cpu"]), ["x/a", "x/b"])

    def test_equal_count_with_different_instances_rejected(self):
        with self.assertRaisesRegex(ValueError, "different instances"):
            validate_results([{"a": Result("SAT", {"cpu": 1})},
                              {"b": Result("SAT", {"cpu": 1})}], ["cpu"])

    def test_incomplete_disagreeing_or_missing_metrics_rejected(self):
        good = {"a": Result("SAT", {"cpu": 1})}
        for result, message in ((Result(), "disagree"),
                                (Result("UNSAT", {"cpu": 1}), "disagree"),
                                (Result("SAT"), "missing metrics")):
            with self.subTest(result=result), self.assertRaisesRegex(ValueError, message):
                validate_results([good, {"a": result}], ["cpu"])
        with self.assertRaisesRegex(ValueError, "incomplete/unsolved"):
            validate_results([{"a": Result()}], ["cpu"])


if __name__ == "__main__":
    unittest.main()
