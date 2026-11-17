"""RAG local: Ollama embeddings + SQLite + MCP stdio. Python 3.10+, sin dependencias."""
from __future__ import annotations

import argparse
import hashlib
import json
import math
import os
import re
import sqlite3
import sys
import urllib.error
import urllib.request
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
DB = Path(os.environ.get("FACTURACION_RAG_DB", str(ROOT / "ai" / ".local" / "rag.sqlite3")))
OLLAMA = os.environ.get("OLLAMA_HOST", "http://localhost:11434").rstrip("/")
MODEL = os.environ.get("FACTURACION_EMBED_MODEL", "nomic-embed-text")
EXTENSIONS = {".md", ".txt", ".cs", ".csproj", ".slnx", ".xsd", ".xml", ".json"}
IGNORED = {"bin", "obj", ".git", ".vs", "node_modules", ".local", "artifacts", "TestResults"}
MAX_FILE_BYTES = 1_000_000
CHUNK_SIZE = 1600
OVERLAP = 240


def embed(texts: list[str]) -> list[list[float]]:
    data = json.dumps({"model": MODEL, "input": texts}).encode("utf-8")
    request = urllib.request.Request(
        OLLAMA + "/api/embed", data=data,
        headers={"Content-Type": "application/json"}, method="POST"
    )
    try:
        with urllib.request.urlopen(request, timeout=120) as response:
            vectors = json.load(response)["embeddings"]
    except (urllib.error.URLError, KeyError) as exc:
        raise RuntimeError(
            f"No se pudieron generar embeddings con Ollama ({MODEL}). "
            f"Ejecuta: ollama pull {MODEL}. Detalle: {exc}"
        ) from exc
    if len(vectors) != len(texts):
        raise RuntimeError("Ollama devolvió un número inesperado de embeddings")
    return vectors


def connect() -> sqlite3.Connection:
    DB.parent.mkdir(parents=True, exist_ok=True)
    conn = sqlite3.connect(DB)
    conn.execute("""CREATE TABLE IF NOT EXISTS chunks (
        path TEXT NOT NULL, ordinal INTEGER NOT NULL, sha256 TEXT NOT NULL,
        model TEXT NOT NULL, body TEXT NOT NULL, embedding TEXT NOT NULL,
        PRIMARY KEY(path, ordinal)
    )""")
    conn.execute("CREATE INDEX IF NOT EXISTS idx_chunks_path ON chunks(path)")
    return conn


def candidates() -> list[Path]:
    paths = []
    for base in (ROOT / "docs", ROOT / "src", ROOT / "test", ROOT / "ai" / "knowledge"):
        if not base.exists():
            continue
        for path in base.rglob("*"):
            if (not path.is_file() or path.suffix.lower() not in EXTENSIONS
                    or any(part in IGNORED for part in path.relative_to(ROOT).parts)
                    or path.is_symlink()):
                continue
            if path.stat().st_size > MAX_FILE_BYTES:
                continue
            # Never ingest secrets, private keys, appsettings or customer data.
            name = path.name.lower()
            if (name.startswith("appsettings") or any(x in name for x in
                    ("secret", "credential", "private", "certificate", ".env", "customer-data"))):
                continue
            paths.append(path)
    return sorted(paths)


def split_text(text: str) -> list[str]:
    # Preserve text exactly, including XSD/XML and code; overlap helps continuity.
    text = text.strip()
    if not text:
        return []
    chunks = []
    step = CHUNK_SIZE - OVERLAP
    for start in range(0, len(text), step):
        chunk = text[start:start + CHUNK_SIZE]
        if chunk.strip():
            chunks.append(chunk)
        if start + CHUNK_SIZE >= len(text):
            break
    return chunks


def index() -> None:
    with connect() as conn:
        existing = {r[0] for r in conn.execute("SELECT DISTINCT path FROM chunks")}
        seen = set()
        updated = 0
        for path in candidates():
            relative = path.relative_to(ROOT).as_posix()
            seen.add(relative)
            raw = path.read_bytes()
            if b"\x00" in raw:
                continue
            content = raw.decode("utf-8-sig", errors="replace")
            sha = hashlib.sha256(raw).hexdigest()
            previous = conn.execute(
                "SELECT sha256, model FROM chunks WHERE path=? LIMIT 1", (relative,)
            ).fetchone()
            if previous == (sha, MODEL):
                continue
            parts = split_text(content)
            vectors = []
            for i in range(0, len(parts), 8):
                vectors.extend(embed(parts[i:i + 8]))
            with conn:
                conn.execute("DELETE FROM chunks WHERE path=?", (relative,))
                conn.executemany(
                    "INSERT INTO chunks VALUES (?, ?, ?, ?, ?, ?)",
                    [(relative, i, sha, MODEL, part, json.dumps(vector))
                     for i, (part, vector) in enumerate(zip(parts, vectors))]
                )
            updated += 1
            print(f"Indexado: {relative} ({len(parts)} fragmentos)", file=sys.stderr)
        for removed in existing - seen:
            conn.execute("DELETE FROM chunks WHERE path=?", (removed,))
        conn.commit()
        total = conn.execute("SELECT COUNT(*) FROM chunks").fetchone()[0]
    print(f"Listo: {updated} archivos actualizados, {total} fragmentos.", file=sys.stderr)


