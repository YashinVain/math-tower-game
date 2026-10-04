using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MathGame.Core;
using MathGame.Data;

namespace MathGame.UI.Menu
{
    // Каждый контрол сразу пишет в GameServices.Instance.Settings.Current и
    // сохраняет на диск — отдельного экрана "Применить/Отмена" в этой
    // версии нет, это осознанное упрощение вертикального среза.
    public class SettingsPanel : MonoBehaviour
    {
        [SerializeField] private Slider numberMinSlider;
        [SerializeField] private TextMeshProUGUI numberMinLabel;
        [SerializeField] private Slider numberMaxSlider;
        [SerializeField] private TextMeshProUGUI numberMaxLabel;

        [SerializeField] private Toggle additionToggle;
        [SerializeField] private Toggle subtractionToggle;
        [SerializeField] private Toggle multiplicationToggle;
        [SerializeField] private Toggle divisionToggle;

        [SerializeField] private Toggle difficultyGrowthToggle;
        [SerializeField] private Slider difficultyGrowthRateSlider;
        [SerializeField] private TextMeshProUGUI difficultyGrowthRateLabel;

        [SerializeField] private Slider timeLimitSlider;
        [SerializeField] private TextMeshProUGUI timeLimitLabel;

        [SerializeField] private Toggle doorsEasyModeToggle;

        [SerializeField] private Button resetProgressButton;
        [SerializeField] private GameObject resetConfirmRoot;
        [SerializeField] private Button resetConfirmYesButton;
        [SerializeField] private Button resetConfirmNoButton;

        // Четыре кнопки автонастройки под возраст (см. AgePresets).
        [SerializeField] private Button age6to8Button;
        [SerializeField] private Button age9to11Button;
        [SerializeField] private Button age12to14Button;
        [SerializeField] private Button age15to16Button;

        private static readonly Color PresetNormalColor = new Color(1f, 1f, 1f, 0.9f);
        private static readonly Color PresetSelectedColor = new Color(0.55f, 0.9f, 0.55f, 1f);

        private bool _isLoading;

        private void Awake()
        {
            age6to8Button.onClick.AddListener(() => ApplyAgePreset(AgePreset.Age6to8));
            age9to11Button.onClick.AddListener(() => ApplyAgePreset(AgePreset.Age9to11));
            age12to14Button.onClick.AddListener(() => ApplyAgePreset(AgePreset.Age12to14));
            age15to16Button.onClick.AddListener(() => ApplyAgePreset(AgePreset.Age15to16));

            numberMinSlider.onValueChanged.AddListener(_ => OnChanged());
            numberMaxSlider.onValueChanged.AddListener(_ => OnChanged());
            additionToggle.onValueChanged.AddListener(_ => OnOperationToggleChanged(additionToggle));
            subtractionToggle.onValueChanged.AddListener(_ => OnOperationToggleChanged(subtractionToggle));
            multiplicationToggle.onValueChanged.AddListener(_ => OnOperationToggleChanged(multiplicationToggle));
            divisionToggle.onValueChanged.AddListener(_ => OnOperationToggleChanged(divisionToggle));
            difficultyGrowthToggle.onValueChanged.AddListener(_ => OnChanged());
            difficultyGrowthRateSlider.onValueChanged.AddListener(_ => OnChanged());
            timeLimitSlider.onValueChanged.AddListener(_ => OnChanged());
            doorsEasyModeToggle.onValueChanged.AddListener(_ => OnChanged());

            resetProgressButton.onClick.AddListener(() => resetConfirmRoot.SetActive(true));
            resetConfirmYesButton.onClick.AddListener(OnResetConfirmed);
            resetConfirmNoButton.onClick.AddListener(() => resetConfirmRoot.SetActive(false));

            resetConfirmRoot.SetActive(false);
        }

        public void LoadFromService()
        {
            _isLoading = true;

            var settings = GameServices.Instance.Settings.Current;
            numberMinSlider.value = settings.numberRangeMin;
            numberMaxSlider.value = settings.numberRangeMax;
            additionToggle.isOn = settings.additionEnabled;
            subtractionToggle.isOn = settings.subtractionEnabled;
            multiplicationToggle.isOn = settings.multiplicationEnabled;
            divisionToggle.isOn = settings.divisionEnabled;
            difficultyGrowthToggle.isOn = settings.difficultyGrowthEnabled;
            difficultyGrowthRateSlider.value = settings.difficultyGrowthRate;
            timeLimitSlider.value = settings.levelTimeLimitSeconds;
            doorsEasyModeToggle.isOn = settings.doorsEasyModeEnabled;

            RefreshLabels(settings);
            RefreshPresetHighlight(settings);
            _isLoading = false;

            // Защита на случай, если в сохранённом файле настроек как-то
            // оказались выключены все четыре операции разом (например, из
            // старой версии сохранения) — генератор примеров в этом случае
            // молча подставляет сложение (см. PickOperation в
            // MathProblemGenerator), и экран настроек должен показывать
            // ровно то же самое, а не "ничего не выбрано".
            if (!additionToggle.isOn && !subtractionToggle.isOn && !multiplicationToggle.isOn && !divisionToggle.isOn)
            {
                additionToggle.isOn = true; // через isOn, не SetIsOnWithoutNotify — чтобы OnOperationToggleChanged сохранил это в настройки
            }

            // Та же защита для диапазона чисел — например, если сохранённый
            // файл старый и в нём ещё нет такого ограничения (SettingsService
            // уже применяет GameSettingsData.ClampNumberRange при загрузке,
            // это просто подстраховка на случай, если settings.Current
            // подменили как-то иначе).
            bool rangeNeedsFix = settings.numberRangeMax - settings.numberRangeMin < GameSettingsData.MinNumberRangeWidth
                || settings.numberRangeMin > GameSettingsData.MaxNumberRangeMin;
            if (rangeNeedsFix)
                OnChanged(); // сам пересчитает и сохранит через ClampNumberRange
        }

