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

        private bool _isLoading;

        private void Awake()
        {
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
            if (settings.numberRangeMax - settings.numberRangeMin < GameSettingsData.MinNumberRangeWidth)
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

        // Держит numberRangeMax как минимум на GameSettingsData.MinNumberRangeWidth
        // больше numberRangeMin — всегда подтягивая "макс.", а не "мин." (у
        // слайдера минимума потолок 20, у слайдера максимума потолок 50, так
        // что места хватит с большим запасом). Так игрок не может зажать
        // диапазон до одного-двух чисел ни подняв "мин.", ни опустив
        // "макс.", а оба слайдера на экране всегда показывают то, что
        // реально сохранено.
        private void ClampNumberRange(out int min, out int max)
        {
            min = Mathf.RoundToInt(numberMinSlider.value);
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
