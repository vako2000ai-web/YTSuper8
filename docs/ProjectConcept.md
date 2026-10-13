# Roller Coaster Heroes — Project Concept & Technical Specification

**Repository:** YTSuper8  
**Version:** 1.0  
**Date:** 2026-10-13  
**Platform:** Windows, Unity, YouTube Live  
**Documentation:** Russian; **all in-game and on-stream text: English**

## 1. Vision

Roller Coaster Heroes is a colorful, autonomous 3D cartoon racing show for YouTube. Ten cartoon heroes race **sequentially** in small roller-coaster carts, collecting coins and encountering boosters, slowdowns and random events. Across three tracks, 30 races determine one champion. Viewers change the course of events with **USD donations through DonationAlerts**. Cheerful background music, expressive characters, sound effects and dynamic cameras make each race entertaining.

### Locked decisions

- **Sequential racing:** One hero on track at a time; ten racers per round; three rounds.
- **Single leaderboard:** One overall tournament score, **including all viewer impacts**. No separate donation-free leaderboard.
- **Currency:** All donation thresholds, prices, displayed amounts and donation reporting in **USD**.
- **Language:** All viewer-facing labels, alerts, commentator lines, graphics, stream titles, descriptions and chat templates are **English**.
- **Audio:** Cheerful looping music, vehicle sounds, coin sounds, boost/brake sounds, cartoon reactions, donation alerts and victory fanfares.
- **Operation:** Automated race progression with administrator controls.
- **Game engine:** Unity 6 + C# using URP; Blender for assets.
- **Streaming:** OBS Studio to YouTube Live; integrated browser overlays.
- **Persistence:** SQLite with event logs and tournament history.

## 2. Players and tournament

Ten distinct cartoon characters with signature carts, colors, animations and personality. Suggested provisional names: Fox, Bolt, Dino, Chick, Ziggy, Bruno, Rocky, Rico, Mia, Ping; final identifiers must be consistent across UI, metadata and database.

Each character races on the same three track configurations. Round 1: ten sequential races on Green Hills; Round 2: ten races on Tropical Rush; Round 3: ten races on Sky Madness. Within each round, use identical fixed track geometry and identical pseudorandom **seed** for initial collectible/power-up placements so heroes face comparable base conditions. Donation events change actual races and results.

### Show state machine

`IDLE → STARTING_SOON → TOURNAMENT_INTRO → NEXT_RACER → COUNTDOWN → RACING → FINISH → RACE_RESULTS → LEADERBOARD → NEXT_RACER / NEXT_ROUND → CHAMPION → STREAM_ENDING`

Allow controlled `PAUSED`, `RECOVERY` and `ABORTED` states. All state transitions must be durable, auditable and restart-safe.

Target race duration: **60–120 seconds**; interstitials 5–10 seconds, plus event presentation; total tournament approximately **40–70 minutes** depending on pacing.

## 3. Track, physics and procedural objects

### Track

A cart follows a 3D spline rail; orientation, banking and camera direction derive from spline tangent/frame. Start with three handcrafted, validated track geometries and procedurally populate objects instead of generating arbitrary unsafe rail shapes in v1.

- **Green Hills:** sunny introductory coaster, gentle hills, basic boosts.
- **Tropical Rush:** waterfalls, tunnels, tight turns, loops and slowdowns.
- **Sky Madness:** floating islands, steep drops and dramatic coin formations.

### Motion

An arcade physical model updates in Unity fixed simulation steps. Simplified longitudinal acceleration:

`a = g sin(theta) + a_motor + a_bonus - a_resistance`

`v_next = clamp(v + a*dt, v_min, v_max)`

Slope sign follows travel direction. Apply acceleration and braking using bounded multipliers or forces; ensure minimum safe speed on climbs and no unintended derailment. Rendering interpolates between simulation steps. Power-ups cannot make the cart stop forever or reach unsafe speeds.

### Collectibles and events

- Gold coin: +1 coin, configurable score value (default 100 points).
- Coin chain: repeated placement with configurable spacing.
- Treasure chest: +5 to +20 coins, seeded.
- Speed Boost: +40% speed for 5 seconds.
- Slow Down: -30% speed for 4 seconds.
- Shield: blocks one negative effect.
- Mega Boost: +60% speed for 6 seconds.
- Gold Rush: +50 support coins (donation effect).
- Decorative effects: sparks, wind, confetti, light flashes and character reactions.

Generation rules: seeded pseudorandom placement, safe spacing around curves, no unreachable collectible, no overlapping critical collisions, bounded consecutive penalties. Log track version, seed and parameters for reproducibility. Treat randomized visuals separately from gameplay randomization.

## 4. Score calculation and rankings

Default v1 formula, configurable in administration:

`TimeScore = max(0, 10000 - 50 * T)` where `T` is finish time in seconds.

