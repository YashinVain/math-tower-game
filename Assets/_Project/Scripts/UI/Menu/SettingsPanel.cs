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
            additionToggle.onValueChanged.AddListener(_ => OnChanged());
            subtractionToggle.onValueChanged.AddListener(_ => OnChanged());
            multiplicationToggle.onValueChanged.AddListener(_ => OnChanged());
            divisionToggle.onValueChanged.AddListener(_ => OnChanged());
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
        }

        private void OnChanged()
        {
            if (_isLoading) return;

            var settings = GameServices.Instance.Settings.Current;
            settings.numberRangeMin = Mathf.RoundToInt(numberMinSlider.value);
            settings.numberRangeMax = Mathf.Max(settings.numberRangeMin, Mathf.RoundToInt(numberMaxSlider.value));
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
