# Верста

Single-page site that calculates the road distance between any two points in Russia (city, address or a point clicked on the map) and the trip cost at a price per km. The UI is in Russian, minimal and focused on that one job.

## Files

- `index.html` is the entire app: markup, CSS and JS in one file. No build step, no package manager.
- `.claude/launch.json` holds a local static server config (`python3 -m http.server 8765`) for previewing.

Opening `index.html` directly from disk (`file://`) must keep working, since that is how the owner uses it. Test both ways.

## External services (all free, no API keys)

| Job | Service | Endpoint |
|---|---|---|
| Address / city search | Photon (komoot) | `https://photon.komoot.io/api/?q=…&bbox=19,41,180,82` |
| Reverse geocoding (map clicks, dragged pins) | Photon | `https://photon.komoot.io/reverse?lat=…&lon=…` |
| Routing, km and travel time | OSRM public demo | `https://router.project-osrm.org/route/v1/driving/…?overview=full&geometries=geojson` |
| Map rendering | MapLibre GL 4.7.1 (unpkg) + OpenFreeMap vector styles | `https://tiles.openfreemap.org/styles/liberty` (light), `/styles/dark` (dark) |

Fonts come from Google Fonts: Unbounded for display, Onest for body text.

## How it works

- `CITIES` is a built-in list of about 117 major Russian cities with coordinates. It gives instant suggestions. Photon results are merged in after a 300 ms debounce, filtered to `countrycode === "RU"`.
- `toPlace(feature)` turns a Photon feature into `{name, region, full, lat, lon}`. Addresses show as "street, house, town".
- `state = {from, to, route, round}`. `update(fit)` fetches the route, then `render()` (numbers) and `drawMap(fit)` (markers and line).
- `route.kind` is `"road"` (OSRM answered), `"noroad"` (OSRM `NoRoute`, e.g. Kamchatka) or `"approx"` (OSRM failed, so the straight line × 1.25 is shown as a dashed line with a note).
- The total is `round(km) × price × (round trip ? 2 : 1)`, formatted with `Intl.NumberFormat("ru-RU")`.
- Map: clicking opens a popup with "Отсюда" / "Сюда" buttons. Both markers are draggable. Both paths go through `setPoint(which, lat, lon)`, which reverse-geocodes the point to a readable label.
- The route line is drawn in the `route-casing` and `route-line` layers, inserted below the first symbol layer so labels stay on top. Map labels are switched to `name:ru`, but road shields (`ref`) are left alone.
- `localStorage` (`versta:*` keys, wrapped in try/catch) remembers the last points, the price and the round-trip toggle.
- "Скопировать расчёт" copies a multi-line report. It uses a synchronous `execCommand("copy")` fallback first because `navigator.clipboard` is unreliable on `file://`.

## Lessons learned (don't repeat these)

- **Basemap freshness matters.** Esri World Street Map is effectively frozen and lacks roads like M-12 (opened 2023), so correct routes looked like they cut through forest. Use a basemap built from current OSM data, like OpenFreeMap.
- CARTO basemaps now require an API key and show "API KEY REQUIRED" tiles.
- `tile.openstreetmap.org` requires a Referer header, and `file://` pages don't send one. Avoid it.
- OSRM `overview=simplified` gives about 27 points for Moscow → Kazan, which draws as straight chords that look like a railway. Keep `overview=full`.
- Leaflet/MapLibre: fit the map *after* the container has its final size. A `ResizeObserver` on `#map` handles this. Don't animate fits.
- A claude.ai Artifact cannot host this app because its sandbox blocks all fetch, XHR and tile requests to other hosts. An offline variant would need a precomputed distance matrix, which limits it to a fixed city list. The owner wants arbitrary addresses, so the app stays a regular static site.
- OSRM's demo server can be slow or rate-limited, and some routes cross other countries (Kaliningrad goes via Lithuania). Toll roads (M-11, M-12) are not excluded and their tolls aren't added.

## Conventions

- Keep it a single, dependency-light HTML file unless the owner asks otherwise.
- Write all UI copy in Russian: short, plain and specific.
- Define colors as tokens on `:root` with dark-mode overrides via `prefers-color-scheme` and `[data-theme]`. Never hard-code a color that works in only one theme.
- Layout: control column on the left with the map filling the rest. Below 820 px it stacks, with the map at 62vh.
- Check changes in a real browser: search, a route, a map click, a pin drag, copy, and the phone width.

## Hosting

Not published yet; the owner wants it local for now. GitHub CLI is logged in as `kh-mitya`, so GitHub Pages is the easy option when asked. Any static host works.
