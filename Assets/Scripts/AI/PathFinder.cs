using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//TODO: 경로 탐색 알고리즘을 코루틴으로 구현하는 스크립트입니다.
//TODO: 구현 기능: A*, Dijkstra, BFS, DFS, Greedy Best-First 알고리즘, 단계별 시각화, 통계 수집

// 다양한 경로 탐색 알고리즘과 시각화 코루틴을 담당하는 클래스
public class PathFinder : MonoBehaviour
{
    // 알고리즘 종류 열거형
    public enum AlgorithmType
    {
        AStar,
        Dijkstra,
        BFS,
        DFS,
        Greedy
    }

    // GridManager 참조
    [SerializeField]
    private GridManager gridManager;

    // AgentController 참조
    [SerializeField]
    private AgentController agentController;

    // 선택된 알고리즘
    [SerializeField]
    private AlgorithmType selectedAlgorithm = AlgorithmType.AStar;

    // 탐색 단계별 애니메이션 딜레이 (초)
    [SerializeField]
    private float searchDelay = 0.01f;

    // 경로 표시 애니메이션 딜레이 (초)
    [SerializeField]
    private float pathDelay = 0.02f;

    // 방문한 노드 수
    public int VisitedCount { get; private set; }

    // 경로 길이 (노드 수)
    public int PathLength { get; private set; }

    // 탐색 소요 시간 (밀리초)
    public float ElapsedMs { get; private set; }

    // 경로 탐색 실행 중 여부
    public bool IsRunning { get; private set; }

    // 알고리즘 외부 설정 프로퍼티
    public AlgorithmType SelectedAlgorithm
    {
        get => selectedAlgorithm;
        set => selectedAlgorithm = value;
    }

    // 현재 실행 중인 탐색 코루틴
    private Coroutine searchCoroutine;

    // 찾은 경로 노드 목록
    private List<GridNode> pathNodes = new List<GridNode>();

    // 경로 탐색 시작
    public void RunPathfinding()
    {
        if (IsRunning) return;
        if (gridManager == null)
        {
            Debug.LogError("GridManager가 할당되지 않았습니다.");
            return;
        }

        gridManager.ResetSearchState();
        pathNodes.Clear();
        VisitedCount = 0;
        PathLength = 0;
        ElapsedMs = 0f;

        searchCoroutine = StartCoroutine(SearchCoroutine());
    }

    // 탐색 강제 정지
    public void StopPathfinding()
    {
        if (searchCoroutine != null)
        {
            StopCoroutine(searchCoroutine);
            searchCoroutine = null;
        }
        IsRunning = false;
        if (gridManager != null)
            gridManager.IsSearching = false;
    }

    // 알고리즘 선택 및 실행 코루틴
    private IEnumerator SearchCoroutine()
    {
        IsRunning = true;
        gridManager.IsSearching = true;
        float startTime = Time.realtimeSinceStartup;

        switch (selectedAlgorithm)
        {
            case AlgorithmType.AStar:
                yield return StartCoroutine(RunAStar());
                break;
            case AlgorithmType.Dijkstra:
                yield return StartCoroutine(RunDijkstra());
                break;
            case AlgorithmType.BFS:
                yield return StartCoroutine(RunBFS());
                break;
            case AlgorithmType.DFS:
                yield return StartCoroutine(RunDFS());
                break;
            case AlgorithmType.Greedy:
                yield return StartCoroutine(RunGreedy());
                break;
        }

        ElapsedMs = (Time.realtimeSinceStartup - startTime) * 1000f;
        IsRunning = false;
        gridManager.IsSearching = false;

        if (pathNodes.Count > 0)
        {
            if (agentController != null)
                agentController.StartMovement(pathNodes);
        }
        else
        {
            Debug.LogWarning("경로를 찾을 수 없습니다.");
        }
    }

    // A* 알고리즘: 휴리스틱(맨해튼 거리)을 사용하여 최단 경로 탐색
    private IEnumerator RunAStar()
    {
        GridNode start = gridManager.StartNode;
        GridNode end = gridManager.EndNode;

        List<GridNode> openList = new List<GridNode>();
        HashSet<GridNode> closedSet = new HashSet<GridNode>();

        start.gCost = 0f;
        start.hCost = gridManager.GetHeuristic(start, end);
        openList.Add(start);
        start.isOpen = true;
        gridManager.RefreshCell(start);

        while (openList.Count > 0)
        {
            openList.Sort((a, b) => a.FCost == b.FCost ?
                a.hCost.CompareTo(b.hCost) : a.FCost.CompareTo(b.FCost));

            GridNode current = openList[0];
            openList.RemoveAt(0);

            if (current == end)
            {
                yield return StartCoroutine(TracePath(current));
                yield break;
            }

            closedSet.Add(current);
            current.isVisited = true;
            current.isOpen = false;
            gridManager.RefreshCell(current);
            VisitedCount++;

            yield return new WaitForSeconds(searchDelay);

            foreach (GridNode neighbor in gridManager.GetNeighbors(current))
            {
                if (closedSet.Contains(neighbor)) continue;

                float newG = current.gCost + 1f;
                if (newG < neighbor.gCost)
                {
                    neighbor.gCost = newG;
                    neighbor.hCost = gridManager.GetHeuristic(neighbor, end);
                    neighbor.parent = current;

                    if (!openList.Contains(neighbor))
                    {
                        openList.Add(neighbor);
                        neighbor.isOpen = true;
                        gridManager.RefreshCell(neighbor);
                    }
                }
            }
        }
    }

