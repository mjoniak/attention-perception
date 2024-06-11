using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Random = System.Random;

public class Trial : MonoBehaviour
{
    public int NoSoundRuns = 20;
    public int SyncSoundRuns = 20;
    public int UnsyncSoundRuns = 20;
    public int PhaseShiftRuns = 20;
    public int BlockSize = 40;
    public int TrainingSetSize = 20;

    public float TargetPeriodSec = 0.5f;
    public float[] DistractorPeriodSec = { 0.125f, 0.25f, 1.0f, 1.5f, 2.0f };
    public float TargetPhaseShiftSec = 0.0f;

    public float ColorChangeTimeSec = 0.0f;
    public float FixationTimeSec = 2.0f;

    public double DistractorDensity = 0.8;
    public float Radius;
    public int Columns;
    public int Rows;
    public float FirstRowHeight;
    public float RowSize;
    public float StimuliDegrees;

    public float BaseG;

    public bool NaturalGravity;

    public Material[] DistractorCubeMaterials;
    public Material[] DistractorSphereMaterials;
    public PrimitiveType TargetShape = PrimitiveType.Sphere;
    public Material TargetMaterial;
    public Texture2D ArrowLeft;

    public AudioClip BounceSound;

    public Canvas InstructionsCanvas;
    public Canvas CorrectCanvas;
    public Canvas IncorrectCanvas;
    public Canvas BlockIntermissionCanvas;
    public Canvas TrainingCanvas;
    public GameObject FixationCross;
    public GameObject Shelves;

    public InputActionProperty LeftInputActionProperty;
    public InputActionProperty RightInputActionProperty;

    private TrialResults trialResults;

    private List<GameObject> allStimuli = new List<GameObject>();
    private RunType runType = RunType.SYNC_SOUND;

    private Random random = new Random();

    private int absoluteTrialCount = -1;

    private bool intermission = true;
    private bool fixation = false;
    private bool blockIntermission = false;
    private bool training = false;

    private List<RunType> trialRunTypes = new List<RunType>();

    void Start()
    {
        trialRunTypes.AddRange(Enumerable.Repeat(RunType.NO_SOUND, NoSoundRuns));
        trialRunTypes.AddRange(Enumerable.Repeat(RunType.SYNC_SOUND, SyncSoundRuns));
        trialRunTypes.AddRange(Enumerable.Repeat(RunType.UNSYNC_SOUND, UnsyncSoundRuns));
        trialRunTypes.AddRange(Enumerable.Repeat(RunType.PHASE_SHIFT_SOUND, PhaseShiftRuns));
        Shuffle(trialRunTypes);

        InitResults(-1);

        LeftInputActionProperty.action.performed += p => OnButtonPressed(true);
        RightInputActionProperty.action.performed += p => OnButtonPressed(false);

        training = true;
        intermission = true;
        TrainingCanvas.gameObject.SetActive(true);

        // NextTrial(0);
    }

    private void OnButtonPressed(bool left)
    {
        Debug.Log("Button pressed: fixation=" + fixation + ", intermission=" + intermission);
        if (fixation) return;
        else if (intermission) OnButtonPressedInIntermissionScreen();
        else OnButtonPressedInTrial(left);
    }

    private void OnButtonPressedInTrial(bool left)
    {
        Debug.Log("Button pressed in trial");
        trialResults.endTime = Time.time;
        trialResults.correct = trialResults.stimuliGrid[trialResults.targetRow].columns[trialResults.targetColumn].left == left;

        var json = JsonUtility.ToJson(trialResults, prettyPrint: true);
        var filename = DateTime.Now.ToString("yyyy-MM-dd--HH-mm-ss") + "-trial-" + this.absoluteTrialCount + ".json";
        Debug.Log(json);
        File.WriteAllText(Application.persistentDataPath + "/" + filename, json);

        if (trialResults.correct)
        {
            Debug.Log("Right!");
            CorrectCanvas.gameObject.SetActive(true);
            intermission = true;
        }
        else
        {
            Debug.Log("Wrong!");
            IncorrectCanvas.gameObject.SetActive(true);
            intermission = true;
        }

        allStimuli.ForEach(Destroy);
        allStimuli = new List<GameObject>();
    }

    private void OnButtonPressedInIntermissionScreen()
    {
        Debug.Log("Button pressed in intermission");
        CorrectCanvas.gameObject.SetActive(false);
        IncorrectCanvas.gameObject.SetActive(false);
        InstructionsCanvas.gameObject.SetActive(false);

        if (training) {
            Debug.Log("Next training");
            TrainingCanvas.gameObject.SetActive(false);
            if (trialResults.trialCount != TrainingSetSize - 1) {
                FixationCross.SetActive(true);
                Shelves.SetActive(false);
                intermission = false;
                fixation = true;
                StartCoroutine(ScheduleNextTrial());
            } else {
                Debug.Log("End training");
                InstructionsCanvas.gameObject.SetActive(true);
                training = false;
                intermission = true;
                trialResults.trialCount = -1;
            } 
        } else if ((trialResults.trialCount + 1) % BlockSize != 0 || blockIntermission) {
            Debug.Log("Next real");
            FixationCross.SetActive(true);
            Shelves.SetActive(false);
            intermission = false;
            fixation = true;
            blockIntermission = false;

            StartCoroutine(ScheduleNextTrial());
        } else {
            Debug.Log("Block intermission");
            BlockIntermissionCanvas.gameObject.SetActive(true);
            blockIntermission = true;
        }

    }

