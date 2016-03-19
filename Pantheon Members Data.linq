<Query Kind="Program">
  <NuGetReference Prerelease="true">Newtonsoft.Json</NuGetReference>
  <NuGetReference>SimpleBrowser</NuGetReference>
  <Namespace>Newtonsoft.Json</Namespace>
  <Namespace>SimpleBrowser</Namespace>
  <Namespace>System.Globalization</Namespace>
  <Namespace>System.Net</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
</Query>

void Main()
{

    //You can either provide login detials below OR use environment variables
    //SKYFORGE_LOGIN, SKYFORGE_PASSWORD, SKYFORGE_PANTHEONID
    //to provide them. Login details provided here will override the environrment
    //details if present.

    //Your username for Alinet portal (this is in email form)
    var username = "";
    //Your password for Aelinet login
    var password = "";

    //You can get this by visiting your pantheon community page in aelinet. It's the last numbers bit in your address bar.
    //for example for Team Rocket this id is 243083329203608905
    var pantheonId = "";

    //Report extended stats for players. Please note that this will significantly increase time to report data if set to true
    //The run time will increase upwards of 4 seconds per Pantheon member (academy members are not parsed for these stats)
    //Please do not fiddle with 4 seconds to make things any faster unless you take responsibility for possibly getting banned
    //for abusing this tool or any other sanctions that may be applied to you and anybody else in pantheon by official customer support
    //YOU HAVE BEEN WARNED!
    var reportDistortionData = false;
    var reportPlayerProfileData = false;

    var browser = new Browser();
    var checkTime = DateTime.Now;
    var members = new List<GuildMember>();

    if (CheckAelinetDetailsWithEnvironmentFallback(ref username, ref password, ref pantheonId))
    {
        LoginToAelinet(browser, username, password);

        NavigateAelinetGuildSection(pantheonId, members, browser, checkTime, MemberType.PantheonMember, reportPlayerProfileData, reportDistortionData);
        NavigateAelinetGuildSection(pantheonId, members, browser, checkTime, MemberType.AcademyMember);
        
        Util.ClearResults();
        members.OrderBy(x => x.Name).Dump();
    }
}

const string TimestampFormat = "HH:mm:ss.f";

static readonly Dictionary<string, string> DistortionToShortCodeLookup = new Dictionary<string, string> {
    {"Onslaught of the Sea: Alciona and Melia", "A1"},
    {"Onslaught of the Sea: Lorro the Cold","A2"},
    {"Onslaught of the Sea: Wise Latanu","A3"},
    {"Onslaught of the Sea: Nautilus","A4"},
    {"Dangerous Greenhouse: Caryolis","B1"},
    {"Dangerous Greenhouse: Malicenia","B2"},
    {"Dangerous Greenhouse: Siringe","B3"},
    {"Dangerous Greenhouse: Nephelis","B4"},
    {"Mechanoid Base: Secret Oculat","C1"},
    {"Mechanoid Base: Scissor Saboteur","C2"},
    {"Mechanoid Base: Operative Secutor","C3"},
    {"Mechanoid Base: Rethiarius Commander","C4"},
    // Unknown distortions at this point:
    //	{"","D1"},
    //	{"","D2"},
    //	{"","D3"},
    //	{"","D4"},
};

static void LoginToAelinet(Browser browser, string username, string password)
{
    //Use uk english rules for parsing rather than current machine locale
    CultureInfo.CurrentCulture = CultureInfo.CreateSpecificCulture("en-gb");

    browser.Navigate($"https://eu.portal.sf.my.com/skyforgenews");

    if (browser.Find(ElementType.TextField, FindBy.Id, "login") == null) return;

    browser.Find(ElementType.TextField, FindBy.Id, "login").Value = username;
    browser.Find(ElementType.TextField, FindBy.Id, "password").Value = password;
    browser.Find(ElementType.Checkbox, FindBy.Id, "remember").Checked = false;
    browser.Find(ElementType.Button, FindBy.Value, "log in").Click();
}

