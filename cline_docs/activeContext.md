# Active Context

## What We're Working On Now
Completed: Add "Today" option to Update Box Scores page for daily box score updates.

## Recent Changes
1. Added "Today" button to Update Box Scores page alongside renamed "Year" button with "Time range" helper text
2. Inverted loop order in RotowireFetchService (days outer, teams inner) so current day's data is fetched last in a tight window
3. Fixed timezone issue — all date calculations now use Pacific time (`America/Los_Angeles`) instead of UTC
4. Added `BsonValueEquals` method to BoxScoreDataAccessLayer for value-based BSON comparison (prevents false-positive updates from int32/int64 type mismatches)
5. Fixed `download_date` field to be updated when existing box score records are modified
6. Added diagnostic logging for TOT/A record updates in box score import
7. Added client-side cache invalidation in BuildUpdateStandings after standings calculation completes
8. Added timestamp-based cache validation in StandingsCacheService — client checks server's `lastModified` timestamp on cache hits and invalidates if server has newer data
9. Updated `lastModified` server endpoint to use `LastBoxScoreUpdate` consistently with data endpoints

## Next Steps
- Remove diagnostic logging from BoxScoreDataAccessLayer once confirmed stable in production
- Monitor cache validation behavior to ensure timestamp checks work correctly
- Consider removing the 5-minute TTL fallback if timestamp validation proves reliable