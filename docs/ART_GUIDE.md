# Руководство по графике (что генерировать и как назвать файлы)

Сейчас в игре вместо картинок цветные квадраты-заглушки. Здесь всё, что
нужно сгенерировать нейросетью, чтобы я подключил графику в игру.

## Общие правила

- **Один стиль на всё.** Сначала сгенерируйте героя. Для остальных
  картинок прикладывайте героя как образец стиля («same art style as the
  attached image») — так игра не будет выглядеть набором разных рисунков.
- **Прозрачный фон.** Персонажи и иконки должны быть PNG с прозрачностью.
  Многие нейросети честную прозрачность не дают — тогда просите
  «on a solid flat pure white background», а фон потом убирайте
  (remove.bg, Photoshop, Photopea и т.п.).
- **Персонажи и двери — квадратные (1:1), 512×512.** Фигура занимает
  примерно 85% кадра, ноги/низ двери касаются нижнего края картинки
  (они «стоят на полу»), по краям небольшие поля. Квадрат нужен, чтобы
  не пришлось переделывать расстановку на экране: размеры в игре сейчас
  тоже квадратные.
- **Фоны — 16:9, 2560×1440**, без текста и без важных деталей у самых краёв:
  на окнах другой формы края могут немного обрезаться.
- Кладите файлы в `Assets/_Project/Art/` с точно такими именами, как ниже —
  тогда подключение не потребует гадать, что есть что.

## Префикс стиля (вставляйте в начало каждого промпта)

```
cute friendly cartoon 2D game art for children, flat colors with soft
outlines, simple shapes, bright cheerful palette, clean vector-like look,
no text, no watermark
```

## Список файлов и промпты

### Персонажи — `Art/Characters/`

| Файл | Промпт (после префикса стиля) |
|---|---|
| `hero.png` | `a small brave young hero character, front view, standing pose, friendly face, simple cape, full body, centered, on a solid flat pure white background` |
| `golem.png` | `a chunky friendly stone golem monster (ogre), front view, standing, big round fists, cute not scary, full body, centered, on a solid flat pure white background` |

По желанию: `golem_2.png`, `golem_3.png` — тот же голем в другом цвете
(фиолетовый, зелёный, оранжевый), чтобы башни не были однообразными.

### Двери — `Art/Doors/`

| Файл | Промпт (после префикса стиля) |
|---|---|
| `door_closed.png` | `a wooden arched dungeon door, closed, front view, metal hinges, centered, bottom of the door touches the bottom edge, on a solid flat pure white background` |
| `door_open.png` | `the same wooden arched door, wide open, warm light glowing from inside, front view, same style as the attached image, on a solid flat pure white background` |
| `door_locked.png` | `the same wooden arched door, closed with a big padlock and chains, front view, same style as the attached image, on a solid flat pure white background` |

### Фоны уровней — `Art/Backgrounds/`

Для всех: `wide 16:9 landscape, empty open ground in the lower third, sky
above, no characters, no text, calm composition, soft gradients`

| Файл | Где используется | Описание в промпте |
|---|---|---|
| `bg_day.png` | уровни 1–2 | `bright sunny day, blue sky, a few white clouds, green grass hills` |
| `bg_sunset.png` | уровни 3–5 | `warm sunset, orange and pink sky, green-yellow hills` |
| `bg_dusk.png` | уровни 6–8 | `purple evening sky, first stars, dark blue-green hills` |
| `bg_night.png` | уровни 9–10 | `starry night, big moon, deep blue sky, dark teal hills, magical mood` |

### Меню и кнопки — `Art/UI/`

| Файл | Размер | Промпт (после префикса стиля) |
|---|---|---|
| `menu_background.png` | 2560×1440 | `a cheerful fantasy landscape with a tall friendly stone tower and a wooden door in the distance, empty center area for buttons, no text` |
| `level_button_frame.png` | 256×256 | `a rounded square game button frame, wooden or stone with a light inner area, empty inside, on a solid flat pure white background` |
| `icon_lock.png` | 256×256 | `a cute golden padlock icon, front view, on a solid flat pure white background` |
| `icon_check.png` | 256×256 | `a bold green check mark icon in a round badge, on a solid flat pure white background` |

## Когда всё готово

Напишите мне, что картинки лежат в `Assets/_Project/Art/`. Я:

1. настрою импорт (спрайты, размер в игре, чтобы 512 px = 1 игровая единица);
2. подключу их вместо квадратов (герой, големы, двери, фоны, кнопки уровней, меню);
3. подгоню размеры и подписи над головами, чтобы ничего не наехало.

Можно присылать частями — например, сначала героя, голема и дверь, фоны
позже.
