#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using PickleballXR;

// Unity 6 renamed PhysicMaterial -> PhysicsMaterial.
#if UNITY_6000_0_OR_NEWER
using PhysMat = UnityEngine.PhysicsMaterial;
using PhysCombine = UnityEngine.PhysicsMaterialCombine;
#else
using PhysMat = UnityEngine.PhysicMaterial;
using PhysCombine = UnityEngine.PhysicMaterialCombine;
#endif

namespace PickleballXR.EditorTools
{
    /// <summary>
    /// Tools > Pickleball XR > Build Court Prefab
    /// Turns the imported PickleballCourt.fbx into a ready-to-use prefab:
    /// pipeline-correct materials (URP Lit or Built-in Standard), box colliders, ball-friendly physics
    /// materials, PickleballSurface tags, the PickleballCourtController, and hidden overlays.
    /// </summary>
    public static class PickleballCourtSetup
    {
        private enum Kind { Opaque, Blend, Cutout }

        private struct Def
        {
            public Color color; public Kind kind;
            public Def(float r, float g, float b, float a, Kind k) { color = new Color(r, g, b, a); kind = k; }
        }

        // Same palette as the FBX/GLB.
        private static readonly System.Collections.Generic.Dictionary<string, Def> Palette =
            new System.Collections.Generic.Dictionary<string, Def>
        {
            { "Floor_Runoff",        new Def(0.075f, 0.259f, 0.220f, 1f, Kind.Opaque) },
            { "Court_Green",         new Def(0.180f, 0.545f, 0.420f, 1f, Kind.Opaque) },
            { "Court_Kitchen",       new Def(0.227f, 0.486f, 0.647f, 1f, Kind.Opaque) },
            { "Line_White",          new Def(0.960f, 0.960f, 0.940f, 1f, Kind.Opaque) },
            { "Net_Mesh",            new Def(0.100f, 0.100f, 0.100f, 1f, Kind.Cutout) },
            { "Net_Tape",            new Def(0.950f, 0.950f, 0.950f, 1f, Kind.Opaque) },
            { "Post_Metal",          new Def(0.350f, 0.370f, 0.380f, 1f, Kind.Opaque) },
            { "Wall_Surface",        new Def(0.800f, 0.860f, 0.840f, 1f, Kind.Opaque) },
            { "Wall_Target",         new Def(0.950f, 0.620f, 0.120f, 1f, Kind.Opaque) },
            { "Sign_Dark",           new Def(0.070f, 0.140f, 0.130f, 1f, Kind.Opaque) },
            { "Button_Red",          new Def(0.850f, 0.150f, 0.150f, 1f, Kind.Opaque) },
            { "Overlay_Red",         new Def(0.900f, 0.150f, 0.150f, 0.35f, Kind.Blend) },
            { "Overlay_Green",       new Def(0.200f, 0.850f, 0.350f, 0.35f, Kind.Blend) },
            { "Overlay_GreenBright", new Def(0.350f, 1.000f, 0.400f, 0.50f, Kind.Blend) },
            { "Overlay_Gold",        new Def(1.000f, 0.850f, 0.200f, 0.60f, Kind.Blend) },
        };