static bool CheckAelinetDetailsWithEnvironmentFallback(ref string username, ref string password, ref string pantheonId)
{
    if (string.IsNullOrWhiteSpace(username))
        username = Environment.GetEnvironmentVariable("SKYFORGE_LOGIN");

    if (string.IsNullOrWhiteSpace(password))
        password = Environment.GetEnvironmentVariable("SKYFORGE_PASSWORD");

    if (string.IsNullOrWhiteSpace(pantheonId))
        pantheonId = Environment.GetEnvironmentVariable("SKYFORGE_PANTHEONID");

    if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password) && !string.IsNullOrWhiteSpace(pantheonId)) return true;

    "Please enter all the required form fields: username, password, pantheonId.".Dump("ERROR");
    return false;
}

static void SetMemberDistortions(GuildMember member, Browser masterBrowser, string csrfToken)
{
    var browser = masterBrowser.CreateReferenceView();

    browser.Accept = "application/json";
    browser.SetHeader("X-Requested-With: XMLHttpRequest");

    browser.Navigate($"https://eu.portal.sf.my.com/api/game/stats/StatsApi:getAvatarStats/{member.MemberId}?csrf_token={csrfToken}");

    var distortions = JsonConvert.DeserializeObject<RootObject>(browser.CurrentHtml).adventureStats.byAdventureStats.Where(x => x.rule.types.Contains(RuleTypes.RULE_TYPE_PVE) && x.rule.types.Contains(RuleTypes.RULE_TYPE_DIMENSION))
                            .Select(x => new AdventureInfo { AdventureType = AdventureType.Distortion, CompletionCount = int.Parse(x.completionsCount), Name = x.rule.name.Trim(' ', '.') }).ToArray();

    foreach (var distortion in distortions.Where(x => DistortionToShortCodeLookup.ContainsKey(x.Name.Replace(" (Rated)", string.Empty).Trim())))
        distortion.ShortCode = DistortionToShortCodeLookup[distortion.Name.Replace(" (Rated)", string.Empty).Trim()] + (distortion.IsRated ? "R" : string.Empty);

    member.A1 = distortions.SingleOrDefault(d => d.ShortCode == nameof(member.A1))?.CompletionCount ?? 0;
    member.A1R = distortions.SingleOrDefault(d => d.ShortCode == nameof(member.A1R))?.CompletionCount ?? 0;
    member.A2 = distortions.SingleOrDefault(d => d.ShortCode == nameof(member.A2))?.CompletionCount ?? 0;
    member.A2R = distortions.SingleOrDefault(d => d.ShortCode == nameof(member.A2R))?.CompletionCount ?? 0;
    member.A3 = distortions.SingleOrDefault(d => d.ShortCode == nameof(member.A3))?.CompletionCount ?? 0;
    member.A3R = distortions.SingleOrDefault(d => d.ShortCode == nameof(member.A3R))?.CompletionCount ?? 0;
    member.A4 = distortions.SingleOrDefault(d => d.ShortCode == nameof(member.A4))?.CompletionCount ?? 0;
    member.A4R = distortions.SingleOrDefault(d => d.ShortCode == nameof(member.A4R))?.CompletionCount ?? 0;
    member.B1 = distortions.SingleOrDefault(d => d.ShortCode == nameof(member.B1))?.CompletionCount ?? 0;
    member.B1R = distortions.SingleOrDefault(d => d.ShortCode == nameof(member.B1R))?.CompletionCount ?? 0;
    member.B2 = distortions.SingleOrDefault(d => d.ShortCode == nameof(member.B2))?.CompletionCount ?? 0;
    member.B2R = distortions.SingleOrDefault(d => d.ShortCode == nameof(member.B2R))?.CompletionCount ?? 0;
    member.B3 = distortions.SingleOrDefault(d => d.ShortCode == nameof(member.B3))?.CompletionCount ?? 0;
    member.B3R = distortions.SingleOrDefault(d => d.ShortCode == nameof(member.B3R))?.CompletionCount ?? 0;
    member.B4 = distortions.SingleOrDefault(d => d.ShortCode == nameof(member.B4))?.CompletionCount ?? 0;
    member.B4R = distortions.SingleOrDefault(d => d.ShortCode == nameof(member.B4R))?.CompletionCount ?? 0;
    member.C1 = distortions.SingleOrDefault(d => d.ShortCode == nameof(member.C1))?.CompletionCount ?? 0;
    member.C1R = distortions.SingleOrDefault(d => d.ShortCode == nameof(member.C1R))?.CompletionCount ?? 0;
    member.C2 = distortions.SingleOrDefault(d => d.ShortCode == nameof(member.C2))?.CompletionCount ?? 0;
    member.C2R = distortions.SingleOrDefault(d => d.ShortCode == nameof(member.C2R))?.CompletionCount ?? 0;
    member.C3 = distortions.SingleOrDefault(d => d.ShortCode == nameof(member.C3))?.CompletionCount ?? 0;
    member.C3R = distortions.SingleOrDefault(d => d.ShortCode == nameof(member.C3R))?.CompletionCount ?? 0;
    member.C4 = distortions.SingleOrDefault(d => d.ShortCode == nameof(member.C4))?.CompletionCount ?? 0;
    member.C4R = distortions.SingleOrDefault(d => d.ShortCode == nameof(member.C4R))?.CompletionCount ?? 0;
}