    // Dijkstra 알고리즘: 모든 방향 균등 탐색, 최단 경로 보장
    private IEnumerator RunDijkstra()
    {
        GridNode start = gridManager.StartNode;
        GridNode end = gridManager.EndNode;

        List<GridNode> openList = new List<GridNode>();
        HashSet<GridNode> closedSet = new HashSet<GridNode>();

        start.gCost = 0f;
        openList.Add(start);
        start.isOpen = true;
        gridManager.RefreshCell(start);

        while (openList.Count > 0)
        {
            openList.Sort((a, b) => a.gCost.CompareTo(b.gCost));
            GridNode current = openList[0];
            openList.RemoveAt(0);

            if (closedSet.Contains(current)) continue;
            closedSet.Add(current);

            if (current == end)
            {
                yield return StartCoroutine(TracePath(current));
                yield break;
            }

            current.isVisited = true;
            current.isOpen = false;
            gridManager.RefreshCell(current);
            VisitedCount++;

            yield return new WaitForSeconds(searchDelay);

            foreach (GridNode neighbor in gridManager.GetNeighbors(current))
            {
                if (closedSet.Contains(neighbor)) continue;

                float newG = current.gCost + 1f;
                if (newG < neighbor.gCost)
                {
                    neighbor.gCost = newG;
                    neighbor.parent = current;
                    openList.Add(neighbor);
                    neighbor.isOpen = true;
                    gridManager.RefreshCell(neighbor);
                }
            }
        }
    }

    // BFS 알고리즘: 가중치 없는 최단 경로 보장, 물결처럼 확산
    private IEnumerator RunBFS()
    {
        GridNode start = gridManager.StartNode;
        GridNode end = gridManager.EndNode;

        Queue<GridNode> queue = new Queue<GridNode>();
        HashSet<GridNode> visited = new HashSet<GridNode>();

        queue.Enqueue(start);
        visited.Add(start);
        start.isOpen = true;
        gridManager.RefreshCell(start);

        while (queue.Count > 0)
        {
            GridNode current = queue.Dequeue();

            if (current == end)
            {
                yield return StartCoroutine(TracePath(current));
                yield break;
            }

            current.isVisited = true;
            current.isOpen = false;
            gridManager.RefreshCell(current);
            VisitedCount++;

            yield return new WaitForSeconds(searchDelay);

            foreach (GridNode neighbor in gridManager.GetNeighbors(current))
            {
                if (!visited.Contains(neighbor))
                {
                    visited.Add(neighbor);
                    neighbor.parent = current;
                    queue.Enqueue(neighbor);
                    neighbor.isOpen = true;
                    gridManager.RefreshCell(neighbor);
                }
            }
        }
    }

    // DFS 알고리즘: 한 방향으로 끝까지 탐색, 최단 경로 미보장
    private IEnumerator RunDFS()
    {
        GridNode start = gridManager.StartNode;
        GridNode end = gridManager.EndNode;

        Stack<GridNode> stack = new Stack<GridNode>();
        HashSet<GridNode> visited = new HashSet<GridNode>();

        stack.Push(start);
        visited.Add(start);
        start.isOpen = true;
        gridManager.RefreshCell(start);

        while (stack.Count > 0)
        {
            GridNode current = stack.Pop();

            if (current == end)
            {
                yield return StartCoroutine(TracePath(current));
                yield break;
            }

            current.isVisited = true;
            current.isOpen = false;
            gridManager.RefreshCell(current);
            VisitedCount++;

            yield return new WaitForSeconds(searchDelay);

            foreach (GridNode neighbor in gridManager.GetNeighbors(current))
            {
                if (!visited.Contains(neighbor))
                {
                    visited.Add(neighbor);
                    neighbor.parent = current;
                    stack.Push(neighbor);
                    neighbor.isOpen = true;
                    gridManager.RefreshCell(neighbor);
                }
            }
        }
    }

    // Greedy Best-First 알고리즘: 목표 거리만 고려, 빠르지만 최단 경로 미보장
    private IEnumerator RunGreedy()
    {
        GridNode start = gridManager.StartNode;
        GridNode end = gridManager.EndNode;

        List<GridNode> openList = new List<GridNode>();
        HashSet<GridNode> visited = new HashSet<GridNode>();

        start.hCost = gridManager.GetHeuristic(start, end);
        openList.Add(start);
        visited.Add(start);
        start.isOpen = true;
        gridManager.RefreshCell(start);

        while (openList.Count > 0)
        {
            openList.Sort((a, b) => a.hCost.CompareTo(b.hCost));
            GridNode current = openList[0];
            openList.RemoveAt(0);

            if (current == end)
            {
                yield return StartCoroutine(TracePath(current));
                yield break;
            }

            current.isVisited = true;
            current.isOpen = false;
            gridManager.RefreshCell(current);
            VisitedCount++;

            yield return new WaitForSeconds(searchDelay);

            foreach (GridNode neighbor in gridManager.GetNeighbors(current))
            {
                if (!visited.Contains(neighbor))
                {
                    visited.Add(neighbor);
                    neighbor.hCost = gridManager.GetHeuristic(neighbor, end);
                    neighbor.parent = current;
                    openList.Add(neighbor);
                    neighbor.isOpen = true;
                    gridManager.RefreshCell(neighbor);
                }
            }
        }
    }

    // 경로 역추적 및 시각화 코루틴
    private IEnumerator TracePath(GridNode endNode)
    {
        List<GridNode> reversed = new List<GridNode>();
        GridNode current = endNode;

        while (current != null)
        {
            reversed.Add(current);
            current.isPath = true;
            gridManager.RefreshCell(current);
            current = current.parent;
            yield return new WaitForSeconds(pathDelay);
        }

        reversed.Reverse();
        pathNodes.AddRange(reversed);
        PathLength = pathNodes.Count;
    }
}
