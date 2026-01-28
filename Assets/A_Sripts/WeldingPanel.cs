using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using static WeldingPanel;

public enum WeldingType
{
    None,

    // Fillet welds
    F1, F2, F3, F4, F5, F6,

    // Groove welds
    G1, G2, G3, G4, G5, G6
}

public static class WeldingTypeExtensions
{
    public static string GetDisplayName(this WeldingType type)
    {
        switch (type)
        {
            case WeldingType.F1: return "1F";
            case WeldingType.F2: return "2F";
            case WeldingType.F3: return "3F";
            case WeldingType.F4: return "4F";
            case WeldingType.F5: return "5F";
            case WeldingType.F6: return "6F";
            case WeldingType.G1: return "1G";
            case WeldingType.G2: return "2G";
            case WeldingType.G3: return "3G";
            case WeldingType.G4: return "4G";
            case WeldingType.G5: return "5G";
            case WeldingType.G6: return "6G";
            default: return "None";
        }
    }
}

public class WeldingPanel : MonoBehaviour
{
    [SerializeField] private Collider weldingCollider;
    [SerializeField] private Transform[] panels;
    [SerializeField] private Material blobErrorMat, blobGoodMat;
    [SerializeField] private GameObject weldScanner;
    [SerializeField] private int checkTimeSec = 2;
    [SerializeField] private Transform[] checkingTransforms;

    private Transform checkerCapsule;
    private WeldCheckerLight checkerLight;
    private Vector3[] checkingPoints;

    public WeldingType weldingType;
    public TextMeshPro typeText;

    [SerializeField] private Vector3 textLocalPos;
    [SerializeField] private Vector3 textLocalRot;
    [SerializeField] private Vector3 textLocalScale;

    private int totalCount = 0; // Track total weld samples
    private int blobCount = 0;  // Track successful weld hits

    public struct WeldingStats
    {
        public float uniformity;
        public float coveragePercent;
        public float travel;
        public int badweldCount;
        public int holesCount;
    }

    private WeldingStats weldingStats;
    private bool isWeldingStatsDone = false;

    void Awake()
    {
        checkingPoints = checkingTransforms.Select(t => t.position).ToArray();
    }

    private void OnEnable()
    {
        UpdateTypeText();
    }

    public void UpdateTypeText()
    {
        if (typeText != null)
            typeText.text = weldingType.GetDisplayName();

        AttachTextToPanel(this.transform);
    }

    void ApplyTextTransform()
    {
        Transform t = typeText.transform;
        t.localPosition = textLocalPos;
        t.localRotation = Quaternion.Euler(textLocalRot);
        t.localScale = textLocalScale;
    }

    public void AttachTextToPanel(Transform panel)
    {
        Transform t = typeText.transform;
        t.SetParent(panel, false);
        ApplyTextTransform();
    }

    public void ReparentKeepWorld(Transform newPanel)
    {
        typeText.transform.SetParent(newPanel, true);
    }

    public void ReparentAndAlign(Transform newPanel)
    {
        Transform t = typeText.transform;
        t.SetParent(newPanel, false);
        t.localPosition = new Vector3(0.029f, -0.0225f, 0.1814f);
        t.localRotation = Quaternion.Euler(89.596f, 51.066f, 51.644f);
        t.localScale = new Vector3(0.0398f, 0.02281f, 0.02279f);
    }

    /// <summary>
    /// Populates welding stats by moving scanner along points.
    /// Works for 1F, 1D, and multi-point 1G welds.
    /// </summary>
    internal void PopulateWeldingStats(out int delayTimeSec)
    {
        delayTimeSec = checkTimeSec;
        isWeldingStatsDone = false;
        weldingStats = new WeldingStats();
        totalCount = 0;
        blobCount = 0;

        // Spawn scanner
        if (checkerCapsule == null)
            checkerCapsule = Instantiate(weldScanner, checkingTransforms[0].position, Quaternion.identity).transform;

        if (checkerLight == null)
            checkerLight = checkerCapsule.GetComponent<WeldCheckerLight>();

        checkerCapsule.rotation = checkingTransforms[0].rotation;

        // Precompute other stats
        weldingStats.uniformity = GetUniformity();
        weldingStats.travel = GetWeldTravelUniformity();
        weldingStats.badweldCount = GetBadWelds();
        weldingStats.holesCount = GetWeldHoles();

        // =========================================================
        // ❌ OLD LeanTween logic commented out
        /*
        LeanTween.move(checkerCapsule.gameObject, checkingPoints, checkTimeSec)
            .setOnUpdate((Vector3 pos) => { ... })
            .setOnComplete(() => { ... });
        */

        // =========================================================
        // ✅ NEW Multi-Point Segment Logic
        StartCoroutine(MoveAlongCheckingPoints(checkingPoints, checkTimeSec, () =>
        {
            if (checkerCapsule) Destroy(checkerCapsule.gameObject);

            weldingStats.coveragePercent = totalCount > 0
                ? (float)blobCount / totalCount
                : 0f;

            isWeldingStatsDone = true;
        }));
    }

