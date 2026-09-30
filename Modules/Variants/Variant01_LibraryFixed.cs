namespace ReviewSamples.Modules.Variants;

/// <summary>Книга библиотечного фонда.</summary>
public sealed class LibraryBook
{
    public LibraryBook(string title, string author, string isbn)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Название книги не может быть пустым", nameof(title));
        if (string.IsNullOrWhiteSpace(author))
            throw new ArgumentException("Автор книги не может быть пустым", nameof(author));
        if (string.IsNullOrWhiteSpace(isbn))
            throw new ArgumentException("ISBN не может быть пустым", nameof(isbn));

        Title = title;
        Author = author;
        Isbn = isbn;
    }

    public string Title { get; }
    public string Author { get; }
    public string Isbn { get; }
}

/// <summary>Читатель библиотеки.</summary>
public sealed class LibraryReader
{
    public LibraryReader(string name, int cardNumber)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Имя читателя не может быть пустым", nameof(name));
        if (cardNumber <= 0)
            throw new ArgumentOutOfRangeException(nameof(cardNumber), "Номер билета должен быть положительным");

        Name = name;
        CardNumber = cardNumber;
    }

    public string Name { get; }
    public int CardNumber { get; }
}

/// <summary>Результат операции выдачи или возврата (вместо строк "ok" / "bad").</summary>
public sealed record LibraryResult(bool IsSuccess, string Message, decimal Fine = 0m)
{
    public static LibraryResult Success(string message, decimal fine = 0m) => new(true, message, fine);

    public static LibraryResult Fail(string message) => new(false, message);
}

/// <summary>Учёт выдачи и возврата книг. Не пишет в консоль и не зависит от текущего времени.</summary>
public sealed class LibraryService
{
    public const int MinLoanDays = 1;
    public const int MaxLoanDays = 60;
    public const decimal FinePerDay = 10m;

    private sealed record Loan(int CardNumber, DateOnly DueDate);

    // Ключ — ISBN: одна книга может быть выдана только одному читателю.
    private readonly Dictionary<string, Loan> _loans = new();

    public LibraryResult IssueBook(LibraryReader reader, LibraryBook book, int days, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(book);
        if (days < MinLoanDays || days > MaxLoanDays)
            throw new ArgumentOutOfRangeException(
                nameof(days), $"Срок выдачи должен быть от {MinLoanDays} до {MaxLoanDays} дней");

        if (_loans.ContainsKey(book.Isbn))
            return LibraryResult.Fail("Книга уже выдана");

        var dueDate = today.AddDays(days);
        _loans[book.Isbn] = new Loan(reader.CardNumber, dueDate);
        return LibraryResult.Success($"Книга выдана до {dueDate:dd.MM.yyyy}");
    }

    public LibraryResult ReturnBook(LibraryReader reader, LibraryBook book, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(book);

        if (!_loans.TryGetValue(book.Isbn, out var loan))
            return LibraryResult.Fail("Книга не числится выданной");
        if (loan.CardNumber != reader.CardNumber)
            return LibraryResult.Fail("Книга выдана другому читателю");

        _loans.Remove(book.Isbn);

        var fine = CalculateFine(loan.DueDate, today);
        return fine > 0
            ? LibraryResult.Success($"Книга возвращена с просрочкой, штраф {fine:F2}", fine)
            : LibraryResult.Success("Книга возвращена в срок");
    }

    private static decimal CalculateFine(DateOnly dueDate, DateOnly today)
    {
        var overdueDays = today.DayNumber - dueDate.DayNumber;
        return overdueDays > 0 ? overdueDays * FinePerDay : 0m;
    }
}
