using DeepU3.Asset;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeepU3.Async
{
    public class SceneWaitLoadedAsyncOperation : BaseAsyncOperation
    {
        private readonly string mSceneName;

        public SceneWaitLoadedAsyncOperation(Scene scene) : this(scene.name)
        {
        }

        public SceneWaitLoadedAsyncOperation(string sceneName)
        {
            mSceneName = sceneName;
            var s = SceneManager.GetSceneByName(sceneName);

            if (s.IsValid())
            {
                if (!AssetManager.IsSceneLoading(ref s))
                {
                    SetComplete(true);
                    return;
                }
            }
            else if (!AssetManager.IsSceneLoading(sceneName))
            {
                SetComplete(false);
                return;
            }

            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        protected override void OnDisposing()
        {
            base.OnDisposing();
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene s, LoadSceneMode mode)
        {
            if (IsDone)
            {
                return;
            }

            if (s.name == mSceneName)
            {
                SetComplete(true);
            }
        }
    }
}