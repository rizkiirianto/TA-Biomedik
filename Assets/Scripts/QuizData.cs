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
    public int gambarPortrait = -1; // Indeks ke array gambarPortraitKarakter, -1 = tidak mengubah portrait
}

[System.Serializable]
public class Step
{
    public string instruction;
    public List<Option> options;
    public string stepType;
    public string speakerName;
    public string minigameID;
    public string cutsceneID;
    public string narrativeImage;
    public float goldTime;
    public float silverTime;
    public string backgroundImage;
    public float optionParentPosX;
    public float optionParentPosY = float.NaN;
    public string optionParentTarget = "optionParents"; // optionParents / optionParents2
    public int gambarPortrait = -1; // Indeks ke array gambarPortraitKarakter, -1 = sembunyikan
    public float quizTimeSeconds = -1f; // <= 0 pakai default dari GameManager atau nonaktif jika default <= 0
    public float lowTimeCueSeconds = -1f; // <= 0 pakai default dari GameManager
    public string timeoutFeedback = "Waktu habis! Coba lagi.";
}

[System.Serializable]
public class QuizData
{
    public List<Step> steps;
    
}