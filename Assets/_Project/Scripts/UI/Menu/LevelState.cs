namespace MathGame.UI.Menu
{
    // Чисто "витринное" состояние кнопки уровня — не хранится нигде, а
    // вычисляется каждый раз при построении списка (см. LevelSelectPanel)
    // из LevelCatalog + IProgressService.
    public enum LevelState
    {
        Locked,
        Unlocked,
        Completed
    }
}
