using UnityEngine;
using TMPro;

//TODO: 마지막 경로 탐색의 성능 지표를 UI 패널에 표시하는 스크립트입니다.
//TODO: 구현 기능: 방문 노드 수, 경로 길이, 계산 시간, 메모리 표시, 패널 On/Off 토글

// PathFinderAI의 마지막 탐색 메트릭을 표시하는 통계 패널 클래스
public class AlgorithmStatsPanel : MonoBehaviour
{
    // PathFinderAI 참조 (메트릭 조회 대상)
    [SerializeField]
    private PathFinderAI pathFinderAI;

    // On/Off 토글 대상이 되는 패널 루트 오브젝트
    [SerializeField]
    private GameObject panelRoot;

    // 지표를 출력할 텍스트
    [SerializeField]
    private TMP_Text statsText;

    // 시작 시 패널 표시 여부
    [SerializeField]
    private bool visibleOnStart = true;

    // 마지막으로 화면에 반영한 메트릭 (변경 감지용)
    private PathFinderAI.SearchMetrics shownMetrics;

    // Start: 참조 검사 및 초기 표시 상태 적용
    private void Start()
    {
        if (pathFinderAI == null)
            Debug.LogError("AlgorithmStatsPanel: PathFinderAI가 할당되지 않았습니다.");

        SetVisible(visibleOnStart);
    }

    // Update: 패널이 켜져 있을 때 메트릭 변경 시 텍스트 갱신
    private void Update()
    {
        if (panelRoot != null && !panelRoot.activeSelf) return;
        if (pathFinderAI == null) return;

        PathFinderAI.SearchMetrics current = pathFinderAI.LastMetrics;
        if (current == null || current == shownMetrics) return;

        shownMetrics = current;
        RefreshText(current);
    }

    // 메트릭을 텍스트로 변환하여 표시
    private void RefreshText(PathFinderAI.SearchMetrics m)
    {
        if (statsText == null) return;

        string status = m.success ? "성공" : "실패";

        statsText.text =
            string.Format(
                "[ {0} ]\n" +
                "결과: {1}\n" +
                "방문 노드: {2}\n" +
                "경로 길이: {3}\n" +
                "계산 시간: {4:F3} ms\n" +
                "최대 메모리: {5}",
                m.algorithmName,
                status,
                m.visitedCount,
                m.pathLength,
                m.elapsedMs,
                m.maxOpenSize
            );
    }

    // 패널 표시 토글 (버튼 OnClick에서 호출)
    public void TogglePanel()
    {
        if (panelRoot == null) return;
        SetVisible(!panelRoot.activeSelf);
    }

    // 패널 표시 상태 설정
    public void SetVisible(bool visible)
    {
        if (panelRoot != null)
            panelRoot.SetActive(visible);
    }
}
