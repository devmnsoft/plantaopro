import importlib.util
import unittest
from pathlib import Path


SCRIPT = Path(__file__).resolve().parents[1] / "check-csharp10-compatibility.py"
SPEC = importlib.util.spec_from_file_location("csharp10_gate", SCRIPT)
MODULE = importlib.util.module_from_spec(SPEC)
assert SPEC.loader is not None
SPEC.loader.exec_module(MODULE)


class CSharp10CompatibilityGateTests(unittest.TestCase):
    def test_detects_raw_string_literal_but_not_verbatim_escaped_quotes(self):
        self.assertIsNotNone(MODULE.RAW_STRING.search('const string sql = """select 1""";'))
        self.assertIsNone(MODULE.RAW_STRING.search('const string sql = @"select ""Id""";'))

    def test_rejects_newer_language_versions(self):
        for version in ("latest", "preview", "11", "12.0"):
            self.assertIsNotNone(
                MODULE.INVALID_LANG_VERSION.search(f"<LangVersion>{version}</LangVersion>")
            )
        self.assertIsNone(MODULE.INVALID_LANG_VERSION.search("<LangVersion>10</LangVersion>"))


if __name__ == "__main__":
    unittest.main()
