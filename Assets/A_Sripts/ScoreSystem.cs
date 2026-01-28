using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ScoreSystem : MonoBehaviour
{
    [SerializeField] private GameObject[] panelPrefabs; // Optional, if panels are spawned dynamically
    [SerializeField] private AudioClip panelDropSound;

    [SerializeField]
    private WeldingPanel[] panels;
    private int currentIndex = 0;
    private AudioSource audioSource;
    [SerializeField] private Transform panelDropTarget;
    [SerializeField] private Button resetButton;

    private Dictionary<WeldingPanel, WeldingScore> panelScores = new Dictionary<WeldingPanel, WeldingScore>();

    public struct WeldingScore
    {
        public int uniformity;
        public int coverage;
        public int travel;
    }

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        // If no panels assigned in inspector, find all active/inactive WeldingPanel objects in scene
        if (panels == null || panels.Length == 0)
            panels = FindObjectsOfType<WeldingPanel>(true); // 'true' to include inactive

        if (panels == null || panels.Length == 0)
        {
            Debug.LogError("No WeldingPanels found in scene!");
            return;
        }

        // Disable all panels initially
        foreach (var p in panels)
            p.gameObject.SetActive(false);

        // Activate first panel
        currentIndex = 0;
        panels[currentIndex].gameObject.SetActive(true);
        //panelDropTarget= panels[currentIndex].transform;
        panelDropTarget.SetPositionAndRotation(panels[currentIndex].transform.position, panels[currentIndex].transform.rotation);
    }

    internal int PopulateScores()
    {
        return CalculatePanelScore(currentPanel: panels[currentIndex]);
    }

    private int CalculatePanelScore(WeldingPanel currentPanel)
    {
        currentPanel.PopulateWeldingStats(out int checkTimeSec);

        // Store score after population completes
        currentPanel.GetWeldResults(out WeldingPanel.WeldingStats weldingResults);

        WeldingScore score = new WeldingScore();

        if (weldingResults.holesCount > 0)
            score.uniformity = 0;
        else
            score.uniformity = Mathf.Clamp((int)Mathf.Round(weldingResults.uniformity * 100) - weldingResults.badweldCount, 0, 100);

        score.coverage = (int)Mathf.Round((weldingResults.coveragePercent * 100));
        score.travel = (int)Mathf.Round((weldingResults.travel * 100));

        // Store per-panel score
        panelScores[currentPanel] = score;

        return checkTimeSec;
    }

    internal WeldingScore GetCurrentPanelScore()
    {
        panels[currentIndex].GetWeldResults(out WeldingPanel.WeldingStats stats);

        WeldingScore score = new WeldingScore
        {
            uniformity = (stats.holesCount > 0) ? 0 : Mathf.Clamp((int)Mathf.Round(stats.uniformity * 100) - stats.badweldCount, 0, 100),
            coverage = (int)Mathf.Round(stats.coveragePercent * 100),
            travel = (int)Mathf.Round(stats.travel * 100)
        };

        return score;
    }


    internal WeldingScore GetOverallScore()
    {
        WeldingScore total = new WeldingScore();

        if (panelScores.Count == 0) return total;

        total.uniformity = (int)panelScores.Values.Average(s => s.uniformity);
        total.coverage = (int)panelScores.Values.Average(s => s.coverage);
        total.travel = (int)panelScores.Values.Average(s => s.travel);

        return total;
    }

    internal void NextPanel()
    {
        // Deactivate current panel
        panels[currentIndex].gameObject.SetActive(false);

        currentIndex++;

        if (currentIndex >= panels.Length)
        {
            // All panels finished → reload scene
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            return;
        }

        // Activate next panel
        panels[currentIndex].gameObject.SetActive(true);
        this.gameObject.SetActive(false);
        ResetPanel();
    }

    internal void ResetPanel()
    {
        panels[currentIndex].ResetWeldTravel();
        AnimatePanel();
    }

    internal void AnimatePanel()
    {
        WeldingPanel panel = panels[currentIndex];
        panelDropTarget.SetPositionAndRotation(panel.transform.position, panel.transform.rotation);
        Vector3 startPos = panel.transform.position;
        Vector3 liftPos = startPos + panel.transform.up * 0.06f;
        //LeanTween.move(panel.gameObject, panelDropTarget.position, 0.2f);
         resetButton.enabled = false;
        LeanTween.move(panel.gameObject, liftPos, 0.8f).setOnComplete(() =>
        {
            LeanTween.move(panel.gameObject, panelDropTarget.position, 0.25f)
                .setEase(LeanTweenType.easeInSine)
                .setOnComplete(() =>
                {
                    resetButton.enabled=true;
                    if (audioSource)
                        audioSource.PlayOneShot(panelDropSound);
                });
        });
    }

}
