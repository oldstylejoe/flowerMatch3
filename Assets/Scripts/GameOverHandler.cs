using UnityEngine;
using UnityEngine.UI;

public class GameOverHandler : MonoBehaviour {

    public Text m_gameOverText;
    public Button m_gameOverButton;
    public Image m_gameOverButtonImage;

    public void GameOver()
    {
        m_gameOverText.enabled = true;
        m_gameOverButton.enabled = true;
        m_gameOverButtonImage.enabled = true;
    }

    public void GameStart()
    {
        m_gameOverText.enabled = false;
        m_gameOverButton.enabled = false;
        m_gameOverButtonImage.enabled = false;
    }

}
