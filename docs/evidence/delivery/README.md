# Delivery evidence captures

Captured 2026-11-22 with Playwright and local Chromium from the real Vue application at `http://127.0.0.1:5174/`, connected through the Vite proxy to a local ASP.NET Core Mock-mode API at `http://127.0.0.1:5081/`. This isolated capture mode was selected so no tenant values appeared in screenshots or video. The API response contained all 98 catalog settings with unset synthetic values, and its write flag was false. The app displayed its explicit synthetic-data notice. No SQL database was contacted during these media captures; the separately running SQL-backed app was checked read-only through `http://127.0.0.1:5173/` / `http://127.0.0.1:5080/` and returned 98 settings with `ReadOnly` capabilities.

| Artifact | Contents |
|---|---|
| `01-main-page-en-readonly.png` | English main page, connected synthetic mode, all 98 loaded, read-only status. |
| `02-language-turkish.png` | Turkish language selection. |
| `03-global-search-technical-id.png` | Global search by original technical control ID. |
| `04-bitmask-pending-draft-readonly.png` | Multi-select bitmask editor and pending local draft; no save action is available. |
| `05-modules-admin-separate-controls.png` | Separate Modules and Admin controls with their independent mapping evidence. |
| `06-synthetic-image-preview-local-only.png` | Browser-local synthetic image preview; not uploaded. |
| `07-image-draft-removed.png` | Discarded image draft and removed preview. |
| `b2b-store-settings-demo.mp4` | 10-second H.264 MP4 converted from Playwright recording. |
| `b2b-store-settings-demo.webm` | Original 10-second Playwright WebM recording. |

Screenshots and both video files were checked for nonzero size. The MP4 was decoded through ffmpeg without errors. Browser console page errors: zero.

**Capture limitation:** no Save button or server-side validation response is exposed in normal read-only mode, so save validation feedback could not be shown without enabling writes. The pending-draft capture instead shows that changes remain drafts and the save control is absent. To capture validation feedback later, use an approved isolated test database, explicitly enabled local test mode, a backed-up/restorable test row, and a safe invalid-value request; never use production or a public demo.
