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

	//Define the region you'd like to work with:
	//Valid options are Region.EU & Region.NA
	GlobalState.Region = Region.EU;

	//You can either provide login details below OR use environment variables
	//SKYFORGE_LOGIN, SKYFORGE_PASSWORD
	//to provide them. Login details provided here will override the environment
	//details if present.

	//Your username for Alinet portal (this is in email form)
	GlobalState.Username = "";
	//Your password for Aelinet login
	GlobalState.Password = "";

	//Report extended stats for players. Please note that this will significantly increase time to report data if set to true
	//The run time will increase upwards of 4 seconds per Pantheon member (academy members are not parsed for these stats)
	//Please do not fiddle with 4 seconds to make things any faster unless you take responsibility for possibly getting banned
	//for abusing this tool or any other sanctions that may be applied to you and anybody else in pantheon by official customer support
	//YOU HAVE BEEN WARNED!
	GlobalState.ReportDistortionData = false;
	GlobalState.ReportPlayerProfileData = false;

	CultureInfo.CurrentCulture = GlobalState.ParsingCulture;

	if (CheckAelinetDetailsWithEnvironmentFallback())
	{
		LoginToAelinet();
		var userLocale = Locale.EN;

		try
		{
			userLocale = (Locale)Enum.Parse(typeof(Locale), GetCurrentAelinetLocale());

			if (userLocale != Locale.EN)
				SetCurrentAelinetLocale(Locale.EN);

			NavigateAelinetGuildSection(MemberType.PantheonMember);
			NavigateAelinetGuildSection(MemberType.AcademyMember);

		}
		finally
		{
			if (userLocale != Locale.EN)
				SetCurrentAelinetLocale(userLocale);
		}

		Util.ClearResults();
		GlobalState.Members.OrderBy(x => x.Name).Dump();
	}
}

const string TimestampFormat = "HH:mm:ss.f";

static string GetCurrentAelinetLocale() => GlobalState.Browser.Select("div.lang-wrap > div.lang-cur-wrap > p").Value.Trim();

static void SetCurrentAelinetLocale(Locale locale)
{
	switch (locale)
	{
		case Locale.EN:
			GlobalState.Browser.Find(ElementType.Anchor, FindBy.Text, GlobalState.Region == Region.EU ? "English (United Kingdom)" : "English (United States)").Click();
			break;
		case Locale.DE:
			GlobalState.Browser.Find(ElementType.Anchor, FindBy.Text, "Deutsch (Deutschland)").Click();
			break;
		case Locale.FR:
			GlobalState.Browser.Find(ElementType.Anchor, FindBy.Text, "Français (France)").Click();
			break;
	}
}

static void SetCsrfToken()
{
	var match = new Regex("<meta content=\"(.*?)\" name=\"csrf_token\"/>").Match(GlobalState.Browser.CurrentHtml);

	if (match.Success)
		GlobalState.CsrfToken = match.Groups[1].Value;

	else throw new ApplicationException("Unable to determine CSRF Token");
}

static void SetPantheonId()
{
	var browser = GlobalState.Browser.CreateReferenceView();

	browser.Accept = "application/json";
	browser.SetHeader("X-Requested-With: XMLHttpRequest");

	browser.Navigate(new Uri(GlobalState.BaseAelinetUri, $"/api/game/widgets/widgetsapi:loadData?csrf_token={GlobalState.CsrfToken}"));

	GlobalState.PantheonId = JsonConvert.DeserializeObject<PlayerWidget.PlayerWidgetData>(browser.CurrentHtml).Resp.Guild?.Url.Split('/').Last();

	if (string.IsNullOrWhiteSpace(GlobalState.PantheonId))
		throw new ApplicationException($"Unable to identify your PantheonId on account {GlobalState.Username}");
}

static readonly Dictionary<int, string> DungeonResourceIdToShortCodeLookup = new Dictionary<int, string>
{
	{2097792503, "A1"},
	{2097792472, "A2"},
	{2097792480, "A3"},
	{2097792466, "A4"},
	{2097792500, "B1"},
	{2097792445, "B2"},
	{2097792424, "B3"},
	{2097792470, "B4"},
	{2097792436, "C1"},
	{2097792421, "C2"},
	{2097792452, "C3"},
	{2097792498, "C4"},
	{2097883211, "A1R"},
	{2097883210, "A2R"},
	{2097883212, "A3R"},
	{2097883213, "A4R"},
	{2097886787, "B1R"},
	{2097886788, "B2R"},
	{2097886789, "B3R"},
	{2097886790, "B4R"},
	{2097890125, "C1R"},
	{2097890128, "C2R"},
	{-1, "C3R"},
	{-2, "C4R"},
	{2097699736, "IntegratorTraining"},
	{2097791491, "Integrator"},
	{2097711241, "MachavannTraining"},
	{2097791489, "Machavann"},
	{2097692907, "ThanatosTraining"},
	{2097791084, "Thanatos"},
	{-3, "OceanidTraining"},
	{-4, "Oceanid"},
	{-5, "DemonTraining"},
	{-6, "Demon"},
	{2097852246, "AkonitaTraining"},
	{-7, "Akonita"},
};

