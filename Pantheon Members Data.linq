<Query Kind="Program">
  <NuGetReference Prerelease="true">Newtonsoft.Json</NuGetReference>
  <NuGetReference>SimpleBrowser</NuGetReference>
  <Namespace>Newtonsoft.Json</Namespace>
  <Namespace>SimpleBrowser</Namespace>
  <Namespace>System.Globalization</Namespace>
  <Namespace>System.Net</Namespace>
</Query>

void Main()
{

    //Your username for Alinet portal (this is in email form)
	const string username = "";
	//Your password for Aelinet login
	const string password = "";
	
	//you can get this by visiting your pantheon community page in aelinet. It's the last numbers bit in your address bar.
	//for example for Team Rocket this id is 243083329203608905
	const string pantheonId = "";

    if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(pantheonId))
    {
        "Please enter all the required form fields above.".Dump("ERROR");
        return;
    }

    var browser = new Browser();
    var checkTime = DateTime.Now;
    var members = new List<GuildMember>();
    var paging = new HashSet<string>();

    browser.Navigate($"https://eu.portal.sf.my.com/guild/members/{pantheonId}");

    if (browser.Find(ElementType.TextField, FindBy.Id, "login") != null)
    {
        browser.Find(ElementType.TextField, FindBy.Id, "login").Value = username;
        browser.Find(ElementType.TextField, FindBy.Id, "password").Value = password;
        browser.Find(ElementType.Checkbox, FindBy.Id, "remember").Checked = true;
        browser.Find(ElementType.Button, FindBy.Value, "log in").Click();
    }

    browser.Navigate($"https://eu.portal.sf.my.com/guild/members/{pantheonId}");
    ParseGuildMembers(browser.XDocument.Root, members, checkTime);

    var page = 2;

    XElement nextLink;
    while ((nextLink = browser.XDocument.Root.XPathSelectElement($"//div[@class=\"paging\"]/a[text()={page}]")) != null)
    {
        var link = nextLink.Attribute("href").Value;
        browser.SetHeader("X-Requested-With: XMLHttpRequest");
        browser.SetHeader("X-Prototype-Version: 1.7");
        browser.Accept = "text/javascript, text/html, application/xml, text/xml, */*";
        browser.Navigate(new Uri(link), "t%3Azoneid=listZone", "application/x-www-form-urlencoded; charset=UTF-8");

        var json = JsonConvert.DeserializeAnonymousType(browser.CurrentHtml, new { inits = new Object(), zones = new { listZone = "" } });
        browser.SetContent(json.zones.listZone);

        ParseGuildMembers(browser.XDocument.Root, members, checkTime);
        page++;
    }

    members.OrderBy(x => x.Name).Dump();
}

static void ParseGuildMembers(XElement documentRoot, List<GuildMember> members, DateTime checkTime)
{
    members.AddRange(documentRoot.XPathSelectElements("//div[@class=\"guild-member\"]/div/div").OfType<XElement>().Select(x => new
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
        IsBanned = x.XPathSelectElement(".//div[@class=\"ubox-name\"]/*/span[@class=\"crossed-out b-tip\"]") != null
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
            : x.DocumentRoot.XPathSelectElement(".//div[@class=\"ubox-name\"]/span/a").Attribute("href").Value
    }));
}

//this is meant to store everything in uniform prestige format instead of suffix format. Makes life easier during analysis & filtering
static long GetNumberInLong(string buffer)
{
    var trimmedBuffer = buffer.Trim();
    
    var suffix = trimmedBuffer[trimmedBuffer.Length - 1];

    if (char.IsNumber(suffix)) suffix = (char)0;

    var number = float.Parse(trimmedBuffer.Replace(suffix.ToString(), "").Trim());

    switch (char.ToUpper(suffix))
    {
        case 'K': return (long)(number * 1000);
        case 'M': return (long)(number * 1000000);
        case (char)0: return (long)(number);
        default: throw new ArgumentException($"Unexpected number suffix: {suffix}", nameof(buffer));
    }

}

class GuildMember
{
    private static readonly GregorianCalendar _calendar = new GregorianCalendar();

    public DateTime CheckTime { get; set; }
    public byte WeekNumber => (byte)_calendar.GetWeekOfYear(CheckTime, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
    public long MemberId => long.Parse(ProfileUrl.Split('/').Last());
    public string Name { get; set; }
    public int Prestige { get; set; }
    public long CreditsDonated { get; set; }
    public int MaterialsDonated { get; set; }
    public bool IsPremium { get; set; }
    public bool IsOnline { get; set; }
    public bool IsBanned { get; set; }
    public bool IsGod { get; set; }
    public string ProfileUrl { get; set; }
    public string ImageUrl { get; set; }
}