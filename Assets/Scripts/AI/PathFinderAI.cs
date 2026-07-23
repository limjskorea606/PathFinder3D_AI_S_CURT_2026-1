using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

//TODO: GridManager의 그리드를 참조하여 경로 탐색 알고리즘을 수행하는 AI 스크립트입니다.
//TODO: 구현 기능: A*, Dijkstra, BFS, DFS, Greedy Best-First(GBF), D*, D*Lite 알고리즘
//TODO: 구현 기능: 탐색 과정 애니메이션, 탐색 메트릭(방문 수, 경로 길이, 계산 시간) 수집, 일괄 비교
//TODO: PlayerAI의 Move 함수에서 SetNextPOS를 호출하면 다음 이동 목표 좌표를 반환합니다.
//TODO: 장벽 상태는 GridManager.WallsDirty 플래그를 통해 감지합니다. 별도 그리드 설정 불필요.

// GridManager 그리드를 기반으로 경로 탐색 알고리즘을 제공하는 AI 클래스
public class PathFinderAI : MonoBehaviour
{
    // 알고리즘 종류 열거형
    public enum Algorithm { AStar, Dijkstra, BFS, DFS, GreedyBestFirst, DStar, DStarLite }

    // 한 번의 탐색 결과 성능 지표를 담는 클래스 (통계 패널, 일괄 비교에서 사용)
    public class SearchMetrics
    {
        // 측정한 알고리즘 이름
        public string algorithmName;

        // 경로 탐색 성공 여부
        public bool success;

        // 탐색 중 방문 확정한 노드 수 (탐색 효율 지표, 낮을수록 효율적)
        public int visitedCount;

        // 최종 경로의 이동 단계 수 (경로 품질 지표)
        public int pathLength;

        // 알고리즘 계산에 소요된 시간 (밀리초)
        public double elapsedMs;

        // 탐색 중 열린 목록의 최대 크기 (메모리 사용 지표)
        public int maxOpenSize;
    }

    // GridManager 참조 (그리드 데이터, 이웃 노드, 좌표 변환 제공)
    [SerializeField]
    private GridManager gridManager;

    // 선택된 알고리즘
    [SerializeField]
    private Algorithm selectedAlgorithm = Algorithm.AStar;

    // Scene 뷰 경로 시각화 활성 여부
    [SerializeField]
    private bool showGizmos = true;

    // 탐색 과정 애니메이션에서 방문 노드 한 칸을 칠하는 간격 (초). 작을수록 빠름
    [SerializeField]
    private float searchStepDelay = 0.008f;

    // 애니메이션 한 프레임에 칠하는 방문 노드 수 (클수록 빠르게 진행)
    [SerializeField]
    private int nodesPerStep = 2;

    // 현재 계산된 경로의 월드 위치 목록
    private List<Vector3> currentPath;

    // currentPath에서 다음으로 반환할 인덱스
    private int pathIndex;

    // 마지막 계산에 사용된 목표 위치 (재계산 판단용)
    private Vector3 lastGoalPos;

    // 경로 재계산 필요 여부 플래그
    private bool pathDirty;

    // 탐색 중 방문 확정한 노드 순서 기록 (애니메이션, 메트릭용)
    private List<GridNode> searchVisitedOrder = new List<GridNode>();

    // 탐색 중 열린 목록 최대 크기 기록 (메트릭용)
    private int searchMaxOpenSize;

    // 마지막 탐색 결과 메트릭 (통계 패널에서 조회)
    public SearchMetrics LastMetrics { get; private set; }

    // 버튼 실행으로 시작점에서 목표점까지 전체 경로를 계산 완료했을 때 발생하는 이벤트
    // 대시보드가 구독하여 알고리즘별 성능을 누적 기록한다 (이동 중 부분 재계산은 제외)
    public event System.Action<SearchMetrics> OnSearchCompleted;

    // 경로 계산 성공 여부
    public bool IsPathReady { get; private set; }

    // 경로 재계산 필요 여부 (PlayerAI의 MoveToPosition 조기 종료 판단용)
    public bool IsPathDirty => pathDirty;