static void LoginToAelinet()
{
	GlobalState.Browser.Navigate(new Uri(GlobalState.BaseAelinetUri, "/skyforgenews"));

	if (GlobalState.Browser.Find(ElementType.TextField, FindBy.Id, "login") == null) return;

	GlobalState.Browser.Find(ElementType.TextField, FindBy.Id, "login").Value = GlobalState.Username;
	GlobalState.Browser.Find(ElementType.TextField, FindBy.Id, "password").Value = GlobalState.Password;
	GlobalState.Browser.Find(ElementType.Checkbox, FindBy.Id, "remember").Checked = false;
	GlobalState.Browser.Find(ElementType.Button, FindBy.Value, "log in").Click();

	if (!GlobalState.Browser.Url.Query.Contains("auth_result=success"))
		throw new ApplicationException($"Login failed for {GlobalState.Region} Aelinet portal!");

	SetCsrfToken();
	SetPantheonId();
}

static bool CheckAelinetDetailsWithEnvironmentFallback()
{
	if (string.IsNullOrWhiteSpace(GlobalState.Username))
		GlobalState.Username = Environment.GetEnvironmentVariable("SKYFORGE_LOGIN");

	if (string.IsNullOrWhiteSpace(GlobalState.Password))
		GlobalState.Password = Environment.GetEnvironmentVariable("SKYFORGE_PASSWORD");

	if (string.IsNullOrWhiteSpace(GlobalState.Username) || string.IsNullOrWhiteSpace(GlobalState.Password))
		throw new ApplicationException("Please enter all the required form fields: username, password.");

	return true;
}

static AdventureType SetAdventureType(DungeonStatsData.Rule rule)
{
	//this is a priority categorisation and order of if statements is important
	if (rule.Types.Contains(DungeonStatsData.RuleTypes.RULE_TYPE_PVE) && rule.Types.Contains(DungeonStatsData.RuleTypes.RULE_TYPE_RIFT)) return AdventureType.Avatar;
	if (rule.Types.Contains(DungeonStatsData.RuleTypes.RULE_TYPE_PVE) && rule.Types.Contains(DungeonStatsData.RuleTypes.RULE_TYPE_DROP_SHIP) && rule.Name.StartsWith("Coming of", StringComparison.OrdinalIgnoreCase)) return AdventureType.TrainingAvatar;
	if (rule.Types.Contains(DungeonStatsData.RuleTypes.RULE_TYPE_PVE) && rule.Types.Contains(DungeonStatsData.RuleTypes.RULE_TYPE_DROP_SHIP)) return AdventureType.Invasion;
	if (rule.Types.Contains(DungeonStatsData.RuleTypes.RULE_TYPE_PVE) && rule.Types.Contains(DungeonStatsData.RuleTypes.RULE_TYPE_DIMENSION)) return AdventureType.Distortion;
	if (rule.Types.Contains(DungeonStatsData.RuleTypes.RULE_TYPE_PVE) && rule.Types.Contains(DungeonStatsData.RuleTypes.RULE_TYPE_SOLO)) return AdventureType.Squad;
	if (rule.Types.Contains(DungeonStatsData.RuleTypes.RULE_TYPE_PVE) && rule.Types.Contains(DungeonStatsData.RuleTypes.RULE_TYPE_GROUP)) return AdventureType.Group;

	return AdventureType.Unknown;
}


