namespace PetSlackBot;

public class MediaService
{
    private static string GetPath(string fileName) => $"/static/gifts/{fileName}.gif";

    private static readonly Dictionary<PetState, string> _petGifs = new()
    {
        [PetState.Dead] = GetPath("dead"),
        [PetState.Working] = GetPath("working"),
        [PetState.Greetings] = GetPath("dance"),
        [PetState.Neutral] = GetPath("neutral"),
        [PetState.Happiness] = GetPath("happiness"),
        [PetState.Tired] = GetPath("tired"),
        [PetState.Hungry] = GetPath("cry"),
        [PetState.Satiety] = GetPath("satiety"),   
    };

    private readonly string _baseUrl;

    public MediaService()
    {
        _baseUrl = "https://unfluctuant-tabitha-nonfrigidly.ngrok-free.dev";
    }

    public string GetGifPathByPetState(PetState state)
    {
        return _petGifs.TryGetValue(state, out var path)
            ? $"{_baseUrl}{path}"
            : $"{_baseUrl}/{GetPath("neutral")}";
    }
}

public enum PetState
{
    Dead,
    Working,
    Greetings,
    Neutral,
    Happiness,
    Tired,
    Hungry,
    Satiety
}