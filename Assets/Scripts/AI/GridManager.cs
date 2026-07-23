using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

//TODO: 3D 그리드를 생성하고 시각화하는 스크립트입니다.
//TODO: 구현 기능: 그리드 생성, 셀 색상 관리, 마우스 장벽 편집, 이웃 노드 반환, 시작/목표점 스냅

// 3D 그리드 생성, 시각화, 편집을 담당하는 매니저 클래스
public class GridManager : MonoBehaviour
{
    // 그리드 열 수
    [SerializeField]
    private int cols = 32;

    // 그리드 행 수
    [SerializeField]
    private int rows = 20;

    // 셀 하나의 크기 (유니티 단위)
    [SerializeField]
    private float cellSize = 1f;

    // 셀 간 간격
    [SerializeField]
    private float cellGap = 0.05f;

    // 셀 두께
    [SerializeField]
    private float cellHeight = 0.1f;

    // Scene 뷰 그리드 시각화 활성 여부
    [SerializeField]
    private bool showGizmos = true;

    // 그리드 중심 위치 (월드 공간). 이 위치를 기준으로 그리드 전체가 생성됨
    [SerializeField]
    private Vector3 gridCenter = Vector3.zero;

    // 시작점 오브젝트 Transform
    [SerializeField]
    private Transform startTransform;

    // 목표점 오브젝트 Transform
    [SerializeField]
    private Transform endTransform;

    // 그리드 노드 2차원 배열
    public GridNode[,] Grid { get; private set; }

    // 시작 노드
    public GridNode StartNode { get; private set; }

    // 목표 노드
    public GridNode EndNode { get; private set; }

    // 경로 탐색 실행 중 여부 (마우스 편집 잠금용)
    public bool IsSearching { get; set; }

    // 장벽 상태 변경 여부 (PathFinderAI 재계산 트리거용)
    public bool WallsDirty { get; private set; }

    // 맵 구조 변경 횟수. 벽 편집, 맵 생성, 시작/목표 변경 시 증가하며 대시보드 초기화 감지에 사용
    public int MapVersion { get; private set; }

    // PathFinderAI가 변경 감지 후 플래그 해제 시 호출
    public void ClearWallsDirty() { WallsDirty = false; }

    // 맵 구조가 변경되었음을 표시. 재계산 플래그를 세우고 맵 버전을 증가시킨다
    private void MarkMapChanged()
    {
        WallsDirty = true;
        MapVersion++;
    }

    //FIXME: 아래 함수는 더이상 사용하지 않습니다. 삭제를 권장합니다.
    // MapGenerator 등 외부에서 장벽 변경 신호를 보낼 때 호출
    public void MarkWallsDirty() { WallsDirty = true; }

    // 셀 색상 정의
    private static readonly Color colorNormal  = new Color(241 / 255f, 239 / 255f, 232 / 255f);
    private static readonly Color colorWall    = new Color( 44 / 255f,  44 / 255f,  42 / 255f);
    private static readonly Color colorStart   = new Color( 29 / 255f, 158 / 255f, 117 / 255f);
    private static readonly Color colorEnd     = new Color(216 / 255f,  90 / 255f,  48 / 255f);
    private static readonly Color colorVisited = new Color(181 / 255f, 212 / 255f, 244 / 255f);
    private static readonly Color colorOpen    = new Color(230 / 255f, 241 / 255f, 251 / 255f);
    private static readonly Color colorPath    = new Color( 55 / 255f, 138 / 255f, 221 / 255f);

    // 마우스 드래그 중 여부
    private bool isMouseDragging;

    // 드래그 시 장벽 설정 방향 (추가 or 제거)
    private bool dragSetWall;

    // 왼쪽 마우스 버튼 InputAction
    private InputAction leftClickAction;

    // 마우스 화면 위치 InputAction
    private InputAction mousePosAction;

    // 셀 부모 오브젝트
    private Transform cellParent;

