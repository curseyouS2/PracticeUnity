using UnityEngine;
using UnityEngine.UI;
using System;

/// <summary>
/// 범용 버튼 - 장소/활동/캐릭터 클릭 시 static 이벤트 발행
/// </summary>
public class LocationButton : MonoBehaviour
{
    public static event Action<DataTable.LocationTable> OnLocationClicked;
    public static event Action<DataTable.ActivityTable> OnActivityClicked;
    public static event Action<CharacterSO> OnCharacterClicked;

    private DataTable.LocationTable location;
    private DataTable.ActivityTable activity;
    private CharacterSO character;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        button?.onClick.AddListener(HandleClick);
    }

    private void OnDestroy()
    {
        button?.onClick.RemoveListener(HandleClick);
    }

    public void SetupAsLocation(DataTable.LocationTable loc)
    {
        location = loc;
        activity = null;
        character = null;
    }

    public void SetupAsActivity(DataTable.ActivityTable act)
    {
        location = null;
        activity = act;
        character = null;
    }

    public void SetupAsCharacter(CharacterSO chara)
    {
        location = null;
        activity = null;
        character = chara;
    }

    private void HandleClick()
    {
        if (location != null)
            OnLocationClicked?.Invoke(location);
        else if (activity != null)
            OnActivityClicked?.Invoke(activity);
        else if (character != null)
            OnCharacterClicked?.Invoke(character);
    }
}
