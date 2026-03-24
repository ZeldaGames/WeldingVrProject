using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class UIcontrols : MonoBehaviour
{
    [SerializeField] private Canvas mainCanvas;
    [SerializeField] private GameObject playbutton, backButton, resetButton, doneButton, scorePanel, checkingText;
    [SerializeField] private WeldingHandle welderHandle;

    [Header("VR Mode Toggle Setup")]
    [SerializeField] private Transform rightControllerTransform;
    [SerializeField] private GameObject controllerVisual; 
    [SerializeField] private GameObject handVisual;       
    [SerializeField] private InputActionProperty activateAction;   // Trigger
    [SerializeField] private InputActionProperty toggleModeAction; // Button (A/X) for Toggle

    [Header("Offsets")]
    [SerializeField] private Vector3 positionOffset;
    [SerializeField] private Vector3 handOffsetRot;

    [SerializeField] private Camera mainCamera;
    [SerializeField] private WeldingType weldingType;
    [SerializeField] private ScoreSystem scoreSys;

    private bool startGame = false;
    private bool isHandMode = false; // Toggle state
    public enum WeldingType { Mig, Tig };

    [Header("Laser Visuals")]
    [SerializeField] private GameObject laserLine; 

    private void Awake()
    {
        backButton.gameObject.SetActive(false);
        resetButton.gameObject.SetActive(false);
        doneButton.gameObject.SetActive(false);
        scorePanel.gameObject.SetActive(false);

        // Initial Visual State
        if (controllerVisual) controllerVisual.SetActive(true);
        if (handVisual) handVisual.SetActive(false);

        Application.targetFrameRate = 90;
    }

    void Start()
    {
        MainPlayButton();
    }

    private void Update()
    {
        // 1. Toggle Logic (Keyboard 'H' ya VR Button)
        if (Input.GetKeyDown(KeyCode.H) || toggleModeAction.action.WasPressedThisFrame())
        {
            ToggleHandController();
        }

        if (startGame)
        {
            float triggerValue = activateAction.action.ReadValue<float>();

            // AGAR welding ho rahi hai (Trigger dabaya hai) 
            // YA phir torch panel ke bilkul paas hai
            if (triggerValue > 0.1f || welderHandle.isWeldingLayer)
            {
                if (laserLine != null) laserLine.SetActive(false); 
            }
            else
            {
                if (laserLine != null) laserLine.SetActive(true); 
            }
            // 2. Torch Movement based on Mode
            if (isHandMode)
            {
                welderHandle.transform.position = handVisual.transform.TransformPoint(positionOffset);
                welderHandle.transform.rotation = handVisual.transform.rotation * Quaternion.Euler(handOffsetRot);
            }
            else
            {
                welderHandle.transform.position = rightControllerTransform.position;
                welderHandle.transform.rotation = rightControllerTransform.rotation;
            }

            // 3. Welding Trigger Logic
            triggerValue = activateAction.action.ReadValue<float>();
            if (triggerValue > 0.1f)
                welderHandle.StartWelding();
            else
                welderHandle.StopWelding();

            welderHandle.GetWeldPoint();
        }
    }

    private void ToggleHandController()
    {
        isHandMode = !isHandMode;
        if (controllerVisual) controllerVisual.SetActive(!isHandMode);
        if (handVisual) handVisual.SetActive(isHandMode);
        Debug.Log("Mode Switched. Hand Mode: " + isHandMode);
    }

    // --- Baki functions (Score/UI) waisay hi hain ---

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

   



        ShowWeldingControls(true);
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
        scorePanel.GetComponentInChildren<ScorePanel>().ShowPanel(uni, cov, spd);
    }

    private void RemoveAllWeldBlobs()
    {
        GameObject[] weldObjects = GameObject.FindGameObjectsWithTag("WeldObject");
        foreach (GameObject obj in weldObjects) Destroy(obj);

        GameObject[] holeObjects = GameObject.FindGameObjectsWithTag("WeldHole");
        foreach (GameObject obj in holeObjects) Destroy(obj);
    }

    public void BackButton()

    {

        //PlayCameraAnimation(CameraAnimation.ToTitle);



        RemoveAllWeldBlobs();



        //scoreSys.ShowPanel(false);



        StartCoroutine(ShowTitleSceneRoutine(2));

    }

    IEnumerator ShowTitleSceneRoutine(float delay)

    {

        //rightHandctrl.gameObject.SetActive(false);

        //leftHandctrl.gameObject.SetActive(false);

        backButton.gameObject.SetActive(false);

        resetButton.gameObject.SetActive(false);



        doneButton.gameObject.SetActive(false);

        scorePanel.gameObject.SetActive(false);



        startGame = false;



        yield return new WaitForSeconds(delay);

        playbutton.gameObject.SetActive(true);

    }

    public void NextButton()

    {

        //Get Next Panel

        // Save current panel score, then move to next panel

        //DoneButton(); // Optional: show results for current panel

        scoreSys.NextPanel();

        RemoveAllWeldBlobs();



        scorePanel.SetActive(false);



        ShowWeldingControls(true);

        //ResetButton();

    }

    public void RetryButton()

    {

        backButton.SetActive(true);

        ResetButton();



    }

    private void ShowWeldingControls(bool show)

    {

        //rightHandctrl.gameObject.SetActive(show);

        welderHandle.gameObject.SetActive(show);



        resetButton.SetActive(show);

        doneButton.SetActive(show);



        if (show)

        {

            startGame = true;

            //rightHandctrl.transform.position = rightCntrlOrigPos;

            //leftHandctrl.transform.position = lefthandCntrlOrigPos;





        }

    }
}