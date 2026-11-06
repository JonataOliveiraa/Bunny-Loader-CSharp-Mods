# lib

Local folder for `BunnyLoader.dll`, the reference the compiler uses. The file stays out
of Git: it is generated from the game and licensed with Bunny Loader.

To extract it from a Bunny Loader APK:

```bash
python tools/extract_api.py path/to/bunny-loader.apk
```

The `.dll` belongs to that APK's version. When you update the app, extract it again.
