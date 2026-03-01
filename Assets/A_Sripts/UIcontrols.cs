using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem; // VR Input ke liye zaroori hai

public class UIcontrols : MonoBehaviour
{
    [SerializeField] private Canvas mainCanvas;
    [SerializeField] private GameObject playbutton, backButton, resetButton, doneButton, scorePanel, checkingText;

    [SerializeField] private WeldingHandle welderHandle;

    // VR mein humein Controller ki zarurat hai bajaye DragImage ke
    [SerializeField] private Transform rightControllerTransform;
    [SerializeField] private InputActionProperty activateAction; // Trigger button

    [SerializeField] private Camera mainCamera; // VR Main Camera
    [SerializeField] private WeldingType weldingType;
    [SerializeField] private ScoreSystem scoreSys;

    private bool startGame = false;
    public enum WeldingType { Mig, Tig };

    private void Awake()
    {
        // Initial setup
        backButton.gameObject.SetActive(false);
        resetButton.gameObject.SetActive(false);
        doneButton.gameObject.SetActive(false);
        scorePanel.gameObject.SetActive(false);

        Application.targetFrameRate = 90; // VR ke liye 90 FPS behtar hai
    }

    void Start()
    {
        MainPlayButton();
    }

    private void Update()
    {
        if (startGame)
        {
            // 1. Torch ko seedha Controller ki position par rakhen
            // Ab UISpaceToWorld ki zarurat nahi, hum seedha controller transform use karenge
            welderHandle.transform.position = rightControllerTransform.position;
            welderHandle.transform.rotation = rightControllerTransform.rotation;

            // 2. Trigger dabanay par welding start/stop
            float triggerValue = activateAction.action.ReadValue<float>();
            if (triggerValue > 0.1f)
            {
                welderHandle.StartWelding();
            }
            else
            {
                welderHandle.StopWelding();
            }

            // 3. Welding point detect karein
            welderHandle.GetWeldPoint();
        }
    }

    public void MainPlayButton()
    {
        startGame = true;
        welderHandle.gameObject.SetActive(true);
        backButton.gameObject.SetActive(true);
        resetButton.gameObject.SetActive(true);
        doneButton.gameObject.SetActive(true);
        playbutton.SetActive(false);
    }

    public void ResetButton()
    {
        RemoveAllWeldBlobs();
        scorePanel.SetActive(false);
        scoreSys.ResetPanel();
    }

    public void DoneButton()
    {
        startGame = false;
        checkingText.SetActive(true);
        float delay = (float)scoreSys.PopulateScores() + 0.2f;

        LeanTween.value(gameObject, 0, 1, delay).setOnComplete(() =>
        {
            checkingText.SetActive(false);
            var scoreResult = scoreSys.GetCurrentPanelScore();
            ShowResultPanel(scoreResult.uniformity, scoreResult.coverage, scoreResult.travel);
        });
    }

    private void ShowResultPanel(int uni, int cov, int spd)
    {
        scorePanel.SetActive(true);
        scorePanel.GetComponent<ScorePanel>().ShowPanel(uni, cov, spd);
    }

    private void RemoveAllWeldBlobs()
    {
        GameObject[] weldObjects = GameObject.FindGameObjectsWithTag("WeldObject");
        foreach (GameObject obj in weldObjects) Destroy(obj);

        GameObject[] holeObjects = GameObject.FindGameObjectsWithTag("WeldHole");
        foreach (GameObject obj in holeObjects) Destroy(obj);
    }
}