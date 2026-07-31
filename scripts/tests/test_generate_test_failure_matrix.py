import json
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest


SCRIPT = Path(__file__).parents[1] / "generate-test-failure-matrix.py"


def trx(failed: int, outcome: str = "Failed") -> str:
    return f'''<?xml version="1.0" encoding="utf-8"?>
<TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010">
  <TestDefinitions><UnitTest id="1" name="Suite.Teste"><TestMethod codeBase="tests.dll" /></UnitTest></TestDefinitions>
  <Results><UnitTestResult testId="1" testName="Suite.Teste" outcome="{outcome}">
    <Output><ErrorInfo><Message>falha real</Message></ErrorInfo></Output>
  </UnitTestResult></Results>
  <ResultSummary outcome="{outcome}"><Counters total="1" executed="1" passed="{1-failed}" failed="{failed}" /></ResultSummary>
</TestRun>'''


class FailureMatrixTests(unittest.TestCase):
    def test_records_build_failure_without_attempting_to_parse_trx(self):
        with tempfile.TemporaryDirectory() as directory:
            output = Path(directory) / "matrix.json"
            subprocess.run(
                [sys.executable, str(SCRIPT), "--build-failed", "--output", str(output)],
                check=True,
            )
            matrix = json.loads(output.read_text(encoding="utf-8"))
            self.assertEqual("BUILD_FAILED", matrix["status"])
            self.assertEqual(0, matrix["execution"]["executed"])
            self.assertEqual([], matrix["failures"])

    def test_generates_rows_from_trx_and_reconciles_final_result(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            baseline, final, output = root / "baseline.trx", root / "final.trx", root / "matrix.json"
            baseline.write_text(trx(1), encoding="utf-8")
            final.write_text(trx(0, "Passed"), encoding="utf-8")
            subprocess.run(
                [sys.executable, str(SCRIPT), str(baseline), "--final-trx", str(final), "--output", str(output)],
                check=True,
            )
            matrix = json.loads(output.read_text(encoding="utf-8"))
            self.assertEqual(1, matrix["execution"]["failed"])
            self.assertEqual("Suite.Teste", matrix["failures"][0]["test"])
            self.assertEqual("PASSED", matrix["failures"][0]["finalResult"])
            self.assertEqual("falha real", matrix["failures"][0]["message"])

    def test_rejects_a_placeholder_or_empty_execution(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            source, output = root / "empty.trx", root / "matrix.json"
            source.write_text(trx(0, "Passed").replace('total="1" executed="1"', 'total="0" executed="0"'), encoding="utf-8")
            result = subprocess.run(
                [sys.executable, str(SCRIPT), str(source), "--output", str(output)],
                capture_output=True,
                text=True,
            )
            self.assertNotEqual(0, result.returncode)
            self.assertFalse(output.exists())


if __name__ == "__main__":
    unittest.main()
