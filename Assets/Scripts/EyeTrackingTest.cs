using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class EyeTrackingTest : MonoBehaviour
{
    public static EyeTrackingTest Instance;

    [System.Serializable]
    public struct Trial
    {
        public string scene_name;
        public bool static_rotation;
    }

    [Header("=== References ===")]
    public Transform head_ref;
    public Transform cal_sphere_ref;
    public Follower cal_follower_ref;
    public Transform gaze_target_ref;
    public AudioSource gaze_audiosrc_ref;
    public Transform head_cursor_ref;
    public Transform left_cursor_ref;
    public Transform right_cursor_ref;
    public Renderer head_cursor_renderer_ref;
    public Renderer left_cursor_renderer_ref;
    public Renderer right_cursor_renderer_ref;


    [Header("=== Settings ===")]
    public Trial[] trials;
    public bool randomize_trials;


    [Header("=== Outcomes (Read-Only) ===")]
    public Trial[] reshuffled_trials;
    public int current_trial_index = 0;
    public Vector3 head_cursor_local_pos = Vector3.zero;

    public CSVWriter writer;

    private void Awake()
    {
        // Initialization
        Instance = this;
        reshuffled_trials = Reshuffle<Trial>(trials, true);
        current_trial_index = -1;
        writer.Initialize();

        // First: calibration write + timestamp
        writer.AddPayload(-1);
        writer.AddPayload("Calibration");
        writer.WriteLine(true);
    }

    // The only thing we do at the start is allow the user to calibrate their head rotation forward.
    // The idea is that at the beginning, the `Calibration Sphere` is always hwading true forward
    // So we need to calibrate the head cursor to consider that as the primary rotation.
    // We do this by rotating the calibration sphere by an offset.
    public void CalibrateHeadForward()
    {
        cal_follower_ref.CalculateRotationOffset();
        head_cursor_ref.localPosition = head_ref.InverseTransformDirection(gaze_target_ref.position - head_ref.position).normalized * 15f;
    }

    // Trials will actually be added additively, to ensure that we don't lose any references to calibrations, etc.
    // When the `NextTrial()` button is pressed, we need to do several things:
    // 1. Unload the previously added trial
    // 2. Increment our trial index
    // 2a. If our next trial index is beyond the list of trials, then we're done; we close up shop.
    // 3. Load in the new additive scene
    // 4. Modify `Eye Session`'s components with the proper references
    // 5. Start the trial.
    public void NextTrial()
    {
        // If the calibration step wasn't completed (which we can check with its `completed` boolean), then initialize calibration
        // Otherwise, continue with loading the next scene.
        if (Calibrator.Instance != null && !Calibrator.Instance.completed)
        {
            Calibrator.Instance.Play();
            return;
        }

        // 1,2,3,4 are all covered below.
        current_trial_index += 1;
        if (current_trial_index >= reshuffled_trials.Length) {
            // End the scene!
#if UNITY_EDITOR
            EditorApplication.ExitPlaymode();
#else
            Application.Quit();
#endif
        }
        else LoadScene();
    }

    // Unity is a bit bonkers because, while in editor mode, additively adding and unloading scenes is blocked
    // The only way to do this is to use events attached to SceneManagement, to fix the unloadsceneasync problem too.
    private void LoadScene()
    {
        SceneManager.sceneLoaded += ActivatorAndUnloader;
        SceneManager.LoadScene(reshuffled_trials[current_trial_index].scene_name, LoadSceneMode.Additive);
    }
    private void ActivatorAndUnloader(Scene scene, LoadSceneMode mode) {
        SceneManager.sceneLoaded -= ActivatorAndUnloader;
        SceneManager.SetActiveScene(scene);
        InitializeScene();
        if (current_trial_index - 1 >= 0) SceneManager.UnloadSceneAsync(reshuffled_trials[current_trial_index - 1].scene_name);
    }


    public void InitializeScene()
    {
        // Write to our writer what our current trial is.
        writer.AddPayload(current_trial_index);
        writer.AddPayload(reshuffled_trials[current_trial_index].scene_name);
        writer.WriteLine(true);

        // Let the EyeSession know what trial we are on.
        // This operations also lets EyeSession get references to our own references.
        EyeSession.Instance.Initialize(
            reshuffled_trials[current_trial_index].scene_name,
            writer.dirName
        );

        // Let the Calibrator initialize its list of steps.
        // This operation also lets Calibrator get references to our own references.
        Calibrator.Instance.Initialize();

        // Force the follower to rotate or not
        cal_follower_ref.follow_rotation = reshuffled_trials[current_trial_index].static_rotation;

        // We don't force the calibrator to play JUST yet. We want the user to wait until they are prepared.
        // They should be able to start the next trial upon clicking the Start button again.
        // The only exception to this is if we're engaging in the first trial - then we force them to start the calibration.
        if (current_trial_index == 0) Calibrator.Instance.Play();
        else ToggleCursorRenderers(true);
    }

    public void ToggleCursorRenderers(bool set_to)
    {
        head_cursor_renderer_ref.enabled = set_to;
        left_cursor_renderer_ref.enabled = set_to;
        right_cursor_renderer_ref.enabled = set_to;
    }

    private T[] Reshuffle<T>(T[] original, bool randomize = true)
    {
        // Copy the array to prevent mutation. Copy only from the start to the end, exclusive
        T[] outcome = new T[original.Length];
        int k = 0;
        for (int i = 0; i < original.Length; i++)
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

    void OnApplicationQuit()
    {
        writer.Disable();
    }
}
