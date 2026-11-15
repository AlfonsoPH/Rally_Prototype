using System.Collections.Generic;
using UnityEngine;

namespace PickleballXR
{
    /// <summary>The three play contexts from the storyboard.</summary>
    public enum CourtMode { Wall, Training, Lesson }

    /// <summary>Toggleable visual guides (storyboard: "Display & Visual Guides" menu).</summary>
    public enum CourtOverlay { KitchenZone, ServeZone, ServiceCourtTarget, Target5x5, Target3x3 }

    /// <summary>
    /// Sits on the root of the PickleballCourt prefab. Finds everything by name, so it keeps
    /// working however Unity nests the FBX root.
    /// </summary>
    [DisallowMultipleComponent]
    public class PickleballCourtController : MonoBehaviour
    {
        // Names must match the FBX hierarchy.
        public const string NetGroup = "Net";
        public const string WallGroup = "PracticeWall";
        public const string OverlaysGroup = "Overlays";

        public const string AnchorPlayerServe = "Anchor_Player_Serve";
        public const string AnchorPlayerTraining = "Anchor_Player_Training";
        public const string AnchorPlayerWall = "Anchor_Player_Wall";
        public const string AnchorBallMachine = "Anchor_BallMachine";
        public const string AnchorCoach = "Anchor_Coach";
        public const string AnchorUIHitsCounter = "Anchor_UI_HitsCounter";
        public const string AnchorUIClock = "Anchor_UI_Clock";
        public const string AnchorUIPausePanel = "Anchor_UI_PausePanel";

        [SerializeField] private CourtMode startMode = CourtMode.Lesson;

        private readonly Dictionary<string, Transform> byName = new Dictionary<string, Transform>();

        public CourtMode Mode { get; private set; }

        private void Awake()
        {
            Index();
            SetMode(startMode);
        }

        /// <summary>Rebuilds the name lookup (call after adding children at runtime).</summary>
        [ContextMenu("Re-index children")]
        public void Index()
        {
            byName.Clear();
            foreach (Transform t in GetComponentsInChildren<Transform>(true))
                if (!byName.ContainsKey(t.name)) byName.Add(t.name, t);
        }

        public Transform Get(string objectName)
        {
            if (byName.Count == 0) Index();
            Transform t;
            return byName.TryGetValue(objectName, out t) ? t : null;
        }

        // ------------------------------------------------------------------ modes
        /// <summary>
        /// Wall mode: practice wall visible, net hidden.
        /// Training / Lesson: net visible, wall hidden.
        /// </summary>
        public void SetMode(CourtMode mode)
        {
            Mode = mode;
            SetActive(Get(NetGroup), mode != CourtMode.Wall);
            SetActive(Get(WallGroup), mode == CourtMode.Wall);
        }

        // ------------------------------------------------------------------ overlays
        public void SetOverlay(CourtOverlay overlay, bool visible)
        {
            switch (overlay)
            {
                case CourtOverlay.KitchenZone:
                    SetActive(Get("Overlay_Kitchen_A"), visible);
                    SetActive(Get("Overlay_Kitchen_B"), visible);
                    break;
                case CourtOverlay.ServeZone: SetActive(Get("Overlay_ServeZone"), visible); break;
                case CourtOverlay.ServiceCourtTarget: SetActive(Get("Overlay_ServiceCourt_Target"), visible); break;
                case CourtOverlay.Target5x5: SetActive(Get("Overlay_Target_5x5ft"), visible); break;
                case CourtOverlay.Target3x3: SetActive(Get("Overlay_Target_3x3ft"), visible); break;
            }
        }

        public void HideAllOverlays()
        {
            foreach (CourtOverlay o in System.Enum.GetValues(typeof(CourtOverlay))) SetOverlay(o, false);
        }

        /// <summary>
        /// Shows the guide for a serve-lesson star level (storyboard difficulty scaling):
        /// 0 = just the serve zone, 1 = the service court, 2 = 5x5 ft box, 3 = 3x3 ft box.
        /// </summary>
        public void ShowServeTarget(int stars)
        {
            stars = Mathf.Clamp(stars, 0, 3);
            HideAllOverlays();
            SetOverlay(CourtOverlay.ServeZone, true);
            SetOverlay(CourtOverlay.ServiceCourtTarget, stars == 1);
            SetOverlay(CourtOverlay.Target5x5, stars == 2);
            SetOverlay(CourtOverlay.Target3x3, stars == 3);
        }

        /// <summary>
        /// Scores where a served ball landed: 0 = out, 1 = in the target service court,
        /// 2 = inside the 5x5 ft box, 3 = inside the 3x3 ft box. Minimum pass = 1 star.
        /// Works even while the overlay objects are hidden.
        /// </summary>
        public int EvaluateServe(Vector3 worldLandingPoint)
        {
            if (InsideFootprint("Overlay_Target_3x3ft", worldLandingPoint)) return 3;
            if (InsideFootprint("Overlay_Target_5x5ft", worldLandingPoint)) return 2;
            if (InsideFootprint("Overlay_ServiceCourt_Target", worldLandingPoint)) return 1;
            return 0;
        }

        private bool InsideFootprint(string objectName, Vector3 worldPoint)
        {
            Transform t = Get(objectName);
            if (t == null) return false;
            MeshFilter mf = t.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) return false;
            Vector3 p = t.InverseTransformPoint(worldPoint);
            Bounds b = mf.sharedMesh.bounds;
            return p.x >= b.min.x && p.x <= b.max.x && p.z >= b.min.z && p.z <= b.max.z;
        }

        // ------------------------------------------------------------------ anchors
        public Transform GetAnchor(string anchorName) { return Get(anchorName); }

        /// <summary>
        /// Moves an XR Origin / rig root so the player stands on an anchor (e.g. AnchorPlayerWall).
        /// </summary>
        public bool MoveRigToAnchor(Transform xrOriginRoot, string anchorName)
        {
            Transform a = Get(anchorName);
            if (a == null || xrOriginRoot == null) return false;
            xrOriginRoot.SetPositionAndRotation(a.position, Quaternion.Euler(0f, a.eulerAngles.y, 0f));
            return true;
        }

        private static void SetActive(Transform t, bool on)
        {
            if (t != null && t.gameObject.activeSelf != on) t.gameObject.SetActive(on);
        }
    }
}
