using Microsoft.Data.Sqlite;

var databasePath = Path.Combine(Environment.CurrentDirectory, "games.db");
var repository = new GameRepository(databasePath);
repository.Initialize();

var isRunning = true;

while (isRunning)
{
    Console.WriteLine();
    Console.WriteLine("=== Registro de videojuegos ===");
    Console.WriteLine("1. Agregar un juego");
    Console.WriteLine("2. Eliminar un juego");
    Console.WriteLine("3. Actualizar un juego");
    Console.WriteLine("4. Mostrar todos los juegos");
    Console.WriteLine("5. Buscar juegos por campo");
    Console.WriteLine("0. Salir");
    Console.Write("Selecciona una opción: ");

    switch (Console.ReadLine())
    {
        case "1":
            AddGame(repository);
            break;
        case "2":
            DeleteGame(repository);
            break;
        case "3":
            UpdateGame(repository);
            break;
        case "4":
            DisplayGames(repository.GetAll());
            break;
        case "5":
            SearchGames(repository);
            break;
        case "0":
            isRunning = false;
            break;
        default:
            Console.WriteLine("Opción no válida.");
            break;
    }
}

static void AddGame(GameRepository repository)
{
    var title = ReadRequiredInput("Título: ");
    var platform = ReadRequiredInput("Plataforma: ");
    var releaseYear = ReadInteger("Año de lanzamiento: ");

    repository.Add(new Game(0, title, platform, releaseYear));
    Console.WriteLine("Juego agregado correctamente.");
}

static void DeleteGame(GameRepository repository)
{
    var id = ReadInteger("ID del juego que deseas eliminar: ");
    Console.WriteLine(repository.Delete(id)
        ? "Juego eliminado correctamente."
        : "No se encontró un juego con ese ID.");
}

static void UpdateGame(GameRepository repository)
{
    var id = ReadInteger("ID del juego que deseas actualizar: ");
    var title = ReadRequiredInput("Nuevo título: ");
    var platform = ReadRequiredInput("Nueva plataforma: ");
    var releaseYear = ReadInteger("Nuevo año de lanzamiento: ");

    Console.WriteLine(repository.Update(new Game(id, title, platform, releaseYear))
        ? "Juego actualizado correctamente."
        : "No se encontró un juego con ese ID.");
}

static void SearchGames(GameRepository repository)
{
    Console.WriteLine("Buscar por: 1. ID  2. Título  3. Plataforma  4. Año");
    Console.Write("Selecciona un campo: ");

    var field = Console.ReadLine() switch
    {
        "1" => GameSearchField.Id,
        "2" => GameSearchField.Title,
        "3" => GameSearchField.Platform,
        "4" => GameSearchField.ReleaseYear,
        _ => (GameSearchField?)null
    };

    if (field is null)
    {
        Console.WriteLine("Campo de búsqueda no válido.");
        return;
    }

    var searchTerm = field is GameSearchField.Id or GameSearchField.ReleaseYear
        ? ReadInteger("Valor: ").ToString()
        : ReadRequiredInput("Valor: ");

    DisplayGames(repository.Search(field.Value, searchTerm));
}

static void DisplayGames(IReadOnlyCollection<Game> games)
{
    if (games.Count == 0)
    {
        Console.WriteLine("No se encontraron juegos.");
        return;
    }

    Console.WriteLine();
    Console.WriteLine("ID | Título | Plataforma | Año");
    foreach (var game in games)
    {
        Console.WriteLine($"{game.Id} | {game.Title} | {game.Platform} | {game.ReleaseYear}");
    }
}

static string ReadRequiredInput(string prompt)
{
    while (true)
    {
        Console.Write(prompt);
        var value = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value.Trim();
        }

        Console.WriteLine("El valor no puede estar vacío.");
    }
}

static int ReadInteger(string prompt)
{
    while (true)
    {
        Console.Write(prompt);
        if (int.TryParse(Console.ReadLine(), out var value))
        {
            return value;
        }

        Console.WriteLine("Ingresa un número entero válido.");
    }
}

internal sealed class GameRepository
{
    private readonly string _connectionString;

    public GameRepository(string databasePath)
    {
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath
        }.ToString();
    }

    public void Initialize()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                CREATE TABLE IF NOT EXISTS Games (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Title TEXT NOT NULL,
                    Platform TEXT NOT NULL,
                    ReleaseYear INTEGER NOT NULL
                );
                """;
            command.ExecuteNonQuery();
        }

        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT COUNT(*) FROM Games;";
            if (Convert.ToInt64(command.ExecuteScalar()) == 0)
            {
                Insert(connection, new Game(0, "The Legend of Zelda: Breath of the Wild", "Nintendo Switch", 2017));
                Insert(connection, new Game(0, "Minecraft", "PC", 2011));
                Insert(connection, new Game(0, "God of War", "PlayStation 4", 2018));
            }
        }
    }

    public void Add(Game game)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        Insert(connection, game);
    }

    public bool Delete(int id)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Games WHERE Id = $id;";
        command.Parameters.AddWithValue("$id", id);
        return command.ExecuteNonQuery() > 0;
    }

    public bool Update(Game game)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE Games
            SET Title = $title, Platform = $platform, ReleaseYear = $releaseYear
            WHERE Id = $id;
            """;
        AddGameParameters(command, game);
        command.Parameters.AddWithValue("$id", game.Id);
        return command.ExecuteNonQuery() > 0;
    }

    public List<Game> GetAll()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Title, Platform, ReleaseYear FROM Games ORDER BY Id;";
        return ReadGames(command);
    }

    public List<Game> Search(GameSearchField field, string searchTerm)
    {
        var columnName = field switch
        {
            GameSearchField.Id => "Id",
            GameSearchField.Title => "Title",
            GameSearchField.Platform => "Platform",
            GameSearchField.ReleaseYear => "ReleaseYear",
            _ => throw new ArgumentOutOfRangeException(nameof(field))
        };

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT Id, Title, Platform, ReleaseYear FROM Games WHERE {columnName} = $searchTerm ORDER BY Id;";
        command.Parameters.AddWithValue("$searchTerm", searchTerm);
        return ReadGames(command);
    }

    private static void Insert(SqliteConnection connection, Game game)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Games (Title, Platform, ReleaseYear)
            VALUES ($title, $platform, $releaseYear);
            """;
        AddGameParameters(command, game);
        command.ExecuteNonQuery();
    }

    private static void AddGameParameters(SqliteCommand command, Game game)
    {
        command.Parameters.AddWithValue("$title", game.Title);
        command.Parameters.AddWithValue("$platform", game.Platform);
        command.Parameters.AddWithValue("$releaseYear", game.ReleaseYear);
    }

    private static List<Game> ReadGames(SqliteCommand command)
    {
        using var reader = command.ExecuteReader();
        var games = new List<Game>();

        while (reader.Read())
        {
            games.Add(new Game(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetInt32(3)));
        }

        return games;
    }
}

internal sealed record Game(int Id, string Title, string Platform, int ReleaseYear);

internal enum GameSearchField
{
    Id,
    Title,
    Platform,
    ReleaseYear
}
