# YTSuper8 — Roller Coaster Heroes

An English-language cartoon roller-coaster tournament for YouTube Live. 10 racers, 3 rounds, **one official leaderboard including USD viewer donations**, cheerful music and sound effects.

## Current stage: First Ride 2.0 — Unity 6.5 / URP

First Ride 2.0 includes twin tubular rails, sleepers and trestles, a detailed rolling cart, an animated cartoon fox, a dynamic chase/wide/finish camera, dimensional coins and effect badges, an English broadcast HUD, original procedural music and separate event sounds. Automatic movement, seeded pickup collection and the original scoring formula are preserved. DonationAlerts integration, full tournament and OBS automation are later milestones.

### Run in Unity

1. Install Unity Hub and **Unity 6.5 (6000.5.4f1)**.
2. Add the repository's **`UnityGame`** folder as a project in Unity Hub and let packages import.
3. Open `Assets/Scenes/FirstRide.unity` (or run **Roller Coaster Heroes → Setup First Ride 2.0** if it is missing), then press Play.
4. Press `R` to restart, `Space` to pause, `C` to change camera and `M` to toggle music.
5. Read the [detailed Windows installation/update/build guide (Russian)](docs/Unity65Windows.md) and [validation record](docs/FirstRide2Validation.md).

The demo is standalone: URP 17.5 and Unity UI 2.5 are declared in the project manifest; no external art or audio downloads are required. Geometry, articulated animation and audio are generated locally. Its curve uses an arc-length lookup; Unity Splines authoring is a later step.

### Repository

- `UnityGame/` — runnable Unity 6.5 URP project, editor setup, gameplay validation and Play Mode smoke checks.
- `Server/` — starter FastAPI backend and tournament state endpoints (no real DonationAlerts handling yet).
- `Dashboard/` — temporary admin/status page.
- `Broadcast/` — English YouTube text templates and planned OBS scenes.
- `docs/ProjectConcept.md` — complete project concept.

### Backend quickstart

```powershell
cd Server
python -m venv .venv
.venv\Scripts\Activate.ps1
pip install -r requirements.txt
uvicorn app:app --reload --host 127.0.0.1 --port 8000
```

Open `http://127.0.0.1:8000/docs`; `Dashboard/index.html` is a static mock until the production dashboard is connected.

## Security

No DonationAlerts client secrets, OAuth tokens, stream keys or account data in version control. USD amounts must use integer cents. Never credit simulated test events to actual tournament standings.
