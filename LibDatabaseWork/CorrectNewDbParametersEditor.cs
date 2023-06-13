//using CliParameters;
//using CliParameters.FieldEditors;
//using CliParameters.Models;
//using CliParametersDataEdit.FieldEditors;
//using DbTools;
//using LibDatabaseWork.Models;
//using Microsoft.Extensions.Logging;

//namespace LibDatabaseWork;

//public sealed class CorrectNewDbParametersEditor : TaskParametersEditor
//{

//    public CorrectNewDbParametersEditor(ILogger logger, IParameters parameters,
//        ParametersTaskInfo parametersTaskInfo, IParametersManager parametersManager) : base(
//        "Correct New Database Parameters Editor", parameters, parametersTaskInfo)
//    {
//        FieldEditors.Add(
//            new EnumFieldEditor<EDataProvider>(nameof(CorrectNewDbParameters.DataProvider), EDataProvider.Sql));
//        FieldEditors.Add(new ConnectionStringFieldEditor(logger, nameof(CorrectNewDbParameters.ConnectionString),
//            nameof(CorrectNewDbParameters.DataProvider), parametersManager));
//        FieldEditors.Add(new IntFieldEditor(nameof(CorrectNewDbParameters.CommandTimeOut), 10000));
//    }

//}