static void SetMemberProfileStats(GuildMember member, Browser masterBrowser)
{
    var browser = masterBrowser.CreateReferenceView();

    browser.Navigate($"https://eu.portal.sf.my.com/user/avatar/{member.MemberId}");
    var tacticalSenseNode = browser.Find("span", FindBy.Text, "Tactical Sense");
    var rawValue = browser.FindClosestAncestor(tacticalSenseNode, "p").XElement.XPathSelectElement("em").Value.Trim('{', '}', ' ').Split('|').Select(x => x.Trim()).First();
    member.TacticalSense = (int)double.Parse(rawValue);
}

static void NavigateAelinetGuildSection(string pantheonId, List<GuildMember> members, Browser masterBrowser, DateTime checkTime, MemberType memberType, bool reportPlayerStatData = false, bool reportDistortionData = false)
{
    var browser = masterBrowser.CreateReferenceView();
    var sectionName = memberType == MemberType.PantheonMember ? "members" : "academy";
    var backgroundTasks = new List<Task>();
    GuildMember[] parsedMembers;
    var nextPage = 2;

    //DEBUGGING AIDS - DON'T MESS WITH THESE UNLESS YOU KNOW WHAT YOU'RE DOING
    var upToPage = 0;
    var skipToPage = 0;
    
    browser.Navigate($"https://eu.portal.sf.my.com/guild/{sectionName}/{pantheonId}");

    var csrfToken = new Uri(browser.Find(ElementType.Anchor, FindBy.PartialText, "English").GetAttribute("href")).Query.Substring(1).Split('&').First(x => x.StartsWith("csrf_token")).Split('=')[1];

    string.Format("{0} Processing Page {1} for {2}...", DateTime.Now.ToString(TimestampFormat), 1, memberType == MemberType.PantheonMember ? "pantheon members" : "academy members").Dump();

    //Handle the landing page (1st page)
    if (skipToPage < 2)
    {        
        parsedMembers = ParseGuildMembers(browser.XDocument.Root, checkTime, memberType).ToArray();
        backgroundTasks.AddRange(SetMemberStats(parsedMembers, memberType, browser, csrfToken, reportPlayerStatData, reportDistortionData));

        members.AddRange(parsedMembers);
    }
    else $"{DateTime.Now.ToString(TimestampFormat)} Skipping page 1".Dump();

    XElement nextLink;
    while (upToPage > 0 && nextPage <= upToPage && (nextLink = browser.XDocument.Root.XPathSelectElement($"//div[@class=\"paging\"]/a[text()={nextPage}]")) != null)
    {
        Util.ClearResults();
        string.Format("{0} Processing Page {1} for {2}...", DateTime.Now.ToString(TimestampFormat), nextPage, memberType == MemberType.PantheonMember ? "pantheon members" : "academy members").Dump();

        var link = nextLink.Attribute("href").Value;
        browser.SetHeader("X-Requested-With: XMLHttpRequest");
        browser.SetHeader("X-Prototype-Version: 1.7");
        browser.Accept = "text/javascript, text/html, application/xml, text/xml, */*";
        browser.Navigate(new Uri(link), "t%3Azoneid=listZone", "application/x-www-form-urlencoded; charset=UTF-8");

        var json = JsonConvert.DeserializeAnonymousType(browser.CurrentHtml, new { inits = new object(), zones = new { listZone = "" } });
        browser.SetContent(json.zones.listZone);

        if (skipToPage - nextPage > 0)
        {
            nextPage++;
            continue;
        }

        parsedMembers = ParseGuildMembers(browser.XDocument.Root, checkTime, memberType).ToArray();
        backgroundTasks.AddRange(SetMemberStats(parsedMembers, memberType, browser, csrfToken, reportPlayerStatData, reportDistortionData));
        members.AddRange(parsedMembers);

        nextPage++;
    }

    Task.WaitAll(backgroundTasks.ToArray());
}

