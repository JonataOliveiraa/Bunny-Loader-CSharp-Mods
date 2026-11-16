import json
import tempfile
import unittest
from pathlib import Path

import download_stats

BASE = "https://github.com/JonataOliveiraa/Bunny-Loader-CSharp-Mods/releases/download/"


def mod(uid, mod_id, tag, name=None):
    return {"uid": uid, "manifest": {"id": mod_id, "name": name or mod_id},
            "download": {"url": f"{BASE}{tag}/{mod_id}.bl"}}


def release(tag, *counts, name="x.bl"):
    return {"tag_name": tag, "assets": [{"name": name, "download_count": c} for c in counts]}


class CountTests(unittest.TestCase):
    def test_tag_id(self):
        self.assertEqual("hellomod", download_stats.tag_id("mod-hellomod-v2.0.0"))
        self.assertEqual("Big_Mod2", download_stats.tag_id("mod-Big_Mod2-v1.0.0-beta.1"))
        self.assertIsNone(download_stats.tag_id("v0.1.0-dev.20261006"))
        self.assertIsNone(download_stats.tag_id("mod-hellomod"))

    def test_every_version_adds_up_and_only_bl_counts(self):
        index = {"mods": [mod("a", "consolaria", "mod-consolaria-v2.2.5"), mod("b", "chestpeek", "mod-chestpeek-v2.0.0")]}
        releases = [
            release("mod-consolaria-v2.2.5", 14),
            release("mod-consolaria-v2.2.4", 23),
            release("mod-consolaria-v2.2.4", 99, name="source.zip"),
            release("mod-other-v1.0.0", 50),
        ]
        self.assertEqual({"a": 37, "b": 0}, download_stats.count(index, releases))

    def test_an_id_change_still_counts_the_old_releases(self):
        index = {"mods": [mod("a", "oldname", "mod-oldname-v1.0.0") | {"manifest": {"id": "newname"}}]}
        releases = [release("mod-oldname-v1.0.0", 2), release("mod-newname-v1.1.0", 3)]
        self.assertEqual({"a": 5}, download_stats.count(index, releases))


class PopularTests(unittest.TestCase):
    def test_top_ten_most_first_ties_by_name_and_no_zeros(self):
        index = {"mods": [mod(f"u{i:02}", f"m{i}", f"mod-m{i}-v1", name=f"Mod {chr(77 - i)}") for i in range(13)]}
        downloads = {f"u{i:02}": (5 if i < 4 else 20 + i) for i in range(13)}
        downloads["u12"] = 0
        top = download_stats.popular(index, downloads)
        self.assertEqual(10, len(top))
        self.assertEqual(["u11", "u10", "u09", "u08", "u07", "u06", "u05", "u04"], [m["uid"] for m in top[:8]])
        # u00..u03 tie at 5: by name, "Mod J" (u03) and "Mod K" (u02) come first.
        self.assertEqual(["u03", "u02"], [m["uid"] for m in top[8:]])
        self.assertEqual({"uid": "u11", "downloads": 31}, top[0])

    def test_nothing_downloaded_is_an_empty_list(self):
        self.assertEqual([], download_stats.popular({"mods": [mod("a", "x", "mod-x-v1")]}, {"a": 0}))


class WriteTests(unittest.TestCase):
    def test_same_numbers_do_not_rewrite_the_file(self):
        with tempfile.TemporaryDirectory() as folder:
            target = Path(folder) / "stats.json"
            self.assertTrue(download_stats.write(target, "downloads", {"a": 1}))
            first = target.read_text(encoding="utf-8")
            self.assertFalse(download_stats.write(target, "downloads", {"a": 1}))
            self.assertEqual(first, target.read_text(encoding="utf-8"))
            self.assertTrue(download_stats.write(target, "downloads", {"a": 2}))
            data = json.loads(target.read_text(encoding="utf-8"))
            self.assertEqual({"format", "updatedAt", "downloads"}, set(data))
            self.assertEqual({"a": 2}, data["downloads"])

    def test_an_empty_list_is_written_once(self):
        with tempfile.TemporaryDirectory() as folder:
            target = Path(folder) / "popular.json"
            self.assertTrue(download_stats.write(target, "mods", []))
            self.assertFalse(download_stats.write(target, "mods", []))


if __name__ == "__main__":
    unittest.main()
