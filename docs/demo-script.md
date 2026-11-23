# Demonstration script

## Public synthetic frontend showcase (about 1–2 minutes)

1. Open https://talhaantep.github.io/B2B/.
2. Point out the explicit synthetic/demo and read-only framing.
3. Browse categories and demonstrate search by a technical ID or setting label.
4. Switch EN/TR and show that labels change while the setting catalogue remains the same.
5. Open a bitmask/multi-select control and explain that changes are browser-only drafts; there is no Save action and no server persistence.
6. State clearly that this public static page makes no API or SQL connection and does not contain real dealer data.

## Local full-stack implementation (only on the approved workstation)

1. Start the local ASP.NET Core API and Vue dev server using the configuration instructions in `README.md`.
2. Select **Connect** and show the 98 settings loaded from the isolated local `Store5_Settings_Test` database. Do not expose raw values or credentials in a recording.
3. Show normal mode is read-only and that the isolated Development write mode is not enabled by default.
4. Explain the fixed Development-only API allowlist, app-owned extension settings and asset-storage limitations.
5. Only if an approved local test is explicitly required, follow the backup/readback/restoration procedure in the test reports. Do not perform this during a public demo.

The existing `docs/evidence/delivery/b2b-store-settings-demo.mp4` is a 10.36-second synthetic Mock-mode UI recording. It is not evidence of a live SQL save or full-stack hosted deployment.
