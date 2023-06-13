namespace LibAppProjectCreator.Models;

public sealed class ReferenceDataModel
{
    public ReferenceDataModel(string projectFilePath, string referenceProjectFilePath)
    {
        ProjectFilePath = projectFilePath;
        ReferenceProjectFilePath = referenceProjectFilePath;
    }

    public string ProjectFilePath { get; set; }
    public string ReferenceProjectFilePath { get; set; }
}