    // 알고리즘 외부 설정 프로퍼티
    public Algorithm SelectedAlgorithm
    {
        get => selectedAlgorithm;
        set => selectedAlgorithm = value;
    }

    // Update: GridManager의 장벽 변경 감지 시 재계산 플래그 설정
    private void Update()
    {
        if (gridManager == null) return;

        if (gridManager.WallsDirty)
        {
            gridManager.ClearWallsDirty();
            pathDirty = true;
        }
    }

    // 외부에서 즉시 경로 재계산을 요청 (벽 오브젝트를 코드로 생성/삭제할 때 호출)
    public void RequestRecalculate()
    {
        pathDirty = true;
    }

    // 다음 이동 목표 위치 반환 (PlayerAI의 Move에서 호출)
    // 목표 변경, 장벽 변경, 경로 소진 시 자동 재계산한다 (이동 중 재계산은 애니메이션 없음).
    public Vector3 SetNextPOS(Vector3 currentPos, Vector3 goalPos)
    {
        bool needRecalc = currentPath == null || goalPos != lastGoalPos || pathDirty;

        if (needRecalc)
        {
            pathDirty   = false;
            lastGoalPos = goalPos;
            CalculatePath(currentPos, goalPos);
            pathIndex = 0;
        }

        if (!IsPathReady || currentPath == null || currentPath.Count == 0)
            return currentPos;

        if (pathIndex >= currentPath.Count)
            return goalPos;

        return currentPath[pathIndex++];
    }

    // 경로를 계산하고 탐색 과정을 애니메이션으로 보여준 뒤 종료하는 코루틴
    // PlayerAI의 Move/Restart 버튼에서 이동 시작 전에 호출한다.
    public IEnumerator PrepareAndAnimate(Vector3 from, Vector3 to)
    {
        // 경로와 방문 순서를 계산 (셀 색은 아직 칠하지 않음)
        List<GridNode> nodePath = ComputeNodePath(from, to, out SearchMetrics metrics);
        LastMetrics = metrics;

        // 전체 경로 탐색 완료를 대시보드 등 구독자에게 알림
        OnSearchCompleted?.Invoke(metrics);

        lastGoalPos = to;
        pathDirty   = false;
        pathIndex   = 0;

        BuildCurrentPath(nodePath);

        // 탐색 과정 애니메이션 재생 (방문 노드를 순서대로 칠한 뒤 경로 강조)
        // StartCoroutine으로 분리하지 않고 직접 yield하여 호출자(PlayerAI)의 코루틴 트리에 포함시킨다.
        // 이렇게 해야 PlayerAI가 Stop 시 상위 코루틴을 멈출 때 애니메이션도 함께 중단된다.
        yield return AnimateSearch(nodePath);
    }

    // 탐색 방문 순서를 단계적으로 칠하고 마지막에 경로를 강조하는 코루틴
    private IEnumerator AnimateSearch(List<GridNode> nodePath)
    {
        if (gridManager == null || gridManager.Grid == null) yield break;

        // 애니메이션 동안 마우스 편집 잠금
        gridManager.IsSearching = true;

        // 이전 시각화 제거 후 시작 상태 표시
        gridManager.ResetSearchState();

        // 방문 노드를 기록된 순서대로 점진적으로 칠함
        int counter = 0;
        foreach (GridNode node in searchVisitedOrder)
        {
            node.isVisited = true;
            gridManager.RefreshCell(node);

            counter++;
            if (counter % Mathf.Max(1, nodesPerStep) == 0)
                yield return new WaitForSeconds(searchStepDelay);
        }

        // 최종 경로를 파란색으로 강조
        if (nodePath != null)
        {
            foreach (GridNode node in nodePath)
            {
                node.isPath = true;
                gridManager.RefreshCell(node);
            }
        }

        gridManager.IsSearching = false;
    }

    // 선택된 알고리즘으로 경로 계산 (애니메이션 없이 즉시 반영, 이동 중 재계산에서 호출)
    private void CalculatePath(Vector3 from, Vector3 to)
    {
        List<GridNode> nodePath = ComputeNodePath(from, to, out SearchMetrics metrics);
        LastMetrics = metrics;

        BuildCurrentPath(nodePath);

        // 경로 시각화 갱신 (방문 셀 + 경로 셀)
        if (gridManager != null && gridManager.Grid != null)
        {
            foreach (GridNode node in searchVisitedOrder)
                node.isVisited = true;
            if (nodePath != null)
                foreach (GridNode node in nodePath)
                    node.isPath = true;
            gridManager.RefreshAllCells();
        }
    }