`CoinScore = 100 * (TrackCoins + BonusCoins + SupportCoins)`

`RaceScore = TimeScore + CoinScore`

`TournamentScore = sum(RaceScore over three rounds)`

**All donation-triggered coins and speed effects contribute to the same official tournament score.** Track coins, bonus coins and viewer support coins are tracked separately as statistical fields but not ranked separately.

Suggested tie-breaks: greater track coins; then shorter total recorded race time; then a seeded bonus race if still tied. Never retroactively modify completed race scores for late donations.

Scoring coefficients require playtesting to balance collecting coins against finishing quickly.

## 5. DonationAlerts integration (USD only)

### Draft donation-action map

| USD amount | Command | Result |
|---:|---|---|
| $1.00 | `COINS <hero>` | +5 support coins |
| $2.00 | `SHOWER <hero>` | +15 support coins |
| $3.00 | `BOOST <hero>` | +40% speed for 5s |
| $4.00 | `SLOW <hero>` | -30% speed for 4s |
| $5.00 | `SHIELD <hero>` | Blocks one slowdown |
| $7.00 | `MEGA <hero>` | +60% speed for 6s |
| $10.00 | `GOLD <hero>` | +50 support coins |

All amounts are **USD**. These are editable proposed presets, not final commercial tariffs. Policy for larger amounts, combined commands, decimals, malformed hero IDs, non-USD payments and refunds must be explicitly configured. Do not infer currency conversions silently.

### Event flow

`DonationAlerts OAuth 2.0 → donation subscription (WebSocket/Centrifugo) → Python donation processor → validation/deduplication → persistent event queue → Unity bridge → game effect → SQLite → OBS overlay`

Use DonationAlerts official API documentation for scopes, subscriptions and event payloads; implement OAuth token refresh securely. On reconnect, reconcile event history where API permissions permit. Store provider event ID as unique key so repeated delivery does not duplicate effects.

**Timing policy:** Apply actions during the targeted hero's active race; otherwise queue for an upcoming race if target is eligible. After the targeted hero has finished their final race, do not modify final scores; classify donations as non-applied support with a clear reason. Define queue expiry, pause handling and late-event ordering.

**Fairness and safety:** Limit concurrent speed effects and slowdown stacking; bound speed multiplier; shield interactions must be deterministic. Do not let donation spikes cause unstable simulation or processing backlog. Maintain a full `donation → queued → applied/rejected` audit trail. Test mode never credits real tournament points.

Note: donations affect outcomes; the show should transparently disclose this to viewers.

## 6. User interface — English only

### In-race HUD

`ROLLER COASTER HEROES`, `ROUND 1/3`, `RACER: BOLT`, `TIME`, `SPEED`, `COINS`, `SCORE`, `COIN BANK`, `RACING`.

### Alerts and state text

`RACE STARTS IN 3... 2... 1... GO!`; `COIN COLLECTED!`; `SPEED BOOST!`; `SLOW DOWN!`; `COIN SHOWER!`; `SHIELD ACTIVATED!`; `NEW DONATION — $3.00`; `FINISH!`; `TOTAL SCORE`; `TOURNAMENT LEADERBOARD`; `NEXT RACER`; `TOURNAMENT CHAMPION!`.

Show large readable text within YouTube-safe margins. Use 1920×1080 reference design and scale to stream output; remain legible on mobile screens.

## 7. Audio

Music style: playful cartoon arcade racing with upbeat, cheerful cues, approximately **125–150 BPM** during races. Separate loops for tournament opening, races, results, transition and victory. Seamless loops and crossfades; avoid music fatigue during long sessions.

Sound events: wheels/rattles, wind, coin ding, escalating coin streak melody, chest opening, whoosh/turbo, comic brake, loop reactions, donation chime, finish fanfare, trophy/victory flourish.

Unity Audio Mixer buses: `Master`, `Music`, `SFX`, `Vehicle`, `Characters`, `Donations`, `Voice`. Duck music for donations and commentary. Provide per-bus volume controls and a mute option. Use music and sounds licensed for YouTube live streaming, archived VODs and monetization.

Voice commentary/TTS is a later extension, initially English.

## 8. Architecture

- **Unity 6 / C#:** track traversal, physics, characters, object spawning, effects, camera, audio, HUD and client event reporting.
- **Python / FastAPI backend:** tournament coordinator, donation processing, reliable event queue, score validation, persistence, REST/WebSocket APIs, OBS event feed.
- **SQLite:** tournament records, rounds, race results, characters, tracks, donations, queued effects, event log, settings.
- **Admin web app:** setup, configuration, dashboards, manual tests, pause/resume, OBS controls.
- **OBS Studio:** game capture, browser overlay, stream scene switcher, audio mix, YouTube output.
- **YouTube:** livestream scheduling, metadata and publishing, moderation and community announcements.

