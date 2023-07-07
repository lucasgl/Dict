

using System.Text.Json;
using System.Text.Json.Serialization;
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


public record PersonResult(string FirstName, string LastName, List<Vaccine>? Vaccines);

public record Person(string FirstName, string LastName) {
    
    public List<int> VaccineIds {get;set;} = new List<int>();
    
    // [JsonIgnore]
    // public virtual List<Vaccine>? Vaccinations {get;set;}
}

public record Vaccine(string Name);

public class PersonRepo
{
    List<object> _savedData = new List<object>();

    Dictionary<int, Person> people = new() {
        {0,new ("Ryan", "Anderson")},
        {1,new ("Dani", "Trugilo")}
    };

    Dictionary<int, Vaccine> vaccines = new() {
        {0,new ("Covid")},
        {1,new ("Flu")}
    };

    public PersonRepo()
    {
        LoadData();
    }

    public PersonResult GetPerson(int id)
    {
        return new(people[id].FirstName, people[id].LastName, 
            vaccines.Where(v => people[id].VaccineIds.Contains(v.Key)).Select(i => i.Value).ToList());
    }

    public Person AddPerson(Person person) {
        people.Add(people.Keys.Max() + 1, person);
        return person;
    }

    public (Person?, bool) AddVaccineToPerson(int id, string vaccineName) {
        try {
            var vaccine = vaccines.First(vaccine => vaccine.Value.Name == vaccineName);
            if(people[id].VaccineIds == null) people[id].VaccineIds = new List<int>();
            people[id].VaccineIds?.Add(vaccine.Key);
            return (people[id], true);
        }
        catch(InvalidOperationException) {
            return (null, false);
        }
    }

    public Vaccine AddVaccine(Vaccine vaccine) {
        vaccines.Add(vaccines.Keys.Max() + 1, vaccine);
        return vaccine;
    }

    public ConsoleColor GetColor(int id)
    {
        var dict = new Dictionary<int, ConsoleColor>() {
            {0,ConsoleColor.DarkBlue},
            {1,ConsoleColor.DarkYellow}
        };
        return dict[id];
    }


    /// <summary>
    /// Convert List<Person> into a List<PersonResult>
    /// Check how it's done in GetPerson and do the same here. it needs to see the vaccine list.
    /// </summary>
    /// <returns></returns>
    public List<PersonResult> GetPeople() 
    {
        return people.Values.ToList();
    }

    public void PersistData() {
        _savedData = new List<object>{people,vaccines};
        File.WriteAllText("data.json", JsonSerializer.Serialize(_savedData));
    }

    public void InitializeData() {
        File.WriteAllText("data.json", JsonSerializer.Serialize(_savedData));
    }

    public void LoadData() {
        _savedData = new List<object>{people,vaccines};
        if(File.Exists("data.json")) {
            _savedData = JsonSerializer.Deserialize<List<object>>(File.ReadAllText("data.json")) ?? _savedData;
            people = ((JsonElement)_savedData[0]).Deserialize<Dictionary<int,Person>>();
            vaccines = ((JsonElement)_savedData[1]).Deserialize<Dictionary<int,Vaccine>>();
        }
        else {
            InitializeData();
            people = (Dictionary<int,Person>)_savedData[0];
            vaccines = (Dictionary<int,Vaccine>)_savedData[1];
        }
    }

    public List<Vaccine> GetVaccines()
    {
        return vaccines.Values.ToList();
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
    public List<PersonResult> People() {
        return personRepo.GetPeople();
    }

    [HttpPost("AddPerson")]
    public string AddPerson(Person person) {
        personRepo.AddPerson(person);
        personRepo.PersistData();
        return $"{person.FirstName} Added!!";
    }

    [HttpGet("AddVaccineToPerson/{personId}/{vaccineName}")]
    public IActionResult AddVaccineToPerson(int personId, string vaccineName) {
        var (person,result) = personRepo.AddVaccineToPerson(personId, vaccineName);
        if(result) {
            personRepo.PersistData();
            return new OkObjectResult(person);
        }
        else {
            return new BadRequestObjectResult($"No vaccine found with name {vaccineName}");
        }
    }

    [HttpGet("AddVaccine/{vaccineName}")]
    public Vaccine AddVaccine(string vaccineName) {
        Vaccine vaccine = new (vaccineName);
        personRepo.AddVaccine(vaccine);
        return vaccine; 
    }

    [HttpGet("Vaccines")]
    public List<Vaccine> Vaccines() {
        return personRepo.GetVaccines();
    }

}


// Android APP -> Bussnee Logic -> HW
// Ios -> Bussnee Logic -> HW
