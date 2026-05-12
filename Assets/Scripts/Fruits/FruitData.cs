using UnityEngine;

namespace Suika
{
    [CreateAssetMenu(fileName = "FruitData", menuName = "Suika/FruitData")]
    public class FruitData : ScriptableObject
    {
        [Header("기본 정보")]
        public int level;           // 1 ~ 11
        public string fruitName;
        public Sprite sprite;

        [Header("물리")]
        public float radius;        // 체리(Lv1) = 0.5f 기준 상대 비율 적용

        [Header("점수")]
        public int mergeScore;      // 머지 시 획득 점수 (Lv11 = 0)

        [Header("드롭 가능 여부")]
        public bool droppable;      // Lv1~5 true, Lv6~11 false
    }
}