    // 셀 색상 설정에 사용하는 머티리얼 프로퍼티 블록 (인스턴스 생성 방지)
    private MaterialPropertyBlock mpb;

    // URP: _BaseColor, Built-in: _Color (둘 다 설정하여 렌더 파이프라인 무관하게 동작)
    private static readonly int colorID     = Shader.PropertyToID("_Color");
    private static readonly int baseColorID = Shader.PropertyToID("_BaseColor");

    // 그리드 좌측 하단 모서리 위치 (gridCenter에서 역산, 내부 계산 전용)
    private Vector3 GridOrigin => gridCenter - new Vector3(
        (cols - 1) * cellSize * 0.5f,
        0f,
        (rows - 1) * cellSize * 0.5f
    );

    // 셀 GameObject 인스턴스 ID → GridNode 빠른 조회 딕셔너리
    private Dictionary<int, GridNode> cellLookup = new Dictionary<int, GridNode>();

    // 이전 프레임 시작점 위치 (이동 감지용)
    private Vector3 prevStartPos;

    // 이전 프레임 목표점 위치 (이동 감지용)
    private Vector3 prevEndPos;

    // Awake: InputAction 생성 및 활성화, 그리드 초기 빌드
    // BuildGrid를 Awake에서 실행하여 PlayerAI.Start()보다 반드시 먼저 완료되도록 보장
    private void Awake()
    {
        leftClickAction = new InputAction("LeftClick", InputActionType.Button, "<Mouse>/leftButton");
        mousePosAction  = new InputAction("MousePosition", InputActionType.Value, "<Mouse>/position");
        leftClickAction.Enable();
        mousePosAction.Enable();
        BuildGrid();
    }

    // OnDestroy: InputAction 비활성화 및 해제
    private void OnDestroy()
    {
        leftClickAction.Disable();
        leftClickAction.Dispose();
        mousePosAction.Disable();
        mousePosAction.Dispose();
    }

    // Update: 시작/목표점 스냅, 마우스 입력 처리
    private void Update()
    {
        if (IsSearching) return;
        UpdateStartEndSnap();
        HandleMouseInput();
    }

