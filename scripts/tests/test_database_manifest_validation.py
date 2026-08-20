import importlib.util
import json
import tempfile
import unittest
from pathlib import Path

MODULE_PATH = Path(__file__).parents[1] / "database_manifest_validation.py"
SPEC = importlib.util.spec_from_file_location("database_manifest_validation", MODULE_PATH)
MODULE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(MODULE)


class DatabaseManifestValidationTests(unittest.TestCase):
    def make_repository(self, install, migrations, sources):
        temporary = tempfile.TemporaryDirectory()
        root = Path(temporary.name)
        (root / "database").mkdir()
        (root / "database/install-manifest.json").write_text(json.dumps(install))
        (root / "database/migration-manifest.json").write_text(json.dumps({"migrations": migrations}))
        for name, sql in sources.items():
            path = root / name
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_text(sql)
        self.addCleanup(temporary.cleanup)
        return root

    def test_rejects_alter_before_create_and_missing_install_migration(self):
        install = {"sections": [{"order": 1, "objects": [{"name": "plantaopro.patch", "source": "database/patch.sql"}]}]}
        migrations = [{"version": "patch", "source": "database/patch.sql", "installRequired": True},
                      {"version": "next", "source": "database/next.sql", "dependsOn": ["patch"], "installRequired": True}]
        root = self.make_repository(install, migrations, {"database/patch.sql": "ALTER TABLE plantaopro.missing ADD COLUMN value text;", "database/next.sql": ""})
        errors = MODULE.validate_database_manifests(root)
        self.assertTrue(any("precedes CREATE TABLE" in error for error in errors))
        self.assertTrue(any("missing from install manifest" in error for error in errors))

    def test_accepts_topological_create_then_alter(self):
        install = {"sections": [{"order": 1, "objects": [{"name": "plantaopro.base", "source": "database/base.sql"}]},
                                {"order": 2, "objects": [{"name": "plantaopro.patch", "source": "database/patch.sql", "dependsOn": ["plantaopro.base"]}]}]}
        migrations = [{"version": "patch", "source": "database/patch.sql", "installRequired": True}]
        root = self.make_repository(install, migrations, {"database/base.sql": "CREATE TABLE plantaopro.target(id int);", "database/patch.sql": "ALTER TABLE plantaopro.target ADD COLUMN value text;"})
        self.assertEqual([], MODULE.validate_database_manifests(root))

    def test_rejects_missing_dependency_for_cross_source_alter(self):
        install = {"sections": [{"order": 1, "objects": [{"name": "plantaopro.base", "source": "database/base.sql"}]},
                                {"order": 2, "objects": [{"name": "plantaopro.patch", "source": "database/patch.sql"}]}]}
        root = self.make_repository(install, [], {"database/base.sql": "CREATE TABLE plantaopro.target(id int);", "database/patch.sql": "ALTER TABLE plantaopro.target ADD COLUMN value text;"})
        self.assertTrue(any("without dependsOn plantaopro.base" in error for error in MODULE.validate_database_manifests(root)))


if __name__ == "__main__":
    unittest.main()
