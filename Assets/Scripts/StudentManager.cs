using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SimpleJSON;

public class StudentManager : MonoBehaviour
{
    [Header("Prefabs and Parents")]
    public GameObject studentItemPrefab;
    public Transform contentParent;

    [Header("UI")]
    public TMP_InputField searchInputField;

    // Store all students and active items
    private List<Student> allStudents = new();
    private List<GameObject> currentItems = new();

    [System.Serializable]
    public class Student
    {
        public string username;
        public string level;
        public string role;
    }

    void Start()
    {
        // Start loading from backend
        StartCoroutine(FetchStudents());

        // Add listener to input field
        if (searchInputField != null)
        {
            searchInputField.onValueChanged.AddListener(ShowFilteredList);
        }
    }

    IEnumerator FetchStudents()
    {
        UnityWebRequest req = UnityWebRequest.Get("http://localhost:5000/api/students");
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Error: " + req.error);
            yield break;
        }

        string json = req.downloadHandler.text;
        Debug.Log("Raw JSON:\n" + json);

        JSONNode studentArray = JSON.Parse(json);
        allStudents.Clear();

        for (int i = 0; i < studentArray.Count; i++)
        {
            var s = new Student
            {
                username = studentArray[i]["username"],
                level = studentArray[i]["level"],
                role = studentArray[i]["role"]
            };
            allStudents.Add(s);
        }

        ShowFilteredList(""); // show all initially
    }

    public void ShowFilteredList(string filter)
    {
        // Clear existing items
        foreach (GameObject go in currentItems)
            Destroy(go);
        currentItems.Clear();

        // Filter and rebuild list
        var filtered = allStudents
            .Where(s => s.username.ToLower().Contains(filter.ToLower()))
            .ToList();

        foreach (var s in filtered)
        {
            GameObject item = Instantiate(studentItemPrefab, contentParent);
            item.GetComponent<StudentItemUI>().Setup(s.username, s.level, OnStudentClicked);
            currentItems.Add(item);
        }
    }

    void OnStudentClicked(string username)
    {
        Debug.Log("Clicked: " + username);
        // Future: open student detail panel here
    }
}