    private IEnumerator ScheduleNextTrial() {
        yield return new WaitForSeconds(FixationTimeSec);

        if (trialResults.trialCount + 1 < trialRunTypes.Count)
        {
            NextTrial(trialResults.trialCount + 1);
        }
        else
        {
            Debug.Log("Quit");
            Application.Quit();
        }
    }

    private void NextTrial(int trialCount) {
        if (!training) {
            Debug.Log("Real run #" + trialCount);
            runType = trialRunTypes[trialCount];
        } else {
            Debug.Log("Training run #" + trialCount);
            int trialsPerRunType = TrainingSetSize / 4;
            int currentRunType = trialCount / trialsPerRunType;
            Debug.Log("Trials per type: " + trialsPerRunType + ", current type = " + currentRunType);
            runType = currentRunType == 0 ? RunType.SYNC_SOUND
                : currentRunType == 1 ? RunType.NO_SOUND
                : currentRunType == 2 ? RunType.UNSYNC_SOUND
                : RunType.PHASE_SHIFT_SOUND;
        }

        runType = RunType.SYNC_SOUND;

        Debug.Log("Starting trial " + trialCount);
        Debug.Log("Type " + runType.ToString());
        InstructionsCanvas.gameObject.SetActive(false);
        FixationCross.SetActive(false);
        Shelves.SetActive(true);
        this.fixation = false;
        this.absoluteTrialCount += 1;

        InitResults(trialCount);
        GenerateStimuli();
        trialResults.startTime = Time.time;
    }

    private void GenerateStimuli()
    {
        float angleStep = StimuliDegrees / (Columns - 1);
        bool distractorWithAudioSelected = false;

        for (int i = 0; i < Rows; i++)
        {
            trialResults.stimuliGrid.Add(new ColumnWrapper { columns = new List<StimuliData>() });
            float angle = (180 - StimuliDegrees) / 2;
            Vector3 lookTarget = new Vector3(0, FirstRowHeight + RowSize * i, 0);

            for (int j = 0; j < Columns; j++)
            {
                float x = Radius * Mathf.Cos(angle * Mathf.Deg2Rad);
                float y = FirstRowHeight + RowSize * i;
                float z = Radius * Mathf.Sin(angle * Mathf.Deg2Rad);

                if (i == trialResults.targetRow && j == trialResults.targetColumn)
                {
                    var stimuliData = new StimuliData
                    {
                        i = i,
                        j = j,
                        angle = angle,
                        shape = TargetShape,
                        material = 0,
                        left = random.NextDouble() < 0.5,
                        periodSec = TargetPeriodSec,
                        phaseShiftSec = runType == RunType.PHASE_SHIFT_SOUND ? TargetPhaseShiftSec : 0.0f,
                        colorChangeTime = ColorChangeTimeSec,
                        target = true,
                        audio = runType == RunType.SYNC_SOUND || runType == RunType.PHASE_SHIFT_SOUND
                    };
                    trialResults.stimuliGrid[i].columns.Add(stimuliData);

                    var material = ColorChangeTimeSec > 0.0f ? DistractorSphereMaterials[random.Next(DistractorSphereMaterials.Length)] : TargetMaterial;
                    AddStimuli(lookTarget, x, y, z, material, stimuliData);
                }
                else if (random.NextDouble() <= DistractorDensity)
                {
                    float cubeProbability = (float)DistractorCubeMaterials.Length / (DistractorCubeMaterials.Length + DistractorSphereMaterials.Length);
                    var distractorShape = random.NextDouble() < cubeProbability ? PrimitiveType.Cube : PrimitiveType.Sphere;
                    var materials = distractorShape == PrimitiveType.Cube ? DistractorCubeMaterials : DistractorSphereMaterials;

                    var stimuliData = new StimuliData
                    {
                        i = i,
                        j = j,
                        angle = angle,
                        shape = distractorShape,
                        material = random.Next(materials.Length),
                        left = random.NextDouble() < 0.5,
                        // periodSec = TargetPeriodSec + random.Next(1, 5) * 0.1f * (random.Next(0, 1) * 2 - 1),
                        periodSec = DistractorPeriodSec[random.Next(DistractorPeriodSec.Length)],
                        phaseShiftSec = 0.0f,
                        colorChangeTime = 0.0f,
                        target = false,
                        audio = runType == RunType.UNSYNC_SOUND && !distractorWithAudioSelected
                    };
                    trialResults.stimuliGrid[i].columns.Add(stimuliData);

                    AddStimuli(lookTarget, x, y, z, materials[stimuliData.material], stimuliData);
                    distractorWithAudioSelected = runType == RunType.UNSYNC_SOUND;
                }
                else
                {
                    trialResults.stimuliGrid[i].columns.Add(new StimuliData
                    {
                        i = i,
                        j = j,
                        angle = angle,
                        target = false,
                        empty = true
                    });
                }

                angle += angleStep;
            }
        }
    }

