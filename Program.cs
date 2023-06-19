

using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddAWSTalker();
//builder.Services.AddAzureTalker();
builder.Services.AddSingleton<PersonRepo>();

var app = builder.Build();

app.MapControllers();

// app.Map("Greet/{person}", (string person) => {
//     Talker talker = new(new TalkBlue());
//     talker.Greet(person);
//     return $"Hello {person}";
// });

// app.Map("GoodBye/{person}", (string person) => {
//     Talker talker = new(new TalkBlue());
//     talker.Greet(person);
//     return $"Hello {person}";
// });

app.Run();



public static class TalkerCloudExtentions 
{
    public static IServiceCollection AddAWSTalker(this IServiceCollection services) 
    {
        return services.AddSingleton<ISystemTalker,TalkRed>();
    }

    public static IServiceCollection AddAzureTalker(this IServiceCollection services) 
    {
        return services.AddSingleton<ISystemTalker,TalkBlue>();
    }
}

public class TalkRed : ISystemTalker
{
    public TalkRed(PersonRepo repo)
    {
        this.repo = repo;
    }
        int count = 0;
    private readonly PersonRepo repo;

    public void Say(string msg) {
        Console.BackgroundColor = repo.GetColor(1);
        Console.WriteLine("{0} {1}", msg, count++);
    }
}

public class TalkBlue : ISystemTalker
{
    public TalkBlue(PersonRepo repo)
    {
        this.repo = repo;
    }
        int count = 0;
    private readonly PersonRepo repo;
    public void Say(string msg) {
        Console.BackgroundColor = repo.GetColor(0);
        Console.WriteLine("{0} {1}", msg, count++);
    }
}

public record Person(string FirstName, string LastName) {
    public List<Vaccine>? Vaccinations {get;set;}
}

public record Vaccine(string Name);

public class PersonRepo
{
    Dictionary<int, Person> dict = new() {
        {0,new ("Ryan", "Anderson")},
        {1,new ("Dani", "Trugilo")},
    };

    public PersonRepo()
    {
        LoadData();
    }

    public Person GetPerson(int id)
    {
        return dict[id];
    }

    public Person AddPerson(Person person) {
        dict.Add(dict.Keys.Max() + 1, person);
        return person;
    }

    public Person AddVaccine(int id, string vaccineName) {
        if(dict[id].Vaccinations == null) dict[id].Vaccinations = new List<Vaccine>();
        dict[id].Vaccinations?.Add(new Vaccine(vaccineName));
        return dict[id];
    }

    public ConsoleColor GetColor(int id)
    {
        var dict = new Dictionary<int, ConsoleColor>() {
            {0,ConsoleColor.DarkBlue},
            {1,ConsoleColor.DarkYellow}
        };
        return dict[id];
    }

    public List<Person> GetPeople() 
    {
        return dict.Values.ToList();
    }

    public void PersistData() {
        File.WriteAllText("data.db", JsonSerializer.Serialize(dict));
    }

    public void InitializeData() {
        File.WriteAllText("data.db", JsonSerializer.Serialize(dict));
    }

    public void LoadData() {
        dict = JsonSerializer.Deserialize<Dictionary<int, Person>>(File.ReadAllText("data.db")) ?? dict; //if null reassign to itself;
    }
}

public interface ISystemTalker 
{
    void Say(string msg);
}

[ApiController]
public class TalkerController {
    readonly ISystemTalker talker;
    private readonly PersonRepo personRepo;

    public TalkerController(ISystemTalker talker,PersonRepo personRepo)
    {
        this.talker = talker;
        this.personRepo = personRepo;
    }

    [HttpGet("Greet/{personId}")]
    public string Greet(int personId) {
        talker.Say($"Hi {personRepo.GetPerson(personId).FirstName}");
        return $"Hi {personRepo.GetPerson(personId).FirstName}";
    }

    [HttpGet("GoodBye/{personId}")]
    public string GoodBye(int personId) {
        talker.Say($"Good Bye {personRepo.GetPerson(personId).FirstName}");
        return $"GoodBye {personRepo.GetPerson(personId).FirstName}";
    }

    [HttpGet("People")]
    public List<Person> People() {
        return personRepo.GetPeople();
    }

    [HttpPost("AddPerson")]
    public string AddPerson(Person person) {
        personRepo.AddPerson(person);
        personRepo.PersistData();
        return $"{person.FirstName} Added!!";
    }

    [HttpGet("AddVaccine/{personId}/{vaccineName}")]
    public Person AddPerson(int personId, string vaccineName) {
        var person = personRepo.AddVaccine(personId, vaccineName);
        personRepo.PersistData();
        return person;
    }

}


// Android APP -> Bussnee Logic -> HW
// Ios -> Bussnee Logic -> HW