    // 노드 경로를 월드 좌표 currentPath로 변환하고 IsPathReady 갱신
    private void BuildCurrentPath(List<GridNode> nodePath)
    {
        if (nodePath != null && nodePath.Count > 0)
        {
            currentPath = new List<Vector3>(nodePath.Count);
            foreach (GridNode n in nodePath)
                currentPath.Add(n.worldPosition);
            IsPathReady = true;
        }
        else
        {
            currentPath = null;
            IsPathReady = false;
        }
    }

    // 경로 탐색 계산 코어: 선택 알고리즘으로 경로와 메트릭을 산출 (셀 색상은 건드리지 않음)
    // 애니메이션, 실시간 재계산, 일괄 비교가 공통으로 사용한다.
    private List<GridNode> ComputeNodePath(Vector3 from, Vector3 to, out SearchMetrics metrics)
    {
        metrics = new SearchMetrics
        {
            algorithmName = GetAlgorithmName(selectedAlgorithm),
            success       = false,
            visitedCount  = 0,
            pathLength    = 0,
            elapsedMs     = 0.0,
            maxOpenSize   = 0
        };

        if (gridManager == null || gridManager.Grid == null)
        {
            UnityEngine.Debug.LogError("GridManager가 할당되지 않았거나 그리드가 초기화되지 않았습니다.");
            return null;
        }

        GridNode startNode = gridManager.GetNodeFromWorldPosition(from);
        GridNode endNode   = gridManager.GetNodeFromWorldPosition(to);

        if (startNode == null || endNode == null)
        {
            UnityEngine.Debug.LogError("시작점 또는 목표점이 그리드 범위를 벗어났습니다.");
            return null;
        }

        if (startNode.isWall || endNode.isWall)
        {
            UnityEngine.Debug.LogWarning("시작점 또는 목표점이 장벽 위에 있습니다.");
            return null;
        }

        return ComputeNodePathByNodes(selectedAlgorithm, startNode, endNode, out metrics);
    }

    // 지정 알고리즘과 노드로 경로/메트릭 산출 (일괄 비교에서 알고리즘별 직접 호출용)
    private List<GridNode> ComputeNodePathByNodes(Algorithm algo, GridNode startNode, GridNode endNode, out SearchMetrics metrics)
    {
        metrics = new SearchMetrics
        {
            algorithmName = GetAlgorithmName(algo),
            success       = false,
            visitedCount  = 0,
            pathLength    = 0,
            elapsedMs     = 0.0,
            maxOpenSize   = 0
        };

        ResetGrid();
        searchVisitedOrder.Clear();
        searchMaxOpenSize = 0;

        Stopwatch sw = Stopwatch.StartNew();

        List<GridNode> nodePath = null;
        switch (algo)
        {
            case Algorithm.AStar:           nodePath = RunAStar(startNode, endNode);     break;
            case Algorithm.Dijkstra:        nodePath = RunDijkstra(startNode, endNode);  break;
            case Algorithm.BFS:             nodePath = RunBFS(startNode, endNode);       break;
            case Algorithm.DFS:             nodePath = RunDFS(startNode, endNode);       break;
            case Algorithm.GreedyBestFirst: nodePath = RunGBF(startNode, endNode);       break;
            case Algorithm.DStar:           nodePath = RunDStar(startNode, endNode);     break;
            case Algorithm.DStarLite:       nodePath = RunDStarLite(startNode, endNode); break;
            default:                        nodePath = RunAStar(startNode, endNode);     break;
        }

        sw.Stop();

        metrics.elapsedMs    = sw.Elapsed.TotalMilliseconds;
        metrics.visitedCount = searchVisitedOrder.Count;
        metrics.maxOpenSize  = searchMaxOpenSize;
        metrics.success      = nodePath != null && nodePath.Count > 0;
        metrics.pathLength   = metrics.success ? nodePath.Count : 0;

        return nodePath;
    }

