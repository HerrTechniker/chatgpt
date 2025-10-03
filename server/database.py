from __future__ import annotations

import sqlite3
from contextlib import contextmanager
from pathlib import Path
from typing import Iterator

_DB_FILE = Path(__file__).resolve().parent / "secure_store.db"


def get_connection() -> sqlite3.Connection:
    connection = sqlite3.connect(_DB_FILE)
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
