#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RocketBoost.Editor
{
    public static class Scene2Inspector
    {
        [MenuItem("Rocket Boost/Inspect Scene 2")]
        public static void InspectScene2()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Scene 2.unity", OpenSceneMode.Single);
            var roots = scene.GetRootGameObjects();
            Debug.Log($"[Scene2Inspector] Roots in Scene 2: {roots.Length}");
            foreach (var r in roots)
            {
                var osc = r.GetComponentInChildren<Oscillator>();
                string oscInfo = osc != null ? $" [HAS OSCILLATOR: {osc.gameObject.name}]" : "";
                Debug.Log($"Root GO: '{r.name}' active={r.activeSelf}{oscInfo}");
                foreach (Transform child in r.transform)
                {
                    var cOsc = child.GetComponent<Oscillator>();
                    string cOscInfo = cOsc != null ? " [OSC]" : "";
                    Debug.Log($"   Child: '{child.name}' pos={child.position}{cOscInfo}");
                }
            }
        }
    }
}
#endif
