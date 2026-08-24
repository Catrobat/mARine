using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CreateEnv.Ecosystem.EditorTools
{
    // Builds a throwaway scene for looking at the reef, so testing needs no AR
    // device, no plane detection and no trip through the environment builder.
    //
    // The scene is created in memory and never written to disk, so nothing in the
    // project changes. Close it without saving when you are done.
    public static class LivingReefPreviewScene
    {
        [MenuItem("Tools/Living Ecosystem/Open Preview Scene")]
        static void Open()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "Living Reef Preview";

            // Camera, looking slightly down at where the reef will sit.
            // Far enough back to see the whole 9 m patch, and high enough to look
            // across it rather than stand in it.
            var cameraGo = new GameObject("Preview Camera", typeof(Camera), typeof(AudioListener));
            cameraGo.transform.position = new Vector3(0f, 4.5f, -14f);
            cameraGo.transform.rotation = Quaternion.Euler(14f, 0f, 0f);
            var camera = cameraGo.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            // Shallow Cabo Verde water, so the models read against the right ground.
            camera.backgroundColor = new Color(0.05f, 0.30f, 0.42f);
            camera.tag = "MainCamera";
            camera.farClipPlane = 200f;

            // A key light, so the Simple Lit meshes actually show their form.
            var lightGo = new GameObject("Sun", typeof(Light));
            lightGo.transform.rotation = Quaternion.Euler(52f, -30f, 0f);
            var light = lightGo.GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.15f;
            light.color = new Color(0.85f, 0.95f, 1f);

            // Seafloor, so the sessile species have something to sit on and the
            // renderer's downward raycast finds ground.
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Seafloor";
            floor.transform.localScale = new Vector3(4f, 1f, 4f);
            var floorRenderer = floor.GetComponent<Renderer>();
            var sand = new Material(Shader.Find("Universal Render Pipeline/Simple Lit")
                                 ?? Shader.Find("Diffuse"));
            // Pale volcanic carbonate sand.
            if (sand.HasProperty("_BaseColor")) sand.SetColor("_BaseColor", new Color(0.78f, 0.73f, 0.62f));
            if (sand.HasProperty("_Color")) sand.SetColor("_Color", new Color(0.78f, 0.73f, 0.62f));
            floorRenderer.sharedMaterial = sand;

            // The harness itself.
            var reef = new GameObject("Living Reef Preview", typeof(LivingReefPreview));
            Selection.activeGameObject = reef;

            EditorGUIUtility.PingObject(reef);
            Debug.Log("[LivingReef] Preview scene ready. Press Play, then open the 'Reef' tab " +
                      "on the right-hand edge of the Game view. " +
                      "Untick 'Tiger Shark' on the Living Reef Preview object before pressing Play " +
                      "to watch the trophic cascade, or use its right-click menu while playing.");
        }
    }
}
