"""Semantic validation for the canonical database installation DAG."""
import json
import re
from pathlib import Path

CREATE_TABLE = re.compile(r"\bCREATE\s+TABLE\s+(?:IF\s+NOT\s+EXISTS\s+)?(?:plantaopro\.)?([a-zA-Z_]\w*)", re.I)
ALTER_TABLE = re.compile(r"\bALTER\s+TABLE\s+(?:IF\s+EXISTS\s+)?(?:ONLY\s+)?(?:plantaopro\.)?([a-zA-Z_]\w*)", re.I)


def executable_sql(sql: str) -> str:
    """Remove comments and string literals so examples/dynamic compatibility SQL are not statements."""
    sql = re.sub(r"/\*.*?\*/", " ", sql, flags=re.S)
    sql = re.sub(r"--[^\n]*", " ", sql)
    return re.sub(r"'(?:''|[^'])*'", "''", sql)


def validate_database_manifests(root: Path, install: dict | None = None) -> list[str]:
    install = install or json.loads((root / "database/install-manifest.json").read_text(encoding="utf-8"))
    migration = json.loads((root / "database/migration-manifest.json").read_text(encoding="utf-8"))
    errors: list[str] = []

    migration_versions = {item["version"] for item in migration["migrations"]}
    seen_versions: set[str] = set()
    for item in migration["migrations"]:
        for dependency in item.get("dependsOn", []):
            if dependency not in migration_versions:
                errors.append(f"migration {item['version']} depends on unknown migration {dependency}")
            elif dependency not in seen_versions:
                errors.append(f"migration {item['version']} appears before dependency {dependency}")
        seen_versions.add(item["version"])

    install_sources: set[str] = set()
    known_objects: set[str] = set()
    created_tables: set[str] = set()
    table_owners: dict[str, str] = {}
    for section in sorted(install["sections"], key=lambda value: value["order"]):
        for obj in section.get("objects", []):
            source = obj.get("source")
            sql = (root / source).read_text(encoding="utf-8") if source else obj.get("sql", "")
            object_name = obj.get("name", source)
            declared_dependencies = {value.lower() for value in obj.get("dependsOn", [])}
            for dependency in obj.get("dependsOn", []):
                normalized = dependency.lower()
                if normalized not in known_objects and normalized.removeprefix("plantaopro.") not in created_tables:
                    errors.append(f"install object {obj.get('name', source)} depends on unavailable object {dependency}")
            # Process executable statements in source order: an ALTER is valid only after its CREATE.
            sql = executable_sql(sql)
            events = [(match.start(), "create", match.group(1).lower()) for match in CREATE_TABLE.finditer(sql)]
            events += [(match.start(), "alter", match.group(1).lower()) for match in ALTER_TABLE.finditer(sql)]
            for _, event, table in sorted(events):
                if event == "alter" and table not in created_tables:
                    errors.append(f"{source or obj.get('name')}: ALTER TABLE plantaopro.{table} precedes CREATE TABLE")
                elif event == "alter" and table_owners.get(table) not in (None, object_name.lower()) and table_owners[table] not in declared_dependencies:
                    errors.append(f"install object {object_name} alters plantaopro.{table} without dependsOn {table_owners[table]}")
                elif event == "create":
                    created_tables.add(table)
                    table_owners.setdefault(table, object_name.lower())
            if source:
                install_sources.add(source)
            if obj.get("name"):
                known_objects.add(obj["name"].lower())

    for item in migration["migrations"]:
        if item.get("status", "active") == "active" and item.get("installRequired") and item["source"] not in install_sources:
            errors.append(f"active install-required migration missing from install manifest: {item['source']}")
    return errors
