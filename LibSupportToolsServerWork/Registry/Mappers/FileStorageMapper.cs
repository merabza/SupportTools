using System;
using LibSupportToolsServerWork.Registry.Paths;
using ParametersManagement.LibFileParameters.Models;
using SupportToolsServerApiContracts.Models;
using SystemTools.SystemToolsShared;

namespace LibSupportToolsServerWork.Registry.Mappers;

//ფაილსაცავი: FileStorageData ↔ StsFileStorageDataModel. FileStoragePath, თუ ლოკალური გზაა და არა URL (მაგ. ftp://),
//PathMapper-ით გარდაიქმნება (README §4.4). მომხმარებელი და პაროლი საიდუმლოა (G2): გადაიცემა, მაგრამ არსად იბეჭდება
public static class FileStorageMapper
{
    public static StsFileStorageDataModel ToContract(string name, FileStorageData fileStorage, PathMapper pathMapper)
    {
        return new StsFileStorageDataModel
        {
            Name = name,
            FileStoragePath = MapPath(fileStorage.FileStoragePath, pathMapper.ToCanonical),
            UserName = fileStorage.UserName,
            Password = fileStorage.Password,
            //FileNameMaxLength = fileStorage.FileNameMaxLength,
            //FileSizeSplitPositionInRow = fileStorage.FileSizeSplitPositionInRow,
            FtpSiteLsFileOffset = fileStorage.FtpSiteLsFileOffset
        };
    }

    public static void ApplyToLocal(StsFileStorageDataModel contract, FileStorageData fileStorage,
        PathMapper pathMapper)
    {
        fileStorage.FileStoragePath = MapPath(contract.FileStoragePath, pathMapper.ToLocal);
        fileStorage.UserName = contract.UserName;
        fileStorage.Password = contract.Password;
        //fileStorage.FileNameMaxLength = contract.FileNameMaxLength;
        //fileStorage.FileSizeSplitPositionInRow = contract.FileSizeSplitPositionInRow;
        fileStorage.FtpSiteLsFileOffset = contract.FtpSiteLsFileOffset;
    }

    public static StsFileStorageDataModel Normalize(StsFileStorageDataModel contract)
    {
        contract.FileStoragePath =
            ContractNormalization.UpperDriveLetter(ContractNormalization.EmptyToNull(contract.FileStoragePath));
        contract.UserName = ContractNormalization.EmptyToNull(contract.UserName);
        contract.Password = ContractNormalization.EmptyToNull(contract.Password);
        return contract;
    }

    private static string? MapPath(string? path, Func<string?, string?> map)
    {
        if (string.IsNullOrEmpty(path))
        {
            return path;
        }

        //URL გზა არ არის და უცვლელი რჩება. Windows-ის ფესვიანი გზა Linux-ზეც გზაა, თუმცა Uri მას ყოველთვის ვერ ცნობს
        bool isLocalPath = PathMapper.IsWindowsRooted(path) || FileStat.IsFileSchema(path);
        return isLocalPath ? map(path) : path;
    }
}
