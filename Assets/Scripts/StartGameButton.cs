using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartGameButton : MonoBehaviour
{
    [SerializeField] private string sceneName = "GameScene";
    private Button button;
    
    private void Awake()
    {
        button = GetComponent<Button>();
    }

    private void OnEnable()
    {
        button?.onClick.AddListener(StartGame);
    }

    private void OnDisable()
    {
        button?.onClick.RemoveListener(StartGame);
    }

    public void StartGame()
    {
        SceneManager.LoadScene(sceneName);
    }
}
