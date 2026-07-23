using System.Collections.Generic;
using System.Text;
using UnityEngine;
using TMPro;

//TODO: 동일한 맵에서 7개 경로 탐색 알고리즘을 일괄 실행하고 결과를 표로 비교하는 스크립트입니다.
//TODO: 구현 기능: 전체 알고리즘 실행, 방문 수/경로 길이/시간 표 출력, 최우수 항목 강조

// 모든 알고리즘을 한 번에 실행하여 성능을 비교하는 클래스
public class AlgorithmBenchmark : MonoBehaviour
{
    // PathFinderAI 참조 (일괄 실행 API 호출 대상)
    [SerializeField]
    private PathFinderAI pathFinderAI;

    // GridManager 참조 (시작/목표 노드 좌표 조회용)
    [SerializeField]
    private GridManager gridManager;

    // 비교 결과를 출력할 텍스트 (등폭 폰트 권장)
    [SerializeField]
    private TMP_Text resultText;

    // On/Off 토글 대상이 되는 결과 패널 루트
    [SerializeField]
    private GameObject panelRoot;

    // 일괄 비교 실행 (버튼 OnClick에서 호출)
    public void RunBenchmark()
    {
        if (pathFinderAI == null || gridManager == null)
        {
            Debug.LogError("AlgorithmBenchmark: PathFinderAI 또는 GridManager가 할당되지 않았습니다.");
            return;
        }
        if (gridManager.StartNode == null || gridManager.EndNode == null)
        {
            Debug.LogWarning("AlgorithmBenchmark: 시작/목표 노드가 없습니다.");
            return;
        }

        Vector3 from = gridManager.StartNode.worldPosition;
        Vector3 to   = gridManager.EndNode.worldPosition;

        List<PathFinderAI.SearchMetrics> results = pathFinderAI.RunAllAlgorithms(from, to);

        if (panelRoot != null)
            panelRoot.SetActive(true);

        BuildResultTable(results);
    }

    // 결과 패널 표시 토글 (버튼 OnClick에서 호출)
    public void TogglePanel()
    {
        if (panelRoot == null) return;
        panelRoot.SetActive(!panelRoot.activeSelf);
    }

    // 메트릭 목록을 정렬된 표 문자열로 변환하여 출력
    private void BuildResultTable(List<PathFinderAI.SearchMetrics> results)
    {
        if (resultText == null) return;
        if (results == null || results.Count == 0)
        {
            resultText.text = "비교 결과가 없습니다.";
            return;
        }

        // 최소값 항목 강조를 위한 기준값 산출 (성공한 항목만 대상)
        int   bestVisited = int.MaxValue;
        int   bestPath    = int.MaxValue;
        double bestTime   = double.MaxValue;

        foreach (PathFinderAI.SearchMetrics m in results)
        {
            if (!m.success) continue;
            if (m.visitedCount < bestVisited) bestVisited = m.visitedCount;
            if (m.pathLength   < bestPath)    bestPath    = m.pathLength;
            if (m.elapsedMs    < bestTime)    bestTime    = m.elapsedMs;
        }

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("=== 알고리즘 일괄 비교 ===");
        sb.AppendLine(string.Format("{0,-10}{1,8}{2,8}{3,11}", "알고리즘", "방문", "경로", "시간(ms)"));
        sb.AppendLine("------------------------------------");

        foreach (PathFinderAI.SearchMetrics m in results)
        {
            if (!m.success)
            {
                sb.AppendLine(string.Format("{0,-10}{1,8}{2,8}{3,11}", m.algorithmName, "-", "-", "실패"));
                continue;
            }

            // 각 지표 최우수 항목에 별표 표시
            string visited = m.visitedCount == bestVisited ? m.visitedCount + "*" : m.visitedCount.ToString();
            string path    = m.pathLength   == bestPath    ? m.pathLength + "*"    : m.pathLength.ToString();
            string time    = string.Format("{0:F3}", m.elapsedMs);
            if (m.elapsedMs == bestTime) time += "*";

            sb.AppendLine(string.Format("{0,-10}{1,8}{2,8}{3,11}", m.algorithmName, visited, path, time));
        }

        sb.AppendLine("------------------------------------");
        sb.AppendLine("* 표시는 해당 항목 최우수");

        resultText.text = sb.ToString();
    }
}