    // 동일한 시작/목표점에서 7개 알고리즘을 모두 실행하여 메트릭 목록을 반환 (일괄 비교 버튼용)
    // 계산만 수행하며 마지막에 그리드 시각화를 복원한다.
    public List<SearchMetrics> RunAllAlgorithms(Vector3 from, Vector3 to)
    {
        List<SearchMetrics> results = new List<SearchMetrics>();

        if (gridManager == null || gridManager.Grid == null)
        {
            UnityEngine.Debug.LogError("그리드가 초기화되지 않아 일괄 비교를 실행할 수 없습니다.");
            return results;
        }

        GridNode startNode = gridManager.GetNodeFromWorldPosition(from);
        GridNode endNode   = gridManager.GetNodeFromWorldPosition(to);

        if (startNode == null || endNode == null || startNode.isWall || endNode.isWall)
        {
            UnityEngine.Debug.LogWarning("시작점 또는 목표점이 유효하지 않아 일괄 비교를 실행할 수 없습니다.");
            return results;
        }

        Algorithm[] all =
        {
            Algorithm.AStar, Algorithm.Dijkstra, Algorithm.BFS, Algorithm.DFS,
            Algorithm.GreedyBestFirst, Algorithm.DStar, Algorithm.DStarLite
        };

        foreach (Algorithm algo in all)
        {
            ComputeNodePathByNodes(algo, startNode, endNode, out SearchMetrics m);
            results.Add(m);
        }

        // 비교 후 그리드 상태 복원
        gridManager.ResetSearchState();

        return results;
    }

    // 알고리즘 열거형을 표시용 문자열로 변환
    public string GetAlgorithmName(Algorithm algo)
    {
        switch (algo)
        {
            case Algorithm.AStar:           return "A*";
            case Algorithm.Dijkstra:        return "Dijkstra";
            case Algorithm.BFS:             return "BFS";
            case Algorithm.DFS:             return "DFS";
            case Algorithm.GreedyBestFirst: return "Greedy BF";
            case Algorithm.DStar:           return "D*";
            case Algorithm.DStarLite:       return "D* Lite";
            default:                        return algo.ToString();
        }
    }

    // 그리드의 경로 시각화를 초기화 (이동 정지 시 PlayerAI에서 호출)
    // 애니메이션 도중 중단된 경우를 대비해 마우스 편집 잠금(IsSearching)도 해제한다.
    public void ClearPathVisualization()
    {
        if (gridManager == null || gridManager.Grid == null) return;
        gridManager.IsSearching = false;
        gridManager.ResetSearchState();
        gridManager.RefreshAllCells();
    }

    // 그리드 탐색 상태 초기화 (장벽 유지)
    private void ResetGrid()
    {
        for (int r = 0; r < gridManager.GetRows(); r++)
            for (int c = 0; c < gridManager.GetCols(); c++)
                gridManager.Grid[r, c].ResetSearchState();
    }

    // 열린 목록 최대 크기 갱신
    private void TrackOpenSize(int openCount)
    {
        if (openCount > searchMaxOpenSize)
            searchMaxOpenSize = openCount;
    }

    // 맨해튼 거리 휴리스틱
    private float Heuristic(GridNode a, GridNode b)
    {
        return Mathf.Abs(a.row - b.row) + Mathf.Abs(a.col - b.col);
    }

    // 경로 역추적: endNode에서 parent 링크를 따라 startNode까지 역추적 후 반전
    // 반환 경로에서 시작 노드는 제외 (Player가 이미 위치하므로)
    private List<GridNode> TracePath(GridNode endNode)
    {
        List<GridNode> path = new List<GridNode>();
        GridNode current    = endNode;

        while (current != null)
        {
            path.Add(current);
            current = current.parent;
        }

        path.Reverse();

        if (path.Count > 0)
            path.RemoveAt(0);

        return path;
    }

