# Сборка вертикального среза в редакторе Unity — пошагово

Весь код уже написан и лежит в `Assets/_Project/Scripts`. Но сцены,
префабы и связи между компонентами в инспекторе (какая ссылка куда
перетащена) Unity хранит не как текст, который можно надёжно сгенерировать
снаружи, — это нужно сделать руками в редакторе. Этот документ — точный
список шагов для этого.

Не обязательно проходить всё за один присест. Если что-то не совпадает
с тем, что видите на экране, или Unity показывает ошибку — сохраните,
опишите (лучше со скриншотом), и разберёмся.

## 0. Установка Unity

1. Скачать и установить **Unity Hub**: https://unity.com/download
2. В Hub: `Installs` → `Install Editor` → найти **Unity 6.3 LTS**
   (номер сборки начинается с `6000.3.`). Если в списке по умолчанию нет —
   вкладка `Archive` на unity.com/releases позволяет найти конкретную
   версию и открыть её напрямую через Hub.
3. При установке редактора отметить модуль **Microsoft Visual Studio
   Community** (если на компьютере ещё нет IDE для C#) и **Windows Build
   Support (IL2CPP)**. Шаблон 2D/3D выбирается позже, при создании
   проекта, — не на этом шаге.
4. Установка займёт время (несколько ГБ) — можно продолжать читать дальше.

### 0.1 Переключить интерфейс Unity на английский (сделать один раз, сразу)

Весь этот документ, вся официальная документация Unity, все видео-туториалы
и форумы используют английские названия меню. Если оставить интерфейс
русским, названия на экране не будут совпадать с тем, что написано здесь
(и в любом другом источнике) — придётся всё время гадать/переводить
обратно. Проще один раз переключить:

1. Откройте проект (см. пункт 1 ниже — можно вернуться к этому шагу сразу
   после открытия).
2. Верхнее меню (по-русски сейчас) → **Правка** → в самом низу списка —
   **Настройки** (или **Preferences**, если уже частично на английском).
3. В открывшемся окне слева список разделов — найдите **Языки** (обычно
   ближе к верху списка).
4. Справа появится выпадающий список, сейчас в нём выбрано **Русский** —
   откройте его и выберите **English**.
5. Закройте окно настроек. Может попросить перезапустить Unity — если да,
   закройте и откройте заново через `OpenInUnity.bat`.

Дальше по всему документу меню называются по-английски — после этого шага
они будут совпадать с тем, что на экране.

## 1. Открытие проекта

Проект живёт в `C:\UnityProjects\Game` — обычной локальной папке, **не**
внутри `OneDrive`. Это важно: изначально проект лежал в
`OneDrive\Desktop\Game`, и это привело к тому, что Unity Hub не находил
проект и/или не мог сохранить сцену — OneDrive пытается синхронизировать
десятки тысяч мелких служебных файлов Unity и блокирует их прямо в момент
записи. Если на этом компьютере включён "Known Folder Move" (сам Рабочий
стол/Документы редиректнуты в OneDrive — как в нашем случае), под новые
Unity-проекты нужна папка вне этого дерева, например `C:\UnityProjects\...`.

Папка `Game` уже содержит `Assets/_Project` с кодом, `ProjectSettings/`,
`Packages/`, `.git` и документацию — то есть это уже полностью готовый,
валидный Unity-проект, просто нужно его открыть, ничего создавать не
нужно:

- Самый надёжный способ — двойной клик по [`OpenInUnity.bat`](../OpenInUnity.bat)
  в корне проекта. Он запускает нужную версию редактора сразу на этой
  папке в обход Unity Hub (Hub у нас периодически глючил с обнаружением
  проектов).
- Либо через Unity Hub: `Projects` → стрелка рядом с `Open` → `Add project
  from disk` → выбрать `C:\UnityProjects\Game` (зайти внутрь папки, чтобы
  в окне были видны `Assets`, `Packages`, `ProjectSettings`, и только
  тогда нажать «Выбрать папку») → открыть из списка.

Первое открытие после переноса займёт несколько минут — Unity заново
импортирует всё и создаёт служебную папку `Library/`.

**Важно:** открывайте проект только ОДНИМ способом за раз и не запускайте
второй раз, если первое окно уже открыто (даже если кажется, что оно
зависло или потерялось за другими окнами — проверьте панель задач/Alt+Tab
сначала). Два запущенных редактора на одной и той же папке проекта
конфликтуют друг с другом за файлы и дают самые странные на вид ошибки.

## 2. TextMeshPro

Почти весь текст в проекте использует TextMeshPro (TMP) — более гибкий
инструмент текста, чем старый Unity `Text`. Ему нужен один набор файлов
("Essential Resources"), который не ставится вместе с Unity автоматически,
а импортируется по кнопке. Сделайте это сейчас, одним из двух способов:

**Способ А (через меню, надёжнее):**

1. В самом верху экрана — строка меню: `File`, `Edit`, `Assets`,
   `GameObject`, `Component`, `Window`, `Help`.
2. Нажмите **`Window`**.
3. В выпадающем списке найдите **`TextMeshPro`** (обычно ближе к низу
   списка) → наведите на него, появится ещё один список сбоку.
4. В нём нажмите **`Import TMP Essential Resources`**.
5. Появится небольшое окно **"Import Unity Package"** со списком файлов —
   внизу справа нажмите синюю кнопку **`Import`**.
6. Подождите, пока Unity импортирует (несколько секунд, в правом нижнем
   углу окна Unity будет крутиться индикатор прогресса).

**Способ Б (само выскочит):** если способа А не находите — просто
переходите к пункту 3 этой инструкции и создавайте первый `TextMeshPro`-
объект (`GameObject → 3D Object → Text - TextMeshPro`). При самой первой
попытке Unity сама покажет окно **"TMP Importer"** с кнопкой **`Import TMP
Essentials`** — нажмите её, подождите импорт, и дальше можно продолжать
как обычно.

Оба способа делают одно и то же, нужно сделать только один раз за всю
жизнь проекта.

## 3. Префабы мини-игры «Башни»

### 3.1 Префаб голема

1. `GameObject → 2D Object → Sprites → Square` — переименовать в `Golem`.
   (У Square сразу есть `SpriteRenderer` с готовым спрайтом-заглушкой —
   реальный арт голема подставим позже.)
2. Добавить компонент **Box Collider 2D** (`Add Component` → найти) — без
   коллайдера клик по голему работать не будет: `GolemView.OnMouseDown`
   реагирует именно на попадание в коллайдер под курсором.
3. Добавить компонент **Golem View** (это наш скрипт `GolemView.cs`).
4. Внутри `Golem` создать дочерний `GameObject → 3D Object → Text -
   TextMeshPro` (**не** "UI → Text - TextMeshPro" — это другой компонент,
   для мировых 2D-объектов нужен именно 3D-вариант), назвать
   `ExpressionLabel`, расположить над голем (например, `Position Y = 1`).
5. На `Golem` в инспекторе `Golem View` → поле `Expression Label` →
   перетащить туда `ExpressionLabel`.
6. Перетащить `Golem` из Hierarchy в `Assets/_Project/Prefabs/Minigames/Towers/`
   — это создаст префаб. После этого можно удалить `Golem` из сцены
   (сама сцена, где вы это делаете, — любая временная, например только что
   созданная `SampleScene`, в финальные сцены проекта этот объект не
   входит).

### 3.2 Префаб героя (используется прямо внутри префаба TowerMinigame, отдельным ассетом не выносим)

Собирается сразу как часть шага 3.3.

### 3.3 Префаб TowerMinigame (корневой контроллер)

1. `GameObject → Create Empty` → переименовать в `TowerMinigame`.
2. Добавить компонент **Tower Minigame Controller**.
3. Внутри `TowerMinigame` создать `GameObject → 2D Object → Sprites →
   Square` → переименовать в `Hero`, поставить примерно в `Position (-2, 0,
   0)` относительно родителя.
4. На `Hero` добавить компонент **Hero View**. (Коллайдер герою не нужен —
   его не кликают.)
5. Внутри `Hero` создать `3D Object → Text - TextMeshPro` → назвать
   `PowerLabel`, расположить над героем (`Position Y = 1`).
6. Внутри `TowerMinigame` создать `GameObject → Create Empty` → назвать
   `GolemSlots`, позиция `(0, 0, 0)` — в это место `TowerMinigameController`
   будет добавлять появляющихся големов.
7. На `TowerMinigame` в инспекторе `Tower Minigame Controller` заполнить:
   - `Hero` → перетащить дочерний `Hero`
   - `Golem Prefab` → перетащить префаб `Golem` из шага 3.1 (из окна
     Project, не из Hierarchy)
   - `Golem Slot Parent` → перетащить дочерний `GolemSlots`
   - `Hero Power Label` → перетащить `PowerLabel` (тот, что внутри `Hero`)
   - Остальные поля (`Golem Spacing`, `Tower Spacing`, `Approach Offset`,
     `Move Duration`) можно оставить как есть — это баланс расстояний и
     скорости анимации, потом легко покрутить.
8. Перетащить `TowerMinigame` в `Assets/_Project/Prefabs/Minigames/Towers/`.
   Удалить из сцены.

## 4. Префабы мини-игры «Двери»

### 4.1 Префаб двери

Аналогично голему:

1. `2D Object → Sprites → Square` → `Door`.
2. Добавить **Box Collider 2D** (обязательно, иначе клик не сработает).
3. Добавить компонент **Door View**.
4. Дочерний `3D Object → Text - TextMeshPro` → `ExpressionLabel`, над
   дверью.
5. В `Door View`: `Expression Label` → `ExpressionLabel`; `Door Renderer` →
   перетащить сам `Door` (его собственный `SpriteRenderer` — компонент на
   том же объекте, это нормально).
6. Перетащить `Door` в `Assets/_Project/Prefabs/Minigames/Doors/`. Удалить
   из сцены.

### 4.2 Префаб DoorMinigame (корневой контроллер)

1. `GameObject → Create Empty` → `DoorMinigame`.
2. Добавить компонент **Door Minigame Controller**.
3. (Необязательно, но рекомендую для целостной картинки) внутри создать
   `2D Object → Sprites → Square` → `Hero` — просто как декорация, без
   скриптов, стоит на месте и не двигается.
4. Дочерний `3D Object → Text - TextMeshPro` → `HeroTargetLabel`, над
   героем.
5. `GameObject → Create Empty` → `Doors`, позиция `(2, 0, 0)` — родитель
   для дверей, чуть впереди героя.
6. В `Door Minigame Controller`:
   - `Hero Target Label` → `HeroTargetLabel`
   - `Door Prefab` → префаб `Door` из шага 4.1
   - `Doors Parent` → дочерний `Doors`
   - `Door Spacing` — можно оставить по умолчанию.
7. Перетащить `DoorMinigame` в `Assets/_Project/Prefabs/Minigames/Doors/`.
   Удалить из сцены.

## 5. Тестовые данные уровней

Теперь, когда префабы существуют, можно создать данные уровней и
привязать их друг к другу.

1. В строке меню Unity: `MathGame → Bootstrap Sample Level Data`. Это
   наш собственный инструмент (`Assets/_Project/Editor/SampleDataMenu.cs`)
   — создаст `LevelCatalog`, `Level_01_Towers`, `Level_02_Doors`,
   `TowerMinigame_01`, `DoorMinigame_01` и `Theme_Default` в
   `Assets/_Project/ScriptableObjects/` и сразу выделит `LevelCatalog` в
   Project-окне.
2. Открыть `ScriptableObjects/Minigames/TowerMinigame_01` → поле
   `Controller Prefab` → перетащить префаб `TowerMinigame` (из шага 3.3).
3. Открыть `ScriptableObjects/Minigames/DoorMinigame_01` → поле
   `Controller Prefab` → перетащить префаб `DoorMinigame` (из шага 4.2).
4. (Необязательно сейчас) Открыть `Level_01_Towers` / `Level_02_Doors` и
   на глаз проверить, что `Sequence` не пустой, а `Level Id` заполнен —
   инструмент уже должен был всё это поставить.

## 6. Сцена Boot

1. `File → New Scene` → шаблон `Basic (Built-in)` или `2D` (не критично,
   эта сцена ничего не показывает). Сохранить как
   `Assets/_Project/Scenes/Boot.unity`.
2. Можно удалить `Main Camera` и `Directional Light` по умолчанию — эта
   сцена мгновенно переключается на `Menu`, ничего не рендерит.
3. `GameObject → Create Empty` → назвать `GameServices`.
4. Добавить компоненты **Game Services** и **Boot Loader**.
5. В `Game Services` → поле `Level Catalog` → перетащить ассет
   `LevelCatalog` (из `ScriptableObjects/`).
6. Сохранить сцену (`Ctrl+S`).

## 7. Сцена Menu

Самая объёмная сцена по количеству UI-элементов. Создать
`Assets/_Project/Scenes/Menu.unity` (`File → New Scene` → сохранить).

### 7.1 Канвас и общая структура

1. `GameObject → UI → Canvas` — Unity сам добавит `EventSystem` в сцену,
   если его ещё нет (без него ни одна кнопка не будет реагировать на
   клики — если вдруг видите, что клики по UI не работают, первым делом
   проверьте, есть ли `EventSystem` в Hierarchy).
2. На `Canvas` → `Canvas Scaler` → `UI Scale Mode = Scale With Screen
   Size`, `Reference Resolution = 1920 x 1080` — чтобы UI масштабировался
   под разные размеры окна одинаково.
3. Внутри `Canvas` создать три дочерних пустых объекта с `Rect Transform`,
   растянутым на весь экран (`Anchor Presets → Stretch/Stretch`, все
   отступы 0): `MainPanel`, `LevelSelectPanel`, `SettingsPanel`.

### 7.2 MainPanel

Внутри `MainPanel` три кнопки: `GameObject → UI → Button - TextMeshPro`,
текст на них — «Играть», «Настройки», «Выход». Назвать объекты
`PlayButton`, `SettingsButton`, `QuitButton`.

### 7.3 LevelSelectPanel

1. Кнопка «Назад» → `BackButton`.
2. `GameObject → Create Empty` внутри панели → `ButtonsContainer`.
   Добавить на него компонент **Grid Layout Group** (`Add Component` →
   найти) — тогда кнопки уровней, которые скрипт создаёт в цикле, сами
   выстроятся сеткой без ручной расстановки координат. Настройте `Cell
   Size` на глаз (например, 160×160) — это чисто визуальная настройка,
   можно менять в любой момент.
3. На объект `LevelSelectPanel` добавить компонент **Level Select Panel**
   → поле `Buttons Container` → перетащить `ButtonsContainer`. Поле
   `Level Button Prefab` заполним в шаге 7.5.
4. Кнопку `BackButton` пока не привязывайте — это сделает `MenuUIController`
   в шаге 7.6.

### 7.4 SettingsPanel

Внутри `SettingsPanel` создать элементы управления (тип Unity-объекта
справа). Расположение свободное — рекомендую добавить на `SettingsPanel`
компонент **Vertical Layout Group**, тогда всё само выстроится в столбец
без ручной расстановки:

| Объект | Тип | Зачем |
|---|---|---|
| `NumberMinSlider` | `UI → Slider` | нижняя граница диапазона чисел |
| `NumberMinLabel` | `UI → Text - TextMeshPro` | показывает текущее значение |
| `NumberMaxSlider` | `UI → Slider` | верхняя граница диапазона |
| `NumberMaxLabel` | `UI → Text - TextMeshPro` | текущее значение |
| `AdditionToggle` | `UI → Toggle` | сложение вкл/выкл |
| `SubtractionToggle` | `UI → Toggle` | вычитание |
| `MultiplicationToggle` | `UI → Toggle` | умножение |
| `DivisionToggle` | `UI → Toggle` | деление |
| `DifficultyGrowthToggle` | `UI → Toggle` | рост сложности вкл/выкл |
| `DifficultyGrowthRateSlider` | `UI → Slider` | на сколько растёт (0..1) |
| `DifficultyGrowthRateLabel` | `UI → Text - TextMeshPro` | текущее значение |
| `TimeLimitSlider` | `UI → Slider` | лимит времени на уровень |
| `TimeLimitLabel` | `UI → Text - TextMeshPro` | текущее значение |
| `DoorsEasyModeToggle` | `UI → Toggle` | упрощённый режим дверей |
| `ResetProgressButton` | `UI → Button - TextMeshPro` | «Сбросить прогресс» |
| `BackButton` | `UI → Button - TextMeshPro` | «Назад» |

Для слайдеров задайте разумные `Min Value`/`Max Value` в инспекторе
самого `Slider`, например: `NumberMinSlider` 0–20, `NumberMaxSlider`
1–50, `DifficultyGrowthRateSlider` 0–1, `TimeLimitSlider` 15–300.

Плюс маленькая вложенная панель подтверждения сброса прогресса — создать
`GameObject → Create Empty` → `ResetConfirmRoot`, внутри два `Button -
TextMeshPro`: `ResetConfirmYesButton` («Да, сбросить»),
`ResetConfirmNoButton` («Отмена»), и текст-предупреждение (просто TMP-текст,
без ссылки в скрипте — он статический).

На `SettingsPanel` добавить компонент **Settings Panel** и перетащить все
перечисленные объекты в соответствующие поля (имена полей в инспекторе
совпадают с именами объектов, только без подчёркивания — например,
`Number Min Slider` ← `NumberMinSlider`).

### 7.5 Префаб LevelButton

1. Внутри `LevelSelectPanel → ButtonsContainer` временно создать
   `UI → Button - TextMeshPro` → назвать `LevelButton`.
2. Внутри него — дочерний TMP-текст `NumberLabel` (номер уровня), и два
   небольших `Image` (или любых GameObject-индикатора) — `LockIcon` и
   `CompletedIcon` — например, просто цветные квадраты-заглушки
   (полупрозрачно-серый для замка, зелёная галочка/квадрат для
   пройденного). Оба на старте можно оставить активными в сцене — скрипт
   сам включает/выключает нужный.
3. На `LevelButton` добавить компонент **Level Button** → заполнить
   `Button` (сам этот объект), `Number Label`, `Lock Icon`, `Completed
   Icon`.
4. Перетащить `LevelButton` в `Assets/_Project/Prefabs/UI/` — это создаст
   префаб. Удалить `LevelButton` из `ButtonsContainer` в сцене (он будет
   создаваться скриптом заново каждый раз).
5. Вернуться на `LevelSelectPanel` → `Level Select Panel` → поле `Level
   Button Prefab` → перетащить только что созданный префаб.

### 7.6 MenuUIController

1. На объект `Canvas` (или отдельный пустой `GameObject` рядом) добавить
   компонент **Menu UI Controller**.
2. Заполнить поля: `Main Panel`, `Level Select Panel` (сам объект с
   компонентом `LevelSelectPanel`), `Settings Panel` (объект с
   `SettingsPanel`), `Play Button`, `Settings Button`, `Quit Button`
   (из `MainPanel`), `Level Select Back Button` (из `LevelSelectPanel`),
   `Settings Back Button` (из `SettingsPanel`).
3. Сохранить сцену.

## 8. Сцена Gameplay

Создать `Assets/_Project/Scenes/Gameplay.unity`.

1. На `Main Camera` добавить компонент **Camera Follow X**. Проверить, что
   у камеры стоит тег `MainCamera` (по умолчанию так и есть).
2. `GameObject → 2D Object → Sprites → Square` → `Background`, растянуть
   большим (`Scale`, например, 30×20), поставить позади остальных
   объектов (`Position Z` побольше, или через `Sprite Renderer → Order in
   Layer = -10`). Добавить компонент **Level Theme Applier** → поле
   `Background Renderer` → перетащить туда же сам `Background` (его
   собственный `SpriteRenderer`).
3. `GameObject → Create Empty` → `MinigameHost`, позиция примерно
   `(-4, -1, 0)` — сюда `GameplayController` будет добавлять текущую
   мини-игру (герой в префабе мини-игры стоит примерно в `(-2,0,0)`
   локально, так что в мировых координатах он окажется около
   `(-6,-1,0)` — не критично, поправите на глаз позже).
4. `GameObject → Create Empty` → `GameFlow`. Добавить компонент
   **Gameplay Controller** (компонент **Countdown Timer** добавится сам —
   он указан как обязательный).
5. `GameObject → UI → Canvas` — для HUD/паузы/результата.
   - Внутри: `TimerLabel` (`UI → Text - TextMeshPro`), `TimerFillBar`
     (`UI → Image`, в инспекторе `Image Type = Filled`) — оба под общим
     `HUD` (`Create Empty`). На `HUD` добавить компонент **Gameplay Hud**
     → заполнить оба поля.
   - `ResultPanel` (`Create Empty`): `TitleLabel` (TMP), `PrimaryButton`
     (`Button - TextMeshPro`, у него текст — отдельный дочерний TMP-объект
     — это и есть `PrimaryButtonLabel`), `SecondaryButton` (`Button -
     TextMeshPro`, текст можно сразу поставить «В меню» — его код не
     меняет). Добавить компонент **Result Panel** → `Root` (сам
     `ResultPanel`), `Title Label`, `Primary Button`, `Primary Button
     Label`, `Secondary Button`.
   - `PausePanel` (`Create Empty`): `ResumeButton`, `ExitButton` (оба
     `Button - TextMeshPro`, с текстом «Продолжить» / «Выйти»). Добавить
     компонент **Pause Panel** → `Root`, `Resume Button`, `Exit Button`.
6. На `GameFlow` → `Gameplay Controller` заполнить: `Minigame Host` →
   `MinigameHost`; `Hud` → объект `HUD`; `Result Panel` → объект
   `ResultPanel`; `Pause Panel` → объект `PausePanel`.
7. Сохранить сцену.

## 9. Build Settings — порядок сцен

`File → Build Settings` (или `File → Build Profiles` в некоторых версиях
6.3) → `Add Open Scenes` для каждой сцены по очереди, либо перетащить все
три файла сцен из Project-окна в список. **Порядок важен**: `Boot` должен
быть первым (индекс 0) — именно с него стартует сборка. Порядок:

1. `Boot`
2. `Menu`
3. `Gameplay`

## 10. Первый запуск

1. Открыть сцену `Boot.unity`, нажать `Play`.
2. Ожидается: мгновенный переход в `Menu`, три кнопки главного экрана.
3. `Играть` → сетка из 2 уровней (уровень 1 доступен, уровень 2 пока
   заблокирован — так и должно быть, пока уровень 1 не пройден).
4. Открыть уровень 1 → должны появиться герой и 2 голема с примерами.
   Клик по голему с ответом ≤ силы героя → победа, герой поглощает силу и
   т.д. до конца всех трёх башен (2→3→4 голема).
5. После победы — экран результата, кнопка «Следующий уровень» ведёт на
   уровень 2 (двери).
6. `Esc` в любой момент внутри уровня → меню паузы.
7. `Настройки` из главного меню → все переключатели должны отражать
   реальные значения и сохраняться (проверить: поменять диапазон чисел,
   выйти в меню и зайти в настройки снова — значение должно остаться).

Если что-то не совпало — это ожидаемо на первой сборке такого объёма
руками, пришлите текст ошибки из консоли Unity (`Window → General →
Console`) или скриншот, разберёмся вместе.

## 11. После того как заработало

Самое время сделать `git add -A`, `git commit`, `git push` — теперь в
репозитории появятся сцены, префабы, ассеты уровней и их `.meta`-файлы
(Unity создаёт их сама при первом импорте каждого файла). Дальше это
нужно будет делать после каждого заметного куска работы — я буду
подсказывать моменты.
