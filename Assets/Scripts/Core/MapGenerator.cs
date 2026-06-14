using System.Collections.Generic;
using UnityEngine;

//TODO: 그리드 위에 벽 프리팹을 랜덤 배치하여 맵을 생성하는 스크립트입니다.
//TODO: 구현 기능: 랜덤 벽 배치, 시작/목표점 보호, BFS 경로 보장, 맵 초기화

// 랜덤 맵 생성 및 벽 오브젝트 배치를 담당하는 클래스
public class MapGenerator : MonoBehaviour
{
    // GridManager 참조 (그리드 정보 및 노드 접근용)
    [SerializeField]
    private GridManager gridManager;

    // 벽으로 배치할 프리팹 (Wall 레이어가 설정되어 있어야 PathFinderAI가 인식함)
    [SerializeField]
    private GameObject wallPrefab;

    // 벽 밀도 (0.0~0.8, 높을수록 벽이 많아짐)
    [SerializeField]
    [Range(0f, 0.8f)]
    private float wallDensity = 0.3f;

    // 경로 보장 실패 시 최대 재시도 횟수
    [SerializeField]
    private int maxRetryCount = 20;

    // 생성된 벽 오브젝트 목록 (초기화 시 제거용)
    private List<GameObject> spawnedWalls = new List<GameObject>();

    // 랜덤 맵 생성 (Inspector 우클릭 또는 외부 버튼에서 호출 가능)
    [ContextMenu("맵 생성")]
    public void GenerateMap()
    {
        if (gridManager == null)
        {
            Debug.LogError("GridManager가 할당되지 않았습니다.");
            return;
        }
        if (wallPrefab == null)
        {
            Debug.LogError("Wall Prefab이 할당되지 않았습니다.");
            return;
        }

        ClearWalls();

        // Grid가 초기화되지 않은 경우 먼저 빌드 (에디터 ContextMenu 호출 대응)
        if (gridManager.Grid == null)
            gridManager.BuildGrid();
        else
            gridManager.ClearAll();

        int rows = gridManager.GetRows();
        int cols = gridManager.GetCols();
        GridNode startNode = gridManager.StartNode;
        GridNode endNode   = gridManager.EndNode;

        bool success = false;

        for (int attempt = 0; attempt < maxRetryCount; attempt++)
        {
            // 이전 시도 벽 상태 초기화
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                    gridManager.Grid[r, c].isWall = false;

            // 랜덤 벽 후보 선정
            List<GridNode> wallNodes = new List<GridNode>();
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    GridNode node = gridManager.Grid[r, c];

                    // 시작점, 목표점은 항상 제외
                    if (node == startNode || node == endNode) continue;

                    if (Random.value < wallDensity)
                    {
                        node.isWall = true;
                        wallNodes.Add(node);
                    }
                }
            }

            // BFS로 시작점에서 목표점까지 경로 존재 여부 확인
            if (HasPathBFS(startNode, endNode))
            {
                foreach (GridNode wallNode in wallNodes)
                    SpawnWall(wallNode);

                gridManager.RefreshAllCells();
                Debug.Log(string.Format("맵 생성 완료. 시도: {0}회, 벽: {1}개", attempt + 1, wallNodes.Count));
                success = true;
                break;
            }
        }

        if (!success)
        {
            Debug.LogWarning(string.Format("최대 재시도 {0}회 초과. wallDensity를 낮춰보세요.", maxRetryCount));
            gridManager.ClearAll();
        }
    }

    // 생성된 벽 오브젝트 전체 제거 및 그리드 초기화
    [ContextMenu("맵 초기화")]
    public void ClearMap()
    {
        ClearWalls();
        if (gridManager != null)
            gridManager.ClearAll();
    }

    // BFS로 시작점에서 목표점까지 경로 존재 여부 확인
    private bool HasPathBFS(GridNode start, GridNode end)
    {
        if (start == null || end == null) return false;

        Queue<GridNode> queue     = new Queue<GridNode>();
        HashSet<GridNode> visited = new HashSet<GridNode>();

        queue.Enqueue(start);
        visited.Add(start);

        while (queue.Count > 0)
        {
            GridNode current = queue.Dequeue();
            if (current == end) return true;

            foreach (GridNode neighbor in gridManager.GetNeighbors(current))
            {
                if (!visited.Contains(neighbor))
                {
                    visited.Add(neighbor);
                    queue.Enqueue(neighbor);
                }
            }
        }
        return false;
    }

    // 지정 노드 위치에 벽 프리팹 생성
    private void SpawnWall(GridNode node)
    {
        GameObject wall = Instantiate(wallPrefab, node.worldPosition, Quaternion.identity);
        spawnedWalls.Add(wall);
    }

    // 생성된 벽 오브젝트 전체 제거
    // 에디터 모드에서는 DestroyImmediate, 플레이 모드에서는 Destroy 사용
    private void ClearWalls()
    {
        foreach (GameObject wall in spawnedWalls)
        {
            if (wall == null) continue;
            if (Application.isPlaying)
                Destroy(wall);
            else
                DestroyImmediate(wall);
        }
        spawnedWalls.Clear();
    }
}