    // A* 알고리즘: 휴리스틱과 실제 비용을 결합하여 최단 경로 탐색
    // 열린 목록은 MinHeap 우선순위 큐로 관리한다 (F 비용 우선, 동률 시 H 비용)
    // 비용 갱신 시 재삽입하고 꺼낼 때 이미 확정된 노드는 건너뛴다 (lazy deletion)
    private List<GridNode> RunAStar(GridNode start, GridNode end)
    {
        MinHeap<GridNode> openList = new MinHeap<GridNode>((a, b) =>
            a.FCost == b.FCost ? a.hCost.CompareTo(b.hCost) : a.FCost.CompareTo(b.FCost));
        HashSet<GridNode> closedSet = new HashSet<GridNode>();

        start.gCost = 0f;
        start.hCost = Heuristic(start, end);
        openList.Push(start);

        while (openList.Count > 0)
        {
            TrackOpenSize(openList.Count);

            GridNode current = openList.Pop();

            // 이미 확정된 노드는 오래된 중복 항목이므로 건너뜀
            if (closedSet.Contains(current)) continue;

            if (current == end)
                return TracePath(current);

            closedSet.Add(current);
            searchVisitedOrder.Add(current);

            foreach (GridNode nb in gridManager.GetNeighbors(current))
            {
                if (closedSet.Contains(nb)) continue;

                float newG = current.gCost + 1f;
                if (newG < nb.gCost)
                {
                    nb.gCost  = newG;
                    nb.hCost  = Heuristic(nb, end);
                    nb.parent = current;
                    openList.Push(nb);
                }
            }
        }
        return null;
    }

    // Dijkstra 알고리즘: 모든 방향 균등 탐색, 최단 경로 보장
    // 열린 목록은 MinHeap 우선순위 큐로 관리한다 (G 비용 우선)
    private List<GridNode> RunDijkstra(GridNode start, GridNode end)
    {
        MinHeap<GridNode> openList  = new MinHeap<GridNode>((a, b) => a.gCost.CompareTo(b.gCost));
        HashSet<GridNode> closedSet = new HashSet<GridNode>();

        start.gCost = 0f;
        openList.Push(start);

        while (openList.Count > 0)
        {
            TrackOpenSize(openList.Count);

            GridNode current = openList.Pop();

            if (closedSet.Contains(current)) continue;
            closedSet.Add(current);
            searchVisitedOrder.Add(current);

            if (current == end)
                return TracePath(current);

            foreach (GridNode nb in gridManager.GetNeighbors(current))
            {
                if (closedSet.Contains(nb)) continue;

                float newG = current.gCost + 1f;
                if (newG < nb.gCost)
                {
                    nb.gCost  = newG;
                    nb.parent = current;
                    openList.Push(nb);
                }
            }
        }
        return null;
    }

    // BFS 알고리즘: 가중치 없는 최단 경로 보장, 물결처럼 확산
    private List<GridNode> RunBFS(GridNode start, GridNode end)
    {
        Queue<GridNode> queue     = new Queue<GridNode>();
        HashSet<GridNode> visited = new HashSet<GridNode>();

        start.gCost = 0f;
        queue.Enqueue(start);
        visited.Add(start);

        while (queue.Count > 0)
        {
            TrackOpenSize(queue.Count);

            GridNode current = queue.Dequeue();
            searchVisitedOrder.Add(current);

            if (current == end)
                return TracePath(current);

            foreach (GridNode nb in gridManager.GetNeighbors(current))
            {
                if (!visited.Contains(nb))
                {
                    visited.Add(nb);
                    nb.gCost  = current.gCost + 1f;
                    nb.parent = current;
                    queue.Enqueue(nb);
                }
            }
        }
        return null;
    }

    // DFS 알고리즘: 한 방향으로 끝까지 탐색, 최단 경로 미보장
    private List<GridNode> RunDFS(GridNode start, GridNode end)
    {
        Stack<GridNode> stack     = new Stack<GridNode>();
        HashSet<GridNode> visited = new HashSet<GridNode>();

        stack.Push(start);
        visited.Add(start);

        while (stack.Count > 0)
        {
            TrackOpenSize(stack.Count);

            GridNode current = stack.Pop();
            searchVisitedOrder.Add(current);

            if (current == end)
                return TracePath(current);

            foreach (GridNode nb in gridManager.GetNeighbors(current))
            {
                if (!visited.Contains(nb))
                {
                    visited.Add(nb);
                    nb.parent = current;
                    stack.Push(nb);
                }
            }
        }
        return null;
    }