Unity-to-backend communication uses a localhost WebSocket with reconnect and event acknowledgments. Server maintains authoritative tournament states and finalized scores; Unity reports actual gameplay events with IDs and monotonic sequence numbers. Log and reconcile inconsistencies rather than trusting UI text.

### Suggested repository layout

```text
YTSuper8/
├── docs/
│   ├── ProjectConcept.md
│   ├── TechnicalSpecification.md
│   ├── API.md
│   └── TestPlan.md
├── UnityGame/
│   └── Assets/
│       ├── Scripts/
│       │   ├── Characters/
│       │   ├── Carts/
│       │   ├── TrackGenerator/
│       │   ├── VehiclePhysics/
│       │   ├── Collectibles/
│       │   ├── PowerUps/
│       │   ├── CameraSystem/
│       │   ├── RaceManager/
│       │   └── Audio/
│       └── Art/
├── Server/
│   ├── DonationAlerts/
│   ├── EventQueue/
│   ├── TournamentManager/
│   ├── Database/
│   └── API/
├── Dashboard/
├── Broadcast/
│   ├── YouTubeMetadata/
│   ├── Branding/
│   ├── OBS/
│   └── Automation/
├── Config/
└── README.md
```

## 9. Database (SQLite)

Suggested tables with primary responsibilities:

- `characters`: immutable hero IDs, display name, model/cart references, visual theme and attributes.
- `tracks`: track ID, geometry version, difficulty, default parameters.
- `tournaments`: tournament ID, start/end, status, winner, score config version.
- `rounds`: round number, track, seed, ordering and status.
- `races`: racer, round, start/end timestamps, elapsed seconds, coin counts, time score, total score, status.
- `donations`: provider event ID (unique), sender display name, amount in integer USD cents, currency, text, parsed command and validation status.
- `game_events`: unique event ID, race ID, logical sequence, type, payload, timestamp.
- `race_effects`: donation ID, target hero/race, effect type, parameters, queued/applied/expired status.
- `settings`: versioned score, odds, donation, sound and broadcast settings.
- `broadcasts`: metadata, planned and actual times, YouTube stream identifier and assets.

Add foreign keys, uniqueness constraints, indexes by race/round/event IDs and migrations. Treat currency precisely (integer cents), never floating-point dollars.

## 10. Admin controls

Functions: create tournament; assign racers/tracks; seed preview; start/pause/resume/abort; monitor current race; inspect scoreboard and donation queue; change donation USD pricing; test effects without charging; mute/mix audio; manage scenes, title, description and stream assets; view logs; export results.

Changes affecting an ongoing tournament should be versioned and controlled rather than silently altering results mid-race.

## 11. YouTube broadcast and promotion

Every tournament produces consistent English-only metadata and on-screen branding.

### Suggested livestream title

**🔴 LIVE: 10 Cartoon Heroes Race Crazy Roller Coasters! 🏆🎢**

Alternatives:
- `🎢 Roller Coaster Heroes LIVE! Crazy Races, Coins & Boosts!`
- `🏆 10 Heroes, 3 Tracks, 1 Champion! | Roller Coaster Heroes LIVE`

### Stream description template

```text
🎢 WELCOME TO ROLLER COASTER HEROES! 🏆

10 crazy cartoon heroes. 3 amazing roller coaster tracks.
1 ultimate champion!

Watch racers fly through wild loops, collect golden coins,
hit speed boosts and survive hilarious slowdowns.

🔥 LIVE TOURNAMENT:
🏎️ 10 cartoon racers
🎢 3 roller coaster tracks
🪙 Coins and surprise power-ups
⚡ Speed boosts and slowdowns
🏆 Live leaderboard

💰 YOU CAN CHANGE THE RACE!
Support your favorite hero with DonationAlerts in USD.
🪙 Add coins | 🚀 Boost | 🐌 Slow down | 🛡️ Shield

DONATE: [DONATIONALERTS_URL]
STREAM: [STREAM_URL]

The official leaderboard includes all viewer-triggered effects.
Which hero will become the champion? Tell us in chat!

👍 LIKE • 🔔 SUBSCRIBE • 📢 SHARE

#RollerCoasterHeroes #GamingLive #CartoonRacing
```

### YouTube search tags

`roller coaster heroes, roller coaster game, cartoon racing, roller coaster racing, racing game live, funny gaming, animated racing, cartoon game, live gaming, virtual racing, crazy roller coaster, gaming tournament, interactive livestream, donation powered game, 3D racing game, coaster challenge, funny race, arcade racing`

Metadata titles/descriptions are templates; season/episode, live status, participants and URLs must be updated automatically. Avoid claiming the broadcast is live when it is not.

### Visual assets and dimensions

