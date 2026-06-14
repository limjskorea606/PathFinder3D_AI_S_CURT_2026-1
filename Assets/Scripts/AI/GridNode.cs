using UnityEngine;

//TODO: 경로 탐색 알고리즘에서 사용하는 그리드 노드 데이터 클래스입니다.
//TODO: 구현 기능: 좌표 저장, 탐색 상태 관리, A* 비용 계산, 셀 오브젝트 참조

// 그리드의 각 셀 데이터를 저장하는 노드 클래스
public class GridNode
{
    // 그리드 행 인덱스
    public int row;

    // 그리드 열 인덱스
    public int col;

    // 장벽 여부
    public bool isWall;

    // 방문 완료 여부 (닫힌 목록)
    public bool isVisited;

    // 최단 경로 포함 여부
    public bool isPath;

    // 열린 목록 포함 여부
    public bool isOpen;

    // 월드 공간 위치
    public Vector3 worldPosition;

    // 경로 역추적용 부모 노드
    public GridNode parent;

    // G 비용: 시작점에서 현재까지 실제 이동 거리
    public float gCost;

    // H 비용: 현재에서 목표까지 휴리스틱 거리
    public float hCost;

    // F 비용 속성 반환 (G + H)
    public float FCost => gCost + hCost;

    // D*Lite 전용: 단방향 선견 비용
    public float rhs;

    // 시각화 셀 오브젝트 참조
    public GameObject cellObject;

    // 생성자: 좌표와 월드 위치 초기화
    public GridNode(int row, int col, Vector3 worldPos)
    {
        this.row = row;
        this.col = col;
        worldPosition = worldPos;
        ResetAll();
    }

    // 모든 상태 초기화 (장벽 포함)
    public void ResetAll()
    {
        isWall = false;
        ResetSearchState();
    }

    // 탐색 상태만 초기화 (장벽 유지)
    public void ResetSearchState()
    {
        isVisited = false;
        isPath = false;
        isOpen = false;
        parent = null;
        gCost = float.MaxValue;
        hCost = 0f;
        rhs   = float.MaxValue;
    }
}
