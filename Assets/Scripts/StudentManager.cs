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

    private List<Student> allStudents = new();
    private List<GameObject> currentItems = new();

    public StudentDetailPanel detailPanel;

    [System.Serializable]
    public class Student
    {
        public string username;
        public string level;
        public string role;
    }

    void Start()
    {
        StartCoroutine(FetchStudents());

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

        ShowFilteredList("");
    }

    public void ShowFilteredList(string filter)
    {
        foreach (GameObject go in currentItems)
            Destroy(go);
        currentItems.Clear();

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
        var student = allStudents.FirstOrDefault(s => s.username == username);
        if (student != null)
            detailPanel.Show(student.username, student.level);
    }

    // Allow other scripts to trigger reload
    public void RefreshList()
    {
        StartCoroutine(FetchStudents());
    }
}