    // Greedy Best-First(GBF) 알고리즘: 목표까지 휴리스틱만 사용, 빠르지만 최단 경로 미보장
    // 열린 목록은 MinHeap 우선순위 큐로 관리한다 (H 비용 우선)
    private List<GridNode> RunGBF(GridNode start, GridNode end)
    {
        MinHeap<GridNode> openList = new MinHeap<GridNode>((a, b) => a.hCost.CompareTo(b.hCost));
        HashSet<GridNode> visited  = new HashSet<GridNode>();

        start.hCost = Heuristic(start, end);
        openList.Push(start);
        visited.Add(start);

        while (openList.Count > 0)
        {
            TrackOpenSize(openList.Count);

            GridNode current = openList.Pop();
            searchVisitedOrder.Add(current);

            if (current == end)
                return TracePath(current);

            foreach (GridNode nb in gridManager.GetNeighbors(current))
            {
                if (!visited.Contains(nb))
                {
                    visited.Add(nb);
                    nb.hCost  = Heuristic(nb, end);
                    nb.parent = current;
                    openList.Push(nb);
                }
            }
        }
        return null;
    }

    // D* 알고리즘: 목표에서 역방향으로 Dijkstra를 수행하여 비용 맵 생성 (Stentz, 1994)
    // 경로는 비용 기울기를 따라 추출.
    private List<GridNode> RunDStar(GridNode start, GridNode end)
    {
        MinHeap<GridNode> openList  = new MinHeap<GridNode>((a, b) => a.gCost.CompareTo(b.gCost));
        HashSet<GridNode> closedSet = new HashSet<GridNode>();

        // 역방향 탐색: end에서 시작하여 각 노드까지 비용을 계산
        end.gCost = 0f;
        openList.Push(end);

        while (openList.Count > 0)
        {
            TrackOpenSize(openList.Count);

            GridNode current = openList.Pop();

            if (closedSet.Contains(current)) continue;
            closedSet.Add(current);
            searchVisitedOrder.Add(current);

            if (current == start) break;

            foreach (GridNode nb in gridManager.GetNeighbors(current))
            {
                if (closedSet.Contains(nb)) continue;

                float newG = current.gCost + 1f;
                if (newG < nb.gCost)
                {
                    nb.gCost = newG;
                    openList.Push(nb);
                }
            }
        }

        return ExtractGradientPath(start, end);
    }

    // D*Lite 알고리즘: 역방향 증분 A* (Koenig & Likhachev, 2002)
    // rhs(s): 단방향 선견 비용, g(s): 현재 추정 비용
    private List<GridNode> RunDStarLite(GridNode start, GridNode end)
    {
        // D*Lite 초기화: 모든 노드 비용을 무한대로 설정
        for (int r = 0; r < gridManager.GetRows(); r++)
            for (int c = 0; c < gridManager.GetCols(); c++)
            {
                gridManager.Grid[r, c].gCost = float.MaxValue;
                gridManager.Grid[r, c].rhs   = float.MaxValue;
            }

        // 목표 노드의 rhs는 0 (자기 자신까지 비용이 없음)
        end.rhs = 0f;

        List<GridNode> openList = new List<GridNode> { end };
        HashSet<GridNode> recorded = new HashSet<GridNode>();

        DStarLiteComputeShortestPath(start, end, openList, recorded);

        if (start.gCost == float.MaxValue)
            return null;

        return ExtractGradientPath(start, end);
    }

