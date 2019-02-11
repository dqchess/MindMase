using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public sealed class DataController {

    private static volatile DataController instance;
    private static object syncRoot = new object();

    private string userDataFileName = "data.json";
    private UserData userData = new UserData();

    public static DataController Instance
    {
        get
        {
            if (instance == null)
            {
                lock (syncRoot)
                {
                    if (instance == null)
                        instance = new DataController();
                }
            }
            return instance;
        }
    }

    public void LoadUserData()
    {
        string filepath = Path.Combine(Application.streamingAssetsPath, userDataFileName);

        if (File.Exists(filepath))
        {
            string dataAsJson = File.ReadAllText(filepath);
            userData = JsonUtility.FromJson<UserData>(dataAsJson);
        }
        else
        {
            Debug.LogError("Cannot load user data!");
        }
    }

    public void SaveUserData()
    {

        string json = JsonUtility.ToJson(userData);
        File.WriteAllText(Application.dataPath + "/" + userDataFileName, json);   
        
    }

    public UserData GetUserData()
    {
        return userData;
    }

}
