using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

//TODO: 게임 내 UI에서 경로 탐색 알고리즘을 선택하는 스크립트입니다.
//TODO: 구현 기능: Dropdown으로 알고리즘 선택, PathFinderAI에 선택값 적용, 현재 알고리즘 텍스트 표시

// 알고리즘 선택 UI를 관리하는 클래스
public class AlgorithmSelector : MonoBehaviour
{
    // PathFinderAI 참조 (알고리즘 변경 적용 대상)
    [SerializeField]
    private PathFinderAI pathFinderAI;

    // 알고리즘 선택 드롭다운 UI
    [SerializeField]
    private TMP_Dropdown algorithmDropdown;

    // 현재 선택된 알고리즘 이름을 표시하는 텍스트 (선택사항)
    [SerializeField]
    private TMP_Text currentAlgorithmText;

    // 드롭다운 항목과 매핑되는 알고리즘 순서 목록
    private readonly List<PathFinderAI.Algorithm> algorithmOrder = new List<PathFinderAI.Algorithm>
    {
        PathFinderAI.Algorithm.AStar,
        PathFinderAI.Algorithm.Dijkstra,
        PathFinderAI.Algorithm.BFS,
        PathFinderAI.Algorithm.DFS,
        PathFinderAI.Algorithm.GreedyBestFirst,
        PathFinderAI.Algorithm.DStar,
        PathFinderAI.Algorithm.DStarLite
    };

    //FIXME: 아래 변수는 더이상 사용하지 않습니다. 삭제를 권장합니다.
    //FIXME: 알고리즘 이름은 PathFinderAI.GetAlgorithmName으로 일원화되었습니다.
    // 드롭다운에 표시할 알고리즘 이름 목록
    private readonly List<string> algorithmNames = new List<string>
    {
        "A*",
        "Dijkstra",
        "BFS",
        "DFS",
        "Greedy Best-First",
        "D*",
        "D* Lite"
    };

    // Start: 드롭다운 초기화 및 현재 알고리즘과 동기화
    private void Start()
    {
        if (pathFinderAI == null)
        {
            Debug.LogError("AlgorithmSelector: PathFinderAI가 할당되지 않았습니다.");
            return;
        }
        if (algorithmDropdown == null)
        {
            Debug.LogError("AlgorithmSelector: Dropdown이 할당되지 않았습니다.");
            return;
        }

        InitDropdown();
    }

    // 드롭다운 항목 등록 및 초기값 설정
    private void InitDropdown()
    {
        algorithmDropdown.ClearOptions();
        algorithmDropdown.AddOptions(BuildAlgorithmNames());

        // PathFinderAI의 현재 알고리즘으로 초기 선택값 동기화
        int currentIndex = algorithmOrder.IndexOf(pathFinderAI.SelectedAlgorithm);
        if (currentIndex < 0) currentIndex = 0;

        algorithmDropdown.SetValueWithoutNotify(currentIndex);
        algorithmDropdown.onValueChanged.AddListener(OnDropdownChanged);

        UpdateAlgorithmText(currentIndex);
    }

    // 드롭다운 값 변경 시 PathFinderAI에 알고리즘 적용
    private void OnDropdownChanged(int index)
    {
        if (index < 0 || index >= algorithmOrder.Count)
        {
            Debug.LogWarning("AlgorithmSelector: 유효하지 않은 인덱스입니다.");
            return;
        }

        PathFinderAI.Algorithm selected = algorithmOrder[index];
        pathFinderAI.SelectedAlgorithm = selected;

        UpdateAlgorithmText(index);

        Debug.Log(string.Format("알고리즘 변경: {0}", pathFinderAI.GetAlgorithmName(selected)));
    }

    // algorithmOrder를 순회하여 드롭다운 표시용 이름 목록 생성
    // 이름은 PathFinderAI.GetAlgorithmName을 재사용하여 단일 소스로 관리한다
    private List<string> BuildAlgorithmNames()
    {
        List<string> names = new List<string>(algorithmOrder.Count);
        foreach (PathFinderAI.Algorithm algo in algorithmOrder)
            names.Add(pathFinderAI.GetAlgorithmName(algo));
        return names;
    }

    // 현재 알고리즘 텍스트 갱신
    private void UpdateAlgorithmText(int index)
    {
        if (currentAlgorithmText == null || pathFinderAI == null) return;
        if (index < 0 || index >= algorithmOrder.Count) return;

        currentAlgorithmText.text = string.Format(
            "Current Algorithm: {0}", pathFinderAI.GetAlgorithmName(algorithmOrder[index]));
    }

    // A* 선택 (버튼 OnClick에서 직접 호출 가능)
    public void SelectAStar()           { SetAlgorithm(PathFinderAI.Algorithm.AStar); }

    // Dijkstra 선택 (버튼 OnClick에서 직접 호출 가능)
    public void SelectDijkstra()        { SetAlgorithm(PathFinderAI.Algorithm.Dijkstra); }

    // BFS 선택 (버튼 OnClick에서 직접 호출 가능)
    public void SelectBFS()             { SetAlgorithm(PathFinderAI.Algorithm.BFS); }

    // DFS 선택 (버튼 OnClick에서 직접 호출 가능)
    public void SelectDFS()             { SetAlgorithm(PathFinderAI.Algorithm.DFS); }

    // Greedy Best-First 선택 (버튼 OnClick에서 직접 호출 가능)
    public void SelectGreedyBestFirst() { SetAlgorithm(PathFinderAI.Algorithm.GreedyBestFirst); }

    // D* 선택 (버튼 OnClick에서 직접 호출 가능)
    public void SelectDStar()           { SetAlgorithm(PathFinderAI.Algorithm.DStar); }

    // D* Lite 선택 (버튼 OnClick에서 직접 호출 가능)
    public void SelectDStarLite()       { SetAlgorithm(PathFinderAI.Algorithm.DStarLite); }

    // 지정 알고리즘으로 드롭다운과 PathFinderAI를 동시에 갱신
    private void SetAlgorithm(PathFinderAI.Algorithm algorithm)
    {
        if (pathFinderAI == null) return;

        int index = algorithmOrder.IndexOf(algorithm);
        if (index < 0) return;

        pathFinderAI.SelectedAlgorithm = algorithm;

        if (algorithmDropdown != null)
            algorithmDropdown.SetValueWithoutNotify(index);

        UpdateAlgorithmText(index);

        Debug.Log(string.Format("알고리즘 변경: {0}", pathFinderAI.GetAlgorithmName(algorithm)));
    }
}