        [MenuItem("Tools/Pickleball XR/Build Court Prefab")]
        public static void BuildPrefab()
        {
            string modelPath = FindModelPath();
            if (modelPath == null)
            {
                EditorUtility.DisplayDialog("Pickleball XR", "Could not find PickleballCourt.fbx in the project.", "OK");
                return;
            }
            string root = Path.GetDirectoryName(Path.GetDirectoryName(modelPath)).Replace('\\', '/');
            EnsureFolder(root + "/Materials");
            EnsureFolder(root + "/Prefabs");
            EnsureFolder(root + "/Physics");

            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            Texture2D netTex = PrepareNetTexture(root + "/Textures/PickleballNet.png");

            // physics
            PhysMat floorPhys = GetPhys(root + "/Physics/Court_Floor.asset", 0.62f, 0.6f, 0.7f);
            PhysMat wallPhys = GetPhys(root + "/Physics/Court_Wall.asset", 0.62f, 0.5f, 0.6f);
            PhysMat netPhys = GetPhys(root + "/Physics/Court_Net.asset", 0.10f, 0.8f, 0.9f);

            GameObject go = Object.Instantiate(model);
            go.name = "PickleballCourt";

            foreach (MeshRenderer r in go.GetComponentsInChildren<MeshRenderer>(true))
            {
                string matName = MaterialFor(r.gameObject.name);
                if (matName == null) continue;
                r.sharedMaterial = GetMaterial(root, matName, netTex);
                if (matName.StartsWith("Overlay_") || matName == "Line_White")
                {
                    r.shadowCastingMode = ShadowCastingMode.Off;
                    r.receiveShadows = false;
                }
            }

            foreach (MeshFilter mf in go.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh == null) continue;
                string n = mf.gameObject.name;
                SurfaceType? type = null; PhysMat phys = floorPhys; float minThickness = 0f;

                if (n == "Floor_Runoff" || n.StartsWith("Court_Service")) type = SurfaceType.Floor;
                else if (n.StartsWith("Court_Kitchen")) type = SurfaceType.Kitchen;
                else if (n == "Net_Mesh") { type = SurfaceType.Net; phys = netPhys; minThickness = 0.04f; }   // thicker: stops fast-ball tunnelling
                else if (n.StartsWith("Net_Post")) { type = SurfaceType.NetPost; phys = netPhys; }
                else if (n == "Wall_Rebound") { type = SurfaceType.ReboundWall; phys = wallPhys; }
                else if (n.StartsWith("Wall_Target")) { type = SurfaceType.WallTarget; phys = wallPhys; }
                else if (n == "Button_EndPause") { type = SurfaceType.EndButton; phys = wallPhys; }
                if (!type.HasValue) continue;

                Bounds b = mf.sharedMesh.bounds;
                BoxCollider bc = mf.gameObject.AddComponent<BoxCollider>();
                bc.center = b.center;
                Vector3 size = b.size;
                if (minThickness > 0f) size.z = Mathf.Max(size.z, minThickness);
                bc.size = size;
                bc.sharedMaterial = phys;

                PickleballSurface s = mf.gameObject.AddComponent<PickleballSurface>();
                s.surface = type.Value;
            }

            // Overlays start hidden (storyboard: keep the world uncluttered). The controller shows them on demand.
            PickleballCourtController ctrl = go.AddComponent<PickleballCourtController>();
            ctrl.Index();
            Transform overlays = ctrl.Get(PickleballCourtController.OverlaysGroup);
            if (overlays != null)
                foreach (Transform child in overlays) child.gameObject.SetActive(false);