    private GameObject AddStimuli(Vector3 lookTarget, float x, float y, float z, Material material, StimuliData stimuliData)
    {
        GameObject stimuli = GameObject.CreatePrimitive(stimuliData.shape);
        stimuli.GetComponent<Renderer>().material = material;
        AddArrow(lookTarget, stimuli, stimuliData.left);
        AddBouncingScript(stimuli,
            periodSec: stimuliData.periodSec,
            phaseShiftSec: stimuliData.phaseShiftSec,
            colorChangeTimeSec: ColorChangeTimeSec,
            finalMaterial: stimuliData.target ? TargetMaterial : null,
            bounceSound: stimuliData.audio ? BounceSound : null
        );
        SetPosition(stimuli, lookTarget, x, y, z);
        allStimuli.Add(stimuli);
        return stimuli;
    }

    private void InitResults(int trialCount)
    {
        trialResults = new TrialResults
        {
            trialCount = trialCount,
            targetRow = random.Next(Rows),
            targetColumn = random.Next(Columns),
            stimuliGrid = new List<ColumnWrapper>(),
            targetPeriodSec = TargetPeriodSec,
            distractorDensity = DistractorDensity,
            radius = Radius,
            columns = Columns,
            rows = Rows,
            firstRowHeight = FirstRowHeight,
            rowSize = RowSize,
            stimuliDegrees = StimuliDegrees,
            runType = runType.ToString(),
            colorChangeTimeSec = ColorChangeTimeSec,
            blockSize = BlockSize,
            training = training
        };
    }

    private void AddArrow(Vector3 lookTarget, GameObject stimuli, bool left)
    {
        var spriteObject = new GameObject();
        var spriteRenderer = spriteObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = Sprite.Create(ArrowLeft, new Rect(0, 0, ArrowLeft.width, ArrowLeft.height), new Vector2(0.5f, 0.5f), 100.0f);
        spriteRenderer.color = Color.black;
        // spriteRenderer.color = stimuli.GetComponent<Renderer>().material.color;
        // stimuli.GetComponent<Renderer>().enabled = false;
        spriteRenderer.flipX = !left;
        spriteObject.transform.parent = stimuli.transform;
        spriteObject.transform.position = new Vector3(0.0f, 0.0f, 0.6f);
        spriteObject.transform.localScale = new Vector3(4.0f, 4.0f, 4.0f);
        spriteObject.transform.LookAt(new Vector2(lookTarget.x, lookTarget.z));
    }

    private static void SetPosition(GameObject stimuli, Vector3 lookTarget, float x, float y, float z)
    {
        stimuli.transform.position = new Vector3(x, y, z);
        stimuli.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        stimuli.transform.LookAt(lookTarget);
    }

    private void AddBouncingScript(
        GameObject stimuli,
        float periodSec,
        float phaseShiftSec = 0.0f,
        float colorChangeTimeSec = 0.0f,
        AudioClip bounceSound = null,
        Material finalMaterial = null)
    {
        var script = stimuli.AddComponent(typeof(BouncingScript)) as BouncingScript;
        if (NaturalGravity) 
            script.G = BaseG;
        else
            script.G /= 4 * BaseG * periodSec * periodSec;
        script.PeriodSec = periodSec;
        script.BounceSound = bounceSound;
        script.SoundPhaseShiftSec = phaseShiftSec;
        script.ColorChangeTimeSec = colorChangeTimeSec;
        script.FinalMaterial = finalMaterial;
    }

    private void Shuffle<T>(List<T> ts) {
        var count = ts.Count;
        var last = count - 1;
        for (var i = 0; i < last; ++i) {
            var r = random.Next(i, count);
            var tmp = ts[i];
            ts[i] = ts[r];
            ts[r] = tmp;
        }
    }

    [Serializable]
    public struct StimuliData 
    {
        public int i, j;
        public float angle;
        public PrimitiveType shape;
        public int material;
        public bool left;
        public float periodSec;
        public float phaseShiftSec;
        public float colorChangeTime;
        public bool target;
        public bool empty;
        public bool audio;
    }

    [Serializable]
    public struct TrialResults 
    {
        public int trialCount;
        public int targetRow, targetColumn;
        public bool correct;
        public float startTime, endTime;
        public List<ColumnWrapper> stimuliGrid;
        public string runType;
        public float colorChangeTimeSec;

        public float targetPeriodSec;
        public double distractorDensity;
        public float radius;
        public int columns;
        public int rows;
        public float firstRowHeight;
        public float rowSize;
        public float stimuliDegrees;
        public int blockSize;
        public bool training;
    }

    [Serializable]
    public struct ColumnWrapper 
    {
        public List<StimuliData> columns;
    }

    public enum RunType 
    {
        NO_SOUND,
        SYNC_SOUND,
        UNSYNC_SOUND,
        PHASE_SHIFT_SOUND
    }
}