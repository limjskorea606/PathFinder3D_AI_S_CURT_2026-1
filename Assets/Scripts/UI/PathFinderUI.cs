using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//TODO: 경로 탐색 시뮬레이터의 UI를 관리하는 스크립트입니다.
//TODO: 구현 기능: 알고리즘 선택 드롭다운, 실행/초기화 버튼, 통계 표시, 키보드 단축키
//TODO: 키보드 단축키: Space(실행/정지), C(전체 초기화), R(탐색 상태만 초기화)
//TODO: UI 요소가 할당되지 않아도 키보드 단축키만으로 동작 가능

// PathFinder, GridManager와 UI 요소를 연결하는 UI 매니저 클래스
public class PathFinderUI : MonoBehaviour
{
    // PathFinder 참조
    [SerializeField]
    private PathFinder pathFinder;

    // GridManager 참조
    [SerializeField]
    private GridManager gridManager;

    // 알고리즘 선택 드롭다운 (선택사항)
    [SerializeField]
    private Dropdown algorithmDropdown;

    // 실행/정지 버튼 (선택사항)
    [SerializeField]
    private Button runButton;

    // 전체 초기화 버튼 (선택사항)
    [SerializeField]
    private Button clearButton;

    // 탐색 상태만 초기화 버튼 (선택사항)
    [SerializeField]
    private Button resetButton;

    // 방문 노드 수 텍스트 (선택사항)
    [SerializeField]
    private Text visitedText;

    // 경로 길이 텍스트 (선택사항)
    [SerializeField]
    private Text pathLengthText;

    // 소요 시간 텍스트 (선택사항)
    [SerializeField]
    private Text elapsedTimeText;

    // 이전 프레임 탐색 실행 상태 (버튼 텍스트 갱신용)
    private bool prevIsRunning;

    // Start: 버튼 이벤트 등록 및 드롭다운 초기화
    private void Start()
    {
        if (runButton != null)
            runButton.onClick.AddListener(OnRunClicked);

        if (clearButton != null)
            clearButton.onClick.AddListener(OnClearClicked);

        if (resetButton != null)
            resetButton.onClick.AddListener(OnResetSearchClicked);

        if (algorithmDropdown != null)
        {
            algorithmDropdown.ClearOptions();
            algorithmDropdown.AddOptions(new List<string>
            {
                "A* (A-Star)",
                "Dijkstra",
                "BFS (너비 우선)",
                "DFS (깊이 우선)",
                "Greedy Best-First"
            });
            algorithmDropdown.onValueChanged.AddListener(OnAlgorithmChanged);
        }

        UpdateStats();
        UpdateRunButtonText();
    }

    // Update: 통계 갱신, 버튼 텍스트 갱신, 키보드 단축키 처리
    private void Update()
    {
        if (pathFinder == null) return;

        // 탐색 상태 변화 시 통계 갱신
        if (pathFinder.IsRunning || prevIsRunning != pathFinder.IsRunning)
        {
            UpdateStats();
            UpdateRunButtonText();
            prevIsRunning = pathFinder.IsRunning;
        }

        // 키보드 단축키
        if (Input.GetKeyDown(KeyCode.Space))
            OnRunClicked();

        if (Input.GetKeyDown(KeyCode.C))
            OnClearClicked();

        if (Input.GetKeyDown(KeyCode.R))
            OnResetSearchClicked();
    }

    // 실행/정지 버튼 클릭 처리
    public void OnRunClicked()
    {
        if (pathFinder == null) return;

        if (pathFinder.IsRunning)
            pathFinder.StopPathfinding();
        else
            pathFinder.RunPathfinding();
    }

    // 전체 초기화 버튼 클릭 처리
    public void OnClearClicked()
    {
        if (pathFinder != null)
            pathFinder.StopPathfinding();
        if (gridManager != null)
            gridManager.ClearAll();

        UpdateStats();
        UpdateRunButtonText();
    }

    // 탐색 상태만 초기화 버튼 클릭 처리 (장벽 유지)
    public void OnResetSearchClicked()
    {
        if (pathFinder != null)
            pathFinder.StopPathfinding();
        if (gridManager != null)
            gridManager.ResetSearchState();

        UpdateStats();
        UpdateRunButtonText();
    }

    // 알고리즘 드롭다운 변경 처리
    private void OnAlgorithmChanged(int index)
    {
        if (pathFinder == null) return;
        pathFinder.SelectedAlgorithm = (PathFinder.AlgorithmType)index;
    }

    // 통계 텍스트 갱신
    private void UpdateStats()
    {
        if (pathFinder == null) return;

        if (visitedText != null)
            visitedText.text = pathFinder.VisitedCount > 0 ? pathFinder.VisitedCount.ToString() : "-";

        if (pathLengthText != null)
            pathLengthText.text = pathFinder.PathLength > 0 ? pathFinder.PathLength.ToString() : "-";

        if (elapsedTimeText != null)
            elapsedTimeText.text = pathFinder.ElapsedMs > 0 ? string.Format("{0}ms", Mathf.RoundToInt(pathFinder.ElapsedMs)) : "-";
    }

    // 실행 버튼 텍스트 갱신
    private void UpdateRunButtonText()
    {
        if (runButton == null || pathFinder == null) return;
        Text btnText = runButton.GetComponentInChildren<Text>();
        if (btnText != null)
            btnText.text = pathFinder.IsRunning ? "정지 ■" : "실행 ▶";
    }
}
