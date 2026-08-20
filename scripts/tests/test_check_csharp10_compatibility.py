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
        self.assertIsNone(MODULE.INVALID_LANG_VERSION.search("<LangVersion>10.0</LangVersion>"))

    def test_detects_post_csharp10_class_and_collection_syntax(self):
        self.assertIsNotNone(MODULE.PRIMARY_CONSTRUCTOR.search("public sealed class Service(IDb db)"))
        invalid = (
            "var values = [];",
            "values ?? []",
            "return [];",
            "value => [value]",
            "var values = [one, two];",
            "Call([one, two]);",
            "Call(first, [second]);",
            "enabled ? [one] : [two]",
            "var nested = [[one], [two]];",
        )
        for source in invalid:
            with self.subTest(source=source):
                self.assertIsNotNone(MODULE.COLLECTION_EXPRESSION.search(source))

    def test_allows_csharp10_brackets(self):
        valid = (
            "string[] names = new string[0];",
            "Guid[] ids = Array.Empty<Guid>();",
            "[HttpGet]",
            "[Authorize(Roles = Roles.Admin)]",
            "public string this[int index] => values[index];",
            "var value = values[0];",
            "var values = new[] { one, two };",
            "var matrix = new int[2, 2];",
        )
        for source in valid:
            with self.subTest(source=source):
                self.assertIsNone(MODULE.COLLECTION_EXPRESSION.search(source))

    def test_detects_raw_css_directives_only_inside_razor_style_blocks(self):
        self.assertIsNotNone(MODULE.RAW_RAZOR_CSS_DIRECTIVE.search("<style>@media(max-width: 1px){}</style>"))
        self.assertIsNotNone(MODULE.RAW_RAZOR_CSS_DIRECTIVE.search("<style>@supports(display:grid){}</style>"))
        self.assertIsNone(MODULE.RAW_RAZOR_CSS_DIRECTIVE.search("<style>@@media(max-width: 1px){}</style>"))
        self.assertIsNone(MODULE.RAW_RAZOR_CSS_DIRECTIVE.search("@media is ordinary Razor text here"))


if __name__ == "__main__":
    unittest.main()
