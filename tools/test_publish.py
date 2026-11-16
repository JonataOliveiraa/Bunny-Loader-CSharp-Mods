import hashlib
import io
import json
import tempfile
import unittest
import zipfile
from pathlib import Path

import publish

UID = "fc731e9a-2844-433b-ade1-5a0ab23165c4"


def files(**manifest):
    m = {"uid": UID, "id": "hellomod", "name": "Minishark Turbo", "version": "2.0.0", "blVersion": 3} | manifest
    return {
        "manifest.json": json.dumps(m).encode(),
        f"{m['id']}.cs": b"class X {}",
        f"{m['id']}.csproj": b"<Project />",
        "port.json": b"{}",
        "Common/Thing.cs": b"class Y {}",
        "icon.png": b"png",
        "banner.png": b"png",
        "description.md": b"Hi ![shot](Assets/shot.png)",
        "description_pt-BR.md": b"Oi",
        "license.md": b"MIT",
        "authors/potato.png": b"png",
        "Assets/shot.png": b"png",
        "obj/project.assets.json": b"{}",
        ".editorconfig": b"",
    }


class PortTests(unittest.TestCase):
    def test_package_leaves_out_build_files_and_is_reproducible(self):
        port = publish.Port("hellomod", files())
        with zipfile.ZipFile(io.BytesIO(port.data)) as z:
            names = z.namelist()
        self.assertEqual(sorted(names), names)
        self.assertIn("hellomod.cs", names)
        self.assertIn("Common/Thing.cs", names)
        for left_out in ("hellomod.csproj", "port.json", "obj/project.assets.json", ".editorconfig"):
            self.assertNotIn(left_out, names)
        again = publish.Port("hellomod", files())
        self.assertEqual(port.sha256, again.sha256)
        self.assertEqual(hashlib.sha256(port.data).hexdigest(), port.sha256)

    def test_release_names(self):
        port = publish.Port("hellomod", files())
        self.assertEqual("mod-hellomod-v2.0.0", port.tag)
        self.assertEqual("https://github.com/JonataOliveiraa/Bunny-Loader-CSharp-Mods/releases/download/"
                         "mod-hellomod-v2.0.0/hellomod.bl", port.url)

    def test_showcase(self):
        port = publish.Port("hellomod", files())
        self.assertEqual(["manifest.json", "icon.png", "banner.png", "description.md", "license.md",
                          "description_pt-BR.md", "authors/potato.png", "Assets/shot.png"], port.showcase())

    def test_refuses_what_the_app_refuses(self):
        for manifest, message in (({"id": "better-reforge"}, "C# class name"),
                                  ({"blVersion": 2}, "blVersion"),
                                  ({"uid": UID.upper()}, "uid"),
                                  ({"version": "v2"}, "version")):
            with self.subTest(manifest=manifest), self.assertRaisesRegex(publish.Fail, message):
                publish.Port("x", files(**manifest))
        no_entry = files()
        del no_entry["hellomod.cs"]
        with self.assertRaisesRegex(publish.Fail, "hellomod.cs"):
            publish.Port("hellomod", no_entry)


class IndexTests(unittest.TestCase):
    def entry(self, port, published_at="2026-11-15T12:00:00Z", index=None):
        return publish.index_entry(index or {"mods": []}, port, port.showcase(), published_at)

    def test_entry_follows_the_contract(self):
        port = publish.Port("hellomod", files())
        entry = self.entry(port)
        self.assertEqual({"uid", "version", "download", "files", "manifest", "publishedAt"}, set(entry))
        self.assertEqual({"url": port.url, "sha256": port.sha256, "size": port.size}, entry["download"])
        self.assertEqual(json.loads(files()["manifest.json"]), entry["manifest"])

    def test_a_new_version_keeps_the_first_date(self):
        first = self.entry(publish.Port("hellomod", files()))
        index = {"mods": [first]}
        second = self.entry(publish.Port("hellomod", files(version="2.1.0")), "2026-12-01T00:00:00Z", index)
        self.assertEqual("2026-11-15T12:00:00Z", second["publishedAt"])

    def test_refuses_another_file_under_a_published_version_and_older_versions(self):
        index = {"mods": [self.entry(publish.Port("hellomod", files(version="2.1.0")))]}
        changed = files(version="2.1.0")
        changed["hellomod.cs"] = b"class Z {}"
        with self.assertRaisesRegex(publish.Fail, "already published"):
            publish.check_against_index(index, publish.Port("hellomod", changed))
        with self.assertRaisesRegex(publish.Fail, "older"):
            publish.check_against_index(index, publish.Port("hellomod", files(version="2.0.9")))
        publish.check_against_index(index, publish.Port("hellomod", files(version="2.1.0")))
        publish.check_against_index(index, publish.Port("hellomod", files(version="2.10.0")))

    def test_refuses_an_id_another_uid_uses(self):
        index = {"mods": [self.entry(publish.Port("hellomod", files()))]}
        other = files(uid="00000000-0000-0000-0000-000000000000")
        with self.assertRaisesRegex(publish.Fail, "already uses this id"):
            publish.check_against_index(index, publish.Port("hellomod", other))

    def test_saved_index_is_sorted_by_name(self):
        a = self.entry(publish.Port("hellomod", files(name="zeta")))
        b = self.entry(publish.Port("hellomod", files(name="Alpha", uid="00000000-0000-0000-0000-000000000000")))
        with tempfile.TemporaryDirectory() as folder:
            publish.save_index(Path(folder), {"mods": [a, b]})
            saved = json.loads((Path(folder) / "index.json").read_text(encoding="utf-8"))
        self.assertEqual(1, saved["format"])
        self.assertEqual(["Alpha", "zeta"], [m["manifest"]["name"] for m in saved["mods"]])


if __name__ == "__main__":
    unittest.main()
