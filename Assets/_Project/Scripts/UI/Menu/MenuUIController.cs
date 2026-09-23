using UnityEngine;
using UnityEngine.UI;

namespace MathGame.UI.Menu
{
    // Три экрана меню — панели одного канваса в сцене Menu, переключаются
    // здесь. Отдельных сцен на каждый экран нет: это чисто "витринная"
    // логика, Core про неё ничего не знает и знать не должен.
    public class MenuUIController : MonoBehaviour
    {
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private LevelSelectPanel levelSelectPanel;
        [SerializeField] private SettingsPanel settingsPanel;

        [SerializeField] private Button playButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private Button levelSelectBackButton;
        [SerializeField] private Button settingsBackButton;

        private void Awake()
        {
            playButton.onClick.AddListener(ShowLevelSelect);
            settingsButton.onClick.AddListener(ShowSettings);
            quitButton.onClick.AddListener(QuitGame);
            levelSelectBackButton.onClick.AddListener(ShowMain);
            settingsBackButton.onClick.AddListener(ShowMain);

            ShowMain();
        }

        private void ShowMain()
        {
            mainPanel.SetActive(true);
            levelSelectPanel.gameObject.SetActive(false);
            settingsPanel.gameObject.SetActive(false);
        }

        private void ShowLevelSelect()
        {
            mainPanel.SetActive(false);
            settingsPanel.gameObject.SetActive(false);
            levelSelectPanel.gameObject.SetActive(true);
            levelSelectPanel.Populate();
        }

        private void ShowSettings()
        {
            mainPanel.SetActive(false);
            levelSelectPanel.gameObject.SetActive(false);
            settingsPanel.gameObject.SetActive(true);
            settingsPanel.LoadFromService();
        }

        private static void QuitGame()
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}
