using System.Xml.Linq;
using Fizzler.Systems.HtmlAgilityPack;
using HtmlAgilityPack;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

//全局变量
var url = "";
var downloadTags = new string[] { };
var name = "";

//执行程序
Console.WriteLine("脚本开始执行");
GetEnv();
var html = await GetHtmlAsync(url);
var all = GetAll(html);
ParseToRss(all);

Console.WriteLine("脚本执行完成");

/// <summary>
/// 从环境变量中获取配置值
/// </summary>
void GetEnv()
{
    url = Environment.GetEnvironmentVariable("URL");
    name = Environment.GetEnvironmentVariable("NAME");
    var temp = Environment.GetEnvironmentVariable("DOWNLOAD_TAGS");
    Console.WriteLine("环境变量读取完成：");
    Console.WriteLine("URL: " + url);
    Console.WriteLine("NAME: " + name);
    Console.WriteLine("DOWNLOAD_TAGS: " + temp);
    if (string.IsNullOrWhiteSpace(url))
    {
        Console.WriteLine("请设置URL变量");
        throw new ArgumentNullException("请设置URL变量");
    }
    if (string.IsNullOrWhiteSpace(name))
    {
        Console.WriteLine("请设置NAME变量");
        throw new ArgumentNullException("请设置NAME变量");
    }
    if (string.IsNullOrWhiteSpace(temp))
    {
        Console.WriteLine("请设置DOWNLOAD_TAGS变量");
        throw new ArgumentNullException("请设置DOWNLOAD_TAGS变量");
    }
    downloadTags = temp.Split([';', ',', ' ', '，']);
}


/// <summary>
/// 从Url获取网页Html内容
/// </summary>
/// <param name="url"></param>
/// <returns></returns>
async Task<string> GetHtmlAsync(string url)
{
    ChromeOptions options = new ChromeOptions();
    // 不显示浏览器
    options.AddArgument("--headless");
    // GPU加速可能会导致Chrome出现黑屏及CPU占用率过高
    options.AddArgument("--nogpu");
    // 设置chrome启动时size大小
    options.AddArgument("--window-size=10,10");

    using (var driver = new ChromeDriver(options))
    {
        try
        {
            driver.Manage().Window.Minimize();
            driver.Navigate().GoToUrl(url);
            // 等待页面动态加载完成
            await Task.Delay(5000);
            // 返回页面源码
            return driver.PageSource;
        }
        catch (NoSuchElementException)
        {
            Console.WriteLine("找不到该元素");
            return string.Empty;
        }
    }
}

/// <summary>
/// 获取所有内容
/// </summary>
/// <param name="html"></param>
/// <returns></returns>
List<LinkModel> GetAll(string? html)
{
    if (html == null)
    {
        throw new ArgumentNullException("未获取到Html内容");
    }

    var htmlDoc = new HtmlDocument();
    htmlDoc.LoadHtml(html);
    var document = htmlDoc.DocumentNode;

    var all = document.QuerySelectorAll(".article-related.download_url");
    var models = new List<LinkModel>();
    foreach (var item in all)
    {
        var downloadTag = item.QuerySelector("h2:first-child")?.InnerHtml;
        if (downloadTag == null)
        {
            continue;
        }
        // Console.WriteLine(downloadTag);
        if (downloadTags.Any(downloadTag.Contains))
        {
            var links = GetLinks(item);
            models.AddRange(links);
        }
    }
    return models;
}

/// <summary>
/// 获取链接
/// </summary>
/// <param name="node"></param>
/// <returns></returns>
List<LinkModel> GetLinks(HtmlNode node)
{
    var links = node.QuerySelectorAll("li");
    var models = new List<LinkModel>();
    foreach (var link in links)
    {
        var magnet = link.QuerySelector("input").Attributes["value"].Value;
        var magnetName = link.SelectSingleNode("./div[1]/a/text()")?.InnerHtml;
        // Console.WriteLine(magnet);
        // Console.WriteLine(magnetName);
        models.Add(new LinkModel() { Title = magnetName, Link = magnet });
    }
    return models;
}

/// <summary>
/// 转成RSS保存
/// </summary>
/// <param name="links"></param>
void ParseToRss(List<LinkModel> links)
{
    var items = new List<XElement>();
    foreach (var item in links)
    {
        items.Add(ParseToRssItem(item));
    }

    var xdoc = new XDocument(
        new XDeclaration("1.0", "utf-8", null),
        new XElement("rss",
            new XAttribute("version", "2.0"),
            new XElement("channel",
                new XElement("title", name),
                new XElement("link", url),
                new XElement("description", name),
                items
            )
    ));
    xdoc.Save($"{name}.xml");
}

/// <summary>
/// 处理RSS子项
/// </summary>
/// <param name="model"></param>
/// <returns></returns>
XElement ParseToRssItem(LinkModel model)
{
    return new XElement("item",
        new XElement("guid",
            new XAttribute("isPermaLink", "false"),
            model.Title),
        new XElement("link", url),
        new XElement("title", model.Title),
        new XElement("description", model.Title),
        new XElement("torrent",
            new XElement("link", url)
        // new XElement("contentLength", "contentLength"),
        // new XElement("pubDate", "pubDate")
        ),
        new XElement("enclosure",
            new XAttribute("type", "application/x-bittorrent"),
            // new XAttribute("length", "612232064"),
            new XAttribute("url", model.Link ?? "")
        )
    );
}

/// <summary>
/// model
/// </summary>
class LinkModel
{
    public string? Link { get; set; }

    public string? Title { get; set; }
}