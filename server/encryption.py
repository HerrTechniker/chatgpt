from __future__ import annotations

import base64
import os
from typing import Optional

from cryptography.fernet import Fernet


_KEY_ENV_VAR = "ENCRYPTION_KEY"


def load_key(key: Optional[str] = None) -> bytes:
    """Return the encryption key as bytes.

    If ``key`` is provided it is used directly. Otherwise the function attempts to
    read the key from the ``ENCRYPTION_KEY`` environment variable.
    """
    key_to_use = key or os.getenv(_KEY_ENV_VAR)
    if not key_to_use:
        raise RuntimeError(
            "Encryption key is not set. Provide a key explicitly or set the "
            f"{_KEY_ENV_VAR} environment variable."
        )
    if isinstance(key_to_use, bytes):
        return key_to_use
    return key_to_use.encode("utf-8")


def encrypt(data: bytes, key: Optional[str] = None) -> bytes:
    """Encrypt ``data`` using the provided key.

    The key must be a URL safe base64-encoded 32-byte key as required by
    :class:`cryptography.fernet.Fernet`.
    """
    fernet = Fernet(load_key(key))
    return fernet.encrypt(data)


def decrypt(token: bytes, key: Optional[str] = None) -> bytes:
    """Decrypt ``token`` and return the original bytes."""
    fernet = Fernet(load_key(key))
    return fernet.decrypt(token)


def generate_key() -> str:
    """Generate a new key and return it as a string.

    ``Fernet`` keys are already URL-safe base64-encoded 32-byte values.
    """
    return Fernet.generate_key().decode("utf-8")


def write_key_to_file(path: str) -> str:
    """Generate a key and write it to ``path``.

    The function returns the generated key to allow printing it for the user.
    """
    key = generate_key()
    with open(path, "w", encoding="utf-8") as key_file:
        key_file.write(key)
    return key


if __name__ == "__main__":
    import argparse

    parser = argparse.ArgumentParser(description="Generate or display Fernet keys.")
    parser.add_argument(
        "--generate", action="store_true", help="Generate a new key and print it"
    )
    parser.add_argument(
        "--write", metavar="PATH", help="Generate a new key and write it to PATH"
    )

    args = parser.parse_args()

    if args.write:
        new_key = write_key_to_file(args.write)
        print(f"Key written to {args.write}. Value: {new_key}")
    elif args.generate:
        print(generate_key())
    else:
        env_key = os.getenv(_KEY_ENV_VAR)
        if env_key:
            print(f"Current key from {_KEY_ENV_VAR}: {env_key}")
        else:
            print(
                "No key set. Use --generate to create one or set the "
                f"{_KEY_ENV_VAR} environment variable."
            )
