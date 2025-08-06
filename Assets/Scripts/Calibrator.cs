using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[ExecuteInEditMode]
public class Calibrator : MonoBehaviour
{

    public static Calibrator Instance;

    [System.Serializable]
    public struct CalibrationStep
    {
        public string debug_name;
        [Header("=== Target ===")]
        public float theta_degrees;
        [Range(0f, 1f)] public float phi_ratio;
        public float radius;
        [Header("=== Settings ===")]
        [Tooltip("If set to 0, then the transition is instantaneous")]
        public float transition_speed;
        public float static_duration;
        public bool play_sound_cue;
    }

    [Header("=== References ===")]
    public Transform gaze_target_ref;
    public AudioSource audio_source_ref;
    public CalibrationParams session_parameters;

    [Header("=== Calibration ===")]
    public CalibrationStep[] steps;
    public bool randomize_steps = true;
    public int calibration_index = 0;
    public bool playing = false;
    public bool completed = false;
    private Vector3 target_dir;
    public UnityEvent onCalibrationEnd;

    public float theta_degrees;
    [Range(0f, 1f)] public float phi_ratio;
    public float phi_degrees => phi_ratio * 360f;
    public float radius;

    private Coroutine step_routine;
    public float simulated_frame_timestep = 1f / 90f;

    private void Awake()
    {
        Instance = this;
    }

    public void Initialize() {
        // Check that parameters are even set.
        if (session_parameters == null) {
            Debug.LogError("Cannot initialize calibration without a reference to parameters");
            return;
        }


        // Start to load in calibration steps. Always load the first step
        steps = new CalibrationStep[session_parameters.steps.Length];
        steps[0] = session_parameters.steps[0];

        // Define the next set of steps. If randomization is toggled,, then we need to randomize
        CalibrationStep[] substeps = Reshuffle<CalibrationStep>(
            session_parameters.steps,
            1,
            session_parameters.steps.Length,
            randomize_steps
        );
        for (int i = 0; i < substeps.Length; i++) steps[i + 1] = substeps[i];

        // Set calibration index to 0
        calibration_index = 0;

        // Get all references from EyeTrackingTest
        gaze_target_ref = EyeTrackingTest.Instance.gaze_target_ref;
        audio_source_ref = EyeTrackingTest.Instance.gaze_audiosrc_ref;
    }

    public void PrevStep()
    {
        calibration_index = (calibration_index == 0) ? steps.Length - 1 : calibration_index - 1;
        RunStep();
    }
    public void NextStep(bool play_all = false)
    {
        // If `play_all` is TRUE, then we're running through all steps.
        // In this case, don't do anything if we've already reached the end.
        if (calibration_index == steps.Length - 1)
        {
            Debug.Log("Completed run-through!");
            playing = false;
            completed = true;
            return;
        }
        calibration_index = (calibration_index == steps.Length - 1) ? 0 : calibration_index + 1;
        RunStep(play_all);
    }
    public void Play()
    {
        calibration_index = 0;
        playing = true;
        if (EyeTrackingTest.Instance != null) EyeTrackingTest.Instance.ToggleCursorRenderers(false);
        RunStep(true);
    }
    public void RunStep(bool play_all = false)
    {
        if (step_routine != null) StopCoroutine(step_routine);
        step_routine = StartCoroutine(StepOperation(play_all));
    }

    public IEnumerator StepOperation(bool play_all)
    {
        if (gaze_target_ref == null)
        {
            Debug.LogError("Cannot Play step: target reference not set");
            yield break;
        }

        // What's the current step?
        CalibrationStep cur_step = steps[calibration_index];

        // Set the destination data
        theta_degrees = steps[calibration_index].theta_degrees;
        phi_ratio = steps[calibration_index].phi_ratio;
        radius = steps[calibration_index].radius;
        target_dir = CalculateLocalVector(theta_degrees, phi_degrees, radius);
        if (EyeSession.Instance != null) EyeSession.Instance.UpdateWriter("Target Loaded", target_dir);

        // If the current step has a transition speed > 0f, then we'll initiate an operation for this
        if (cur_step.transition_speed > 0f)
        {
            if (EyeSession.Instance != null) EyeSession.Instance.UpdateWriter("Targit Transitioning", target_dir);
            Vector3 diff = target_dir - gaze_target_ref.localPosition;
            while (Vector3.Distance(gaze_target_ref.localPosition, target_dir) > 0.05f)
            {
                diff = target_dir - gaze_target_ref.localPosition;
                gaze_target_ref.position += diff.normalized * cur_step.transition_speed * Time.unscaledDeltaTime;
                yield return null;
            }
        }
        if (EyeSession.Instance != null) EyeSession.Instance.UpdateWriter("Target Set", target_dir);
        gaze_target_ref.localPosition = target_dir;
        yield return null;

        // Play sound bite if needed
        if (cur_step.play_sound_cue && audio_source_ref != null) audio_source_ref.Play();

        // For the remainder of the step's duration, we wait until the duration ends
        yield return new WaitForSeconds(cur_step.static_duration);

        // Run the next step if necessary
        if (play_all) NextStep(play_all);
    }

    // Knuth shuffle algorithm :: courtesy of Wikipedia
    private T[] Reshuffle<T>(T[] original, int start_index, int end_index, bool randomize = true)
    {
        // Copy the array to prevent mutation. Copy only from the start to the end, exclusive
        T[] outcome = new T[end_index - start_index];
        int k = 0;
        for (int i = start_index; i < end_index; i++)
        {
            outcome[k] = original[i];
            k++;
        }

        // Iterate through the outcome to randomize, if needed
        if (randomize)
        {
            for (int i = 0; i < outcome.Length; i++)
            {
                T tmp = outcome[i];
                int r = Random.Range(i, outcome.Length);
                outcome[i] = outcome[r];
                outcome[r] = tmp;
            }
        }

        // Return the randomized subarray
        return outcome;
    }

    private void Update()
    {
        // If we're playing, then we auto-update
        if (EyeSession.Instance != null && playing) {
            EyeSession.Instance.UpdateWriter("Playing", target_dir);
        }
    }

    private void LateUpdate()
    {
        if (completed) onCalibrationEnd?.Invoke();
    }

    public Vector3 CalculateLocalVector(float theta_d, float phi_d, float r)
    {
        float theta_rad = Mathf.Deg2Rad * theta_d;
        float phi_rad = Mathf.Deg2Rad * phi_d;

        // calculate spherical coordinates in local space
        float x = Mathf.Sin(theta_rad) * Mathf.Cos(phi_rad);
        float y = Mathf.Sin(theta_rad) * Mathf.Sin(phi_rad);
        float z = Mathf.Cos(theta_rad);

        Vector3 local_dir = Vector3.Normalize(new Vector3(x, y, z)) * r;
        return local_dir;
    }
}
