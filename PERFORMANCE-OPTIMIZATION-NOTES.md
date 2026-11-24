# MonsterASP performance update

This source update reduces unnecessary SQL round trips during settings refresh and save:

- Settings catalog reads now combine target-row uniqueness validation and value retrieval in one SQL statement (`COUNT_BIG(*) OVER()`).
- Save verification reads use the same combined query rather than a separate COUNT query followed by SELECT.
- The Vue save flow now consumes the committed values already returned by the API, rather than reloading the full settings catalog after each save group and again at the end. A single fallback reload remains only for older API responses without returned values.
- The save flow no longer reloads static deployment capabilities after every save.

This should reduce avoidable wait time, but it cannot eliminate latency caused by MonsterASP shared hosting or SQL Server resource contention. The update has not been compiled in this preparation environment because the dependency installation timed out and .NET SDK is unavailable here. The existing local publish script should be run on the user's Windows machine before replacing the deployed files.
