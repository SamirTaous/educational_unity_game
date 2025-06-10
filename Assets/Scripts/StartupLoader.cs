using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class StartupLoaderWithFade : MonoBehaviour
{
    public string sceneToLoad = "SpaceMenu";
    public float delayBeforeFade = 1.5f;
    public float fadeDuration = 1f;

    private CanvasGroup canvasGroup;

    void Start()
    {
        canvasGroup = FindObjectOfType<CanvasGroup>();
        StartCoroutine(FadeAndLoad());
    }

    IEnumerator FadeAndLoad()
    {
        yield return new WaitForSeconds(delayBeforeFade);

        float t = 0;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, t / fadeDuration);
            yield return null;
        }

        SceneManager.LoadScene(sceneToLoad);
    }
}
