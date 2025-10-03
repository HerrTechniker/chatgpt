from __future__ import annotations

import os
import sqlite3
from pathlib import Path

_DB_FILE_ENV_VAR = "SECURE_STORE_DB"


def _default_db_path() -> Path:
    """Return the default location of the SQLite file."""

    return Path(__file__).resolve().parent / "secure_store.db"


def _db_file_path() -> Path:
    """Determine the SQLite file path.

    The location can be overridden by setting the ``SECURE_STORE_DB``
    environment variable to an absolute or relative path. Relative paths are
    resolved against the current working directory to make offline usage with
    custom storage locations straightforward.
    """

    env_path = os.getenv(_DB_FILE_ENV_VAR)
    if not env_path:
        return _default_db_path()

    candidate = Path(env_path).expanduser()
    if not candidate.is_absolute():
        candidate = (Path.cwd() / candidate).resolve()
    else:
        candidate = candidate.resolve()
    return candidate


def get_database_path() -> Path:
    """Public helper that exposes the resolved database path."""

    return _db_file_path()


def get_connection() -> sqlite3.Connection:
    connection = sqlite3.connect(_db_file_path())
    connection.row_factory = sqlite3.Row
    return connection


def initialize_database() -> None:
    with get_connection() as connection:
        connection.execute(
            """
            CREATE TABLE IF NOT EXISTS records (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                filename TEXT NOT NULL,
                content_type TEXT,
                metadata TEXT,
                encrypted_data BLOB NOT NULL
            )
            """
        )


def insert_record(
    filename: str, content_type: str, metadata: str | None, encrypted_data: bytes
) -> int:
    with get_connection() as connection:
        cursor = connection.execute(
            """
            INSERT INTO records (filename, content_type, metadata, encrypted_data)
            VALUES (?, ?, ?, ?)
            """,
            (filename, content_type, metadata, encrypted_data),
        )
        connection.commit()
        return int(cursor.lastrowid)


def fetch_metadata(record_id: int) -> sqlite3.Row | None:
    with get_connection() as connection:
        cursor = connection.execute(
            "SELECT id, filename, content_type, metadata FROM records WHERE id = ?",
            (record_id,),
        )
        return cursor.fetchone()


def fetch_all_metadata() -> list[sqlite3.Row]:
    with get_connection() as connection:
        cursor = connection.execute(
            "SELECT id, filename, content_type, metadata FROM records ORDER BY id"
        )
        return cursor.fetchall()


def fetch_record_blob(record_id: int) -> sqlite3.Row | None:
    with get_connection() as connection:
        cursor = connection.execute(
            """
            SELECT id, filename, content_type, encrypted_data
            FROM records
            WHERE id = ?
            """,
            (record_id,),
        )
        return cursor.fetchone()
