using System.Collections;
using UnityEngine;
using MathGame.Utils;

namespace MathGame.Minigames.Towers
{
    // Картинка башни. Башня не рисуется целиком (нейросеть плохо считает
    // этажи), а собирается из трёх одинаковых блоков:
    //   нижний этаж (с дверью) → средние этажи → верхний (с зубцами и флагом).
    // Для башни на 2 этажа это нижний + верхний, на 3 — нижний + средний +
    // верхний, на 4 — нижний + два средних + верхний. Блоки стоят друг на
    // друге с шагом floorHeight, поэтому этажи точно совпадают с
    // гоблинами: гоблин этажа i стоит на каменной площадке i-го блока.
    //
    // Башня рисуется ПОЗАДИ гоблинов (sortingOrder 0, у гоблинов 1), чтобы
    // гоблинов всегда было видно.
    public class TowerView : MonoBehaviour
    {
        [SerializeField] private Sprite baseSprite;
        [SerializeField] private Sprite midSprite;
        [SerializeField] private Sprite topSprite;

        // Расстояние между этажами. Размер самих блоков в мире задаётся при
        // импорте картинок (ArtSpecs.TowerFloorHeight) — числа должны
        // совпадать.
        [SerializeField] private float floorHeight = 2.35f;

        // На какой высоте от низа блока находится верх каменной площадки,
        // на которой стоит гоблин. Измерено по картинкам: у нижнего и
        // среднего блока площадка на 14.1% высоты (0.141 × 2.35 ≈ 0.33), у
        // верхнего — на 11.7% высоты уже увеличенной картинки (≈ 0.32).
        [SerializeField] private float baseStandHeight = 0.33f;
        [SerializeField] private float midStandHeight = 0.33f;
        [SerializeField] private float topStandHeight = 0.32f;

        // Сдвиг верхнего блока по X. Блоки башни нарисованы строго по центру,
        // поэтому 0 (у прежних нарисованных нейросетью картинок было 0.03;
        // поле переименовано, чтобы старое значение из префаба не мешало).
        [SerializeField] private float topShiftX = 0f;

        public float FloorHeight => floorHeight;

        // Высота (от основания башни), на которой стоят ноги гоблина этажа
        // floorIndex в башне из floorCount этажей.
        public float GetStandHeight(int floorIndex, int floorCount)
        {
            float standOnBlock;
            if (floorIndex == 0) standOnBlock = baseStandHeight;
            else if (floorIndex == floorCount - 1) standOnBlock = topStandHeight;
            else standOnBlock = midStandHeight;
            return floorIndex * floorHeight + standOnBlock;
        }

        // Собирает башню нужной высоты (старую убирает).
        public void Build(int floorCount)
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
                Destroy(transform.GetChild(i).gameObject);

            floorCount = Mathf.Max(2, floorCount);
            for (int i = 0; i < floorCount; i++)
            {
                Sprite sprite = i == 0 ? baseSprite : (i == floorCount - 1 ? topSprite : midSprite);
                if (sprite == null) continue;

                var floor = new GameObject("Floor_" + i);
                floor.transform.SetParent(transform, false);
                float x = (i == floorCount - 1) ? topShiftX : 0f;
                floor.transform.localPosition = new Vector3(x, i * floorHeight, 0f);

                var spriteRenderer = floor.AddComponent<SpriteRenderer>();
                spriteRenderer.sprite = sprite;
                spriteRenderer.sortingOrder = 0;
            }
        }

        // "Выскакивает" из земли: вырастает по высоте от основания башни
        // (у блоков pivot внизу, поэтому масштаб по Y идёт строго вверх).
        public void PlayAppear(float duration = 0.3f)
        {
            StopAllCoroutines();
            transform.localScale = new Vector3(1f, 0.01f, 1f);
            StartCoroutine(SimpleTween.ScaleTo(transform, Vector3.one, duration));
        }
    }
}
