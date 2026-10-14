#!/usr/bin/env python3
"""Generate missing Unity .meta files under Assets/.

Unity normally writes .meta files itself, but code and assets authored outside
the Editor (e.g. by Claude Code in a headless container) need them too: scenes
and assets reference each other by GUID, and committing stable GUIDs keeps
every clone of the repo identical.

GUIDs are deterministic: md5("pachacuti/" + <path relative to project root>),
so a file always gets the same GUID wherever this script runs. Existing .meta
files are never touched.

Usage:  python3 tools/gen_meta.py            # from the project root
        python3 tools/gen_meta.py --guid Assets/Pachacuti/Scripts/Runtime/SliceDirector.cs
"""
import hashlib
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

TAIL = "  userData: \n  assetBundleName: \n  assetBundleVariant: \n"


def guid_for(rel_path: str) -> str:
    return hashlib.md5(("pachacuti/" + rel_path.replace(os.sep, "/")).encode("utf-8")).hexdigest()


def importer_block(rel_path: str, is_dir: bool) -> str:
    if is_dir:
        return "folderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n" + TAIL
    ext = os.path.splitext(rel_path)[1].lower()
    if ext == ".cs":
        return ("MonoImporter:\n  externalObjects: {}\n  serializedVersion: 2\n  defaultReferences: []\n"
                "  executionOrder: 0\n  icon: {instanceID: 0}\n" + TAIL)
    if ext == ".asmdef":
        return "AssemblyDefinitionImporter:\n  externalObjects: {}\n" + TAIL
    if ext == ".shader":
        return ("ShaderImporter:\n  externalObjects: {}\n  defaultTextures: []\n  nonModifiableTextures: []\n"
                "  preprocessorOverride: 0\n" + TAIL)
    if ext == ".hlsl":
        return "ShaderIncludeImporter:\n  externalObjects: {}\n" + TAIL
    if ext == ".mat":
        return "NativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 2100000\n" + TAIL
    if ext == ".asset":
        return "NativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 11400000\n" + TAIL
    if ext in (".txt", ".csv", ".json", ".bytes", ".md", ".xml", ".yaml"):
        return "TextScriptImporter:\n  externalObjects: {}\n" + TAIL
    if ext in (".ttf", ".otf"):
        name = os.path.splitext(os.path.basename(rel_path))[0].split("-")[0]
        return ("TrueTypeFontImporter:\n  externalObjects: {}\n  serializedVersion: 4\n  fontSize: 16\n"
                "  forceTextureCase: -2\n  characterSpacing: 0\n  characterPadding: 1\n  includeFontData: 1\n"
                f"  fontNames:\n  - {name}\n  fallbackFontReferences: []\n  customCharacters: \n"
                "  fontRenderingMode: 0\n  ascentCalculationMode: 1\n  useLegacyBoundsCalculation: 0\n"
                "  shouldRoundAdvanceValue: 1\n" + TAIL)
    # .unity scenes and anything else
    return "DefaultImporter:\n  externalObjects: {}\n" + TAIL


def write_meta(abs_path: str) -> bool:
    meta = abs_path + ".meta"
    if os.path.exists(meta):
        return False
    rel = os.path.relpath(abs_path, ROOT)
    body = f"fileFormatVersion: 2\nguid: {guid_for(rel)}\n" + importer_block(rel, os.path.isdir(abs_path))
    with open(meta, "w", newline="\n") as f:
        f.write(body)
    return True


def main() -> int:
    if len(sys.argv) == 3 and sys.argv[1] == "--guid":
        print(guid_for(sys.argv[2]))
        return 0
    assets = os.path.join(ROOT, "Assets")
    created = 0
    for dirpath, dirnames, filenames in os.walk(assets):
        dirnames[:] = [d for d in dirnames if not d.startswith(".") and not d.endswith("~")]
        for d in dirnames:
            created += write_meta(os.path.join(dirpath, d))
        for f in filenames:
            if f.endswith(".meta") or f.startswith("."):
                continue
            created += write_meta(os.path.join(dirpath, f))
    # Orphaned metas (asset deleted) confuse Unity; report them.
    for dirpath, _, filenames in os.walk(assets):
        for f in filenames:
            if f.endswith(".meta") and not os.path.exists(os.path.join(dirpath, f[:-5])):
                print("orphan meta:", os.path.relpath(os.path.join(dirpath, f), ROOT))
    print(f"created {created} .meta file(s)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