static IEnumerable<Task> SetMemberStats(IEnumerable<GuildMember> members, MemberType memberType, Browser browser, string csrfToken, bool reportPlayerStatData = false, bool reportDistortionData = false)
{
    //ticks here get truncated in value but irrelevant for the purpose of seed;
    var fudger = new Random((int)DateTime.Now.Ticks);

    if (memberType == MemberType.PantheonMember && (reportDistortionData || reportPlayerStatData))
        foreach (var member in members.Where(m => m.MemberType == MemberType.PantheonMember))
        {
            $"{DateTime.Now.ToString(TimestampFormat)} Processing {member.Name}".Dump();
            Thread.Sleep(1000 + fudger.Next(500, 3000));

            yield return Task.Run(() =>
            {
                if (reportDistortionData)
                    SetMemberDistortions(member, browser, csrfToken);
            })
            .ContinueWith(_ =>
            {
                if (reportPlayerStatData)
                    SetMemberProfileStats(member, browser);
            });
        }
}

static IEnumerable<GuildMember> ParseGuildMembers(XElement documentRoot, DateTime checkTime, MemberType memberType)
{
    return documentRoot.XPathSelectElements("//div[@class=\"guild-member\"]/div/div").OfType<XElement>().Select(x => new
    {
        DocumentRoot = x,
        CheckTime = checkTime,
        Prestige = (int)GetNumberInLong(x.XPathSelectElements(".//div[@class=\"guild-member-td-c\"]/p").ElementAt(0).Value),
        CreditsDonated = GetNumberInLong(x.XPathSelectElements(".//div[@class=\"guild-member-td-c\"]/p").ElementAt(1).Value),
        MaterialsDonated = (int)GetNumberInLong(x.XPathSelectElements(".//div[@class=\"guild-member-td-c\"]/p").ElementAt(2).Value),
        ImageUrl = x.XPathSelectElement(".//div[@class=\"guild-member-td-b\"]/div/div/a/div/img").Attribute("src").Value,
        IsGod = x.XPathSelectElement(".//div[@class=\"guild-member-td-b\"]/div/div").Attribute("class").Value.Contains("set-godness"),
        IsPremium = x.XPathSelectElement(".//div[@class=\"guild-member-td-b\"]/div/div").Attribute("class").Value.Contains("set-premium"),
        IsOnline = x.XPathSelectElement(".//div[@class=\"guild-member-td-b\"]/div/div").Attribute("class").Value.Contains("set-ingame"),
        IsBanned = x.XPathSelectElement(".//div[@class=\"ubox-name\"]/*/span[@class=\"crossed-out b-tip\"]") != null,
    })
    .Select(x => new GuildMember
    {
        CheckTime = checkTime,
        Prestige = x.Prestige,
        CreditsDonated = x.CreditsDonated,
        MaterialsDonated = x.MaterialsDonated,
        ImageUrl = x.ImageUrl[0] == '/' ? "https://eu.portal.sf.my.com" + x.ImageUrl : x.ImageUrl,
        IsGod = x.IsGod,
        IsPremium = x.IsPremium,
        IsOnline = x.IsOnline,
        IsBanned = x.IsBanned,
        Name = x.IsBanned ? x.DocumentRoot.XPathSelectElement(".//div[@class=\"ubox-name\"]/*/span[@class=\"crossed-out b-tip\"]").Value.Trim()
            : x.DocumentRoot.XPathSelectElement(".//div[@class=\"ubox-name\"]/span/a").Value.Trim(),
        ProfileUrl = x.IsBanned ? x.DocumentRoot.XPathSelectElement(".//div[@class=\"ubox-title\"]/a").Attribute("href").Value
            : x.DocumentRoot.XPathSelectElement(".//div[@class=\"ubox-name\"]/span/a").Attribute("href").Value,
        MemberType = memberType
    });
}

