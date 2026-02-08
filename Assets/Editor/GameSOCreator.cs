#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// SO 에셋 생성 에디터 윈도우
/// Unity 메뉴: Game > SO 에셋 생성기
/// </summary>
public class GameSOCreator : EditorWindow
{
    private enum Tab { Activity, Location, Ending }
    private Tab currentTab;
    private Vector2 scrollPos;

    // ── Activity 필드 ──
    private string actId = "";
    private string actName = "";
    private string actDesc = "";
    private int actDuration = 60;
    private int actCost;
    private int actIntelligence, actCharm, actCourage, actMoral;
    private int actPhysical, actMental, actFatigue;
    private int actMoney;

    // ── Location 필드 ──
    private string locId = "";
    private string locName = "";
    private string locDesc = "";
    private int locOpenHour = 12, locOpenMin = 0;
    private int locCloseHour = 18, locCloseMin = 0;
    private List<ActivitySO> locActivities = new List<ActivitySO>();
    private Sprite locIcon;
    private Sprite locBg;
    // 활동 선택용
    private ActivitySO[] allActivities;
    private bool[] activityToggles;

    // ── Ending 필드 ──
    private string endId = "";
    private string endName = "";
    private string endDesc = "";
    private int endPriority = 99;
    private List<StatRequirement> endConditions = new List<StatRequirement>();
    private Sprite endImage;

    // ── 공통 ──
    private string savePath = "Assets/_Main/Resources/";
    private string statusMessage = "";
    private MessageType statusType;

    [MenuItem("Game/SO 에셋 생성기")]
    public static void ShowWindow()
    {
        var window = GetWindow<GameSOCreator>("SO 에셋 생성기");
        window.minSize = new Vector2(400, 500);
    }

    private void OnEnable()
    {
        RefreshActivityList();
    }

    private void RefreshActivityList()
    {
        allActivities = Resources.LoadAll<ActivitySO>("")
            .Concat(LoadAllAssetsOfType<ActivitySO>())
            .Distinct()
            .ToArray();
        activityToggles = new bool[allActivities.Length];
    }