static void SetMemberDistortions(GuildMember member)
{
	var browser = GlobalState.Browser.CreateReferenceView();

	browser.Accept = "application/json";
	browser.SetHeader("X-Requested-With: XMLHttpRequest");

	browser.Navigate(new Uri(GlobalState.BaseAelinetUri, $"/api/game/stats/StatsApi:getAvatarStats/{member.MemberId}?csrf_token={GlobalState.CsrfToken}"));

	var dungeonData = JsonConvert.DeserializeObject<DungeonStatsData.AvatarStatisticsData>(browser.CurrentHtml).AdventureStats.ByAdventureStats.Where(x => x.Rule.Types.Contains(DungeonStatsData.RuleTypes.RULE_TYPE_PVE))
						.Select(x => new AdventureInfo
						{
							AdventureType = SetAdventureType(x.Rule),
							ResourceId = x.Rule.ResourceId,
							ShortCode = DungeonResourceIdToShortCodeLookup.ContainsKey(x.Rule.ResourceId) ? DungeonResourceIdToShortCodeLookup[x.Rule.ResourceId] : x.Rule.ResourceId.ToString(),
							CompletionCount = int.Parse(x.CompletionsCount, GlobalState.ParsingCulture),
							Name = x.Rule.Name.Trim(' ', '.')
						})
						.ToArray();

	member.A1 = dungeonData.SingleOrDefault(d => d.AdventureType == AdventureType.Distortion && d.ShortCode == nameof(member.A1))?.CompletionCount ?? 0;
	member.A1R = dungeonData.SingleOrDefault(d => d.AdventureType == AdventureType.Distortion && d.ShortCode == nameof(member.A1R))?.CompletionCount ?? 0;
	member.A2 = dungeonData.SingleOrDefault(d => d.AdventureType == AdventureType.Distortion && d.ShortCode == nameof(member.A2))?.CompletionCount ?? 0;
	member.A2R = dungeonData.SingleOrDefault(d => d.AdventureType == AdventureType.Distortion && d.ShortCode == nameof(member.A2R))?.CompletionCount ?? 0;
	member.A3 = dungeonData.SingleOrDefault(d => d.AdventureType == AdventureType.Distortion && d.ShortCode == nameof(member.A3))?.CompletionCount ?? 0;
	member.A3R = dungeonData.SingleOrDefault(d => d.AdventureType == AdventureType.Distortion && d.ShortCode == nameof(member.A3R))?.CompletionCount ?? 0;
	member.A4 = dungeonData.SingleOrDefault(d => d.AdventureType == AdventureType.Distortion && d.ShortCode == nameof(member.A4))?.CompletionCount ?? 0;
	member.A4R = dungeonData.SingleOrDefault(d => d.AdventureType == AdventureType.Distortion && d.ShortCode == nameof(member.A4R))?.CompletionCount ?? 0;
	member.B1 = dungeonData.SingleOrDefault(d => d.AdventureType == AdventureType.Distortion && d.ShortCode == nameof(member.B1))?.CompletionCount ?? 0;
	member.B1R = dungeonData.SingleOrDefault(d => d.AdventureType == AdventureType.Distortion && d.ShortCode == nameof(member.B1R))?.CompletionCount ?? 0;
	member.B2 = dungeonData.SingleOrDefault(d => d.AdventureType == AdventureType.Distortion && d.ShortCode == nameof(member.B2))?.CompletionCount ?? 0;
	member.B2R = dungeonData.SingleOrDefault(d => d.AdventureType == AdventureType.Distortion && d.ShortCode == nameof(member.B2R))?.CompletionCount ?? 0;
	member.B3 = dungeonData.SingleOrDefault(d => d.AdventureType == AdventureType.Distortion && d.ShortCode == nameof(member.B3))?.CompletionCount ?? 0;
	member.B3R = dungeonData.SingleOrDefault(d => d.AdventureType == AdventureType.Distortion && d.ShortCode == nameof(member.B3R))?.CompletionCount ?? 0;
	member.B4 = dungeonData.SingleOrDefault(d => d.AdventureType == AdventureType.Distortion && d.ShortCode == nameof(member.B4))?.CompletionCount ?? 0;
	member.B4R = dungeonData.SingleOrDefault(d => d.AdventureType == AdventureType.Distortion && d.ShortCode == nameof(member.B4R))?.CompletionCount ?? 0;
	member.C1 = dungeonData.SingleOrDefault(d => d.AdventureType == AdventureType.Distortion && d.ShortCode == nameof(member.C1))?.CompletionCount ?? 0;
	member.C1R = dungeonData.SingleOrDefault(d => d.AdventureType == AdventureType.Distortion && d.ShortCode == nameof(member.C1R))?.CompletionCount ?? 0;
	member.C2 = dungeonData.SingleOrDefault(d => d.AdventureType == AdventureType.Distortion && d.ShortCode == nameof(member.C2))?.CompletionCount ?? 0;
	member.C2R = dungeonData.SingleOrDefault(d => d.AdventureType == AdventureType.Distortion && d.ShortCode == nameof(member.C2R))?.CompletionCount ?? 0;
	member.C3 = dungeonData.SingleOrDefault(d => d.AdventureType == AdventureType.Distortion && d.ShortCode == nameof(member.C3))?.CompletionCount ?? 0;
	member.C3R = dungeonData.SingleOrDefault(d => d.AdventureType == AdventureType.Distortion && d.ShortCode == nameof(member.C3R))?.CompletionCount ?? 0;
	member.C4 = dungeonData.SingleOrDefault(d => d.AdventureType == AdventureType.Distortion && d.ShortCode == nameof(member.C4))?.CompletionCount ?? 0;
	member.C4R = dungeonData.SingleOrDefault(d => d.AdventureType == AdventureType.Distortion && d.ShortCode == nameof(member.C4R))?.CompletionCount ?? 0;

	member.IntegratorTraining = dungeonData.SingleOrDefault(d => d.AdventureType == AdventureType.TrainingAvatar && d.ShortCode == nameof(member.IntegratorTraining))?.CompletionCount ?? 0;
	member.MachavannTraining = dungeonData.SingleOrDefault(d => d.AdventureType == AdventureType.TrainingAvatar && d.ShortCode == nameof(member.MachavannTraining))?.CompletionCount ?? 0;
	member.ThanatosTraining = dungeonData.SingleOrDefault(d => d.AdventureType == AdventureType.TrainingAvatar && d.ShortCode == nameof(member.ThanatosTraining))?.CompletionCount ?? 0;
	member.OceanidTraining = dungeonData.SingleOrDefault(d => d.AdventureType == AdventureType.TrainingAvatar && d.ShortCode == nameof(member.OceanidTraining))?.CompletionCount ?? 0;
	member.DemonTraining = dungeonData.SingleOrDefault(d => d.AdventureType == AdventureType.TrainingAvatar && d.ShortCode == nameof(member.DemonTraining))?.CompletionCount ?? 0;
	member.GorgonideTraining = dungeonData.SingleOrDefault(d => d.AdventureType == AdventureType.TrainingAvatar && d.ShortCode == nameof(member.GorgonideTraining))?.CompletionCount ?? 0;

	member.Integrator = dungeonData.SingleOrDefault(d => d.AdventureType == AdventureType.Avatar && d.ShortCode == nameof(member.Integrator))?.CompletionCount ?? 0;
	member.Machavann = dungeonData.SingleOrDefault(d => d.AdventureType == AdventureType.Avatar && d.ShortCode == nameof(member.Machavann))?.CompletionCount ?? 0;
	member.Thanatos = dungeonData.SingleOrDefault(d => d.AdventureType == AdventureType.Avatar && d.ShortCode == nameof(member.Thanatos))?.CompletionCount ?? 0;
	member.Oceanid = dungeonData.SingleOrDefault(d => d.AdventureType == AdventureType.Avatar && d.ShortCode == nameof(member.Oceanid))?.CompletionCount ?? 0;
	member.Demon = dungeonData.SingleOrDefault(d => d.AdventureType == AdventureType.Avatar && d.ShortCode == nameof(member.Demon))?.CompletionCount ?? 0;
	member.Gorgonide = dungeonData.SingleOrDefault(d => d.AdventureType == AdventureType.Avatar && d.ShortCode == nameof(member.Gorgonide))?.CompletionCount ?? 0;
}

