using System.Collections.Generic;
using System.Text;
using UnityEngine;
using TMPro;

//TODO: 동일한 맵에서 알고리즘을 실행할 때마다 성능 지표를 누적 기록하는 대시보드 스크립트입니다.
//TODO: 구현 기능: 알고리즘별 1레코드 누적, 맵 변경 시 자동 초기화, 표 출력
//TODO: 컬럼: 알고리즘명, 계산시간(ms), 방문 노드 수, 피크 메모리, 이동 칸 수
//TODO: PathFinderAI.OnSearchCompleted 이벤트를 구독하여 버튼 실행 시마다 기록한다.

// 한 맵에 대한 알고리즘별 성능을 표로 누적 표시하는 대시보드 클래스
public class AlgorithmDashboard : MonoBehaviour
{
    // PathFinderAI 참조 (탐색 완료 이벤트 구독 대상)
    [SerializeField]
    private PathFinderAI pathFinderAI;

    // GridManager 참조 (맵 변경 감지용)
    [SerializeField]
    private GridManager gridManager;

    // 대시보드 표를 출력할 텍스트 (등폭 폰트 권장)
    [SerializeField]
    private TMP_Text dashboardText;

    // On/Off 토글 대상이 되는 대시보드 패널 루트
    [SerializeField]
    private GameObject panelRoot;

    // 실패한 탐색도 표에 기록할지 여부
    [SerializeField]
    private bool recordFailures = false;

    // 알고리즘별 성능 레코드 (실행 순서 유지, 알고리즘당 최신 1개만 보관)
    private List<PathFinderAI.SearchMetrics> records = new List<PathFinderAI.SearchMetrics>();

    // 마지막으로 확인한 맵 버전 (변경 감지용)
    private int lastMapVersion = -1;

    // OnEnable: 탐색 완료 이벤트 구독
    private void OnEnable()
    {
        if (pathFinderAI != null)
            pathFinderAI.OnSearchCompleted += RecordMetrics;
    }

    // OnDisable: 이벤트 구독 해제
    private void OnDisable()
    {
        if (pathFinderAI != null)
            pathFinderAI.OnSearchCompleted -= RecordMetrics;
    }

    // Start: 참조 검사 및 초기 표 출력
    private void Start()
    {
        if (pathFinderAI == null)
            Debug.LogError("AlgorithmDashboard: PathFinderAI가 할당되지 않았습니다.");
        if (gridManager == null)
            Debug.LogError("AlgorithmDashboard: GridManager가 할당되지 않았습니다.");

        if (gridManager != null)
            lastMapVersion = gridManager.MapVersion;

        RefreshTable();
    }

    // Update: 맵 변경 감지 시 레코드 초기화
    private void Update()
    {
        if (gridManager == null) return;

        if (gridManager.MapVersion != lastMapVersion)
        {
            lastMapVersion = gridManager.MapVersion;
            records.Clear();
            RefreshTable();
        }
    }

    // 탐색 완료 시 알고리즘별 레코드 갱신 (이벤트 콜백)
    // 같은 알고리즘이 이미 있으면 최신 값으로 교체하여 한 맵당 알고리즘당 1개만 유지한다
    private void RecordMetrics(PathFinderAI.SearchMetrics metrics)
    {
        if (metrics == null) return;
        if (!metrics.success && !recordFailures) return;

        // 동일 알고리즘 레코드 탐색
        int existingIndex = -1;
        for (int i = 0; i < records.Count; i++)
        {
            if (records[i].algorithmName == metrics.algorithmName)
            {
                existingIndex = i;
                break;
            }
        }

        if (existingIndex >= 0)
            records[existingIndex] = metrics;
        else
            records.Add(metrics);

        RefreshTable();
    }

    // 레코드 전체 초기화 (수동 초기화 버튼에서 호출 가능)
    public void ClearDashboard()
    {
        records.Clear();
        RefreshTable();
    }

    // 대시보드 패널 표시 토글 (버튼 OnClick에서 호출)
    public void TogglePanel()
    {
        if (panelRoot == null) return;
        panelRoot.SetActive(!panelRoot.activeSelf);
    }

    // 레코드 목록을 표 문자열로 변환하여 출력
    private void RefreshTable()
    {
        if (dashboardText == null) return;

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("===== 알고리즘 성능 대시보드 =====");
        sb.AppendLine(string.Format("{0,-9}{1,7}{2,5}{3,5}{4,5}",
            "알고리즘", "ms", "방문", "메모리", "이동"));
        sb.AppendLine("-------------------------------");

        if (records.Count == 0)
        {
            sb.AppendLine("아직 실행 기록이 없습니다.");
            sb.AppendLine("알고리즘을 선택하고 Move를 눌러보세요.");
        }
        else
        {
            foreach (PathFinderAI.SearchMetrics m in records)
            {
                if (!m.success)
                {
                    sb.AppendLine(string.Format("{0,-9}{1,7}{2,5}{3,5}{4,5}",
                        m.algorithmName, "실패", "-", "-", "-"));
                    continue;
                }

                sb.AppendLine(string.Format("{0,-9}{1,7:F2}{2,5}{3,5}{4,5}",
                    m.algorithmName, m.elapsedMs, m.visitedCount, m.maxOpenSize, m.pathLength));
            }
        }

        sb.AppendLine("-------------------------------");
        sb.AppendLine("방문=탐색한 셀 수, 메모리=최대 프론티어 크기");

        dashboardText.text = sb.ToString();
    }
}
