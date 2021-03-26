using UnityEngine;

namespace MLAPIQuickStart
{
    public class SceneReference : MonoBehaviour
    {
        [SerializeField] private SceneScript sceneScript;
        public SceneScript SceneScript => sceneScript;
    }
}