static void SetMemberProfileStats(GuildMember member)
{
	var browser = GlobalState.Browser.CreateReferenceView();

	browser.Navigate(new Uri(GlobalState.BaseAelinetUri, $"/user/avatar/{member.MemberId}"));

	var stats = browser.Select("#portalPageBody div.region-page-body div p > span")
				.Select(b => b.Value)
				.Zip(browser.Select("#portalPageBody div.region-page-body div p > span + em")
							.Select(b => b.Value),
						(k, v) => new
						{
							Stat = k,
							Value = string.Join(" - ", v.Trim().Split('-').SelectMany(rV => rV.Split('|').Select(x => x.Trim(' ', '{', '}', '%'))).Where((_, i) => i % 2 == 0))
						})
				.ToDictionary(x => x.Stat, x => x.Value, StringComparer.OrdinalIgnoreCase);

	if (stats.ContainsKey("Tactical Sense"))
		member.TacticalSense = (int)double.Parse(stats["Tactical Sense"], GlobalState.ParsingCulture);
}

static void NavigateAelinetGuildSection(MemberType memberType)
{
	var browser = GlobalState.Browser.CreateReferenceView();
	var sectionName = memberType == MemberType.PantheonMember ? "members" : "academy";
	var backgroundTasks = new List<Task>();
	GuildMember[] parsedMembers;
	var nextPage = 2;

	//DEBUGGING AIDS - DON'T MESS WITH THESE UNLESS YOU KNOW WHAT YOU'RE DOING
	//THIS TAKES EFFECT ON BOTH TYPES OF MEMBERS
	var skipToPage = 0;
	var upToPage = 0;

	browser.Navigate(new Uri(GlobalState.BaseAelinetUri, $"/guild/{sectionName}/{GlobalState.PantheonId}"));

	string.Format("{0} Processing Page {1} for {2}...", DateTime.Now.ToString(TimestampFormat), 1, memberType == MemberType.PantheonMember ? "pantheon members" : "academy members").Dump();

	//Handle the landing page (1st page)
	if (skipToPage < 2)
	{
		parsedMembers = ParseGuildMembers(browser, memberType).ToArray();
		backgroundTasks.AddRange(SetMemberStats(parsedMembers, memberType));

		GlobalState.Members.AddRange(parsedMembers);
	}

	HtmlResult nextPageAnchor;
	while ((nextPageAnchor = browser.Select("div.paging-alt > div.paging-wrap > a.paging-control.paging-next")).Exists)
	{
		Util.ClearResults();
		string.Format("{0} Processing Page {1} for {2}...", DateTime.Now.ToString(TimestampFormat), nextPage, memberType == MemberType.PantheonMember ? "pantheon members" : "academy members").Dump();

		var link = nextPageAnchor.GetAttribute("href");
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

		parsedMembers = ParseGuildMembers(browser, memberType).ToArray();
		backgroundTasks.AddRange(SetMemberStats(parsedMembers, memberType));
		GlobalState.Members.AddRange(parsedMembers);

		if (upToPage > 0 && (nextPage - upToPage + 1 > 0)) break;

		nextPage++;
	}

	Task.WaitAll(backgroundTasks.ToArray());
}

