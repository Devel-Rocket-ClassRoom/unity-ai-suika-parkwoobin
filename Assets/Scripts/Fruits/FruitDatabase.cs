using UnityEngine;

namespace Suika
{
    /// <summary>
    /// 전체 과일 배열을 보관하는 ScriptableObject.
    /// Assets/ScriptableObjects/FruitDatabase.asset 으로 생성 후
    /// Inspector 에서 FruitData[] 를 Lv1~Lv11 순서로 채운다.
    /// </summary>
    [CreateAssetMenu(fileName = "FruitDatabase", menuName = "Suika/FruitDatabase")]
    public class FruitDatabase : ScriptableObject
    {
        [Tooltip("Lv1(Cherry) ~ Lv11(Watermelon) 순서로 등록")]
        public FruitData[] fruits;

        /// <summary>레벨(1-based)로 FruitData 반환. 범위 초과 시 null.</summary>
        public FruitData GetByLevel(int level)
        {
            int idx = level - 1;
            if (idx < 0 || idx >= fruits.Length)
                return null;
            return fruits[idx];
        }
    }
}
