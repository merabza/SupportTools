namespace LibAppProjectCreator.Models;

public sealed class ReferenceDataModel
{
    // ReSharper disable once ConvertToPrimaryConstructor
    public ReferenceDataModel(string projectFilePath, string referenceProjectFilePath)
    {
        ProjectFilePath = projectFilePath;
        ReferenceProjectFilePath = referenceProjectFilePath;
    }

    public string ProjectFilePath { get; set; }
    public string ReferenceProjectFilePath { get; set; }
}
