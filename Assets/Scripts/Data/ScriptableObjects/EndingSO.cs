using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "Ending", menuName = "Game/Ending")]
public class EndingSO : ScriptableObject
{
    public string id;
    public string endingName;
    [TextArea(3, 6)]
    public string description;

    [Header("Conditions")]
    public List<StatRequirement> conditions;

    [Header("Priority")]
    [Tooltip("낮을수록 우선순위 높음")]
    public int priority = 100;

    [Header("Visuals")]
    public Sprite endingImage;
}
