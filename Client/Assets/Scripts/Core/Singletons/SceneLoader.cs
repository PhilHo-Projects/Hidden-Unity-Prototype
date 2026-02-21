using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Core.Singletons
{
    public class SceneLoader : IndestructibleSingletonBehaviour<SceneLoader>
    {
        [SerializeField] private Animator transition;
        [SerializeField] private float transitionTime = 1f;
        [SerializeField] private CanvasGroup canvasGroup;

        protected override void OnSingletonAwake()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        public void LoadScene(string sceneName)
        {
            StartCoroutine(LoadSceneAsync(sceneName));
        }

        private IEnumerator LoadSceneAsync(string sceneName)
        {
            transition.SetTrigger("Start");
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
            asyncLoad.allowSceneActivation = false;

            yield return new WaitForSeconds(transitionTime);
    
            asyncLoad.allowSceneActivation = true;
    
            while (!asyncLoad.isDone)
            {
                yield return null;
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            canvasGroup.alpha = 1;
            transition.Play("Crossfade_End", -1, 0f);
        }
        
        protected override void OnSingletonDestroy()
        {
            canvasGroup.alpha = 0;
        }

    }
}
