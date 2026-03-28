namespace MediaOrchestrator
{
    /// <summary>
    ///     Запись об устройстве захвата (камера, микрофон), распознанном через <c>ffmpeg -list_devices</c>.
    /// </summary>
    public sealed class Device
    {
        public string Name { get; set; }
        public string AlternativeName { get; set; }
    }
}
