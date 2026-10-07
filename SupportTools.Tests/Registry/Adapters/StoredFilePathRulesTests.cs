using LibSupportToolsServerWork.Registry.Adapters;
using Xunit;

namespace SupportTools.Tests.Registry.Adapters;

//the stored file path rule of B8 (SupportToolsServer PathRules and StoredFile.PathMaxLength): a stored file is synced
//only when the server would take its canonical path
public sealed class StoredFilePathRulesTests
{
    [Theory]
    [InlineData(@"D:\a")]
    [InlineData(@"D:\1WorkSecurity\AppFake\dl360\appsettings.json")]
    [InlineData(@"d:\1WorkSecurity\appsettings.json")]
    [InlineData(@"D:\folder.name\file.name.json")]
    [InlineData(@"D:\.hidden\file")]
    public void IsValidCanonicalPath_WhenPathIsAnAbsoluteWindowsFilePath_ReturnsTrue(string path)
    {
        // Act
        bool result = StoredFilePathRules.IsValidCanonicalPath(path);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("D:")]
    [InlineData(@"D:\")]
    [InlineData(@"1:\a")]
    [InlineData(@"DD\a")]
    [InlineData("D:/a")]
    [InlineData(@"\\files.example.test\share\a")]
    [InlineData("appsettings.json")]
    [InlineData(@"D:\a\\b")]
    [InlineData(@"D:\a\")]
    [InlineData(@"D:\a.\b")]
    [InlineData(@"D:\a\.")]
    [InlineData(@"D:\a\..")]
    [InlineData(@"D:\a \b")]
    [InlineData(@"D:\a<b")]
    [InlineData(@"D:\a>b")]
    [InlineData(@"D:\a:b")]
    [InlineData("D:\\a\"b")]
    [InlineData("D:\\a/b")]
    [InlineData(@"D:\a|b")]
    [InlineData(@"D:\a?b")]
    [InlineData(@"D:\a*b")]
    [InlineData("D:\\a\tb")]
    public void IsValidCanonicalPath_WhenServerWouldRefuseThePath_ReturnsFalse(string path)
    {
        // Act
        bool result = StoredFilePathRules.IsValidCanonicalPath(path);

        // Assert
        Assert.False(result);
    }

    //the length of the server column
    [Theory]
    [InlineData(StoredFilePathRules.PathMaxLength, true)]
    [InlineData(StoredFilePathRules.PathMaxLength + 1, false)]
    public void IsValidCanonicalPath_ForPathLength_AcceptsUpToTheServerColumnLength(int length, bool expected)
    {
        // Arrange
        string path = @"D:\" + new string('a', length - 3);

        // Act
        bool result = StoredFilePathRules.IsValidCanonicalPath(path);

        // Assert
        Assert.Equal(expected, result);
    }
}
