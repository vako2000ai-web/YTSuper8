# Roller Coaster Heroes — First Ride 2.0

Open this folder in **Unity 6.5 / 6000.5.4f1**. Packages: Universal RP 17.5.0, Unity UI 2.5.0.
Open `Assets/Scenes/FirstRide.unity`, then Play. If needed, run **Roller Coaster Heroes → Setup First Ride 2.0** first.

`R`: restart · `Space`: pause · `C`: chase/wide camera · `M`: music toggle.

The environment is constructed on entering Play Mode: twin tubular rails and supports, detailed cart, articulated fox, 70 seeded pickups, a dynamic camera, English HUD and original synthesized music/SFX. The same arc-length model drives geometry and movement. Rendering interpolates fixed simulation steps. Score remains `max(0, 10000 - 50 * seconds) + 100 * coins`.

- `RideModel.cs`: deterministic simulation and track sampling.
- `RideArt.cs`: shared materials, rail meshes, batched sleepers/supports/park.
- `HeroCart.cs`: cart model and procedural fox animation.
- `RideCamera.cs`: chase/wide camera, look-ahead, FOV, finish orbit.
- `RideAudio.cs`: original 132 BPM music, coin/boost/brake/finish cues and wheels.
- `RideHud.cs`: scaled Unity UI overlay and results.
- `FirstRideBootstrap.cs`: scene construction, input, pickup presentation and lifecycle.
- `Assets/Editor/FirstRideProject.cs`: setup, validation, two-ride Play Mode smoke check, screenshots and Windows build.

Use **Roller Coaster Heroes → Validate gameplay** for deterministic regression checks and **Build Windows x64** for an executable.
Full instructions: [Windows setup/update guide](../docs/Unity65Windows.md). Actual verification: [validation record](../docs/FirstRide2Validation.md).

This milestone is a single-racer demo. Tournament orchestration, persistent results, DonationAlerts, OBS integration, final DCC art and a production AudioMixer are future work.