        // Хотя бы одна операция должна остаться включённой — иначе
        // генератор примеров молча подставляет сложение (см. PickOperation
        // в MathProblemGenerator), и в настройках было бы видно "выключено
        // всё", а в игре всё равно шли бы примеры на сложение — ровно
        // такой рассинхрон между экраном и игрой заметил игрок. Поэтому
        // последнюю оставшуюся галочку снять нельзя — она просто
        // возвращается обратно.
        private void OnOperationToggleChanged(Toggle changedToggle)
        {
            if (_isLoading) return;

            bool anyEnabled = additionToggle.isOn || subtractionToggle.isOn || multiplicationToggle.isOn || divisionToggle.isOn;
            if (!anyEnabled)
            {
                changedToggle.SetIsOnWithoutNotify(true); // без Notify — иначе получим повторный вызов этого же метода
                return;
            }

            OnChanged();
        }

        // Держит numberRangeMin не выше GameSettingsData.MaxNumberRangeMin
        // (иначе сумму двух чисел из диапазона нельзя сделать маленькой —
        // см. подробный комментарий там) и numberRangeMax — как минимум на
        // GameSettingsData.MinNumberRangeWidth больше numberRangeMin, всегда
        // подтягивая "макс.", а не "мин." (у слайдера максимума потолок 50,
        // места хватит с большим запасом). Так игрок не может ни поднять
        // "мин." настолько, что маленькие примеры станут непостроимыми, ни
        // зажать весь диапазон до нескольких чисел, а оба слайдера на
        // экране всегда показывают то, что реально сохранено.
        private void ClampNumberRange(out int min, out int max)
        {
            min = Mathf.Clamp(Mathf.RoundToInt(numberMinSlider.value), 0, GameSettingsData.MaxNumberRangeMin);
            max = Mathf.Max(Mathf.RoundToInt(numberMaxSlider.value), min + GameSettingsData.MinNumberRangeWidth);

            numberMinSlider.SetValueWithoutNotify(min);
            numberMaxSlider.SetValueWithoutNotify(max);
        }

        private void OnChanged()
        {
            if (_isLoading) return;

            ClampNumberRange(out int min, out int max);

            var settings = GameServices.Instance.Settings.Current;
            settings.numberRangeMin = min;
            settings.numberRangeMax = max;
            settings.additionEnabled = additionToggle.isOn;
            settings.subtractionEnabled = subtractionToggle.isOn;
            settings.multiplicationEnabled = multiplicationToggle.isOn;
            settings.divisionEnabled = divisionToggle.isOn;
            settings.difficultyGrowthEnabled = difficultyGrowthToggle.isOn;
            settings.difficultyGrowthRate = difficultyGrowthRateSlider.value;
            settings.levelTimeLimitSeconds = timeLimitSlider.value;
            settings.doorsEasyModeEnabled = doorsEasyModeToggle.isOn;

            GameServices.Instance.Settings.Save();
            RefreshLabels(settings);
            RefreshPresetHighlight(settings);
        }

        // Выставляет сразу все настройки под выбранный возраст, сохраняет
        // и перерисовывает экран по новым значениям (LoadFromService
        // заново читает всё из тех же настроек — отдельно обновлять каждый
        // слайдер и галочку здесь не нужно).
        private void ApplyAgePreset(AgePreset preset)
        {
            var settings = GameServices.Instance.Settings.Current;
            AgePresets.Apply(settings, preset);
            GameServices.Instance.Settings.Save();
            LoadFromService();
        }

        // Подсвечивает кнопку возраста, чьи значения сейчас в точности
        // выставлены. Если игрок после выбора пресета что-то подвинул
        // руками — подсветка пропадает: настройки уже "свои".
        private void RefreshPresetHighlight(GameSettingsData settings)
        {
            SetPresetColor(age6to8Button, AgePresets.Matches(settings, AgePreset.Age6to8));
            SetPresetColor(age9to11Button, AgePresets.Matches(settings, AgePreset.Age9to11));
            SetPresetColor(age12to14Button, AgePresets.Matches(settings, AgePreset.Age12to14));
            SetPresetColor(age15to16Button, AgePresets.Matches(settings, AgePreset.Age15to16));
        }

        private static void SetPresetColor(Button button, bool selected)
        {
            if (button.targetGraphic != null)
                button.targetGraphic.color = selected ? PresetSelectedColor : PresetNormalColor;
        }

        private void RefreshLabels(GameSettingsData settings)
        {
            numberMinLabel.text = settings.numberRangeMin.ToString();
            numberMaxLabel.text = settings.numberRangeMax.ToString();
            difficultyGrowthRateLabel.text = $"{settings.difficultyGrowthRate:P0} / уровень";
            timeLimitLabel.text = $"{settings.levelTimeLimitSeconds:0} сек";
        }

        private void OnResetConfirmed()
        {
            GameServices.Instance.Progress.ResetProgress();
            resetConfirmRoot.SetActive(false);
        }
    }
}
