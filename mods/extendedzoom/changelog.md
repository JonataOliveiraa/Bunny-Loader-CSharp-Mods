## 2.0.1

The camera limits, ApplyPixelZoom, ZoomScale and PinchZoomMapEnabled use the typed API and its On_ hooks instead of raw hooks and calls by name.

## 2.0.0

Ported from JavaScript to C#. The camera limits, ApplyPixelZoom, get_ZoomScale and PinchZoomMapEnabled have no C# API, so they use raw hooks and calls by name. Missing game objects (no GUI or camera yet) now skip that frame instead of turning the map control off for the session.
