using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class DialogueLine
{
    public string speakerName;
    [TextArea(3,10)]
    public string text;
}

[CreateAssetMenu(menuName = "Dialogue/Sequence")]
public class DialogueSequence : ScriptableObject
{
    public string sequenceID;  // ← AGGIUNGI QUESTO CAMPO
    public List<DialogueLine> lines;
}
