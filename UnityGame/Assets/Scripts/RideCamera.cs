using UnityEngine;

namespace RollerCoasterHeroes
{
    public sealed class RideCamera
    {
        public Camera Camera { get; }
        public bool Wide { get; private set; }
        Vector3 velocity;
        float finishTime;
        public RideCamera(Transform root)
        {
            foreach (var existing in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)) existing.enabled = false;
            foreach (var listener in Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None)) listener.enabled = false;
            var go = new GameObject("First Ride director camera"); go.transform.SetParent(root);
            Camera = go.AddComponent<Camera>(); Camera.tag = "MainCamera";
            Camera.clearFlags = CameraClearFlags.SolidColor; Camera.backgroundColor = new Color(.53f, .80f, .96f);
            Camera.nearClipPlane = .15f; Camera.farClipPlane = 950; Camera.fieldOfView = 62;
            go.AddComponent<AudioListener>();
        }
        public void Toggle() { Wide = !Wide; }
        public void Reset(RideModel model) { velocity = Vector3.zero; finishTime = 0; Tick(model, 0, true); }

        public void Tick(RideModel model, float dt, bool snap = false, float? renderDistance = null)
        {
            float d = renderDistance ?? model.Distance;
            Vector3 at = model.Track.Position(d);
            Quaternion frame = model.Track.Rotation(d);
            // Use horizontal heading for the boom: steep climbs cannot push the camera through rails.
            Vector3 heading = Vector3.ProjectOnPlane(frame * Vector3.forward, Vector3.up).normalized;
            Vector3 side = Vector3.Cross(Vector3.up, heading);
            float speed01 = Mathf.InverseLerp(7, 37, model.Speed);
            float back = Wide ? 14 : Mathf.Lerp(8.5f, 11, speed01);
            float shoulder = Wide ? 7 : 3.6f;
            Vector3 desired = at - heading * back + side * shoulder + Vector3.up * (Wide ? 7 : 4.4f);
            Vector3 look = Vector3.Lerp(at + Vector3.up * 1.6f, model.Track.Position(d + 13) + Vector3.up, .17f);
            if (model.Finished)
            {
                finishTime += dt;
                float orbit = Mathf.Min(finishTime * 15, 145);
                desired = at + Quaternion.AngleAxis(orbit, Vector3.up) * (-heading * 8 + side * 4) + Vector3.up * 3.6f;
                look = at + Vector3.up * 1.8f;
            }
            // Keep the boom above the sampled rail envelope when cresting a hill.
            for (float offset = -back; offset <= 2; offset += 2)
                desired.y = Mathf.Max(desired.y, model.Track.Position(d + offset).y + 2.6f);
            Camera.transform.position = snap ? desired : Vector3.SmoothDamp(Camera.transform.position, desired, ref velocity, .18f, Mathf.Infinity, dt);
            Quaternion aim = Quaternion.LookRotation(look - Camera.transform.position, Vector3.up);
            Camera.transform.rotation = snap ? aim : Quaternion.Slerp(Camera.transform.rotation, aim, 1 - Mathf.Exp(-9 * dt));
            Camera.fieldOfView = Mathf.Lerp(Camera.fieldOfView, model.Finished ? 56 : 60 + speed01 * 9, snap ? 1 : 1 - Mathf.Exp(-3 * dt));
        }
    }
}