static IEnumerable<Task> SetMemberStats(IEnumerable<GuildMember> members, MemberType memberType)
{
	//ticks here get truncated in value but irrelevant for the purpose of seed;
	var fudger = new Random((int)DateTime.Now.Ticks);

	if (memberType == MemberType.PantheonMember && (GlobalState.ReportDistortionData || GlobalState.ReportPlayerProfileData))
		foreach (var member in members.Where(m => m.MemberType == MemberType.PantheonMember))
		{
			$"{DateTime.Now.ToString(TimestampFormat)} Processing {member.Name}".Dump();
			Thread.Sleep(1000 + fudger.Next(500, 3000));

			yield return Task.Run(() =>
			{
				if (GlobalState.ReportDistortionData)
					SetMemberDistortions(member);
			})
			.ContinueWith(_ =>
			{
				if (GlobalState.ReportPlayerProfileData)
					SetMemberProfileStats(member);
			});
		}
}

static IEnumerable<GuildMember> ParseGuildMembers(Browser browser, MemberType memberType)
{

	return browser.Select("div.guild-member").Select(x => new GuildMember
	{
		CheckTime = GlobalState.CheckTime,
		Prestige = (int)GetNumberInLong(x.Select("div.guild-member-td-c > p").ElementAt(0).Value),
		CreditsDonated = GetNumberInLong(x.Select("div.guild-member-td-c > p").ElementAt(1).Value),
		MaterialsDonated = (int)GetNumberInLong(x.Select("div.guild-member-td-c > p").ElementAt(2).Value),
		ImageUrl = x.Select("div.guild-member-td-b > div.ubox div.upic-img > img").GetAttribute("src").StartsWith("/") ?
			new Uri(GlobalState.BaseAelinetUri, x.Select("div.guild-member-td-b > div.ubox div.upic-img > img").GetAttribute("src")).ToString() :
			x.Select("div.guild-member-td-b > div.ubox div.upic-img > img").GetAttribute("src"),
		IsGod = x.Select("div.guild-member-td-b > div.ubox > div.set-godness").Exists,
		IsPremium = x.Select("div.guild-member-td-b > div.ubox > div.set-premium").Exists,
		IsOnline = x.Select("div.guild-member-td-b > div.ubox > div.set-ingame").Exists,
		IsBanned = x.Select("div.guild-member-td-b > div.ubox div.ubox-name > span > span.crossed-out").Exists,
		Name = x.Select("div.guild-member-td-b > div.ubox .ubox-name > span > a").Exists ?
				x.Select("div.guild-member-td-b > div.ubox .ubox-name > span > a").Value.Trim() :
				x.Select("div.guild-member-td-b > div.ubox .ubox-title > a").Value.Trim(),
		ProfileUrl = x.Select("div.guild-member-td-b > div.ubox .ubox-title > a").GetAttribute("href"),
		MemberType = memberType
	});
}

