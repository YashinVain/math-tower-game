using UnityEngine;

namespace MathGame.Utils
{
    // Висит на камере в сцене Gameplay. Любая мини-игра с героем в мировых
    // координатах при старте выставляет Target — камера плавно следует по X,
    // создавая ощущение одной непрерывной сцены при переходе между башнями
    // внутри уровня, без резких склеек между "экранами". Unity сравнивает
    // уничтоженный объект с null как true, поэтому отдельно сбрасывать
    // Target при смене сцены не нужно.
    public class CameraFollowX : MonoBehaviour
    {
        public static Transform Target;

        [SerializeField] private float offsetX = 0f;
        [SerializeField] private float smoothTime = 0.35f;

        private float _velocity;

        private void LateUpdate()
        {
            if (Target == null) return;

            var position = transform.position;
            float desiredX = Target.position.x + offsetX;
            position.x = Mathf.SmoothDamp(position.x, desiredX, ref _velocity, smoothTime);
            transform.position = position;
        }
    }
}
