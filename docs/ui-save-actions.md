# Save and connection UI

The settings-load confirmation banner has been removed. Connection status is represented by the small top-bar state. Errors remain visible.

Save and Discard actions are shown in the page heading area, not in a sticky bottom bar. They are rendered only when the API capability response authorizes the local `IsolatedLocalDemo` target and at least one pending setting change is supported. In normal read-only mode, the application does not present an actionable Save button. This is intentional; the UI does not override backend authorization.
