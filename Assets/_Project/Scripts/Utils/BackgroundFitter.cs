using UnityEngine;

namespace MathGame.Utils
{
    // Растягивает фон так, чтобы он всегда закрывал весь экран камеры, при
    // любой форме окна и при любом размере обзора (у башен и дверей
    // orthographicSize может отличаться), и при этом не искажался.
    //
    // Режим "cover" (как background-size: cover в вебе): картинку
    // увеличиваем ровно настолько, чтобы закрыть экран и по ширине, и по
    // высоте; то, что вылезает за край, просто обрезается. Низ картинки
    // всегда совпадает с нижним краем экрана — ведь именно внизу картинки
    // земля, на которой стоят герой и гоблины, и её нельзя обрезать;
    // на очень широком окне обрежется только небо сверху.
    //
    // Фон — ребёнок камеры (едет вместе с ней), см. SceneBuilderMenu.
    [RequireComponent(typeof(SpriteRenderer))]
    public class BackgroundFitter : MonoBehaviour
    {
        // Высота "земли" в мире (на ней стоят ноги героя и дверей): у мини-игр
        // это Y = 0, камера смотрит на неё сверху (cameraY в контроллерах).
        [SerializeField] private float groundWorldY = 0f;

        private SpriteRenderer _renderer;
        private Camera _camera;

        // Доля высоты картинки (снизу), где на ней нарисована линия пола. Если
        // больше нуля, картинка ставится так, чтобы эта линия пришлась ровно
        // на землю мини-игры, а не просто на низ экрана: тогда у стены двери
        // стоят у её основания при любой форме окна. 0 — прежнее поведение.
        public float FloorLine { get; set; }

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _camera = GetComponentInParent<Camera>();
        }

        // LateUpdate: мини-игра меняет orthographicSize камеры в Begin(),
        // уже после создания сцены, — пересчитываем каждый кадр, это дёшево.
        private void LateUpdate()
        {
            if (_camera == null || !_camera.orthographic || _renderer.sprite == null) return;

            float viewHeight = _camera.orthographicSize * 2f;
            float viewWidth = viewHeight * _camera.aspect;

            // bounds — размер спрайта в мировых единицах при масштабе 1
            // (зависит от Pixels Per Unit), центр отсчитывается от pivot.
            Bounds bounds = _renderer.sprite.bounds;
            if (bounds.size.x <= 0f || bounds.size.y <= 0f) return;

            float scale = Mathf.Max(viewWidth / bounds.size.x, viewHeight / bounds.size.y);

            // Сдвиг низа картинки относительно низа экрана (<= 0: картинка
            // уходит ниже экрана, чтобы линия пола встала на землю).
            float bottomOffset = 0f;
            if (FloorLine > 0f)
            {
                // На какой высоте над низом экрана стоит земля.
                float groundY = _camera.orthographicSize - (_camera.transform.position.y - groundWorldY);
                groundY = Mathf.Clamp(groundY, 0f, viewHeight * 0.6f);

                // Картинка должна дотягиваться и до верха экрана.
                float neededHeight = (viewHeight - groundY) / (1f - FloorLine);
                scale = Mathf.Max(scale, neededHeight / bounds.size.y);
                bottomOffset = groundY - FloorLine * bounds.size.y * scale;
            }
            transform.localScale = new Vector3(scale, scale, 1f);

            float x = -bounds.center.x * scale;
            float y = -_camera.orthographicSize + bottomOffset - bounds.min.y * scale;
            transform.localPosition = new Vector3(x, y, transform.localPosition.z);
        }
    }
}
