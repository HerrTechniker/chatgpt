# Encrypted Database System

This repository contains a minimal encrypted database system for securely storing files (such as images) and associated metadata. The system exposes a simple REST API, implemented with Flask, that encrypts data using the AES-based [`cryptography`](https://cryptography.io/) `Fernet` algorithm before persisting the records to a SQLite database.

## Overview

* **Encryption**: AES-128 in CBC mode with PKCS7 padding via Fernet. The secret key must be provided as a base64-encoded string via the `ENCRYPTION_KEY` environment variable.
* **Storage**: SQLite database (`server/secure_store.db`) that stores encrypted blobs along with metadata (filename, content type, and arbitrary text metadata).
* **API**:
  * `POST /records` – Upload a new file along with optional metadata. Returns the numeric record id.
  * `GET /records` – List metadata for all stored files.
  * `GET /records/<id>` – Download the decrypted file.
  * `GET /records/<id>/metadata` – Retrieve metadata for a specific record.

## Getting Started

1. Create and activate a virtual environment (optional but recommended).
2. Install dependencies:

   ```bash
   pip install -r server/requirements.txt
   ```

3. Generate an encryption key (only once). You can use the helper script:

   ```bash
   python -m server.encryption --generate
   ```

   Save the printed key somewhere safe. Set it as an environment variable before running the server:

   ```bash
   export ENCRYPTION_KEY="<your generated key>"
   ```

4. Start the server:

   ```bash
   python -m server.app
   ```

   The API listens on `http://0.0.0.0:8000`. Ensure that the server machine is reachable via Ethernet or the desired network interface.

## Using the API

Below are example `curl` commands for interacting with the API.

### Upload a file

```bash
curl -X POST \
     -F "file=@/path/to/image.png" \
     -F "filename=custom-name.png" \
     -F "metadata={\"description\": \"Sample image\"}" \
     -F "content_type=image/png" \
     http://localhost:8000/records
```

### List all records

```bash
curl http://localhost:8000/records
```

### Download a specific record

```bash
curl -o output.png http://localhost:8000/records/<id>
```

### Retrieve metadata only

```bash
curl http://localhost:8000/records/<id>/metadata
```

## Security Considerations

* Protect the `ENCRYPTION_KEY` value. Anyone with this key can decrypt stored data.
* Enable HTTPS (e.g., via a reverse proxy such as Nginx with TLS certificates) to protect data in transit.
* Consider integrating authentication/authorization for production deployments.
* Regularly back up `server/secure_store.db` and ensure backup security.
