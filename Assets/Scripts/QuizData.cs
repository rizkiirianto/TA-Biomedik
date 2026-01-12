using System.Collections.Generic;

// Atribut [System.Serializable] penting agar Unity dapat membaca kelas ini dari JSON
[System.Serializable]
public class Option
{
    public string text;
    public bool isCorrect;
    public string feedback;
    public string narrative;
    public string narrativeImage;
    public string backgroundImage;
}

[System.Serializable]
public class Step
{
    public string instruction;
    public List<Option> options;
    public string stepType;
    public string speakerName;
    public string minigameID;
    public string narrativeImage;
    public float goldTime;
    public float silverTime;
    public string backgroundImage;
}

[System.Serializable]
public class QuizData
{
    public List<Step> steps;
    
}