# YTSuper8 — Roller Coaster Heroes

An English-language cartoon roller-coaster tournament for YouTube Live. 10 racers, 3 rounds, **one official leaderboard including USD viewer donations**, cheerful music and sound effects.

## Current stage: Milestone 1 — First Ride (source prototype)

Source prototype included: generated roller-coaster path, moving cart, chase camera, collectible coins, boost/slow zones, scoreboard, procedural demo audio. **Not yet compiled or playtested in the Unity Editor.** DonationAlerts integration, full tournament and OBS automation are later milestones.

### Run in Unity

1. Install Unity Hub and a **Unity 6** editor; create a **3D (URP)** project named `RollerCoasterHeroes`.
2. Copy `UnityGame/Assets/Scripts/` from this repository into that Unity project's `Assets/Scripts/`.
3. Open a blank scene and enter Play mode. `FirstRideBootstrap` will build its simple demo environment automatically.
4. Press `R` to restart, `M` to mute music.
5. Replace placeholder primitives with original character/cart assets later.

The first demo is intentionally standalone and needs no extra asset packages. It runs entirely with built-in Unity APIs. Its track curve is sampled procedurally; using Unity Splines for authoring is a later step.

### Repository

- `UnityGame/` — Unity source prototypes (copy scripts into a Unity 6 project).
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
