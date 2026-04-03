namespace FyreWorksAI.Shared.Core.Services.Status;

//******************************//
//***** StatusMessageFormatter**//
//******************************//
public static class StatusMessageFormatter
{
    public static string WithTimestamp(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return string.Empty;
        }

        return $"{message.Trim()} {DateTime.Now:MM/dd/yyyy h:mm:ss tt}";
    }
}
