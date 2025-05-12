// ReadingSceneManager.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class ReadingSceneManager : MonoBehaviour
{
    public TMP_Text passageText;
    public TMP_Text wordCountText;
    public Button startRecordingButton;
    public Button stopRecordingButton;
    public Button playRecordingButton;

    private List<float> recordedSamples = new List<float>();
    private int sampleRate = 44100;
    private AudioClip currentClip;
    private float recordingStartTime;
    private string microphoneDevice;
    private AudioSource audioSource;
    private bool isPlaying = false;
    private bool isRecording = false;
    private int currentPlaybackSample = 0;

    [System.Serializable]
    public class ReadingData
    {
        public string passage;
    }

    void Start()
    {
        microphoneDevice = Microphone.devices.Length > 0 ? Microphone.devices[0] : null;
        if (microphoneDevice == null)
        {
            Debug.LogError("No microphone detected!");
            startRecordingButton.interactable = false;
            stopRecordingButton.interactable = false;
            playRecordingButton.interactable = false;
        }

        stopRecordingButton.interactable = false;
        playRecordingButton.interactable = false;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        LoadReadingPassage();

        startRecordingButton.onClick.AddListener(StartOrPauseRecording);
        stopRecordingButton.onClick.AddListener(StopRecording);
        playRecordingButton.onClick.AddListener(PlayOrPauseRecording);
    }

    void LoadReadingPassage()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("readingtext");
        if (jsonFile != null)
        {
            ReadingData data = JsonUtility.FromJson<ReadingData>(jsonFile.text);
            passageText.text = data.passage;
            UpdateWordCount(data.passage);
        }
        else
        {
            passageText.text = "[Reading text not found]";
            Debug.LogError("readingtext.json not found in Resources folder.");
        }
    }

    void UpdateWordCount(string text)
    {
        int count = text.Split(new[] { ' ', '\n' }, System.StringSplitOptions.RemoveEmptyEntries).Length;
        wordCountText.text = count + " Words";
    }

    void StartOrPauseRecording()
    {
        if (isPlaying) return;

        if (!isRecording)
        {
            currentClip = Microphone.Start(microphoneDevice, false, 60, sampleRate);
            recordingStartTime = Time.time;
            stopRecordingButton.interactable = true;
            startRecordingButton.GetComponentInChildren<TMP_Text>().text = "Pause";
            playRecordingButton.interactable = false;
            isRecording = true;
        }
        else
        {
            int length = Microphone.GetPosition(microphoneDevice);
            Microphone.End(microphoneDevice);
            isRecording = false;

            if (length > 0 && currentClip != null)
            {
                float[] segment = new float[length * currentClip.channels];
                currentClip.GetData(segment, 0);
                recordedSamples.AddRange(segment);
            }

            stopRecordingButton.interactable = false;
            startRecordingButton.GetComponentInChildren<TMP_Text>().text = "Start";
            playRecordingButton.interactable = true;
        }
    }

    void StopRecording()
    {
        if (isRecording)
        {
            int length = Microphone.GetPosition(microphoneDevice);
            Microphone.End(microphoneDevice);
            isRecording = false;

            if (length > 0 && currentClip != null)
            {
                float[] segment = new float[length * currentClip.channels];
                currentClip.GetData(segment, 0);
                recordedSamples.AddRange(segment);
            }
        }

        stopRecordingButton.interactable = false;
        startRecordingButton.GetComponentInChildren<TMP_Text>().text = "Start";
        startRecordingButton.interactable = true;
        playRecordingButton.interactable = true;
    }

    void PlayOrPauseRecording()
    {
        if (recordedSamples.Count == 0) return;

        if (isPlaying)
        {
            audioSource.Pause();
            currentPlaybackSample = audioSource.timeSamples;
            isPlaying = false;
            playRecordingButton.GetComponentInChildren<TMP_Text>().text = "Play";
        }
        else
        {
            AudioClip combined = AudioClip.Create("CombinedRecording", recordedSamples.Count, 1, sampleRate, false);
            combined.SetData(recordedSamples.ToArray(), 0);

            audioSource.clip = combined;
            audioSource.timeSamples = currentPlaybackSample;
            audioSource.Play();

            isPlaying = true;
            playRecordingButton.GetComponentInChildren<TMP_Text>().text = "Pause";
        }
    }
}