def cosine(a: list[float], b: list[float]) -> float:
    if len(a) != len(b):
        return -1.0
    denominator = math.sqrt(sum(x*x for x in a)) * math.sqrt(sum(y*y for y in b))
    return sum(x*y for x, y in zip(a, b)) / denominator if denominator else 0.0


def search(query: str, limit: int = 5) -> list[dict]:
    if not query.strip():
        return []
    vector = embed([query])[0]
    with connect() as conn:
        rows = conn.execute(
            "SELECT path, ordinal, body, embedding FROM chunks WHERE model=?", (MODEL,)
        ).fetchall()
    scored = [
        {"source": path, "chunk": ordinal, "score": round(cosine(vector, json.loads(embedding)), 4),
         "text": body, "verification": "Consultar fuente oficial y vigencia antes de uso fiscal"}
        for path, ordinal, body, embedding in rows
    ]
    return sorted(scored, key=lambda r: r["score"], reverse=True)[:max(1, min(limit, 10))]


def reply(msg: dict) -> dict | None:
    method = msg.get("method")
    identifier = msg.get("id")
    if identifier is None:
        return None
    if method == "initialize":
        return {"jsonrpc": "2.0", "id": identifier, "result": {
            "protocolVersion": "2024-11-05",
            "capabilities": {"tools": {}},
            "serverInfo": {"name": "facturacion-rag", "version": "0.1.0"}}}
    if method == "ping":
        return {"jsonrpc": "2.0", "id": identifier, "result": {}}
    if method == "tools/list":
        return {"jsonrpc": "2.0", "id": identifier, "result": {"tools": [{
            "name": "search_fiscal_knowledge",
            "description": "Busca fragmentos locales de documentación fiscal y código C#; no verifica vigencia legal.",
            "inputSchema": {"type": "object", "properties": {
                "query": {"type": "string", "description": "Pregunta o requisito a investigar"},
                "limit": {"type": "integer", "minimum": 1, "maximum": 10, "default": 5}
            }, "required": ["query"]}
        }]}}
    if method == "tools/call":
        params = msg.get("params", {})
        if params.get("name") != "search_fiscal_knowledge":
            return {"jsonrpc": "2.0", "id": identifier,
                    "error": {"code": -32602, "message": "Herramienta desconocida"}}
        args = params.get("arguments") or {}
        try:
            matches = search(str(args.get("query", "")), int(args.get("limit", 5)))
            result = {"content": [{"type": "text", "text": json.dumps(matches, ensure_ascii=False)}]}
        except Exception as exc:
            result = {"content": [{"type": "text", "text": str(exc)}], "isError": True}
        return {"jsonrpc": "2.0", "id": identifier, "result": result}
    return {"jsonrpc": "2.0", "id": identifier,
            "error": {"code": -32601, "message": "Método no soportado"}}


def serve() -> None:
    # MCP stdio usa mensajes JSON-RPC, uno por línea. stdout reservado al protocolo.
    for line in sys.stdin:
        try:
            message = json.loads(line)
            response = reply(message)
            if response is not None:
                print(json.dumps(response, ensure_ascii=False), flush=True)
        except Exception as exc:
            print(f"Error MCP: {exc}", file=sys.stderr)


def main() -> None:
    parser = argparse.ArgumentParser(description="RAG local de facturación y C#")
    parser.add_argument("command", choices=("index", "search", "serve"))
    parser.add_argument("query", nargs="?")
    parser.add_argument("--limit", type=int, default=5)
    args = parser.parse_args()
    if args.command == "index":
        index()
    elif args.command == "search":
        if not args.query:
            parser.error("search requiere una pregunta")
        print(json.dumps(search(args.query, args.limit), ensure_ascii=False, indent=2))
    else:
        serve()


if __name__ == "__main__":
    main()
