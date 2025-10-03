"""Command line utilities for interacting with the encrypted store offline."""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path
from typing import Any

from . import database, encryption


def _load_metadata(metadata: str | None, metadata_file: str | None) -> str | None:
    if metadata and metadata_file:
        raise ValueError("Provide metadata via either --metadata or --metadata-file, not both.")

    if metadata_file:
        data = Path(metadata_file).read_text(encoding="utf-8")
        # Validate JSON to ensure consistent formatting when stored.
        json.loads(data)
        return data

    if metadata:
        # Ensure provided metadata is valid JSON.
        json.loads(metadata)
        return metadata

    return None


def _cmd_init_database(_: argparse.Namespace) -> None:
    database.initialize_database()
    db_path = database.get_database_path()
    print(f"Database initialized at {db_path}")


def _cmd_add(args: argparse.Namespace) -> None:
    file_path = Path(args.file)
    if not file_path.is_file():
        raise SystemExit(f"File not found: {file_path}")

    payload = file_path.read_bytes()
    if not payload:
        raise SystemExit("Cannot store empty files.")

    filename = args.filename or file_path.name
    try:
        metadata = _load_metadata(args.metadata, args.metadata_file)
    except ValueError as exc:
        raise SystemExit(str(exc)) from exc
    encrypted = encryption.encrypt(payload)
    record_id = database.insert_record(
        filename=filename,
        content_type=args.content_type,
        metadata=metadata,
        encrypted_data=encrypted,
    )
    print(f"Stored record {record_id} ({filename})")


def _cmd_list(_: argparse.Namespace) -> None:
    rows = database.fetch_all_metadata()
    for row in rows:
        metadata_preview: str | None = row["metadata"]
        if metadata_preview and len(metadata_preview) > 60:
            metadata_preview = metadata_preview[:57] + "..."
        print(
            f"id={row['id']} filename={row['filename']} content_type={row['content_type']} "
            f"metadata={metadata_preview}"
        )


def _cmd_get(args: argparse.Namespace) -> None:
    row = database.fetch_record_blob(args.record_id)
    if row is None:
        raise SystemExit(f"Record {args.record_id} not found")

    decrypted = encryption.decrypt(row["encrypted_data"])
    output_path = Path(args.output)
    output_path.write_bytes(decrypted)
    print(f"Record {args.record_id} written to {output_path}")


def _cmd_metadata(args: argparse.Namespace) -> None:
    row = database.fetch_metadata(args.record_id)
    if row is None:
        raise SystemExit(f"Record {args.record_id} not found")

    metadata = row["metadata"]
    if metadata:
        parsed: Any = json.loads(metadata)
        json.dump(parsed, sys.stdout, indent=2)
        print()
    else:
        print("{}")


def _build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description="Offline encrypted store utilities")
    subparsers = parser.add_subparsers(dest="command", required=True)

    init_parser = subparsers.add_parser("init", help="Create the database file if needed")
    init_parser.set_defaults(func=_cmd_init_database)

    add_parser = subparsers.add_parser("add", help="Encrypt and store a file")
    add_parser.add_argument("file", help="Path to the file to store")
    add_parser.add_argument(
        "--filename",
        help="Optional filename to record (defaults to the uploaded filename)",
    )
    add_parser.add_argument(
        "--content-type",
        default="application/octet-stream",
        help="MIME type for the stored file",
    )
    add_parser.add_argument("--metadata", help="JSON string with metadata")
    add_parser.add_argument(
        "--metadata-file",
        help="Path to a JSON file containing metadata",
    )
    add_parser.set_defaults(func=_cmd_add)

    list_parser = subparsers.add_parser("list", help="Display stored record metadata")
    list_parser.set_defaults(func=_cmd_list)

    get_parser = subparsers.add_parser("get", help="Decrypt a stored record to disk")
    get_parser.add_argument("record_id", type=int, help="Record identifier to retrieve")
    get_parser.add_argument("--output", required=True, help="Destination file path")
    get_parser.set_defaults(func=_cmd_get)

    metadata_parser = subparsers.add_parser("metadata", help="Show metadata for a record")
    metadata_parser.add_argument("record_id", type=int, help="Record identifier")
    metadata_parser.set_defaults(func=_cmd_metadata)

    return parser


def main(argv: list[str] | None = None) -> None:
    database.initialize_database()
    parser = _build_parser()
    args = parser.parse_args(argv)
    args.func(args)


if __name__ == "__main__":
    main()
