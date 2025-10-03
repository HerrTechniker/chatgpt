from __future__ import annotations

import io
from http import HTTPStatus
from typing import Any

from flask import Flask, jsonify, request, send_file

from . import database
from . import encryption

app = Flask(__name__)


def _serialize_row(row: Any) -> dict[str, Any]:
    return {
        "id": row["id"],
        "filename": row["filename"],
        "content_type": row["content_type"],
        "metadata": row["metadata"],
    }


@app.before_first_request
def setup_database() -> None:
    database.initialize_database()


@app.post("/records")
def create_record() -> tuple[Any, int]:
    upload = request.files.get("file")
    if upload is None:
        return jsonify({"error": "Missing file upload"}), HTTPStatus.BAD_REQUEST

    filename = request.form.get("filename") or upload.filename or "untitled"
    metadata = request.form.get("metadata")
    content_type = request.form.get("content_type") or upload.mimetype or "application/octet-stream"

    raw_data = upload.read()
    if not raw_data:
        return jsonify({"error": "Uploaded file is empty"}), HTTPStatus.BAD_REQUEST

    encrypted_data = encryption.encrypt(raw_data)

    record_id = database.insert_record(filename, content_type, metadata, encrypted_data)

    return jsonify({"id": record_id}), HTTPStatus.CREATED


@app.get("/records")
def list_records() -> Any:
    rows = database.fetch_all_metadata()
    return jsonify([_serialize_row(row) for row in rows])


@app.get("/records/<int:record_id>")
def download_record(record_id: int):
    row = database.fetch_record_blob(record_id)
    if row is None:
        return jsonify({"error": "Record not found"}), HTTPStatus.NOT_FOUND

    decrypted = encryption.decrypt(row["encrypted_data"])

    return send_file(
        io.BytesIO(decrypted),
        mimetype=row["content_type"] or "application/octet-stream",
        as_attachment=True,
        download_name=row["filename"],
    )


@app.get("/records/<int:record_id>/metadata")
def fetch_metadata(record_id: int):
    row = database.fetch_metadata(record_id)
    if row is None:
        return jsonify({"error": "Record not found"}), HTTPStatus.NOT_FOUND
    return jsonify(_serialize_row(row))


if __name__ == "__main__":
    # Ensure the database exists when running the server directly
    database.initialize_database()
    app.run(host="0.0.0.0", port=8000)
