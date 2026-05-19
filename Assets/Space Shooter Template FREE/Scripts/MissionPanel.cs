using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 화면 왼쪽에 미션 진행도를 (현재/목표) 형태로 표시하는 패널입니다.
/// </summary>
public class MissionPanel : MonoBehaviour
{
    public static MissionPanel instance;

    [System.Serializable]
    public class MissionData
    {
        public string description;   // 미션 설명 (예: "총알을 피해보세요")
        public int targetCount;      // 목표 수치 (예: 3)
        [HideInInspector] public int currentCount = 0;
        [HideInInspector] public bool isCompleted = false;
    }

    [Header("UI 연결")]
    public GameObject panelRoot;
    public List<GameObject> missionTextObjs = new List<GameObject>();

    [Header("미션 목록 (인스펙터에서 편집)")]
    public List<MissionData> missions = new List<MissionData>()
    {
        new MissionData { description = "총알을 피해보세요",         targetCount = 3  },
        new MissionData { description = "해킹존에서 적을 해킹하세요", targetCount = 100 },
        new MissionData { description = "적의 체력을 0으로 만드세요", targetCount = 1  },
    };

    void Awake()
    {
        if (instance == null) instance = this;
    }

    void Start()
    {
        RefreshUI();
    }

    /// <summary>
    /// 미션 설명(description)에 특정 키워드가 포함된 미션의 인덱스를 찾습니다.
    /// 못 찾으면 -1을 반환합니다. (인덱스 불일치 버그 원천 차단)
    /// </summary>
    public int FindMissionIndexByKeyword(string keyword)
    {
        if (missions == null) return -1;
        for (int i = 0; i < missions.Count; i++)
        {
            if (missions[i] != null && !string.IsNullOrEmpty(missions[i].description) && missions[i].description.Contains(keyword))
            {
                return i;
            }
        }
        return -1;
    }

    /// <summary>
    /// 미션 설명에 특정 키워드가 포함된 미션의 진행도를 올립니다.
    /// </summary>
    public void AddProgressByKeyword(string keyword, int amount = 1)
    {
        int index = FindMissionIndexByKeyword(keyword);
        if (index != -1)
        {
            AddProgress(index, amount);
        }
    }

    /// <summary>
    /// 미션 설명에 특정 키워드가 포함된 미션의 진행도를 특정 값으로 강제 동기화합니다.
    /// </summary>
    public void SetProgressByKeyword(string keyword, int current)
    {
        int index = FindMissionIndexByKeyword(keyword);
        if (index != -1)
        {
            SetProgress(index, current);
        }
    }

    /// <summary>
    /// 해당 인덱스 미션의 진행도를 amount만큼 올립니다.
    /// 목표치에 도달하면 자동 완료됩니다.
    /// </summary>
    public void AddProgress(int index, int amount = 1)
    {
        if (index < 0 || index >= missions.Count) return;
        MissionData m = missions[index];
        if (m.isCompleted) return;

        m.currentCount = Mathf.Min(m.currentCount + amount, m.targetCount);
        RefreshUI();

        if (m.currentCount >= m.targetCount)
        {
            m.isCompleted = true;
            RefreshUI();

            if (index < missionTextObjs.Count && missionTextObjs[index] != null)
                StartCoroutine(CompletedFlash(missionTextObjs[index]));
        }
    }

    /// <summary>
    /// 진행도를 특정 값으로 강제 설정합니다. (예: 해킹 HP를 퍼센트로 동기화할 때)
    /// </summary>
    public void SetProgress(int index, int current)
    {
        if (index < 0 || index >= missions.Count) return;
        MissionData m = missions[index];
        if (m.isCompleted) return;

        m.currentCount = Mathf.Clamp(current, 0, m.targetCount);
        RefreshUI();

        if (m.currentCount >= m.targetCount)
        {
            m.isCompleted = true;
            RefreshUI(); // 완료 처리 후 UI를 한 번 더 갱신해 줘야 [V] 체크마크가 표시됩니다!
            if (index < missionTextObjs.Count && missionTextObjs[index] != null)
                StartCoroutine(CompletedFlash(missionTextObjs[index]));
        }
    }

    public bool IsAllCompleted()
    {
        foreach (var m in missions)
            if (!m.isCompleted) return false;
        return true;
    }

    void RefreshUI()
    {
        for (int i = 0; i < missions.Count; i++)
        {
            if (i >= missionTextObjs.Count || missionTextObjs[i] == null) continue;
            MissionData m = missions[i];

            // 완료 전 괄호 [ ] 의 색상을 선명한 흰색(#FFFFFF)으로 강제 설정합니다.
            string prefix = m.isCompleted ? "<color=#50FF50>[V]</color> " : "<color=#FFFFFF>[ ]</color> ";
            string progress = m.targetCount > 1
                ? $" ({m.currentCount}/{m.targetCount})"
                : "";
            string text = prefix + m.description + progress;

            SetText(missionTextObjs[i], text);
            SetColor(missionTextObjs[i], m.isCompleted ? new Color(0.5f, 1f, 0.5f) : Color.white);
        }
    }

    IEnumerator CompletedFlash(GameObject obj)
    {
        for (int i = 0; i < 4; i++)
        {
            SetColor(obj, Color.yellow);
            yield return new WaitForSeconds(0.1f);
            SetColor(obj, new Color(0.5f, 1f, 0.5f));
            yield return new WaitForSeconds(0.1f);
        }
    }

    // ── 텍스트 헬퍼 ─────────────────────────────────────────
    void SetText(GameObject obj, string value)
    {
        if (obj == null) return;
        var legacy = obj.GetComponent<Text>();
        if (legacy != null) { legacy.text = value; return; }
        foreach (var comp in obj.GetComponents<Component>())
        {
            if (comp == null) continue;
            var prop = comp.GetType().GetProperty("text");
            if (prop != null && prop.CanWrite) { prop.SetValue(comp, value, null); return; }
        }
    }

    void SetColor(GameObject obj, Color color)
    {
        if (obj == null) return;
        var legacy = obj.GetComponent<Text>();
        if (legacy != null) { legacy.color = color; return; }
        foreach (var comp in obj.GetComponents<Component>())
        {
            if (comp == null) continue;
            var prop = comp.GetType().GetProperty("color");
            if (prop != null && prop.CanWrite) { prop.SetValue(comp, color, null); return; }
        }
    }
}