| Asset | Target size |
|---|---|
| Main livestream thumbnail | 3840×2160 (16:9) |
| Starting Soon | 1920×1080 |
| Tournament Intro | 1920×1080 |
| Race Starting | 1920×1080 |
| Next Racer | 1920×1080 |
| Leaderboard | 1920×1080 |
| Champion | 1920×1080 |
| Be Right Back | 1920×1080 |
| Stream Ending | 1920×1080 |
| Channel banner | 2560×1440 |
| Shorts cover | 1080×1920 |

Design language: colorful theme park, looping red rails, gold coins, glossy cartoon carts, clear English typography, consistent logo, exuberant reactions. Asset production must preserve editable text layers and safe areas when feasible.

### OBS scenes

```text
01_STARTING_SOON
02_TOURNAMENT_INTRO
03_RACE_LIVE
04_RACE_RESULTS
05_LEADERBOARD
06_NEXT_RACER
07_CHAMPION
08_BE_RIGHT_BACK
09_STREAM_ENDING
```

Provide scene switching from tournament state, manual override and browser overlays from local backend. OBS captures Unity output and audio; stream keys and authentication tokens are never stored in the repo.

### Pinned live chat message

```text
🎢 WELCOME TO ROLLER COASTER HEROES LIVE! 🏆
10 heroes race across 3 crazy tracks!
🪙 Collect coins | 🚀 Boost | 🐌 Slow down
💰 Support your racer via DonationAlerts: [LINK]
🏁 Who will win? Drop your prediction!
👍 LIKE • 🔔 SUBSCRIBE • 🎉 ENJOY THE SHOW!
```

### Pre-stream social post

```text
🎢 THE CRAZIEST ROLLER COASTER TOURNAMENT IS COMING!
10 heroes. 3 wild tracks. 1 champion.
And YOU can change the race LIVE!
📺 [STREAM LINK]
#RollerCoasterHeroes #GamingLive #CartoonRacing
```

### Post-tournament announcement

```text
🏆 WE HAVE A NEW CHAMPION!
🥇 Champion: [HERO NAME]
⭐ Total Score: [SCORE]
🪙 Coins Collected: [COINS]
Thanks for watching and supporting our racers!
Subscribe for the next tournament!
```

Broadcast preparation workflow: `Prepare Tournament` generates metadata, thumbnail from template, scenes/overlays, hero labels, donation URL and community copy. Final publishing/scheduling can be manual until authenticated YouTube APIs are added.

## 12. Implementation schedule (8-week baseline)

| Week | Work | Definition of done |
|---|---|---|
| 1 | Unity scene, URP, spline, cart, chase camera, finish | One complete automatic race |
| 2 | Arcade physics, seeded coins, boosts, slowdowns, score | Stable replayable racing gameplay |
| 3 | 10 characters, 3 tracks, 30 races, standings, SQLite | Full autonomous tournament |
| 4 | DonationAlerts OAuth, event stream, USD action queue, deduplication | Verified donation event affects selected racer once |
| 5 | English HUD, cartoon effects, cameras, intro/results/champion screens | Visually polished complete tournament |
| 6 | Music, SFX, Audio Mixer, ducking, licensing verification | Broadcast-quality consistent audio |
| 7 | OBS scenes, overlays, admin controls, restart recovery, YouTube metadata | Automated end-to-end stream workflow |
| 8 | Test all edge cases, tune gameplay, run long broadcast, package Windows build | Release candidate for regular shows |

### Milestone 1: First Ride

One hero + one cart + one spline track + chase camera + automatic finish, with English `TIME`, `SPEED`, `COINS`, `SCORE` HUD and cheerful music. Implement this before live donations.

## 13. Acceptance tests

1. All ten racers finish each of three tracks without manual driving.
2. Per-round objects are placed deterministically with a saved seed.
3. Races log time, collected coins, effects and final score accurately.
4. Official standings include donations and choose one champion.
5. USD donation of a configured threshold targets the selected hero and triggers the correct action.
6. Duplicate notifications never duplicate rewards.
7. Late or invalid commands follow documented deterministic policy and cannot retroactively change finished results.
8. The entire player-facing UI and broadcast assets are in English.
9. Cheerful background music and expected event SFX function during the tournament.
10. OBS scenes and overlay data switch correctly across race states.
11. Persisted tournaments survive application restart; no secret credentials in source control.
12. A 30-race rehearsal completes without critical interruption.

## 14. Future extensions

English dual-commentator TTS, viewer polls, seasonal competitions, advanced camera director, new tracks/heroes, procedurally assembled track sections, automated YouTube livestream scheduling, highlights/Shorts generation, community ranking, richer visual donation effects.

## 15. Guiding principles

**Spectacle first; deterministic rules; one authoritative leaderboard; transparent viewer impact; reliable donations; English-only presentation; USD-only pricing; cheerful sound; stream-ready automation.**

All values described as defaults (scoring, effects, prices and schedule) remain configurable during testing before public launch.