    private static T[] LoadAllAssetsOfType<T>() where T : Object
    {
        return AssetDatabase.FindAssets($"t:{typeof(T).Name}")
            .Select(guid => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid)))
            .Where(a => a != null)
            .ToArray();
    }

    private void OnGUI()
    {
        // 탭 바
        EditorGUILayout.Space(5);
        currentTab = (Tab)GUILayout.Toolbar((int)currentTab,
            new[] { "활동 (Activity)", "장소 (Location)", "엔딩 (Ending)" });
        EditorGUILayout.Space(10);

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        switch (currentTab)
        {
            case Tab.Activity: DrawActivityTab(); break;
            case Tab.Location: DrawLocationTab(); break;
            case Tab.Ending: DrawEndingTab(); break;
        }

        EditorGUILayout.EndScrollView();

        // 상태 메시지
        if (!string.IsNullOrEmpty(statusMessage))
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox(statusMessage, statusType);
        }
    }

    #region Activity Tab

    private void DrawActivityTab()
    {
        EditorGUILayout.LabelField("새 활동 만들기", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        actId = EditorGUILayout.TextField("ID", actId);
        actName = EditorGUILayout.TextField("활동 이름", actName);
        EditorGUILayout.LabelField("설명");
        actDesc = EditorGUILayout.TextArea(actDesc, GUILayout.Height(40));

        EditorGUILayout.Space(5);
        actDuration = EditorGUILayout.IntField("소요 시간 (분)", actDuration);
        actCost = EditorGUILayout.IntField("비용", actCost);

        // 스탯 변화
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("스탯 변화", EditorStyles.boldLabel);

        EditorGUI.indentLevel++;
        actIntelligence = EditorGUILayout.IntField("지력", actIntelligence);
        actCharm = EditorGUILayout.IntField("매력", actCharm);
        actCourage = EditorGUILayout.IntField("용기", actCourage);
        actMoral = EditorGUILayout.IntField("도덕성", actMoral);
        actMoney = EditorGUILayout.IntField("돈", actMoney);

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("건강 변화", EditorStyles.miniLabel);
        actPhysical = EditorGUILayout.IntField("체력", actPhysical);
        actMental = EditorGUILayout.IntField("정신력", actMental);
        actFatigue = EditorGUILayout.IntField("피로도", actFatigue);
        EditorGUI.indentLevel--;

        DrawCreateButton("Activity", CreateActivityAsset, ValidateActivity);
    }

    private bool ValidateActivity()
    {
        if (string.IsNullOrWhiteSpace(actId))
        {
            ShowStatus("ID를 입력해주세요.", MessageType.Warning);
            return false;
        }
        if (string.IsNullOrWhiteSpace(actName))
        {
            ShowStatus("활동 이름을 입력해주세요.", MessageType.Warning);
            return false;
        }
        return true;
    }

    private void CreateActivityAsset()
    {
        string folder = savePath + "Activities/";
        EnsureFolder(folder);

        string path = folder + actId + ".asset";
        if (AssetExists(path)) return;

        var activity = CreateInstance<ActivitySO>();
        activity.id = actId;
        activity.activityName = actName;
        activity.description = actDesc;
        activity.durationMinutes = actDuration;
        activity.cost = actCost;
        activity.statChanges = new StatChanges
        {
            intelligence = actIntelligence,
            charm = actCharm,
            courage = actCourage,
            moral = actMoral,
            money = actMoney,
            physical = actPhysical,
            mental = actMental,
            fatigue = actFatigue
        };
        activity.requirements = new List<StatRequirement>();

        AssetDatabase.CreateAsset(activity, path);
        AssetDatabase.SaveAssets();

        ShowStatus($"활동 '{actName}' 생성 완료! ({path})", MessageType.Info);
        Selection.activeObject = activity;
        EditorGUIUtility.PingObject(activity);
        RefreshActivityList();
        ClearActivityFields();
    }

    private void ClearActivityFields()
    {
        actId = actName = actDesc = "";
        actDuration = 60;
        actCost = 0;
        actIntelligence = actCharm = actCourage = actMoral = 0;
        actPhysical = actMental = actFatigue = actMoney = 0;
    }

    #endregion

    #region Location Tab

    private void DrawLocationTab()
    {
        EditorGUILayout.LabelField("새 장소 만들기", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        locId = EditorGUILayout.TextField("ID", locId);
        locName = EditorGUILayout.TextField("장소 이름", locName);
        EditorGUILayout.LabelField("설명");
        locDesc = EditorGUILayout.TextArea(locDesc, GUILayout.Height(40));

        // 이용 시간
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("이용 가능 시간", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("시작", GUILayout.Width(40));
        locOpenHour = EditorGUILayout.IntSlider(locOpenHour, 0, 23, GUILayout.Width(200));
        EditorGUILayout.LabelField(":", GUILayout.Width(10));
        locOpenMin = EditorGUILayout.IntPopup(locOpenMin,
            new[] { "00", "30" }, new[] { 0, 30 }, GUILayout.Width(50));
        EditorGUILayout.LabelField($"({locOpenHour:D2}:{locOpenMin:D2})");
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("종료", GUILayout.Width(40));
        locCloseHour = EditorGUILayout.IntSlider(locCloseHour, 0, 24, GUILayout.Width(200));
        EditorGUILayout.LabelField(":", GUILayout.Width(10));
        locCloseMin = EditorGUILayout.IntPopup(locCloseMin,
            new[] { "00", "30" }, new[] { 0, 30 }, GUILayout.Width(50));
        EditorGUILayout.LabelField($"({locCloseHour:D2}:{locCloseMin:D2})");
        EditorGUILayout.EndHorizontal();

        // 활동 선택
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("포함할 활동", EditorStyles.boldLabel);

        if (allActivities == null || allActivities.Length == 0)
        {
            EditorGUILayout.HelpBox("활동 에셋이 없습니다. 먼저 활동을 생성해주세요.", MessageType.Info);
        }
        else
        {
            EditorGUI.indentLevel++;
            for (int i = 0; i < allActivities.Length; i++)
            {
                if (allActivities[i] == null) continue;
                string label = $"{allActivities[i].activityName} ({allActivities[i].id})";
                activityToggles[i] = EditorGUILayout.ToggleLeft(label, activityToggles[i]);
            }
            EditorGUI.indentLevel--;
        }

        if (GUILayout.Button("활동 목록 새로고침", GUILayout.Height(20)))
        {
            RefreshActivityList();
        }

        // 비주얼
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("비주얼 (선택)", EditorStyles.boldLabel);
        locIcon = (Sprite)EditorGUILayout.ObjectField("아이콘", locIcon, typeof(Sprite), false);
        locBg = (Sprite)EditorGUILayout.ObjectField("배경 이미지", locBg, typeof(Sprite), false);

        DrawCreateButton("Location", CreateLocationAsset, ValidateLocation);
    }

    private bool ValidateLocation()
    {
        if (string.IsNullOrWhiteSpace(locId))
        {
            ShowStatus("ID를 입력해주세요.", MessageType.Warning);
            return false;
        }
        if (string.IsNullOrWhiteSpace(locName))
        {
            ShowStatus("장소 이름을 입력해주세요.", MessageType.Warning);
            return false;
        }
        return true;
    }

    private void CreateLocationAsset()
    {
        string folder = savePath + "Locations/";
        EnsureFolder(folder);

        string path = folder + locId + ".asset";
        if (AssetExists(path)) return;

        var location = CreateInstance<LocationSO>();
        location.id = locId;
        location.locationName = locName;
        location.description = locDesc;
        location.openTimeMinutes = locOpenHour * 60 + locOpenMin;
        location.closeTimeMinutes = locCloseHour * 60 + locCloseMin;
        location.locationIcon = locIcon;
        location.backgroundImage = locBg;

        location.activities = new List<ActivitySO>();
        for (int i = 0; i < allActivities.Length; i++)
        {
            if (activityToggles[i] && allActivities[i] != null)
            {
                location.activities.Add(allActivities[i]);
            }
        }

        AssetDatabase.CreateAsset(location, path);
        AssetDatabase.SaveAssets();

        ShowStatus($"장소 '{locName}' 생성 완료! (활동 {location.activities.Count}개 포함) ({path})", MessageType.Info);
        Selection.activeObject = location;
        EditorGUIUtility.PingObject(location);
        ClearLocationFields();
    }

    private void ClearLocationFields()
    {
        locId = locName = locDesc = "";
        locOpenHour = 12; locOpenMin = 0;
        locCloseHour = 18; locCloseMin = 0;
        locIcon = null; locBg = null;
        activityToggles = new bool[allActivities.Length];
    }

    #endregion

    #region Ending Tab

    private void DrawEndingTab()
    {
        EditorGUILayout.LabelField("새 엔딩 만들기", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        endId = EditorGUILayout.TextField("ID", endId);
        endName = EditorGUILayout.TextField("엔딩 이름", endName);
        EditorGUILayout.LabelField("설명");
        endDesc = EditorGUILayout.TextArea(endDesc, GUILayout.Height(60));
        endPriority = EditorGUILayout.IntField("우선순위 (낮을수록 우선)", endPriority);

        // 조건
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("달성 조건", EditorStyles.boldLabel);

        for (int i = 0; i < endConditions.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();

            var cond = endConditions[i];
            cond.statType = (StatType)EditorGUILayout.EnumPopup(cond.statType, GUILayout.Width(120));
            EditorGUILayout.LabelField(">=", GUILayout.Width(20));
            cond.minValue = EditorGUILayout.IntField(cond.minValue, GUILayout.Width(60));
            endConditions[i] = cond;

            if (GUILayout.Button("X", GUILayout.Width(25)))
            {
                endConditions.RemoveAt(i);
                i--;
            }

            EditorGUILayout.EndHorizontal();
        }

        if (GUILayout.Button("+ 조건 추가", GUILayout.Height(22)))
        {
            endConditions.Add(new StatRequirement());
        }

        // 비주얼
        EditorGUILayout.Space(10);
        endImage = (Sprite)EditorGUILayout.ObjectField("엔딩 이미지 (선택)", endImage, typeof(Sprite), false);

        DrawCreateButton("Ending", CreateEndingAsset, ValidateEnding);
    }

    private bool ValidateEnding()
    {
        if (string.IsNullOrWhiteSpace(endId))
        {
            ShowStatus("ID를 입력해주세요.", MessageType.Warning);
            return false;
        }
        if (string.IsNullOrWhiteSpace(endName))
        {
            ShowStatus("엔딩 이름을 입력해주세요.", MessageType.Warning);
            return false;
        }
        return true;
    }

    private void CreateEndingAsset()
    {
        string folder = savePath + "Endings/";
        EnsureFolder(folder);

        string path = folder + endId + ".asset";
        if (AssetExists(path)) return;

        var ending = CreateInstance<EndingSO>();
        ending.id = endId;
        ending.endingName = endName;
        ending.description = endDesc;
        ending.priority = endPriority;
        ending.conditions = new List<StatRequirement>(endConditions);
        ending.endingImage = endImage;

        AssetDatabase.CreateAsset(ending, path);
        AssetDatabase.SaveAssets();

        ShowStatus($"엔딩 '{endName}' 생성 완료! ({path})", MessageType.Info);
        Selection.activeObject = ending;
        EditorGUIUtility.PingObject(ending);
        ClearEndingFields();
    }

    private void ClearEndingFields()
    {
        endId = endName = endDesc = "";
        endPriority = 99;
        endConditions.Clear();
        endImage = null;
    }

    #endregion

    #region Helpers

    private void DrawCreateButton(string typeName, System.Action createAction, System.Func<bool> validate)
    {
        EditorGUILayout.Space(15);

        GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
        if (GUILayout.Button($"에셋 생성", GUILayout.Height(30)))
        {
            if (validate())
            {
                createAction();
            }
        }
        GUI.backgroundColor = Color.white;
    }

    private void ShowStatus(string message, MessageType type)
    {
        statusMessage = message;
        statusType = type;
        Repaint();
    }

    private bool AssetExists(string path)
    {
        if (AssetDatabase.LoadAssetAtPath<Object>(path) != null)
        {
            ShowStatus($"이미 존재하는 에셋입니다: {path}", MessageType.Warning);
            return true;
        }
        return false;
    }

    private static void EnsureFolder(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
            AssetDatabase.Refresh();
        }
    }

    #endregion
}
#endif
