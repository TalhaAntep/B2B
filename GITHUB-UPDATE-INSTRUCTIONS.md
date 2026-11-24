# GitHub update instructions

This archive is a full repository snapshot intended to be extracted over a local clone of `https://github.com/TalhaAntep/B2B`.

1. Clone or open the existing repository locally and switch to the `main` branch.
2. Extract this archive into the repository root, allowing project files to be overwritten. Do not copy the outer archive folder as a nested subfolder.
3. Review `git status` and confirm no `.bak`, `.env`, `settings.local.json`, SQL connection string, bearer token, or hosting secret is staged.
4. Commit and push:

```powershell
git status
git add .
git commit -m "Update MonsterASP hosted full-stack implementation"
git push origin main
```

The `public-demo` branch should remain the separate GitHub Pages static synthetic build. Do not merge the MonsterASP server configuration or secrets into the Pages artifact/workflow.
