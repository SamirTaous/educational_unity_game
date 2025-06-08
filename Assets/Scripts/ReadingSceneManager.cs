using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class ReadingSceneManager : MonoBehaviour
{
    public TMP_Text passageText;
    public TMP_Text wordCountText;

    public Button startRecordingButton;
    public Button stopRecordingButton;
    public Button playRecordingButton;
    public Button saveRecordingButton;

    public GameObject passagePanel;
    public GameObject startRecordingButtonObject;
    public GameObject stopRecordingButtonObject;
    public GameObject playRecordingButtonObject;

    public GameObject finishPanel;
    public TMP_Text finishTitleText;
    public TMP_Text finishScoreText;
    public Button restartButton;
    public Button backToMenuButton;

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
    public class TextWrapper
    {
        public TextData text;
    }

    [System.Serializable]
    public class TextData
    {
        public string id;
        public string text_content;
    }

    [System.Serializable]
    public class RecordingData
    {
        public string id;
        public string original_text;
        public string audio_base64;
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
            saveRecordingButton.interactable = false;
        }

        stopRecordingButton.interactable = false;
        playRecordingButton.interactable = false;
        saveRecordingButton.interactable = true;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        StartCoroutine(LoadReadingPassageByIndex(SessionData.selectedTextIndex));

        startRecordingButton.onClick.AddListener(StartOrPauseRecording);
        stopRecordingButton.onClick.AddListener(StopRecording);
        playRecordingButton.onClick.AddListener(PlayOrPauseRecording);
        saveRecordingButton.onClick.AddListener(SaveRecording);

        restartButton.onClick.AddListener(RestartScene);
        backToMenuButton.onClick.AddListener(GoToMainMenu);

        if (finishPanel != null)
            finishPanel.SetActive(false);
    }

    IEnumerator LoadReadingPassageByIndex(int index)
    {
        string url = $"http://localhost:5000/api/text-by-index/{index}";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Failed to load text from API: " + request.error);
                if (passageText != null) passageText.text = "[Failed to load reading text]";
            }
            else
            {
                string json = request.downloadHandler.text;
                TextWrapper wrapper = JsonUtility.FromJson<TextWrapper>(json);
                if (wrapper != null && wrapper.text != null && !string.IsNullOrEmpty(wrapper.text.text_content))
                {
                    string passage = wrapper.text.text_content;
                    if (passageText != null) passageText.text = passage;
                    if (wordCountText != null) UpdateWordCount(passage);
                }
                else
                {
                    Debug.LogError("Parsed response is null or missing text_content.");
                }
            }
        }
    }

    void UpdateWordCount(string text)
    {
        if (wordCountText == null || string.IsNullOrEmpty(text)) return;
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

    void SaveRecording()
    {
        if (recordedSamples.Count == 0 || passageText == null || string.IsNullOrEmpty(passageText.text))
        {
            Debug.LogWarning("No recording or passage text to save.");
            return;
        }

        byte[] wavBytes = ConvertToWavBytes(recordedSamples.ToArray(), sampleRate);
        string audioBase64 = System.Convert.ToBase64String(wavBytes);

        RecordingData payload = new RecordingData
        {
            id = SessionData.user_id,
            original_text = passageText.text,
            audio_base64 = audioBase64
        };

        string json = JsonUtility.ToJson(payload);
        StartCoroutine(PostRecording(json));
    }

    byte[] ConvertToWavBytes(float[] samples, int frequency)
    {
        int headerSize = 44;
        int sampleCount = samples.Length;
        int byteCount = sampleCount * 2;

        using (MemoryStream stream = new MemoryStream(headerSize + byteCount))
        using (BinaryWriter writer = new BinaryWriter(stream))
        {
            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(headerSize + byteCount - 8);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
            writer.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)1);
            writer.Write(frequency);
            writer.Write(frequency * 2);
            writer.Write((short)2);
            writer.Write((short)16);

            writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            writer.Write(byteCount);

            foreach (float s in samples)
            {
                short val = (short)(Mathf.Clamp(s, -1f, 1f) * short.MaxValue);
                writer.Write(val);
            }

            return stream.ToArray();
        }
    }

    IEnumerator PostRecording(string jsonPayload)
    {
        string url = "http://localhost:5000/api/reading-recordings";
        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Recording successfully saved to backend.");
            ShowFinishPanel();
        }
        else
        {
            Debug.LogError("Error saving recording: " + request.error);
        }
    }

    void ShowFinishPanel()
    {
        if (finishPanel != null) finishPanel.SetActive(true);
        if (finishTitleText != null) finishTitleText.text = "Thank you!";
        if (finishScoreText != null) finishScoreText.text = "Your recording has been saved.";

        if (passagePanel != null) passagePanel.SetActive(false);
        if (startRecordingButtonObject != null) startRecordingButtonObject.SetActive(false);
        if (stopRecordingButtonObject != null) stopRecordingButtonObject.SetActive(false);
        if (playRecordingButtonObject != null) playRecordingButtonObject.SetActive(false);
        if (saveRecordingButton != null) saveRecordingButton.gameObject.SetActive(false);
        if (wordCountText != null) wordCountText.gameObject.SetActive(false);
    }

    void RestartScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void GoToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
