#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RocketBoost.Editor
{
    public static class AllScenesSnapshotter
    {
        [MenuItem("Rocket Boost/Capture All Scene Snapshots")]
        public static void CaptureAll()
        {
            // Main Menu 3D Spaceport
            CaptureScene("Assets/Scenes/MainMenu.unity", "Assets/UI/MainMenu_Snapshot.png");

            // Sector 1: Baseline Sector
            CaptureScene("Assets/Scenes/Scene 1.unity", "Assets/UI/Scene1_Snapshot.png");

            // Sector 2: Starting view showing upgraded elevated spaceport launch apron, runway lights & command facility
            CaptureScene("Assets/Scenes/Scene 2.unity", "Assets/UI/Scene2_Start_Snapshot.png",
                new Vector3(-43.7f, 10.5f, -18f), new Vector3(-43.7f, 8.0f, 0f));

            // Sector 2: Direct view of the re-tuned fair moving obstacle at x = 15 with generous clearance
            CaptureScene("Assets/Scenes/Scene 2.unity", "Assets/UI/Scene2_Snapshot.png",
                new Vector3(15f, 9.0f, -22f), new Vector3(15f, 7.5f, 0f));

            // Sector 3: Starting view with Deep Space background, celestial bodies, and fair course
            CaptureScene("Assets/Scenes/Scene 3.unity", "Assets/UI/Scene3_Start_Snapshot.png",
                new Vector3(-43.7f, 6.5f, -14f), new Vector3(-40f, 6.5f, 0f));

            // Sector 3: Cinematic overview of the gauntlet course with ringed gas giant backdrop
            CaptureScene("Assets/Scenes/Scene 3.unity", "Assets/UI/Scene3_Snapshot.png", 
                new Vector3(0f, 16f, -42f), new Vector3(0f, 14f, 0f));
        }

        public static void CaptureScene(string scenePath, string outPngPath, Vector3? customCamPos = null, Vector3? customLookAt = null)
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            Camera cam = Camera.main;
            if (cam == null) return;

            Vector3 origPos = cam.transform.position;
            Quaternion origRot = cam.transform.rotation;

            if (customCamPos.HasValue)
            {
                cam.transform.position = customCamPos.Value;
                if (customLookAt.HasValue)
                {
                    cam.transform.LookAt(customLookAt.Value);
                }
            }

            int width = 960;
            int height = 540;
            RenderTexture rt = new RenderTexture(width, height, 24);
            cam.targetTexture = rt;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);

            cam.Render();
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();

            cam.targetTexture = null;
            RenderTexture.active = null;
            Object.DestroyImmediate(rt);

            if (customCamPos.HasValue)
            {
                cam.transform.position = origPos;
                cam.transform.rotation = origRot;
            }

            File.WriteAllBytes(outPngPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            Debug.Log($"[SNAPSHOT] Captured {scenePath} to {outPngPath}");
        }
    }
}
#endif