//this is meant to store everything in uniform prestige format instead of suffix format. Makes life easier during analysis & filtering
static long GetNumberInLong(string buffer)
{
	var trimmedBuffer = buffer.Trim();

	var suffix = trimmedBuffer[trimmedBuffer.Length - 1];

	if (char.IsNumber(suffix)) suffix = (char)0;

	var number = double.Parse(trimmedBuffer.Replace(suffix.ToString(), "").Trim(), GlobalState.ParsingCulture);

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
public class GuildMember
{
	static readonly GregorianCalendar Calendar = new GregorianCalendar();

	public DateTime CheckTime { get; set; }
	public byte WeekNumber => (byte)Calendar.GetWeekOfYear(CheckTime, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
	public long MemberId => long.Parse(ProfileUrl.Split('/').Last(), GlobalState.ParsingCulture);
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
	public int IntegratorTraining { get; set; }
	public int Integrator { get; set; }
	public int MachavannTraining { get; set; }
	public int Machavann { get; set; }
	public int ThanatosTraining { get; set; }
	public int Thanatos { get; set; }
	//Placeholder columns that I don't know the name of at this point!
	public int OceanidTraining { get; set; }
	public int Oceanid { get; set; }
	public int DemonTraining { get; set; }
	public int Demon { get; set; }
	public int GorgonideTraining { get; set; }
	public int Gorgonide { get; set; }
}

public static class GlobalState
{
	public static string Username { get; set; }
	public static string Password { get; set; }
	public static Region Region { get; set; }
	public static string PantheonId { get; set; }
	public static bool ReportDistortionData { get; set; }
	public static bool ReportPlayerProfileData { get; set; }
	public static Uri BaseAelinetUri => Region == Region.EU ? new Uri("https://eu.portal.sf.my.com") : new Uri("https://na.portal.sf.my.com");
	public static string CsrfToken { get; set; }
	public static Browser Browser { get; } = new Browser();
	public static List<GuildMember> Members { get; } = new List<GuildMember>();
	public static DateTime CheckTime { get; } = DateTime.Now;
	public static CultureInfo ParsingCulture { get; } = CultureInfo.CreateSpecificCulture("en-GB");
}

public class AdventureInfo
{
	public int ResourceId { get; set; }
	public string ShortCode { get; set; }
	public string Name { get; set; }
	public AdventureType AdventureType { get; set; }
	public int CompletionCount { get; set; }
	public bool IsRated => !string.IsNullOrEmpty(Name) && Name.EndsWith("(Rated)");
}

public enum MemberType
{
	PantheonMember,
	AcademyMember
}

public enum Locale
{
	EN,
	DE,
	FR
}

public enum Region
{
	EU,
	NA
}

public enum AdventureType
{
	Unknown,
	Squad,
	Group,
	Distortion,
	Invasion,
	TrainingAvatar,
	Avatar
}

#endregion

#region Json2CSharp classes
// ReSharper disable InconsistentNaming
// ReSharper disable ClassNeverInstantiated.Global
public class DungeonStatsData
{
	public class AllDaysStats
	{

		[JsonProperty("pvpDeaths")]
		public string PvpDeaths { get; set; }

		[JsonProperty("pveBossKills")]
		public string PveBossKills { get; set; }

		[JsonProperty("pveMobKills")]
		public string PveMobKills { get; set; }

		[JsonProperty("pvpKills")]
		public string PvpKills { get; set; }

		[JsonProperty("pvpAssists")]
		public string PvpAssists { get; set; }

		[JsonProperty("pveDeaths")]
		public string PveDeaths { get; set; }
	}

	public class DailyStat
	{

		[JsonProperty("pvpDeaths")]
		public string PvpDeaths { get; set; }

		[JsonProperty("pveBossKills")]
		public string PveBossKills { get; set; }

		[JsonProperty("pveMobKills")]
		public string PveMobKills { get; set; }

		[JsonProperty("pvpKills")]
		public string PvpKills { get; set; }

		[JsonProperty("pvpAssists")]
		public string PvpAssists { get; set; }

		[JsonProperty("pveDeaths")]
		public string PveDeaths { get; set; }
	}

	public class PvpStats
	{

		[JsonProperty("skill")]
		public double Skill { get; set; }

		[JsonProperty("ratingGamesCount")]
		public int RatingGamesCount { get; set; }
	}

	public class CharacterClass
	{

		[JsonProperty("resourceId")]
		public int ResourceId { get; set; }

		[JsonProperty("name")]
		public string Name { get; set; }

		[JsonProperty("imageSrc")]
		public string ImageSrc { get; set; }
	}

	public class Stats
	{

		[JsonProperty("pvpDeaths")]
		public string PvpDeaths { get; set; }

		[JsonProperty("pveBossKills")]
		public string PveBossKills { get; set; }

		[JsonProperty("pveMobKills")]
		public string PveMobKills { get; set; }

		[JsonProperty("pvpKills")]
		public string PvpKills { get; set; }

		[JsonProperty("pvpAssists")]
		public string PvpAssists { get; set; }

		[JsonProperty("pveDeaths")]
		public string PveDeaths { get; set; }
	}

	public class ClassStat
	{

		[JsonProperty("characterClass")]
		public CharacterClass CharacterClass { get; set; }

		[JsonProperty("stats")]
		public Stats Stats { get; set; }

		[JsonProperty("secondsPlayed")]
		public string SecondsPlayed { get; set; }

		[JsonProperty("secondsActivePlayed")]
		public string SecondsActivePlayed { get; set; }
	}

	public class AvatarStats
	{

		[JsonProperty("trueSkillMultiplier")]
		public int TrueSkillMultiplier { get; set; }

		[JsonProperty("ratingGamesTreshold")]
		public int RatingGamesTreshold { get; set; }

		[JsonProperty("secondsPlayed")]
		public string SecondsPlayed { get; set; }

		[JsonProperty("allDaysStats")]
		public AllDaysStats AllDaysStats { get; set; }

		[JsonProperty("dailyStats")]
		public IList<DailyStat> DailyStats { get; set; }

		[JsonProperty("daysToShow")]
		public string DaysToShow { get; set; }

		[JsonProperty("pvpStats")]
		public PvpStats PvpStats { get; set; }

		[JsonProperty("classStats")]
		public IList<ClassStat> ClassStats { get; set; }

		[JsonProperty("secondsActivePlayed")]
		public string SecondsActivePlayed { get; set; }

		[JsonProperty("timestamp")]
		public long Timestamp { get; set; }
	}

	public class AdventureTypes
	{

		[JsonProperty("dimension_type")]
		public string DimensionType { get; set; }

		[JsonProperty("air_type")]
		public string AirType { get; set; }

		[JsonProperty("solo_type")]
		public string SoloType { get; set; }

		[JsonProperty("pve_type")]
		public string PveType { get; set; }

		[JsonProperty("group_type")]
		public string GroupType { get; set; }

		[JsonProperty("pvp_type")]
		public string PvpType { get; set; }
	}

	public class Medal
	{

		[JsonProperty("medalCount")]
		public string MedalCount { get; set; }

		[JsonProperty("medalKind")]
		public string MedalKind { get; set; }
	}

	public class Rule
	{

		[JsonProperty("image")]
		public string Image { get; set; }

		[JsonProperty("types")]
		public IList<RuleTypes> Types { get; set; }

		[JsonProperty("resourceId")]
		public int ResourceId { get; set; }

		[JsonProperty("name")]
		public string Name { get; set; }
	}

	public class ByAdventureStat
	{

		[JsonProperty("failtureCount")]
		public string FailtureCount { get; set; }

		[JsonProperty("bestLadderRatingGrade")]
		public string BestLadderRatingGrade { get; set; }

		[JsonProperty("medals")]
		public IList<Medal> Medals { get; set; }

		[JsonProperty("bestLadderRating")]
		public string BestLadderRating { get; set; }

		[JsonProperty("timeSpent")]
		public string TimeSpent { get; set; }

		[JsonProperty("bestScore")]
		public string BestScore { get; set; }

		[JsonProperty("bestLadderTimeGrade")]
		public string BestLadderTimeGrade { get; set; }

		[JsonProperty("pvpWins")]
		public string PvpWins { get; set; }

		[JsonProperty("rule")]
		public Rule Rule { get; set; }

		[JsonProperty("bestLadderTime")]
		public string BestLadderTime { get; set; }

		[JsonProperty("completionsCount")]
		public string CompletionsCount { get; set; }
	}

	public class AdventureStats
	{

		[JsonProperty("encyclopediaLink")]
		public string EncyclopediaLink { get; set; }

		[JsonProperty("adventureTypes")]
		public AdventureTypes AdventureTypes { get; set; }

		[JsonProperty("byAdventureStats")]
		public IList<ByAdventureStat> ByAdventureStats { get; set; }
	}

	public class Switchers
	{

		[JsonProperty("adventureStatsEnabled")]
		public bool AdventureStatsEnabled { get; set; }

		[JsonProperty("displayTrueSkill")]
		public bool DisplayTrueSkill { get; set; }
	}

	public class AvatarStatisticsData
	{

		[JsonProperty("avatarStats")]
		public AvatarStats AvatarStats { get; set; }

		[JsonProperty("adventureStats")]
		public AdventureStats AdventureStats { get; set; }

		[JsonProperty("switchers")]
		public Switchers Switchers { get; set; }
	}

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
}

public class PlayerWidget
{
	public class Guild
	{

		[JsonProperty("guildRight")]
		public string GuildRight { get; set; }

		[JsonProperty("guildPicUrl")]
		public string GuildPicUrl { get; set; }

		[JsonProperty("url")]
		public string Url { get; set; }

		[JsonProperty("guildName")]
		public string GuildName { get; set; }
	}

	public class WidStats
	{

		[JsonProperty("dailyCalculatedTrueSkill")]
		public double DailyCalculatedTrueSkill { get; set; }

		[JsonProperty("ratingGamesThreshold")]
		public int RatingGamesThreshold { get; set; }

		[JsonProperty("skill")]
		public double Skill { get; set; }

		[JsonProperty("ratingGamesCount")]
		public int RatingGamesCount { get; set; }

		[JsonProperty("dailyRatingGamesCount")]
		public int DailyRatingGamesCount { get; set; }

		[JsonProperty("url")]
		public string Url { get; set; }
	}

	public class Avatar
	{

		[JsonProperty("avatarName")]
		public string AvatarName { get; set; }

		[JsonProperty("classImageUrl")]
		public string ClassImageUrl { get; set; }

		[JsonProperty("prestige")]
		public int Prestige { get; set; }

		[JsonProperty("url")]
		public string Url { get; set; }
	}

	public class User
	{

		[JsonProperty("userPicUrl")]
		public string UserPicUrl { get; set; }

		[JsonProperty("nickName")]
		public string NickName { get; set; }

		[JsonProperty("url")]
		public string Url { get; set; }
	}

	public class Friends
	{

		[JsonProperty("communitiesCount")]
		public int CommunitiesCount { get; set; }

		[JsonProperty("friendsCount")]
		public int FriendsCount { get; set; }

		[JsonProperty("url")]
		public string Url { get; set; }
	}

	public class Stats
	{
		[JsonProperty("allAdventuresCompletionsCount")]
		public string AllAdventuresCompletionsCount { get; set; }

		[JsonProperty("ratingGamesTreshold")]
		public int RatingGamesTreshold { get; set; }

		[JsonProperty("dailyCalculatedTrueSkill")]
		public double DailyCalculatedTrueSkill { get; set; }

		[JsonProperty("allDaysStats")]
		public DungeonStatsData.AllDaysStats AllDaysStats { get; set; }

		[JsonProperty("allAdventuresFailureCount")]
		public string AllAdventuresFailureCount { get; set; }

		[JsonProperty("pvpStats")]
		public DungeonStatsData.PvpStats PvpStats { get; set; }
	}

	public class Resp
	{

		[JsonProperty("guild")]
		public Guild Guild { get; set; }

		[JsonProperty("stats")]
		public Stats Stats { get; set; }

		[JsonProperty("avatar")]
		public Avatar Avatar { get; set; }

		[JsonProperty("user")]
		public User User { get; set; }

		[JsonProperty("friends")]
		public Friends Friends { get; set; }
	}

	public class PlayerWidgetData
	{
		[JsonProperty("resp")]
		public Resp Resp { get; set; }
	}
}

// ReSharper restore ClassNeverInstantiated.Global
// ReSharper restore InconsistentNaming
// ReSharper restore MemberCanBePrivate.Global
// ReSharper restore UnusedAutoPropertyAccessor.Global
#endregion