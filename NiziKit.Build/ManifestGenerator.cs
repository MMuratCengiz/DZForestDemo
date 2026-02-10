namespace NiziKit.Build;

// ManifestGenerator is no longer needed.
// Asset packs are discovered at runtime:
//   - Published mode: binary .nizipack files are scanned from the assets directory
//   - Dev mode: asset files are scanned by extension from the assets directory
// The NiziPackBuilder handles creating binary packs at publish time.