    // D*Lite: 역방향 최단 경로 계산 루프
    private void DStarLiteComputeShortestPath(GridNode start, GridNode end, List<GridNode> openList, HashSet<GridNode> recorded)
    {
        while (openList.Count > 0)
        {
            TrackOpenSize(openList.Count);

            openList.Sort((a, b) =>
            {
                float ka1 = DStarLiteKey1(a, start);
                float kb1 = DStarLiteKey1(b, start);
                if (Mathf.Approximately(ka1, kb1))
                    return DStarLiteKey2(a).CompareTo(DStarLiteKey2(b));
                return ka1.CompareTo(kb1);
            });

            GridNode u  = openList[0];
            float topK1 = DStarLiteKey1(u, start);
            float topK2 = DStarLiteKey2(u);
            float sK1   = DStarLiteKey1(start, start);
            float sK2   = DStarLiteKey2(start);

            bool topSmaller = topK1 < sK1 ||
                              (Mathf.Approximately(topK1, sK1) && topK2 < sK2);

            if (!topSmaller && Mathf.Approximately(start.rhs, start.gCost))
                break;

            openList.RemoveAt(0);

            // 처음 처리하는 노드만 방문 순서에 기록 (중복 방지)
            if (recorded.Add(u))
                searchVisitedOrder.Add(u);

            if (u.gCost > u.rhs)
            {
                // 과소일관(overconsistent): 비용 확정
                u.gCost = u.rhs;
                foreach (GridNode nb in gridManager.GetNeighbors(u))
                    DStarLiteUpdateVertex(nb, end, openList);
            }
            else
            {
                // 과대일관(underconsistent): 비용 재산출 필요
                u.gCost = float.MaxValue;
                DStarLiteUpdateVertex(u, end, openList);
                foreach (GridNode nb in gridManager.GetNeighbors(u))
                    DStarLiteUpdateVertex(nb, end, openList);
            }
        }
    }

    // D*Lite: 단일 노드 rhs 갱신
    private void DStarLiteUpdateVertex(GridNode u, GridNode end, List<GridNode> openList)
    {
        if (u == end) return;

        float minRhs = float.MaxValue;

        foreach (GridNode nb in gridManager.GetNeighbors(u))
        {
            float cost = 1f + nb.gCost;
            if (cost < minRhs)
                minRhs = cost;
        }

        u.rhs = minRhs;

        openList.Remove(u);

        if (!Mathf.Approximately(u.gCost, u.rhs))
            openList.Add(u);
    }

    // D*Lite 우선순위 키 1: min(g, rhs) + h(start, u)
    private float DStarLiteKey1(GridNode u, GridNode start)
    {
        float minVal = Mathf.Min(u.gCost, u.rhs);
        if (minVal == float.MaxValue) return float.MaxValue;
        return minVal + Heuristic(start, u);
    }

    // D*Lite 우선순위 키 2: min(g, rhs)
    private float DStarLiteKey2(GridNode u)
    {
        return Mathf.Min(u.gCost, u.rhs);
    }

    // D* / D*Lite 공용: 비용 기울기(gCost 감소 방향)를 따라 start에서 end까지 경로 추출
    private List<GridNode> ExtractGradientPath(GridNode start, GridNode end)
    {
        List<GridNode> path       = new List<GridNode>();
        HashSet<GridNode> visited = new HashSet<GridNode>();
        GridNode current          = start;

        while (current != end)
        {
            if (visited.Contains(current))
            {
                UnityEngine.Debug.LogWarning("경로 추출 중 순환이 감지되었습니다. 경로를 찾을 수 없습니다.");
                return null;
            }

            visited.Add(current);
            path.Add(current);

            GridNode best  = null;
            float bestCost = float.MaxValue;

            foreach (GridNode nb in gridManager.GetNeighbors(current))
            {
                if (nb.gCost < bestCost)
                {
                    bestCost = nb.gCost;
                    best     = nb;
                }
            }

            if (best == null || bestCost == float.MaxValue)
                return null;

            current = best;
        }

        path.Add(end);

        if (path.Count > 0)
            path.RemoveAt(0);

        return path;
    }

    // Scene 뷰에서 오브젝트 선택 시 현재 경로를 파란 구체와 선으로 표시
    private void OnDrawGizmosSelected()
    {
        if (!showGizmos || currentPath == null || currentPath.Count == 0) return;

        Gizmos.color = new Color(0.2f, 0.5f, 1f, 0.9f);
        for (int i = 0; i < currentPath.Count; i++)
        {
            Gizmos.DrawSphere(currentPath[i], 0.2f);
            if (i < currentPath.Count - 1)
                Gizmos.DrawLine(currentPath[i], currentPath[i + 1]);
        }
    }
}