    private IEnumerator MoveAlongCheckingPoints(Vector3[] points, float totalTime, System.Action onComplete)
    {
        if (points == null || points.Length < 2)
            yield break;

        // Calculate total path length
        float totalLength = 0f;
        float[] segmentLengths = new float[points.Length - 1];
        for (int i = 0; i < points.Length - 1; i++)
        {
            segmentLengths[i] = Vector3.Distance(points[i], points[i + 1]);
            totalLength += segmentLengths[i];
        }

        float minSegmentTime = 0.1f; // Minimum time per segment (seconds)

        for (int i = 0; i < points.Length - 1; i++)
        {
            Vector3 start = points[i];
            Vector3 end = points[i + 1];

            // Segment time proportional to segment length, but clamped to minimum
            float segmentTime = Mathf.Max(totalTime * (segmentLengths[i] / totalLength), minSegmentTime);

            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / segmentTime;
                t = Mathf.Min(t, 1f); // clamp to 1
                checkerCapsule.position = Vector3.Lerp(start, end, t);

                // Raycast check
                bool hasBlob = RaycastCheckWeld(checkerCapsule);
                totalCount++;
                if (hasBlob)
                {
                    blobCount++;
                    checkerLight.ShowColor(true);
                    checkerCapsule.GetComponent<AudioSource>().pitch = 1f;
                }
                else
                {
                    checkerLight.ShowColor(false);
                    checkerCapsule.GetComponent<AudioSource>().pitch = 1.3f;
                }

                yield return null;
            }
        }

        onComplete?.Invoke();
    }


    internal bool GetWeldResults(out WeldingStats stats)
    {
        stats = weldingStats;
        return isWeldingStatsDone;
    }

    private bool RaycastCheckWeld(Transform checkPos)
    {
        bool hasBlob = false;

        // ✅ NEW: Use local up direction for all welds
        Vector3 checkPosWithGap = checkPos.position + checkPos.up * 0.1f;
        if (Physics.Raycast(checkPosWithGap, -checkPos.up, out RaycastHit hit))
        {
            hasBlob = hit.transform.gameObject.layer == 9;
            //Debug.DrawRay(checkPosWithGap, -checkPos.up, hasBlob ? Color.green : Color.red, 0.1f);
        }

        return hasBlob;
    }

    private int GetBadWelds()
    {
        int count = panels.Sum(panel => panel.GetComponentsInChildren<WeldingBlobSet>().Length);

        LeanTween.delayedCall(checkTimeSec, () =>
        {
            foreach (var panel in panels)
            {
                foreach (var blob in panel.GetComponentsInChildren<WeldingBlobSet>())
                {
                    blob.gameObject.layer = 8;
                    blob.GetComponent<Renderer>().material = blobErrorMat;
                }
            }

            foreach (var blob in weldingCollider.transform.GetComponentsInChildren<WeldingBlobSet>())
            {
                blob.GetComponent<Renderer>().material = blobGoodMat;
            }
        });

        return count;
    }

    private int GetWeldHoles()
    {
        return GameObject.FindGameObjectsWithTag("WeldHole").Length;
    }

    private float GetUniformity()
    {
        GameObject[] weldObjects = GameObject.FindGameObjectsWithTag("WeldObject");
        if (weldObjects.Length == 0) return 0f;

        float minScale = weldObjects.Min(obj => obj.transform.localScale.x);
        float maxScale = weldObjects.Max(obj => obj.transform.localScale.x);
        return ((minScale + maxScale) / 2f) / maxScale;
    }

    // Weld Travel
    private List<float> weldTravels = new List<float>();
    internal void AddWeldTravel(float weldTravel) => weldTravels.Add(weldTravel);
    internal void ResetWeldTravel() => weldTravels.Clear();

    private float GetWeldTravelUniformity()
    {
        if (weldTravels.Count <= 10) return 0f;
        float idealTime = 0.419f;
        float averageTime = weldTravels.Average();
        return 1 - Mathf.Abs(idealTime - averageTime) / idealTime;
    }
}
