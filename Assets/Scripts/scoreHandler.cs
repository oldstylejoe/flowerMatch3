using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class scoreHandler : MonoBehaviour {

    private int m_score = 0;
    public Text m_displayText; 

    public void IncrementScore(int score)
    {
        m_score += score;
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        m_displayText.text = "Score: " + m_score.ToString();
    }

}
