using System;
using System.Collections.Generic;

//TODO: 경로 탐색 알고리즘의 열린 목록(open list)을 위한 제네릭 이진 최소 힙입니다.
//TODO: 구현 기능: Push, Pop, Count. 비교 함수를 주입하여 A*, Dijkstra, GBF, D* 등에 재사용
//TODO: 매 반복 List.Sort를 대체하여 우선순위 큐 연산을 O(log n)으로 수행합니다.

// 비교 함수 기반 이진 최소 힙 (MonoBehaviour 아님, 순수 자료구조)
public class MinHeap<T>
{
    // 힙 요소를 저장하는 내부 배열
    private readonly List<T> items;

    // 두 요소의 우선순위를 비교하는 함수 (음수면 첫 인수가 우선)
    private readonly Comparison<T> compare;

    // 현재 힙에 담긴 요소 수
    public int Count => items.Count;

    // 생성자: 우선순위 비교 함수를 받아 초기화
    public MinHeap(Comparison<T> comparison)
    {
        items   = new List<T>();
        compare = comparison;
    }

    // 요소를 힙에 삽입하고 상향 정렬
    public void Push(T item)
    {
        items.Add(item);
        SiftUp(items.Count - 1);
    }

    // 최소 우선순위 요소를 제거하고 반환
    public T Pop()
    {
        T root   = items[0];
        int last = items.Count - 1;

        items[0] = items[last];
        items.RemoveAt(last);

        if (items.Count > 0)
            SiftDown(0);

        return root;
    }

    // 지정 인덱스 요소를 부모와 비교하며 위로 이동
    private void SiftUp(int i)
    {
        while (i > 0)
        {
            int parent = (i - 1) / 2;
            if (compare(items[i], items[parent]) >= 0) break;

            Swap(i, parent);
            i = parent;
        }
    }

    // 지정 인덱스 요소를 자식과 비교하며 아래로 이동
    private void SiftDown(int i)
    {
        int n = items.Count;

        while (true)
        {
            int left     = 2 * i + 1;
            int right     = 2 * i + 2;
            int smallest = i;

            if (left < n && compare(items[left], items[smallest]) < 0)
                smallest = left;
            if (right < n && compare(items[right], items[smallest]) < 0)
                smallest = right;

            if (smallest == i) break;

            Swap(i, smallest);
            i = smallest;
        }
    }

    // 두 인덱스 요소 교환
    private void Swap(int a, int b)
    {
        T temp   = items[a];
        items[a] = items[b];
        items[b] = temp;
    }
}