//this is meant to store everything in uniform prestige format instead of suffix format. Makes life easier during analysis & filtering
static long GetNumberInLong(string buffer)
{
    var trimmedBuffer = buffer.Trim();

    var suffix = trimmedBuffer[trimmedBuffer.Length - 1];

    if (char.IsNumber(suffix)) suffix = (char)0;

    var number = double.Parse(trimmedBuffer.Replace(suffix.ToString(), "").Trim());

    switch (char.ToUpper(suffix))
    {
        case 'K': return (long)(number * 1000);
        case 'M': return (long)(number * 1000000);
        case (char)0: return (long)(number);
        default: throw new ArgumentException($"Unexpected number suffix: {suffix}", nameof(buffer));
    }

}

#region DTOs

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable MemberCanBePrivate.Global
class GuildMember
{
    static readonly GregorianCalendar Calendar = new GregorianCalendar();

    public DateTime CheckTime { get; set; }
    public byte WeekNumber => (byte)Calendar.GetWeekOfYear(CheckTime, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
    public long MemberId => long.Parse(ProfileUrl.Split('/').Last());
    public string Name { get; set; }
    public int Prestige { get; set; }
    public long CreditsDonated { get; set; }
    public int MaterialsDonated { get; set; }
    public bool IsPremium { get; set; }
    public bool IsOnline { get; set; }
    public bool IsBanned { get; set; }
    public bool IsGod { get; set; }
    public MemberType MemberType { get; set; }
    public string ProfileUrl { get; set; }
    public string ImageUrl { get; set; }
    public int TacticalSense { get; set; }
    public int A1 { get; set; }
    public int A1R { get; set; }
    public int A2 { get; set; }
    public int A2R { get; set; }
    public int A3 { get; set; }
    public int A3R { get; set; }
    public int A4 { get; set; }
    public int A4R { get; set; }
    public int B1 { get; set; }
    public int B1R { get; set; }
    public int B2 { get; set; }
    public int B2R { get; set; }
    public int B3 { get; set; }
    public int B3R { get; set; }
    public int B4 { get; set; }
    public int B4R { get; set; }
    public int C1 { get; set; }
    public int C1R { get; set; }
    public int C2 { get; set; }
    public int C2R { get; set; }
    public int C3 { get; set; }
    public int C3R { get; set; }
    public int C4 { get; set; }
    public int C4R { get; set; }
    //unknown distortions at this time but reserving space
    public int D1 { get; set; }
    public int D1R { get; set; }
    public int D2 { get; set; }
    public int D2R { get; set; }
    public int D3 { get; set; }
    public int D3R { get; set; }
    public int D4 { get; set; }
    public int D4R { get; set; }
}

enum MemberType
{
    PantheonMember,
    AcademyMember
}

class AdventureInfo
{
    public string ShortCode { get; set; }
    public string Name { get; set; }
    public AdventureType AdventureType { get; set; }
    public int CompletionCount { get; set; }
    public bool IsRated => !string.IsNullOrEmpty(Name) && Name.EndsWith("(Rated)");
}

enum AdventureType
{
    Squad,
    Group,
    Distortion,
    Invasion,
    Avatar
}

#region Json2CSharp classes
// ReSharper disable InconsistentNaming
// ReSharper disable ClassNeverInstantiated.Global
public enum RuleTypes
{
    RULE_TYPE_AIR, //No idea what this is
    RULE_TYPE_DIMENSION, //Distortion
    RULE_TYPE_SOLO, //Squad
    RULE_TYPE_GROUP,
    RULE_TYPE_PVE,
    RULE_TYPE_PVP,
    RULE_TYPE_DROP_SHIP, //Invasion missions including training avatar
    RULE_TYPE_CUBE, //Training stuff?
    RULE_TYPE_RIFT //Champion
}

public class DailyStat
{
    public string pvpDeaths { get; set; }
    public string pveBossKills { get; set; }
    public string pveMobKills { get; set; }
    public string pvpAssists { get; set; }
    public string pvpKills { get; set; }
    public string pveDeaths { get; set; }
}

public class AllDaysStats
{
    public string pvpDeaths { get; set; }
    public string pveBossKills { get; set; }
    public string pveMobKills { get; set; }
    public string pvpAssists { get; set; }
    public string pvpKills { get; set; }
    public string pveDeaths { get; set; }
}

public class PvpStats
{
    public double skill { get; set; }
    public int ratingGamesCount { get; set; }
}

public class Stats
{
    public string pvpDeaths { get; set; }
    public string pveBossKills { get; set; }
    public string pveMobKills { get; set; }
    public string pvpAssists { get; set; }
    public string pvpKills { get; set; }
    public string pveDeaths { get; set; }
}

public class CharacterClass
{
    public int resourceId { get; set; }
    public string name { get; set; }
    public string imageSrc { get; set; }
}

public class ClassStat
{
    public Stats stats { get; set; }
    public CharacterClass characterClass { get; set; }
    public string secondsPlayed { get; set; }
    public string secondsActivePlayed { get; set; }
}

public class AvatarStats
{
    public int trueSkillMultiplier { get; set; }
    public int ratingGamesTreshold { get; set; }
    public DailyStat[] dailyStats { get; set; }
    public AllDaysStats allDaysStats { get; set; }
    public string secondsPlayed { get; set; }
    public string daysToShow { get; set; }
    public PvpStats pvpStats { get; set; }
    public ClassStat[] classStats { get; set; }
    public string secondsActivePlayed { get; set; }
    public long timestamp { get; set; }
}

public class AdventureTypes
{
    public string dimension_type { get; set; }
    public string air_type { get; set; }
    public string solo_type { get; set; }
    public string pve_type { get; set; }
    public string group_type { get; set; }
    public string pvp_type { get; set; }
}

public class Rule
{
    public string image { get; set; }
    public RuleTypes[] types { get; set; }
    public int resourceId { get; set; }
    public string name { get; set; }
}

public class Medal
{
    public int medalCount { get; set; }
    public string medalKind { get; set; }
}

public class ByAdventureStat
{
    public string failtureCount { get; set; }
    public string bestLadderRatingGrade { get; set; }
    public Medal[] medals { get; set; }
    public string bestLadderRating { get; set; }
    public string timeSpent { get; set; }
    public string bestScore { get; set; }
    public string bestLadderTimeGrade { get; set; }
    public string pvpWins { get; set; }
    public Rule rule { get; set; }
    public string completionsCount { get; set; }
    public string bestLadderTime { get; set; }
}

public class AdventureStats
{
    public string encyclopediaLink { get; set; }
    public AdventureTypes adventureTypes { get; set; }
    public ByAdventureStat[] byAdventureStats { get; set; }
}

public class Switchers
{
    public bool adventureStatsEnabled { get; set; }
    public bool displayTrueSkill { get; set; }
}

public class RootObject
{
    public AvatarStats avatarStats { get; set; }
    public AdventureStats adventureStats { get; set; }
    public Switchers switchers { get; set; }
}
// ReSharper restore ClassNeverInstantiated.Global
// ReSharper restore InconsistentNaming
// ReSharper restore MemberCanBePrivate.Global
// ReSharper restore UnusedAutoPropertyAccessor.Global
#endregion
#endregion