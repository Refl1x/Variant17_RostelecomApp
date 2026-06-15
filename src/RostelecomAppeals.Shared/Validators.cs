using System.Text.RegularExpressions;

namespace RostelecomAppeals.Shared;

public static class Validators
{
    private static readonly Regex Text = new(@"^[A-Za-zА-Яа-яЁё0-9 .,""'№()/_:;!?+\-]+$", RegexOptions.Compiled);
    private static readonly Regex Phone = new(@"^\+7[0-9]{10}$", RegexOptions.Compiled);

    public static List<string> ValidateAppeal(AppealDto a)
    {
        var errors = new List<string>();
        CheckText(errors, a.ApplicantName, 100, "ФИО/организация");
        if (string.IsNullOrWhiteSpace(a.ContactPhone) || !Phone.IsMatch(a.ContactPhone))
            errors.Add("Телефон должен быть в формате +7XXXXXXXXXX.");
        CheckText(errors, a.ConnectionAddress, 150, "Адрес подключения");
        CheckText(errors, a.Description, 200, "Описание обращения");
        if (a.TypeId <= 0) errors.Add("Выберите тип обращения.");
        if (a.StatusId <= 0) errors.Add("Выберите статус обращения.");
        if (a.PriorityId <= 0) errors.Add("Выберите приоритет.");
        if (a.RegisteredAt > DateTime.UtcNow.AddMinutes(5)) errors.Add("Дата регистрации не может быть позднее текущей даты.");
        return errors;
    }

    public static List<string> ValidateComment(string text, bool internalComment)
    {
        var max = internalComment ? 400 : 250;
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(text)) errors.Add("Комментарий не может быть пустым.");
        if (text.Length > max) errors.Add($"Комментарий не должен быть длиннее {max} символов.");
        return errors;
    }

    private static void CheckText(List<string> errors, string value, int max, string label)
    {
        if (string.IsNullOrWhiteSpace(value)) errors.Add($"Поле «{label}» не может быть пустым.");
        else if (value.Length > max) errors.Add($"Поле «{label}» не должно быть длиннее {max} символов.");
        else if (!Text.IsMatch(value)) errors.Add($"Поле «{label}» содержит недопустимые символы.");
    }
}