            string prefabPath = root + "/Prefabs/PickleballCourt.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);
            AssetDatabase.SaveAssets();
            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);
            Debug.Log("[Pickleball XR] Prefab built: " + prefabPath);
        }

        [MenuItem("Tools/Pickleball XR/Add Court To Scene")]
        public static void AddToScene()
        {
            string[] guids = AssetDatabase.FindAssets("PickleballCourt t:Prefab");
            if (guids.Length == 0) { BuildPrefab(); guids = AssetDatabase.FindAssets("PickleballCourt t:Prefab"); }
            if (guids.Length == 0) return;
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guids[0]));
            GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Undo.RegisterCreatedObjectUndo(inst, "Add Pickleball Court");
            Selection.activeGameObject = inst;
        }

        // ------------------------------------------------------------------ helpers
        private static string FindModelPath()
        {
            foreach (string guid in AssetDatabase.FindAssets("PickleballCourt t:Model"))
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                if (p.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase)) return p;
            }
            return null;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        /// <summary>Object name -> palette material name.</summary>
        private static string MaterialFor(string n)
        {
            if (n == "Floor_Runoff") return "Floor_Runoff";
            if (n.StartsWith("Court_Kitchen")) return "Court_Kitchen";
            if (n.StartsWith("Court_Service")) return "Court_Green";
            if (n.StartsWith("Lines_") || n == "Wall_NetHeightLine") return "Line_White";
            if (n == "Net_Mesh") return "Net_Mesh";
            if (n == "Net_Tape") return "Net_Tape";
            if (n.StartsWith("Net_Post") || n == "Panel_EndPause_Stand") return "Post_Metal";
            if (n == "Wall_Rebound") return "Wall_Surface";
            if (n.StartsWith("Wall_Target")) return "Wall_Target";
            if (n == "Sign_HitsCounter" || n == "Panel_EndPause") return "Sign_Dark";
            if (n == "Button_EndPause") return "Button_Red";
            if (n.StartsWith("Overlay_Kitchen")) return "Overlay_Red";
            if (n == "Overlay_ServeZone" || n == "Overlay_ServiceCourt_Target") return "Overlay_Green";
            if (n == "Overlay_Target_5x5ft") return "Overlay_GreenBright";
            if (n == "Overlay_Target_3x3ft") return "Overlay_Gold";
            return null;
        }

        private static Shader LitShader()
        {
            if (GraphicsSettings.currentRenderPipeline != null)
            {
                Shader urp = Shader.Find("Universal Render Pipeline/Lit");
                if (urp != null) return urp;
            }
            return Shader.Find("Standard");
        }

        private static Material GetMaterial(string root, string name, Texture2D netTex)
        {
            string path = root + "/Materials/" + name + ".mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader sh = LitShader();
            if (m == null) { m = new Material(sh); AssetDatabase.CreateAsset(m, path); }
            else if (m.shader != sh) m.shader = sh;

            Def d = Palette[name];
            bool urp = m.HasProperty("_Surface");   // URP Lit has _Surface, Standard has _Mode

            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", d.color);
            if (m.HasProperty("_Color")) m.SetColor("_Color", d.color);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.25f);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0.25f);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);

            if (d.kind == Kind.Blend)
            {
                if (urp)
                {
                    m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 0f);
                    m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                }
                else if (m.HasProperty("_Mode"))
                {
                    m.SetFloat("_Mode", 3f);
                    m.EnableKeyword("_ALPHABLEND_ON");
                }
                if (m.HasProperty("_SrcBlend")) m.SetFloat("_SrcBlend", 5f);
                if (m.HasProperty("_DstBlend")) m.SetFloat("_DstBlend", 10f);
                if (m.HasProperty("_ZWrite")) m.SetFloat("_ZWrite", 0f);
                m.SetOverrideTag("RenderType", "Transparent");
                m.SetShaderPassEnabled("ShadowCaster", false);
                m.renderQueue = (int)RenderQueue.Transparent;
            }
            else if (d.kind == Kind.Cutout)
            {
                if (m.HasProperty("_AlphaClip")) m.SetFloat("_AlphaClip", 1f);
                if (m.HasProperty("_Mode")) m.SetFloat("_Mode", 1f);
                if (m.HasProperty("_Cutoff")) m.SetFloat("_Cutoff", 0.25f);
                if (m.HasProperty("_Cull")) m.SetFloat("_Cull", 0f);
                m.EnableKeyword("_ALPHATEST_ON");
                m.SetOverrideTag("RenderType", "TransparentCutout");
                m.renderQueue = (int)RenderQueue.AlphaTest;
                if (netTex != null)
                {
                    if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", netTex);
                    if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", netTex);
                }
            }
            EditorUtility.SetDirty(m);
            return m;
        }

        private static Texture2D PrepareNetTexture(string path)
        {
            TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) return null;
            if (!ti.alphaIsTransparency || !ti.mipMapsPreserveCoverage)
            {
                ti.alphaIsTransparency = true;
                ti.mipMapsPreserveCoverage = true;   // keeps thin net strands from vanishing at distance
                ti.alphaTestReferenceValue = 0.25f;
                ti.wrapMode = TextureWrapMode.Repeat;
                ti.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static PhysMat GetPhys(string path, float bounce, float dynFriction, float statFriction)
        {
            PhysMat p = AssetDatabase.LoadAssetAtPath<PhysMat>(path);
            if (p == null) { p = new PhysMat(); AssetDatabase.CreateAsset(p, path); }
            p.bounciness = bounce;
            p.dynamicFriction = dynFriction;
            p.staticFriction = statFriction;
            p.bounceCombine = PhysCombine.Maximum;   // ball material can stay at bounciness 0
            p.frictionCombine = PhysCombine.Average;
            EditorUtility.SetDirty(p);
            return p;
        }
    }
}
#endif
