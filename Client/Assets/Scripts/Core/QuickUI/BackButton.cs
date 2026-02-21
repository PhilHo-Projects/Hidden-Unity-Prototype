using Core.Singletons;
using UnityEngine;
using UnityEngine.UI;

namespace Core.QuickUI
{
    public class BackButton : MonoBehaviour
    {
        [SerializeField] Button backButton;
        void Start()
        {
            backButton.onClick.AddListener(() => SceneLoader.Instance.LoadScene("Intro"));

        }
    }
}
