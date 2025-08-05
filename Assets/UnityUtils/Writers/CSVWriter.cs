using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Unity.Mathematics;
using Helpers;

[System.Serializable]
public class CSVWriter
{
    public string fileName = null;
    public string dirName = null;
    public bool writeUnixTime = true;
    public bool append_zero_to_filename = false;
    public List<string> columns;

    [SerializeField, ReadOnlyInsp] private string filePath;
    private StreamWriter eventWriter;
    private List<string> payload = new List<string>();
    private bool _is_active = false;
    public bool is_active => _is_active;

    public bool Initialize() {
        string dname = $"{Application.persistentDataPath}/{Helpers.SaveSystemMethods.GetCurrentDateTime()}";
        if (dirName != null && dirName.Length > 0) {
            dname = $"{Application.persistentDataPath}/{dirName}";
        }
        Helpers.SaveSystemMethods.CheckOrCreateDirectory(dname);

        string fname = (fileName != null && fileName.Length > 0) ? fileName : System.DateTime.Now.ToString("HH-mm-ss");
        filePath = (append_zero_to_filename) ? Path.Combine(dname, fname+"_0.csv") : Path.Combine(dname, fname+".csv");

        int counter = 1;
        while(File.Exists(filePath)) {
            filePath = Path.Combine(dname, fname+$"_{counter}.csv");
            counter++;
        }

        eventWriter = new StreamWriter(new FileStream(filePath, FileMode.Create), Encoding.UTF8);
        // Header Line, if any columns are added to `columns`
        if (columns.Count > 0) {
            if (writeUnixTime) columns.Insert(0,"unix_ms");
            eventWriter.WriteLine(String.Join(',', columns));
        }

        _is_active = true;
        return true;
    }


    public bool WriteLine() {
        if (payload.Count == 0) return false;
        WriteLine(String.Join(",",payload), writeUnixTime);
        payload = new List<string>();
        return true;
    }
    public bool WriteLine(bool add_unix) {
        if (payload.Count == 0) return false;
        WriteLine(String.Join(",",payload), add_unix);
        payload = new List<string>();
        return true;
    }
    public bool WriteLine(string newLine) {
        eventWriter.WriteLine(newLine);
        return true;
    }
    public bool WriteLine(string newLine, bool add_unix) {
        if (add_unix) eventWriter.WriteLine($"{GetUnixTime()},{newLine}");
        else eventWriter.WriteLine(newLine);
        return true;
    }

    public void AddPayload(string to_add) {
        payload.Add(to_add);
    }
    public void AddPayload(int to_add) {
        payload.Add(to_add.ToString());
    }
    public void AddPayload(float to_add) {
        payload.Add(to_add.ToString());
    }
    public void AddPayload(long to_add) {
        payload.Add($"{to_add}");
    }
    public void AddPayload(Vector3 to_add) {
        payload.Add(to_add.x.ToString());
        payload.Add(to_add.y.ToString());
        payload.Add(to_add.z.ToString());
    }
    public void AddPayload(float3 to_add) {
        payload.Add(to_add[0].ToString());
        payload.Add(to_add[1].ToString());
        payload.Add(to_add[2].ToString());
    }
    public void AddPayload(Quaternion to_add) {
        AddPayload(to_add.eulerAngles);
    }
    public void AddPayload(List<string> to_add) {
        foreach(string a in to_add) payload.Add(a);
    }
    public void AddPayload(string[] to_add) {
        foreach(string a in to_add) payload.Add(a);
    }

    public static long GetUnixTime() {
        DateTime currentTime = DateTime.UtcNow;
        return ((DateTimeOffset)currentTime).ToUnixTimeMilliseconds();
    }

    public void Disable() {
        if (_is_active) {
            eventWriter.Flush();
            eventWriter.Close();
        }
        _is_active = false;
    }
}