    // 그리드 생성 및 셀 오브젝트 초기화
    public void BuildGrid()
    {
        if (cellParent != null)
            Destroy(cellParent.gameObject);

        cellLookup.Clear();
        cellParent = new GameObject("GridCells").transform;
        cellParent.SetParent(transform);

        Grid = new GridNode[rows, cols];

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                Vector3 worldPos = GetWorldPosition(r, c);
                GridNode node = new GridNode(r, c, worldPos);
                Grid[r, c] = node;

                GameObject cell = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cell.name = string.Format("Cell_{0}_{1}", r, c);
                cell.transform.SetParent(cellParent);
                cell.transform.position = worldPos;
                cell.transform.localScale = new Vector3(cellSize - cellGap, cellHeight, cellSize - cellGap);

                node.cellObject = cell;
                cellLookup[cell.GetInstanceID()] = node;
                SetCellColor(node, colorNormal);
            }
        }

        InitStartEndNodes();

        // 새 그리드 생성은 맵 변경으로 간주 (대시보드 초기화 대상)
        MapVersion++;
    }

    // 시작/목표 노드 초기 설정
    private void InitStartEndNodes()
    {
        if (startTransform != null)
        {
            StartNode = GetNodeFromWorldPosition(startTransform.position);
            prevStartPos = startTransform.position;
        }
        if (StartNode == null) StartNode = Grid[rows / 2, 2];

        if (endTransform != null)
        {
            EndNode = GetNodeFromWorldPosition(endTransform.position);
            prevEndPos = endTransform.position;
        }
        if (EndNode == null) EndNode = Grid[rows / 2, cols - 3];

        RefreshAllCells();
    }

    // 시작/목표 Transform 이동 감지 및 그리드 스냅 처리
    private void UpdateStartEndSnap()
    {
        if (startTransform != null && startTransform.position != prevStartPos)
        {
            prevStartPos = startTransform.position;
            GridNode newNode = GetNodeFromWorldPosition(startTransform.position);
            if (newNode != null && newNode != EndNode)
            {
                StartNode = newNode;
                StartNode.isWall = false;
                startTransform.position = new Vector3(StartNode.worldPosition.x, startTransform.position.y, StartNode.worldPosition.z);
                MarkMapChanged();
                RefreshAllCells();
            }
        }

        if (endTransform != null && endTransform.position != prevEndPos)
        {
            prevEndPos = endTransform.position;
            GridNode newNode = GetNodeFromWorldPosition(endTransform.position);
            if (newNode != null && newNode != StartNode)
            {
                EndNode = newNode;
                EndNode.isWall = false;
                endTransform.position = new Vector3(EndNode.worldPosition.x, endTransform.position.y, EndNode.worldPosition.z);
                MarkMapChanged();
                RefreshAllCells();
            }
        }
    }

    // 마우스 클릭/드래그로 장벽 토글
    private void HandleMouseInput()
    {
        if (leftClickAction.WasPressedThisFrame())
        {
            GridNode node = GetNodeFromMouseRaycast();
            if (node != null && node != StartNode && node != EndNode)
            {
                isMouseDragging = true;
                dragSetWall = !node.isWall;
                node.isWall = dragSetWall;
                RefreshCell(node);
                MarkMapChanged();
            }
        }
        else if (leftClickAction.IsPressed() && isMouseDragging)
        {
            GridNode node = GetNodeFromMouseRaycast();
            if (node != null && node != StartNode && node != EndNode)
            {
                node.isWall = dragSetWall;
                RefreshCell(node);
                MarkMapChanged();
            }
        }
        else if (leftClickAction.WasReleasedThisFrame())
        {
            isMouseDragging = false;
        }
    }

    // 마우스 위치 레이캐스트로 그리드 노드 반환
    private GridNode GetNodeFromMouseRaycast()
    {
        if (Camera.main == null) return null;
        Vector2 mousePos = mousePosAction.ReadValue<Vector2>();
        Ray ray = Camera.main.ScreenPointToRay(mousePos);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            int id = hit.collider.gameObject.GetInstanceID();
            if (cellLookup.TryGetValue(id, out GridNode node))
                return node;
        }
        return null;
    }

    // 월드 위치를 가장 가까운 그리드 노드로 변환
    public GridNode GetNodeFromWorldPosition(Vector3 worldPos)
    {
        Vector3 local = worldPos - GridOrigin;
        int c = Mathf.RoundToInt(local.x / cellSize);
        int r = Mathf.RoundToInt(local.z / cellSize);
        if (r >= 0 && r < rows && c >= 0 && c < cols)
            return Grid[r, c];
        return null;
    }

    // 행, 열 인덱스로 월드 위치 반환
    public Vector3 GetWorldPosition(int row, int col)
    {
        return GridOrigin + new Vector3(col * cellSize, 0f, row * cellSize);
    }

    // 노드의 상하좌우 이웃 노드 반환 (장벽 및 범위 밖 제외)
    public List<GridNode> GetNeighbors(GridNode node)
    {
        List<GridNode> result = new List<GridNode>();
        int[] dr = { 0, 1, 0, -1 };
        int[] dc = { 1, 0, -1, 0 };

        for (int i = 0; i < 4; i++)
        {
            int nr = node.row + dr[i];
            int nc = node.col + dc[i];
            if (nr >= 0 && nr < rows && nc >= 0 && nc < cols && !Grid[nr, nc].isWall)
                result.Add(Grid[nr, nc]);
        }
        return result;
    }

    //FIXME: 아래 함수는 더이상 사용하지 않습니다. 삭제를 권장합니다.
    //FIXME: 휴리스틱 계산은 PathFinderAI.Heuristic에서 처리합니다.
    // 맨해튼 거리 휴리스틱 반환
    public float GetHeuristic(GridNode a, GridNode b)
    {
        return Mathf.Abs(a.row - b.row) + Mathf.Abs(a.col - b.col);
    }

    // 탐색 상태 초기화 (장벽 유지)
    public void ResetSearchState()
    {
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
                Grid[r, c].ResetSearchState();
        RefreshAllCells();
    }

    // 전체 그리드 초기화 (장벽 포함)
    public void ClearAll()
    {
        if (Grid == null) return;
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
                Grid[r, c].ResetAll();
        MarkMapChanged();
        RefreshAllCells();
    }

    // 전체 셀 색상 갱신
    public void RefreshAllCells()
    {
        if (Grid == null) return;
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
                RefreshCell(Grid[r, c]);
    }

    // 단일 셀 색상 갱신
    public void RefreshCell(GridNode node)
    {
        if (node == null || node.cellObject == null) return;

        Color color;
        if (node == StartNode)        color = colorStart;
        else if (node == EndNode)     color = colorEnd;
        else if (node.isWall)         color = colorWall;
        else if (node.isPath)         color = colorPath;
        else if (node.isVisited)      color = colorVisited;
        else if (node.isOpen)         color = colorOpen;
        else                          color = colorNormal;

        SetCellColor(node, color);
    }

    // 셀 오브젝트의 머티리얼 색상 설정 (MaterialPropertyBlock으로 인스턴스 생성 방지)
    // URP(_BaseColor)와 Built-in(_Color) 모두 대응하기 위해 두 프로퍼티에 동시 설정
    private void SetCellColor(GridNode node, Color color)
    {
        if (node.cellObject == null) return;
        Renderer rend = node.cellObject.GetComponent<Renderer>();
        if (rend == null) return;

        mpb ??= new MaterialPropertyBlock();
        mpb.SetColor(colorID, color);
        mpb.SetColor(baseColorID, color);
        rend.SetPropertyBlock(mpb);
    }

    // 그리드 열 수 반환
    public int GetCols() => cols;

    // 그리드 행 수 반환
    public int GetRows() => rows;

    // startTransform, endTransform의 현재 위치를 기준으로 시작/목표 노드를 강제 재동기화
    // 맵 생성 직전 등에서 호출하여 실제 시작점과 목표점 노드를 정확히 반영한다
    // 반영된 시작/목표 노드는 장벽이 아니도록 보장한다
    public void SyncStartEnd()
    {
        if (Grid == null) return;

        if (startTransform != null)
        {
            GridNode n = GetNodeFromWorldPosition(startTransform.position);
            if (n != null)
            {
                StartNode = n;
                prevStartPos = startTransform.position;
            }
        }

        if (endTransform != null)
        {
            GridNode n = GetNodeFromWorldPosition(endTransform.position);
            if (n != null && n != StartNode)
            {
                EndNode = n;
                prevEndPos = endTransform.position;
            }
        }

        // 시작/목표 노드는 장벽이 될 수 없음
        if (StartNode != null) StartNode.isWall = false;
        if (EndNode != null)   EndNode.isWall = false;
    }

    // 시작점과 목표점을 벽이 아닌 노드 중 랜덤으로 변경 (랜덤 위치 버튼)
    public void RandomizeStartEnd()
    {
        if (Grid == null)
        {
            Debug.LogWarning("그리드가 초기화되지 않았습니다.");
            return;
        }

        // 벽이 아닌 노드 후보 수집
        List<GridNode> candidates = new List<GridNode>();
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
                if (!Grid[r, c].isWall)
                    candidates.Add(Grid[r, c]);

        if (candidates.Count < 2)
        {
            Debug.LogWarning("벽이 아닌 노드가 2개 미만입니다. 위치를 변경할 수 없습니다.");
            return;
        }

        // 랜덤 시작 노드 선택
        int startIdx    = Random.Range(0, candidates.Count);
        GridNode newStart = candidates[startIdx];
        candidates.RemoveAt(startIdx);

        // 시작과 다른 랜덤 목표 노드 선택
        int endIdx    = Random.Range(0, candidates.Count);
        GridNode newEnd = candidates[endIdx];

        StartNode = newStart;
        EndNode   = newEnd;

        // 연결된 Transform 위치 갱신 (UpdateStartEndSnap 재진입 방지를 위해 prev도 갱신)
        if (startTransform != null)
        {
            startTransform.position = new Vector3(newStart.worldPosition.x, startTransform.position.y, newStart.worldPosition.z);
            prevStartPos = startTransform.position;
        }
        if (endTransform != null)
        {
            endTransform.position = new Vector3(newEnd.worldPosition.x, endTransform.position.y, newEnd.worldPosition.z);
            prevEndPos = endTransform.position;
        }

        MarkMapChanged();
        RefreshAllCells();
        Debug.Log(string.Format("시작/목표 위치 변경. 시작: ({0},{1}), 목표: ({2},{3})", newStart.row, newStart.col, newEnd.row, newEnd.col));
    }

    // Scene 뷰에서 그리드 경계를 항상 표시 (오브젝트 미선택 시에도 표시)
    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        // 그리드 전체 경계 박스 (gridCenter 기준)
        Vector3 boundsSize = new Vector3(cols * cellSize, 0.2f, rows * cellSize);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(gridCenter, boundsSize);

        // 그리드 좌측 하단 기준점 표시
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(GridOrigin, cellSize * 0.15f);
    }

    // Scene 뷰에서 오브젝트 선택 시 셀 상세 표시
    // 플레이 중에는 런타임 상태(장벽, 시작/목표, 탐색 상태)를, 에디터에서는 그리드 구조만 표시
    private void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;

        Vector3 cellScale = new Vector3(cellSize * 0.9f, cellHeight, cellSize * 0.9f);

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                Vector3 worldPos = GetWorldPosition(r, c);

                if (Application.isPlaying && Grid != null)
                {
                    GridNode node = Grid[r, c];

                    if (node == StartNode)
                    {
                        // 시작 노드: 초록
                        Gizmos.color = new Color(0.1f, 0.85f, 0.5f, 0.9f);
                        Gizmos.DrawCube(worldPos, cellScale);
                    }
                    else if (node == EndNode)
                    {
                        // 목표 노드: 주황
                        Gizmos.color = new Color(0.9f, 0.35f, 0.1f, 0.9f);
                        Gizmos.DrawCube(worldPos, cellScale);
                    }
                    else if (node.isWall)
                    {
                        // 장벽 셀: 빨간 반투명 박스
                        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.7f);
                        Gizmos.DrawCube(worldPos, cellScale);
                    }
                    else if (node.isPath)
                    {
                        // 경로 셀: 파랑
                        Gizmos.color = new Color(0.2f, 0.55f, 0.9f, 0.8f);
                        Gizmos.DrawCube(worldPos, cellScale);
                    }
                    else if (node.isVisited)
                    {
                        // 방문 완료 셀: 하늘색 와이어
                        Gizmos.color = new Color(0.6f, 0.8f, 1f, 0.4f);
                        Gizmos.DrawWireCube(worldPos, cellScale);
                    }
                    else
                    {
                        // 이동 가능 셀: 흰색 와이어
                        Gizmos.color = new Color(1f, 1f, 1f, 0.12f);
                        Gizmos.DrawWireCube(worldPos, cellScale);
                    }
                }
                else
                {
                    // 에디터 모드: 와이어 박스로 그리드 구조만 표시
                    Gizmos.color = new Color(1f, 1f, 1f, 0.1f);
                    Gizmos.DrawWireCube(worldPos, cellScale);
                }
            }
        }
    }
}
