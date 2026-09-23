using UnityEngine;
using UnityEngine.SceneManagement;

namespace MathGame.Core
{
    // Лежит на объекте в сцене Boot вместе с GameServices. Единственная
    // задача — как только сервисы подняты (см. GameServices.Awake), сразу
    // перейти в главное меню.
    public class BootLoader : MonoBehaviour
    {
        private void Start()
        {
            SceneManager.LoadScene(SceneNames.Menu);
        }
    }
}
