namespace PetSlackBot;

public class MediaService
{
    private static string GetPath(string fileName) => $"/static/gifs/{fileName}.gif";

    private static readonly Dictionary<PetState, string> _petGifs = new()
    {
        [PetState.Dead] = GetPath(""),
        [PetState.Greetings] = GetPath(""),
        [PetState.Neutral] = GetPath(""),
        [PetState.Happiness] = GetPath(""),
        [PetState.Tired] = GetPath(""),
        [PetState.Hungry] = GetPath(""),
        [PetState.Satiety] = GetPath(""),
    };

    private readonly string _baseUrl;

    public MediaService(IConfiguration config)
    {
        _baseUrl = config["BaseUrl"] ?? "http://localhost:8080/";
    }

    public string GetGifPathByPetState(PetState state)
    {
        return _petGifs.TryGetValue(state, out var path)
            ? $"{_baseUrl}{path}"
            : $"{_baseUrl}/{GetPath("default")}";
    }
}

public enum PetState
{
    Dead,
    Greetings,
    Neutral,
    Happiness,
    Tired,
    Hungry,
    Satiety
}